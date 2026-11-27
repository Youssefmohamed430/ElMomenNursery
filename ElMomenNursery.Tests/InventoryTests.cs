using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Application.Services;
using ElMomenNursery.Domain.Entities;
using ElMomenNursery.Domain.Abstractions;
using ElMomenNursery.Infrastructure.Persistence;
using ElMomenNursery.Infrastructure.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ElMomenNursery.Tests;

public sealed class InventoryTests
{
    [Fact]
    public async Task Category_update_is_persisted_and_category_with_items_cannot_be_deleted()
    {
        await using var db = await CreateDb(); var uow = new TestUnitOfWork(db);
        var categories = new InventoryCategoryService(uow, NullLogger<InventoryCategoryService>.Instance, new TestPermissionService(), new TestSessionManager(null));
        var created = await categories.CreateCategoryAsync(new CreateCategoryDto { Name = "ألعاب", IsActive = true });
        Assert.True(created.IsSuccess);
        var updated = await categories.UpdateCategoryAsync(created.Value.Id, new UpdateCategoryDto { Name = "ألعاب تعليمية", Description = "وصف", IsActive = false });
        Assert.True(updated.IsSuccess);
        Assert.Equal("ألعاب تعليمية", (await db.InventoryCategories.SingleAsync()).Name);

        var category = await db.InventoryCategories.SingleAsync();
        db.InventoryItems.Add(new InventoryItem { Name = "مكعبات", CategoryId = category.Id, Category = category, UnitPrice = 2m, IsActive = true });
        await db.SaveChangesAsync();
        var deletion = await categories.DeleteCategoryAsync(category.Id);
        Assert.Equal("Inventory.CategoryHasItems", deletion.Error.Code);
    }

    [Fact]
    public async Task Category_tree_updates_parent_rejects_cycles_and_protects_children()
    {
        await using var db = await CreateDb(); var uow = new TestUnitOfWork(db);
        var service = new InventoryCategoryService(uow, NullLogger<InventoryCategoryService>.Instance, new TestPermissionService(), new TestSessionManager(null));
        var root = (await service.CreateCategoryAsync(new CreateCategoryDto { Name = "كتب", IsActive = true })).Value;
        var child = (await service.CreateCategoryAsync(new CreateCategoryDto { Name = "إنجليزي", IsActive = true, ParentCategoryId = root.Id })).Value;
        var leaf = (await service.CreateCategoryAsync(new CreateCategoryDto { Name = "مستوى أول", IsActive = true, ParentCategoryId = child.Id })).Value;

        var cycle = await service.UpdateCategoryAsync(root.Id, new UpdateCategoryDto { Name = root.Name, IsActive = true, ParentCategoryId = leaf.Id });
        Assert.Equal(InventoryErrors.InvalidParentCategory, cycle.Error);
        var move = await service.UpdateCategoryAsync(leaf.Id, new UpdateCategoryDto { Name = leaf.Name, IsActive = true, ParentCategoryId = root.Id });
        Assert.True(move.IsSuccess);
        Assert.Equal(root.Id, (await db.InventoryCategories.SingleAsync(x => x.Id == leaf.Id)).ParentCategoryId);
        Assert.Equal("Inventory.CategoryHasSubcategories", (await service.DeleteCategoryAsync(root.Id)).Error.Code);

        var tree = await service.GetCategoryTreeAsync(true);
        Assert.True(tree.IsSuccess);
        Assert.Equal(root.Id, tree.Value.Single().Id);
        Assert.Contains(tree.Value.Single().Children, x => x.Id == leaf.Id);
    }

    [Fact]
    public async Task Items_can_only_be_assigned_to_leaf_categories()
    {
        await using var db = await CreateDb(); var uow = new TestUnitOfWork(db);
        var root = new InventoryCategory { Name = "كتب", IsActive = true };
        var leaf = new InventoryCategory { Name = "مستوى أول", IsActive = true, ParentCategory = root, ParentCategoryId = root.Id };
        db.AddRange(root, leaf); await db.SaveChangesAsync();
        var service = new InventoryItemService(uow, NullLogger<InventoryItemService>.Instance, new TestPermissionService(), new TestSessionManager(null));
        var parentItem = await service.CreateItemAsync(new CreateItemDto { Name = "خطأ", CategoryId = root.Id, UnitPrice = 1m, Quantity = 1, IsActive = true });
        Assert.Equal(InventoryErrors.CategoryMustBeLeafForItems, parentItem.Error);
        var leafItem = await service.CreateItemAsync(new CreateItemDto { Name = "صحيح", CategoryId = leaf.Id, UnitPrice = 1m, Quantity = 1, IsActive = true });
        Assert.True(leafItem.IsSuccess);
    }

    [Fact]
    public async Task Item_create_normalizes_duplicates_and_records_price_change_in_audit()
    {
        await using var db = await CreateDb(); var uow = new TestUnitOfWork(db);
        var category = new InventoryCategory { Name = "ألعاب", IsActive = true }; db.InventoryCategories.Add(category); await db.SaveChangesAsync();
        var service = new InventoryItemService(uow, NullLogger<InventoryItemService>.Instance, new TestPermissionService(), new TestSessionManager(null));
        var item = await service.CreateItemAsync(new CreateItemDto { Name = "ألعاب", CategoryId = category.Id, UnitPrice = 10m, Quantity = 4, IsActive = true });
        Assert.True(item.IsSuccess); Assert.Equal("ألعاب", item.Value.CategoryName);
        var invalidQuantity = await service.CreateItemAsync(new CreateItemDto { Name = "لعبة سالبة", CategoryId = category.Id, UnitPrice = 1m, Quantity = -1, IsActive = true });
        Assert.Equal("Inventory.InvalidQuantity", invalidQuantity.Error.Code);
        var duplicate = await service.CreateItemAsync(new CreateItemDto { Name = "العاب", CategoryId = category.Id, UnitPrice = 12m, Quantity = 4, IsActive = true });
        Assert.Equal("Inventory.DuplicateItemName", duplicate.Error.Code);
        var stale = await service.UpdateItemAsync(item.Value.Id, new UpdateItemDto
        {
            Name = "لعبة", CategoryId = category.Id, UnitPrice = 12.5m, Quantity = 7,
            ExpectedQuantity = 3, QuantityAdjustmentReason = "تصحيح الجرد", IsActive = true
        });
        Assert.Equal(InventoryErrors.QuantityChanged, stale.Error);
        var updated = await service.UpdateItemAsync(item.Value.Id, new UpdateItemDto { Name = "لعبة", CategoryId = category.Id, UnitPrice = 12.5m, Quantity = 7, ExpectedQuantity = 4, QuantityAdjustmentReason = "تصحيح الجرد", IsActive = true });
        Assert.True(updated.IsSuccess);
        Assert.Equal(7, updated.Value.Quantity);
        var audit = await db.AuditLogs.SingleAsync(x => x.Action == "InventoryItem.Updated");
        Assert.Contains("10.00", audit.ChangeSummary);
        Assert.Contains("12.50", audit.ChangeSummary);
        Assert.Contains("4 قطعة", audit.ChangeSummary);
        Assert.Contains("7 قطعة", audit.ChangeSummary);
        var movement = await db.StockMovements.SingleAsync(x => x.InventoryItemId == item.Value.Id);
        Assert.Equal(3, movement.Quantity);
        Assert.Equal(ElMomenNursery.Domain.Enums.StockMovementType.Adjustment, movement.MovementType);
        Assert.Equal("تصحيح الجرد", movement.Notes);
    }

    [Fact]
    public async Task View_and_manage_permissions_are_enforced()
    {
        await using var db = await CreateDb(); var uow = new TestUnitOfWork(db);
        var denied = new TestPermissionService("Inventory.View", "Inventory.Manage");
        var item = new InventoryItemService(uow, NullLogger<InventoryItemService>.Instance, denied, new TestSessionManager(null));
        var category = new InventoryCategoryService(uow, NullLogger<InventoryCategoryService>.Instance, denied, new TestSessionManager(null));
        Assert.Equal("Inventory.PermissionDenied", (await item.GetItemsAsync(new InventoryItemFilter())).Error.Code);
        Assert.Equal("Inventory.PermissionDenied", (await category.CreateCategoryAsync(new CreateCategoryDto { Name = "فئة", IsActive = true })).Error.Code);
    }

    [Fact]
    public async Task Invoice_checks_combined_quantity_for_duplicate_item_lines()
    {
        await using var db = await CreateDb();
        var category = new InventoryCategory { Name = "لوازم", IsActive = true };
        var item = new InventoryItem { Name = "دفتر", Category = category, CategoryId = category.Id, UnitPrice = 10m, Quantity = 4, IsActive = true };
        db.AddRange(category, item); await db.SaveChangesAsync();
        var service = new InvoiceService(new TestUnitOfWork(db), NullLogger<InvoiceService>.Instance,
            new TestPermissionService(), new TestSessionManager(null));
        var request = new CreateInvoiceDto("عميل", null,
        [
            new CreateInvoiceLineDto(item.Id, "دفتر", 10m, 3),
            new CreateInvoiceLineDto(item.Id, "دفتر", 10m, 2)
        ], ElMomenNursery.Domain.Enums.PaymentMethod.Cash, null, null);

        var result = await service.CreateInvoiceAsync(request);

        Assert.Equal("Invoices.InsufficientStock", result.Error.Code);
        Assert.Equal(4, (await db.InventoryItems.SingleAsync()).Quantity);
        Assert.Empty(await db.Invoices.ToListAsync());
    }

    [Fact]
    public async Task Invoice_item_sale_price_is_a_snapshot_and_does_not_change_inventory_price()
    {
        await using var db = await CreateDb();
        var category = new InventoryCategory { Name = "لوازم", IsActive = true };
        var item = new InventoryItem { Name = "دفتر", Category = category, CategoryId = category.Id, UnitPrice = 20m, Quantity = 5, IsActive = true };
        db.AddRange(category, item); await db.SaveChangesAsync();
        var service = new InvoiceService(new TestUnitOfWork(db), NullLogger<InvoiceService>.Instance,
            new TestPermissionService(), new TestSessionManager(null));
        var result = await service.CreateInvoiceAsync(new CreateInvoiceDto("عميل", null,
            [new CreateInvoiceLineDto(item.Id, item.Name, 15m, 2)], ElMomenNursery.Domain.Enums.PaymentMethod.Cash, null, null));

        Assert.True(result.IsSuccess);
        Assert.Equal(15m, (await db.InvoiceLines.SingleAsync()).UnitPriceSnapshot);
        Assert.Equal(20m, (await db.InventoryItems.SingleAsync()).UnitPrice);
        Assert.Equal(3, (await db.InventoryItems.SingleAsync()).Quantity);
    }

    [Fact]
    public async Task Manual_income_can_store_an_optional_payer_name()
    {
        await using var db = await CreateDb();
        var service = new FinanceService(new TestUnitOfWork(db), NullLogger<FinanceService>.Instance,
            new TestPermissionService(), new TestSessionManager(null));
        var result = await service.CreateManualIncomeAsync(new CreateManualIncomeDto
        {
            IncomeDate = DateOnly.FromDateTime(DateTime.Today), Source = "استضافة طفل", Amount = 100m,
            Description = "استضافة طفل: ليلى", PayerName = "ليلى"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("ليلى", (await db.ManualIncomes.SingleAsync()).PayerName);
        Assert.Equal("ليلى", result.Value.PayerName);
    }

    private static async Task<NurseryDbContext> CreateDb()
    {
        var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<NurseryDbContext>().UseSqlite(connection).AddInterceptors(new SoftDeleteInterceptor(new SessionManager())).Options;
        var db = new NurseryDbContext(options); await db.Database.EnsureCreatedAsync(); return db;
    }
}
