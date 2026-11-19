using ElMomenNursery.Application.Abstractions.Persistence;
using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Domain.Abstractions;
using ElMomenNursery.Domain.Entities;
using ElMomenNursery.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ElMomenNursery.Application.Services;

public sealed class InvoiceService(IUnitOfWork uow, ILogger<InvoiceService> logger, IPermissionService permissions, ISessionManager sessions) : IInvoiceService
{
    private bool Allowed(string permission) => PermissionGuard.Check(permissions, permission) is null;

    public async Task<Result<IReadOnlyList<InventoryItemDto>>> GetAvailableItemsAsync(CancellationToken ct = default)
    {
        if (!Allowed("Payments.View")) return InventoryErrors.PermissionDenied;
        var rows = await uow.Repository<InventoryItem>().Query().Include(x => x.Category)
            .Where(x => x.IsActive && x.Quantity > 0 && x.Category.IsActive)
            .OrderBy(x => x.Name).ToListAsync(ct);
        return Result.Success<IReadOnlyList<InventoryItemDto>>(rows.Select(x => new InventoryItemDto { Id=x.Id, Name=x.Name, CategoryId=x.CategoryId, CategoryName=x.Category.Name, UnitPrice=x.UnitPrice, Quantity=x.Quantity, IsActive=x.IsActive, CategoryIsActive=x.Category.IsActive }).ToArray());
    }

    public async Task<Result<InvoiceReceiptDto>> CreateInvoiceAsync(CreateInvoiceDto dto, CancellationToken ct = default)
    {
        if (!Allowed("Payments.Create")) return InventoryErrors.PermissionDenied;
        if (string.IsNullOrWhiteSpace(dto.CustomerName))
            return new Error("Invoices.CustomerRequired", "اسم العميل مطلوب لإصدار الفاتورة.");
        if (dto.Lines.Count == 0 || dto.Lines.Any(x => string.IsNullOrWhiteSpace(x.Description) || x.UnitPrice < 0 || x.Quantity <= 0 || decimal.Round(x.UnitPrice, 2) != x.UnitPrice))
            return new Error("Invoices.InvalidLines", "أضف بندًا واحدًا على الأقل وتأكد من صحة الوصف والسعر والكمية.");
        var child = dto.ChildId is Guid childId ? await uow.Repository<Child>().FirstOrDefaultAsync(x => x.Id == childId, asNoTracking:false, cancellationToken:ct) : null;
        if (dto.ChildId is not null && child is null) return new Error("Invoices.ChildNotFound", "لم يتم العثور على الطفل المحدد.");
        var inventoryIds = dto.Lines.Where(x => x.InventoryItemId.HasValue).Select(x => x.InventoryItemId!.Value).Distinct().ToArray();
        var items = await uow.Repository<InventoryItem>().Query(asNoTracking:false).Where(x => inventoryIds.Contains(x.Id) && x.IsActive).ToDictionaryAsync(x => x.Id, ct);
        if (items.Count != inventoryIds.Length) return new Error("Invoices.ItemUnavailable", "أحد الأصناف لم يعد متاحًا للبيع.");
        foreach (var line in dto.Lines.Where(x => x.InventoryItemId.HasValue))
        {
            var item = items[line.InventoryItemId!.Value];
            if (line.Quantity > item.Quantity) return new Error("Invoices.InsufficientStock", $"الكمية المتاحة من «{item.Name}» هي {item.Quantity}.");
        }
        var now = DateTimeOffset.Now;
        var year = now.Year;
        var settingKey = $"Invoices.Sequence.{year}";
        var setting = await uow.Repository<AppSetting>().FirstOrDefaultAsync(x => x.Key == settingKey, asNoTracking:false, cancellationToken:ct);
        var sequence = setting is null ? 1 : int.Parse(setting.Value, System.Globalization.CultureInfo.InvariantCulture) + 1;
        if (setting is null) await uow.Repository<AppSetting>().AddAsync(new AppSetting { Key=settingKey, Value=sequence.ToString(System.Globalization.CultureInfo.InvariantCulture), Description="عداد أرقام الفواتير السنوي." }, ct);
        else setting.Value = sequence.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var invoice = new Invoice { InvoiceNumber=$"I-{year}-{sequence:000000}", InvoiceDate=DateOnly.FromDateTime(now.Date), ChildId=child?.Id, Child=child,
            CustomerName=Clean(dto.CustomerName), CreatedByUserId=sessions.CurrentUser?.UserId, TotalAmount=dto.Lines.Sum(x => x.UnitPrice*x.Quantity), Status=InvoiceStatus.Posted, PaymentMethod=dto.PaymentMethod, PaymentReference=Clean(dto.Reference), Notes=Clean(dto.Notes) };
        foreach (var source in dto.Lines)
        {
            var item = source.InventoryItemId is Guid id ? items[id] : null;
            var line = new InvoiceLine { Invoice=invoice, InvoiceId=invoice.Id, InventoryItemId=item?.Id, InventoryItem=item,
                ItemNameSnapshot=Clean(source.Description)!, UnitPriceSnapshot=source.UnitPrice, Quantity=source.Quantity, LineTotal=source.UnitPrice*source.Quantity };
            invoice.Lines.Add(line);
        }
        await uow.BeginTransactionAsync(ct);
        try
        {
            await uow.Repository<Invoice>().AddAsync(invoice, ct);
            foreach (var line in invoice.Lines.Where(x => x.InventoryItem is not null))
            {
                var item = line.InventoryItem!;
                item.Quantity -= line.Quantity;
                await uow.Repository<StockMovement>().AddAsync(new StockMovement { InventoryItem=item, InventoryItemId=item.Id, Quantity=-line.Quantity, MovementType=StockMovementType.SaleOut, OccurredAt=now, InvoiceLine=line, InvoiceLineId=line.Id, RecordedByUserId=sessions.CurrentUser?.UserId, Notes=$"فاتورة {invoice.InvoiceNumber}" }, ct);
            }
            await uow.Repository<AuditLog>().AddAsync(new AuditLog { Action="Invoice.Created", EntityType=nameof(Invoice), EntityId=invoice.Id.ToString("D"), UserAccountId=sessions.CurrentUser?.UserId, ChangeSummary=$"invoice={invoice.InvoiceNumber};total={invoice.TotalAmount};lines={invoice.Lines.Count}" }, ct);
            await uow.CommitTransactionAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { await uow.RollbackTransactionAsync(CancellationToken.None); throw; }
        catch (Exception ex) { await uow.RollbackTransactionAsync(CancellationToken.None); logger.LogError(ex,"Invoice posting failed; invoiceId={InvoiceId}",invoice.Id); return PersistenceErrorMapper.FromException(ex); }
        return Result.Success(ToReceipt(invoice, child, now, dto.PaymentMethod), "تم حفظ الفاتورة وتحديث كميات المخزن.");
    }

    private static InvoiceReceiptDto ToReceipt(Invoice invoice, Child? child, DateTimeOffset now, PaymentMethod method) => new(invoice.Id, invoice.InvoiceNumber, now, invoice.CustomerName, child?.FullName,
        invoice.Lines.Select(x => new InvoiceReceiptLineDto(x.ItemNameSnapshot,x.UnitPriceSnapshot,x.Quantity,x.LineTotal)).ToArray(),invoice.TotalAmount,method,invoice.PaymentReference);
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
