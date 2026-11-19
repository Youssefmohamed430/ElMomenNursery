using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Application.Services;
using ElMomenNursery.Domain.Entities;
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
        var updated = await service.UpdateItemAsync(item.Value.Id, new UpdateItemDto { Name = "لعبة", CategoryId = category.Id, UnitPrice = 12.5m, Quantity = 7, IsActive = true });
        Assert.True(updated.IsSuccess);
        Assert.Equal(7, updated.Value.Quantity);
        var audit = await db.AuditLogs.SingleAsync(x => x.Action == "InventoryItem.Updated");
        Assert.Contains("10.00", audit.ChangeSummary);
        Assert.Contains("12.50", audit.ChangeSummary);
        Assert.Contains("4 قطعة", audit.ChangeSummary);
        Assert.Contains("7 قطعة", audit.ChangeSummary);
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

    private static async Task<NurseryDbContext> CreateDb()
    {
        var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<NurseryDbContext>().UseSqlite(connection).AddInterceptors(new SoftDeleteInterceptor(new SessionManager())).Options;
        var db = new NurseryDbContext(options); await db.Database.EnsureCreatedAsync(); return db;
    }
}
