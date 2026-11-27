namespace ElMomenNursery.Application.Services;

public sealed class InvoiceService(IUnitOfWork uow, ILogger<InvoiceService> logger, IPermissionService permissions, ISessionManager sessions) : IInvoiceService
{
    public async Task<Result<IReadOnlyList<InvoiceListItemDto>>> GetInvoicesAsync(CancellationToken ct = default)
    {
        if (!permissions.HasPermission("Payments.View")) return CommonErrors.PermissionDenied;
        var rows = await uow.Repository<Invoice>().Query().Include(x => x.Child).Include(x => x.Lines)
            .OrderByDescending(x => x.InvoiceDate).ThenByDescending(x => x.CreatedAt).ToListAsync(ct);
        return Result.Success<IReadOnlyList<InvoiceListItemDto>>(rows.Select(x => new InvoiceListItemDto(
            x.Id, x.InvoiceNumber, x.InvoiceDate, x.CustomerName ?? x.Child?.FullName ?? "عميل نقدي", x.TotalAmount,
            x.Status, x.PaymentMethod, x.PaymentReference,
            x.Lines.Select(line => new InvoiceReceiptLineDto(line.ItemNameSnapshot, line.UnitPriceSnapshot, line.Quantity, line.LineTotal)).ToArray())).ToArray());
    }
    private bool Allowed(string permission) => PermissionGuard.Check(permissions, permission) is null;

    public async Task<Result<IReadOnlyList<InventoryItemDto>>> GetAvailableItemsAsync(CancellationToken ct = default)
    {
        if (!Allowed("Payments.View")) return InventoryErrors.PermissionDenied;
        var rows = await uow.Repository<InventoryItem>().Query().Include(x => x.Category)
            .Where(x => x.IsActive && x.Quantity > 0 && x.Category.IsActive)
            .OrderBy(x => x.Name).ToListAsync(ct);
        return Result.Success<IReadOnlyList<InventoryItemDto>>(rows.Select(x => new InventoryItemDto { Id=x.Id, Name=x.Name, CategoryId=x.CategoryId, CategoryName=x.Category.Name, UnitPrice=x.UnitPrice, Quantity=x.Quantity, IsActive=x.IsActive, CategoryIsActive=x.Category.IsActive }).ToArray());
    }

    public async Task<Result<IReadOnlyList<InventoryCategoryLookupDto>>> GetInvoiceCategoriesAsync(CancellationToken ct = default)
    {
        if (!Allowed("Payments.View")) return InventoryErrors.PermissionDenied;
        var rows = await uow.Repository<InventoryCategory>().Query().Include(x => x.ParentCategory).Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(ct);
        return Result.Success<IReadOnlyList<InventoryCategoryLookupDto>>(rows.Select(x => new InventoryCategoryLookupDto(x.Id, x.Name, x.IsActive, x.ParentCategoryId, x.ParentCategory?.Name)).ToArray());
    }

    public async Task<Result<IReadOnlyList<ChildLookupDto>>> GetInvoiceChildrenAsync(CancellationToken ct = default)
    {
        if (!Allowed("Payments.Create")) return InventoryErrors.PermissionDenied;
        var rows = await uow.Repository<Child>().Query().OrderBy(x => x.FullName).ToListAsync(ct);
        return Result.Success<IReadOnlyList<ChildLookupDto>>(rows.Select(x => new ChildLookupDto(x.Id, x.FullName)).ToArray());
    }

    public async Task<Result<IReadOnlyList<InventoryItemDto>>> BrowseAvailableItemsAsync(Guid? categoryId, string? searchText, CancellationToken ct = default)
    {
        if (!Allowed("Payments.View")) return InventoryErrors.PermissionDenied;
        var query = uow.Repository<InventoryItem>().Query().Include(x => x.Category).Where(x => x.IsActive && x.Category.IsActive);
        if (categoryId is Guid selectedCategory)
        {
            var categories = await uow.Repository<InventoryCategory>().Query().Select(x => new { x.Id, x.ParentCategoryId }).ToListAsync(ct);
            var children = categories.ToLookup(x => x.ParentCategoryId, x => x.Id);
            var ids = new HashSet<Guid>(); var pending = new Stack<Guid>(); pending.Push(selectedCategory);
            while (pending.Count > 0) { var id = pending.Pop(); if (!ids.Add(id)) continue; foreach (var descendant in children[id]) pending.Push(descendant); }
            query = query.Where(x => ids.Contains(x.CategoryId));
        }
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var term = searchText.Trim();
            if (Guid.TryParse(term, out var itemId)) query = query.Where(x => x.Name.Contains(term) || x.Id == itemId);
            else query = query.Where(x => x.Name.Contains(term));
        }
        var rows = await query.OrderBy(x => x.Name).ToListAsync(ct);
        return Result.Success<IReadOnlyList<InventoryItemDto>>(rows.Select(x => new InventoryItemDto
        { Id = x.Id, Name = x.Name, CategoryId = x.CategoryId, CategoryName = x.Category.Name, UnitPrice = x.UnitPrice,
          Quantity = x.Quantity, IsActive = x.IsActive, CategoryIsActive = x.Category.IsActive }).ToArray());
    }

    public async Task<Result<InvoiceReceiptDto>> CreateInvoiceAsync(CreateInvoiceDto dto, CancellationToken ct = default)
    {
        if (!Allowed("Payments.Create")) return CommonErrors.PermissionDenied;
        if (string.IsNullOrWhiteSpace(dto.CustomerName))
            return new Error("Invoices.CustomerRequired", "اسم العميل مطلوب لإصدار الفاتورة.");
        if (dto.Lines.Count == 0 || dto.Lines.Any(x => string.IsNullOrWhiteSpace(x.Description) || x.UnitPrice < 0 || x.Quantity <= 0 || decimal.Round(x.UnitPrice, 2) != x.UnitPrice))
            return new Error("Invoices.InvalidLines", "أضف بندًا واحدًا على الأقل وتأكد من صحة الوصف والسعر والكمية.");
        var child = dto.ChildId is Guid childId ? await uow.Repository<Child>().FirstOrDefaultAsync(x => x.Id == childId, asNoTracking:false, cancellationToken:ct) : null;
        if (dto.ChildId is not null && child is null) return new Error("Invoices.ChildNotFound", "لم يتم العثور على الطفل المحدد.");
        var inventoryIds = dto.Lines.Where(x => x.InventoryItemId.HasValue).Select(x => x.InventoryItemId!.Value).Distinct().ToArray();
        var items = await uow.Repository<InventoryItem>().Query(asNoTracking:false).Include(x => x.Category)
            .Where(x => inventoryIds.Contains(x.Id) && x.IsActive && x.Category.IsActive).ToDictionaryAsync(x => x.Id, ct);
        if (items.Count != inventoryIds.Length)
        {
            logger.LogWarning("Invoice creation rejected; inventory item unavailable; requestedCount={RequestedCount}; availableCount={AvailableCount}", inventoryIds.Length, items.Count);
            return new Error("Invoices.ItemUnavailable", "أحد الأصناف لم يعد متاحًا للبيع.");
        }
        foreach (var group in dto.Lines.Where(x => x.InventoryItemId.HasValue).GroupBy(x => x.InventoryItemId!.Value))
        {
            var item = items[group.Key];
            var requested = group.Sum(x => x.Quantity);
            if (requested > item.Quantity)
            {
                logger.LogWarning("Invoice creation rejected due to insufficient stock; itemId={ItemId}; requestedQuantity={RequestedQuantity}; availableQuantity={AvailableQuantity}", item.Id, requested, item.Quantity);
                return new Error("Invoices.InsufficientStock", $"الكمية المتاحة من «{item.Name}» هي {item.Quantity}.");
            }
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
            logger.LogInformation("Invoice created successfully; operation={Operation}; invoiceNumber={InvoiceNumber}; totalAmount={TotalAmount}; lineCount={LineCount}",
                "CreateInvoice", invoice.InvoiceNumber, invoice.TotalAmount, invoice.Lines.Count);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { await uow.RollbackTransactionAsync(CancellationToken.None); throw; }
        catch (Exception ex) { await uow.RollbackTransactionAsync(CancellationToken.None); logger.LogError(ex,"Invoice posting failed; invoiceId={InvoiceId}",invoice.Id); return PersistenceErrorMapper.FromException(ex); }
        return Result.Success(ToReceipt(invoice, child, now, dto.PaymentMethod, sessions.CurrentUser?.FullName), "تم حفظ الفاتورة وتحديث كميات المخزن.");
    }

    private static InvoiceReceiptDto ToReceipt(Invoice invoice, Child? child, DateTimeOffset now, PaymentMethod method, string? issuedByName) => new(invoice.Id, invoice.InvoiceNumber, now, invoice.CustomerName, child?.FullName,
        invoice.Lines.Select(x => new InvoiceReceiptLineDto(x.ItemNameSnapshot,x.UnitPriceSnapshot,x.Quantity,x.LineTotal)).ToArray(),invoice.TotalAmount,method,invoice.PaymentReference, issuedByName);
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
