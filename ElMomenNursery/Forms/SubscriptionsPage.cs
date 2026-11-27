using System.Diagnostics;
using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Domain.Enums;
using ElMomenNursery.Domain.Abstractions;
using ElMomenNursery.Controls;
using ElMomenNursery.Styling;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace ElMomenNursery.Forms;

public sealed class SubscriptionsPage : UserControl
{
    private readonly IServiceScopeFactory _scopes;
    private readonly UserSession _session;
    private readonly IExcelExportService _excel;
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RightToLeft = RightToLeft.Yes, RowHeadersVisible = false };
    private readonly TextBox _search = new() { Width = 170, PlaceholderText = "بحث باسم الطفل", RightToLeft = RightToLeft.Yes };
    private readonly ComboBox _status = new() { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _classroom = new() { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly DateTimePicker _from = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", ShowCheckBox = true, Width = 135 };
    private readonly DateTimePicker _to = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", ShowCheckBox = true, Width = 135 };
    private readonly CheckBox _balance = new() { Text = "عليه متبقي فقط", AutoSize = true };
    private readonly Label _summary = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };
    private readonly Button _add = Button("إضافة اشتراك");
    private readonly Button _invoice = Button("فاتورة بيع من المخزن");
    private readonly Button _invoiceHistory = Button("سجل الفواتير");
    private readonly Button _edit = Button("تعديل");
    private readonly Button _pay = Button("تسجيل دفعة");
    private readonly Button _fine = Button("تسجيل غرامة غياب 15 يوم");
    private readonly Button _cancel = Button("إلغاء الاشتراك");
    private readonly Button _delete = Button("حذف");
    private readonly Button _receipt = Button("طباعة / إعادة طباعة إيصال");
    private readonly Button _export = Button("تصدير Excel");
    private List<SubscriptionDto> _rows = [];
    private int _loadVersion;

    public SubscriptionsPage(IServiceScopeFactory scopes, UserSession session, IExcelExportService excel)
    {
        _scopes = scopes; _session = session; _excel = excel; Dock = DockStyle.Fill; Padding = new Padding(10); RightToLeft = RightToLeft.Yes; BackColor = BrandPalette.AppBackground;
        _status.Items.AddRange(["الكل", "مستحق", "مدفوع جزئيًا", "مدفوع", "ملغي", "معفى"]); _status.SelectedIndex = 0;
        _classroom.Items.Add(new ClassroomFilterOptionDto(Guid.Empty, "كل الفصول")); _classroom.SelectedIndex = 0;
        var currentMonth = DateTime.Today;
        _from.Value = new DateTime(currentMonth.Year, currentMonth.Month, 1);
        _to.Value = new DateTime(currentMonth.Year, currentMonth.Month, DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month));
        _from.Checked = true;
        _to.Checked = true;
        _grid.Columns.AddRange([new DataGridViewTextBoxColumn { HeaderText = "الطفل" }, new DataGridViewTextBoxColumn { HeaderText = "من" }, new DataGridViewTextBoxColumn { HeaderText = "إلى" }, new DataGridViewTextBoxColumn { HeaderText = "المستحق" },
            new DataGridViewTextBoxColumn { HeaderText = "المدفوع" }, new DataGridViewTextBoxColumn { HeaderText = "المتبقي" }, new DataGridViewTextBoxColumn { HeaderText = "الحالة" },
            new DataGridViewTextBoxColumn { HeaderText = "ملاحظات" }, new DataGridViewTextBoxColumn { HeaderText = "تاريخ الإضافة" }, new DataGridViewTextBoxColumn { Visible = false }]);
        _add.Visible = session.HasPermission("Subscriptions.Manage"); _edit.Visible = _cancel.Visible = _delete.Visible = session.HasPermission("Subscriptions.Manage");
        _pay.Visible = session.HasPermission("Payments.Create"); _fine.Visible = session.HasPermission("Payments.Create") && session.HasPermission("Finance.ManageIncome"); _receipt.Visible = session.HasPermission("Payments.View"); _export.Visible = session.HasPermission("Subscriptions.Export");
        var apply = Button("تطبيق"); var clear = Button("مسح الفلاتر"); var refresh = Button("تحديث");
        _invoice.Visible = session.HasPermission("Payments.Create");
        _invoiceHistory.Visible = session.HasPermission("Payments.View");
        var filter = new FilterCard { Name = "subscriptions.filters" };
        filter.AddField("subscriptions.search", "اسم الطفل", _search, 2);
        filter.AddField("subscriptions.status", "حالة الاشتراك", _status);
        filter.AddField("subscriptions.classroom", "الفصل", _classroom);
        filter.AddGroup("subscriptions.period", "فترة الاشتراك", 2, _from, _to);
        filter.AddField("subscriptions.balance", "الرصيد", _balance);
        filter.AddActions(apply, clear);
        var toolbar = new ActionToolbar { Name = "subscriptions.actions" };
        toolbar.Add(refresh, _add, _invoice, _invoiceHistory, _edit, _pay, _fine, _cancel, _delete, _receipt, _export);
        _grid.Dock = DockStyle.None; _summary.Dock = DockStyle.None;
        Controls.Add(_grid); Controls.Add(_summary); Controls.Add(toolbar); Controls.Add(filter);
        Resize += (_, _) => LayoutContent(); LayoutContent();
        void LayoutContent()
        {
            var margin = 8; var width = Math.Max(240, ClientSize.Width - margin * 2);
            filter.SetBounds(margin, margin, width, filter.Height); filter.Arrange();
            toolbar.SetBounds(margin, filter.Bottom + 10, width, toolbar.ArrangeItems(width));
            _summary.SetBounds(margin, toolbar.Bottom + 4, width, 34);
            _grid.SetBounds(margin, _summary.Bottom + 4, width, Math.Max(140, ClientSize.Height - _summary.Bottom - margin));
        }
        apply.Click += (_, _) => _ = UiGuard.RunAsync(this, "Subscriptions.Load", LoadAsync);
        clear.Click += (_, _) =>
        {
            _search.Clear(); _status.SelectedIndex = 0; _classroom.SelectedIndex = 0;
            _from.Checked = false; _to.Checked = false; _balance.Checked = false;
            _ = UiGuard.RunAsync(this, "Subscriptions.ClearFilters", LoadAsync);
        };
        refresh.Click += (_, _) => _ = UiGuard.RunAsync(this, "Subscriptions.Load", LoadAsync);
        _add.Click += (_, _) => _ = UiGuard.RunAsync(this, "Subscriptions.Add", () => EditAsync(null));
        _edit.Click += (_, _) => { if (Selected() is { } row) _ = UiGuard.RunAsync(this, "Subscriptions.Edit", () => EditAsync(row)); };
        _invoice.Click += (_, _) => _ = UiGuard.RunAsync(this, "Subscriptions.Invoice", async () => { using var form = new CashierInvoiceForm(_scopes); if (form.ShowDialog(FindForm()) == DialogResult.OK) await LoadAsync(); });
        _invoiceHistory.Click += (_, _) => { using var form = new InvoiceHistoryForm(_scopes); form.ShowDialog(FindForm()); };
        _cancel.Click += (_, _) => _ = UiGuard.RunAsync(this, "Subscriptions.Cancel", CancelAsync);
        _delete.Click += (_, _) => _ = UiGuard.RunAsync(this, "Subscriptions.Delete", DeleteAsync);
        _pay.Click += (_, _) => _ = UiGuard.RunAsync(this, "Subscriptions.Pay", RecordPaymentAsync);
        _fine.Click += (_, _) => _ = UiGuard.RunAsync(this, "Subscriptions.AbsenceFine", RecordAbsenceFineAsync);
        _receipt.Click += (_, _) => _ = UiGuard.RunAsync(this, "Subscriptions.Receipt", PrintAsync);
        _export.Click += (_, _) => _ = UiGuard.RunAsync(this, "Subscriptions.Export", () => GridExcelExport.SaveAsync((IWin32Window?)FindForm() ?? this, _grid, _excel, "تصدير الاشتراكات", "الاشتراكات"));
        _grid.SelectionChanged += (_, _) => UpdateActions();
        _grid.KeyDown += (_, e) => { if (e.KeyCode == Keys.F5) { e.Handled = true; _ = UiGuard.RunAsync(this, "Subscriptions.Load", LoadAsync); } };
        Load += (_, _) => _ = UiGuard.RunAsync(this, "Subscriptions.InitialLoad", async () => { await LoadClassroomsAsync(); await LoadAsync(); });
    }

    private async Task LoadClassroomsAsync()
    {
        await UiGuard.RunAsync(this, "Subscriptions.LoadClassrooms", async () =>
        {
            await using var scope = _scopes.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
            var result = await service.GetClassroomsForFilterAsync();
            if (result.IsFailure) return;
            _classroom.Items.Clear();
            _classroom.Items.Add(new ClassroomFilterOptionDto(Guid.Empty, "كل الفصول"));
            foreach (var classroom in result.Value) _classroom.Items.Add(classroom);
            _classroom.SelectedIndex = 0;
        }, "تعذر تحميل قائمة الفصول.");
    }

    private async Task LoadAsync()
    {
        var version = Interlocked.Increment(ref _loadVersion);
        var selected = Selected()?.Id;
        await UiGuard.RunAsync(this, "Subscriptions.Load", async () =>
        {
            var status = _status.SelectedItem?.ToString() switch { "مستحق" => SubscriptionStatus.Pending, "مدفوع جزئيًا" => SubscriptionStatus.PartiallyPaid, "مدفوع" => SubscriptionStatus.Paid, "ملغي" => SubscriptionStatus.Cancelled, "معفى" => SubscriptionStatus.Waived, _ => (SubscriptionStatus?)null };
            await using var scope = _scopes.CreateAsyncScope();
            var svc = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
            var result = await svc.GetSubscriptionsAsync(new SubscriptionFilter { SearchText = _search.Text, Status = status,
                ClassroomId = (_classroom.SelectedItem as ClassroomFilterOptionDto)?.Id is Guid id && id != Guid.Empty ? id : null,
                PeriodFrom = _from.Checked ? DateOnly.FromDateTime(_from.Value) : null, PeriodTo = _to.Checked ? DateOnly.FromDateTime(_to.Value) : null, OnlyWithBalance = _balance.Checked });
            if (IsDisposed || version != _loadVersion) return;
            if (result.IsFailure) { ShowError(result.Error.Description); return; }
            _rows = result.Value.ToList();
            Render(selected);
        }, "تعذر تحميل الاشتراكات.");
    }

    private void Render(Guid? keep = null)
    {
        _grid.Rows.Clear(); foreach (var row in _rows)
        {
            var i = _grid.Rows.Add(row.ChildName, row.PeriodStart.ToString("dd/MM/yyyy"), row.PeriodEnd.ToString("dd/MM/yyyy"),
                row.AmountDue.ToString("N2"), row.PaidAmount.ToString("N2"), row.RemainingAmount.ToString("N2"), StatusName(row.Status), row.Notes ?? "—", row.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy"), row.Id);
            _grid.Rows[i].Tag = row.Id; if (keep == row.Id) { _grid.Rows[i].Selected = true; _grid.CurrentCell = _grid.Rows[i].Cells[0]; }
        }
        _summary.Text = $"عدد الاشتراكات: {_rows.Count}  |  المستحق: {_rows.Sum(x => x.AmountDue):N2}  |  المدفوع: {_rows.Sum(x => x.PaidAmount):N2}  |  المتبقي: {_rows.Sum(x => x.RemainingAmount):N2}"; UpdateActions();
    }

    private async Task EditAsync(SubscriptionDto? item)
    {
        using var form = new SubscriptionEditorForm(_scopes, _session, item);
        if (form.ShowDialog(FindForm()) == DialogResult.OK) { await LoadAsync(); if (form.CreatedReceipt is not null) await CreateAndOpenReceiptAsync(form.CreatedReceipt, false); }
    }

    private async Task CancelAsync()
    {
        if (Selected() is not { } row) return;
        var reason = Prompt("سبب إلغاء الاشتراك"); if (reason is null) return;
        await UiGuard.RunAsync(this, "Subscriptions.Cancel", async () =>
        {
            await using var scope = _scopes.CreateAsyncScope();
            var svc = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
            var res = await svc.CancelSubscriptionAsync(row.Id, reason);
            if (!ResultPresenter.Present(this, "إلغاء الاشتراك", res)) return;
            await LoadAsync();
        }, "تعذر إلغاء الاشتراك.");
    }

    private async Task DeleteAsync()
    {
        if (Selected() is not { } row) return;
        if (row.PaidAmount > 0) { ShowError(SubscriptionErrors.HasPayments.Description); return; }
        if (!AppDialog.Confirm(this, "تأكيد الحذف", "سيتم حذف الاشتراك المحدد. هل تريد المتابعة؟")) return;
        await UiGuard.RunAsync(this, "Subscriptions.Delete", async () =>
        {
            await using var scope = _scopes.CreateAsyncScope();
            var svc = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
            var res = await svc.DeleteSubscriptionAsync(row.Id);
            if (!ResultPresenter.Present(this, "حذف الاشتراك", res)) return;
            await LoadAsync();
        }, "تعذر حذف الاشتراك.");
    }

    private async Task RecordAbsenceFineAsync()
    {
        using var form = new AbsenceFineForm(_scopes);
        if (form.ShowDialog(FindForm()) != DialogResult.OK || form.Child is null) return;
        await UiGuard.RunAsync(this, "Subscriptions.RecordAbsenceFine", async () =>
        {
            await using var scope = _scopes.CreateAsyncScope();
            var finance = scope.ServiceProvider.GetRequiredService<IFinanceService>();
            var result = await finance.CreateAbsenceFineAsync(form.Child.Id, DateOnly.FromDateTime(form.FineDate), form.Notes);
            if (ResultPresenter.Present(this, "تسجيل غرامة الغياب", result)) await LoadAsync();
        }, "تعذر تسجيل غرامة الغياب.");
    }

    private async Task RecordPaymentAsync()
    {
        if (Selected() is not { } row) return;
        using var form = new PaymentEditorForm(row.RemainingAmount, _scopes);
        if (form.ShowDialog(FindForm()) != DialogResult.OK || form.Values is null) return;
        await UiGuard.RunAsync(this, "Subscriptions.RecordPayment", async () =>
        {
            await using var scope = _scopes.CreateAsyncScope();
            var svc = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
            var result = await svc.RecordPaymentAsync(row.Id, form.Values);
            if (!ResultPresenter.Present(this, "تسجيل دفعة", result)) return;
            await LoadAsync();
            await CreateAndOpenReceiptAsync(result.Value, false);
        }, "تعذر تسجيل الدفعة.");
    }

    private async Task PrintAsync()
    {
        if (Selected() is not { } row) return;
        await UiGuard.RunAsync(this, "Subscriptions.Print", async () =>
        {
            await using var scope = _scopes.CreateAsyncScope();
            var svc = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
            var payments = await svc.GetPaymentsForSubscriptionAsync(row.Id);
            if (payments.IsFailure) { ShowError(payments.Error.Description); return; }
            if (payments.Value.Count == 0) { ShowError("لا توجد إيصالات لهذا الاشتراك."); return; }
            var chosen = payments.Value.Count == 1 ? payments.Value[0] : SelectReceipt(payments.Value);
            if (chosen is null) return;
            await CreateAndOpenReceiptAsync(chosen, true);
        }, "تعذر تجهيز الإيصال.");
    }

    private async Task CreateAndOpenReceiptAsync(PaymentReceiptDto receipt, bool copy)
    {
        await UiGuard.RunAsync(this, "Subscriptions.CreateAndOpenReceipt", async () =>
        {
            await using var scope = _scopes.CreateAsyncScope();
            var pdf = scope.ServiceProvider.GetRequiredService<IReceiptPdfService>();
            var fileResult = await pdf.CreateReceiptFileAsync(receipt, copy);
            if (!ResultPresenter.Present(this, "الإيصال", fileResult)) return;
            var path = fileResult.Value;

            // Print to the configured Windows printer; the PDF remains available if printing is unavailable.
            try
            {
                var printer = scope.ServiceProvider.GetRequiredService<IReceiptPrinterService>();
                var printerResult = await printer.PrintReceiptAsync(receipt, copy);
                if (printerResult.IsFailure)
                    Log.ForContext<SubscriptionsPage>().Warning("Receipt PDF saved but thermal printer failed: {Error}", printerResult.Error.Description);
            }
            catch (Exception ex)
            {
                Log.ForContext<SubscriptionsPage>().Error(ex, "Thermal printer call failed");
            }

            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            if (AppDialog.Confirm(this, "حفظ نسخة", "هل تريد حفظ نسخة من الإيصال في موقع تختاره؟"))
            {
                using var dialog = new SaveFileDialog { Filter = "ملف PDF (*.pdf)|*.pdf", DefaultExt = "pdf", AddExtension = true, FileName = Path.GetFileName(path), OverwritePrompt = true };
                if (dialog.ShowDialog(this) == DialogResult.OK) File.Copy(path, dialog.FileName, true);
            }
        }, "تم حفظ الدفعة، لكن تعذر فتح الإيصال. يمكنك إعادة طباعته لاحقًا.");
    }

    private SubscriptionDto? Selected() => _grid.SelectedRows.Count == 0 ? null : _rows.FirstOrDefault(x => x.Id == _grid.SelectedRows[0].Tag as Guid?);
    private void UpdateActions() { var row = Selected(); _edit.Enabled = _cancel.Enabled = _delete.Enabled = row is not null; _pay.Enabled = row is { RemainingAmount: > 0 }; _receipt.Enabled = row is not null; }
    private void ShowError(string message) => AppDialog.ShowInformation((IWin32Window?)FindForm() ?? this, "الاشتراكات والمدفوعات", message);
    private static string StatusName(SubscriptionStatus s) => s switch { SubscriptionStatus.Pending => "مستحق", SubscriptionStatus.PartiallyPaid => "مدفوع جزئيًا", SubscriptionStatus.Paid => "مدفوع", SubscriptionStatus.Cancelled => "ملغي", SubscriptionStatus.Waived => "معفى", _ => s.ToString() };
    private static Button Button(string text) => new() { Text = text, AutoSize = true, Height = 36, UseVisualStyleBackColor = true };
    private static string? Prompt(string title)
    {
        using var form = new ResponsiveForm { Text = title, Width = 430, Height = 170, StartPosition = FormStartPosition.CenterParent, RightToLeft = RightToLeft.Yes, RightToLeftLayout = true, FormBorderStyle = FormBorderStyle.FixedDialog, MinimizeBox = false, MaximizeBox = false };
        var box = new TextBox { Left = 18, Top = 20, Width = 375, RightToLeft = RightToLeft.Yes }; var ok = Button("موافق"); ok.DialogResult = DialogResult.OK; ok.SetBounds(286, 65, 105, 34); var cancel = Button("إلغاء"); cancel.DialogResult = DialogResult.Cancel; cancel.SetBounds(174,65,105,34);
        form.Controls.AddRange([box, ok, cancel]); form.AcceptButton = ok; form.CancelButton = cancel; return form.ShowDialog() == DialogResult.OK ? box.Text : null;
    }
    private PaymentReceiptDto? SelectReceipt(IReadOnlyList<PaymentReceiptDto> receipts)
    {
        using var dialog = new ResponsiveForm { Text = "اختيار إيصال", Width = 460, Height = 190, StartPosition = FormStartPosition.CenterParent, RightToLeft = RightToLeft.Yes, RightToLeftLayout = true, FormBorderStyle = FormBorderStyle.FixedDialog, MinimizeBox = false, MaximizeBox = false };
        var list = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Left = 24, Top = 24, Width = 395 };
        foreach (var receipt in receipts) list.Items.Add(new ReceiptChoice(receipt));
        list.SelectedIndex = 0;
        var accept = Button("اختيار"); accept.SetBounds(300, 82, 110, 36); accept.DialogResult = DialogResult.OK;
        var cancel = Button("إلغاء"); cancel.SetBounds(180, 82, 110, 36); cancel.DialogResult = DialogResult.Cancel;
        dialog.Controls.AddRange([list, accept, cancel]); dialog.AcceptButton = accept; dialog.CancelButton = cancel;
        return dialog.ShowDialog(this) == DialogResult.OK && list.SelectedItem is ReceiptChoice selected ? selected.Receipt : null;
    }
    private sealed record ReceiptChoice(PaymentReceiptDto Receipt)
    {
        public override string ToString() => $"{Receipt.ReceiptNumber} — {Receipt.PaidAt.ToLocalTime():dd/MM/yyyy HH:mm} — {Receipt.PaidNow:N2} ج.م";
    }
}

internal sealed class SubscriptionEditorForm : ResponsiveForm
{
    private readonly IServiceScopeFactory _scopes; private readonly UserSession _session; private readonly SubscriptionDto? _item;
    private readonly ComboBox _child = new() { DropDownStyle = ComboBoxStyle.DropDown, AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems, Width = 300 };
    private readonly ComboBox _statusOverride = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
    private readonly DateTimePicker _start = new() { Format = DateTimePickerFormat.Short, Width = 300 };
    private readonly DateTimePicker _end = new() { Format = DateTimePickerFormat.Short, Width = 300 };
    private readonly NumericUpDown _due = new() { Maximum = 10000000, DecimalPlaces = 2, ThousandsSeparator = true, Width = 300 };
    private readonly NumericUpDown _initial = new() { Maximum = 10000000, DecimalPlaces = 2, ThousandsSeparator = true, Width = 300 };
    private readonly Button _saveButton = new() { Text = "حفظ", DialogResult = DialogResult.None, Width = 110 };
    private readonly ComboBox _method = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
    private readonly TextBox _reference = new() { Width = 300 }; private readonly TextBox _notes = new() { Width = 300 };
    private CancellationTokenSource? _childSearchCancellation;
    private bool _loadingChildren;
    public PaymentReceiptDto? CreatedReceipt { get; private set; }
    public SubscriptionEditorForm(IServiceScopeFactory scopes, UserSession session, SubscriptionDto? item)
    {
        _scopes = scopes; _session = session; _item = item; Text = item is null ? "إضافة اشتراك" : "تعديل اشتراك"; Width = 470; Height = 610; StartPosition = FormStartPosition.CenterParent; RightToLeft = RightToLeft.Yes; RightToLeftLayout = true; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
        _method.Items.AddRange(["نقدًا", "تحويل بنكي", "أخرى"]); _method.SelectedIndex = 0; _start.Value = DateTime.Today; _end.Value = DateTime.Today.AddMonths(1);
        Shown += async (_, _) => await LookupOptions.LoadAsync(_scopes, _method, "PaymentMethod", ["نقدًا", "تحويل بنكي", "أخرى"]);
        _statusOverride.Items.AddRange(["تلقائي (حسب المدفوع)", "مستحق", "مدفوع جزئيًا", "مدفوع", "ملغي", "معفى"]); _statusOverride.SelectedIndex = 0;
        _statusOverride.SelectedIndexChanged += (_, _) => { if (_statusOverride.SelectedIndex == 5 && _item is null) { _due.Value = 0; _initial.Value = 0; } _initial.Enabled = _item is null && _statusOverride.SelectedIndex != 5; };
        _child.TextUpdate += async (_, _) => { if (!_loadingChildren && _item is null) await SearchChildrenAsync(_child.Text); };
        _child.Enabled = item is null;
        var fields = new List<(string, Control)> { ("الطفل", _child), ("من", _start), ("إلى", _end), ("المستحق", _due), ("حالة الاشتراك", _statusOverride) };
        if (session.HasPermission("Payments.Create")) fields.AddRange([("المدفوع الآن", _initial), ("طريقة الدفع", _method), ("المرجع", _reference)]);
        fields.Add(("ملاحظات", _notes));
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 2, RowCount = fields.Count + 1, AutoScroll = true }; panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110)); panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < fields.Count; i++) { panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); panel.Controls.Add(new Label { Text = fields[i].Item1, TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, i); fields[i].Item2.Anchor = AnchorStyles.Left | AnchorStyles.Right; panel.Controls.Add(fields[i].Item2, 1, i); }
        var save = _saveButton; var cancel = new Button { Text = "إلغاء", DialogResult = DialogResult.Cancel, Width = 110 }; var actions = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill }; actions.Controls.AddRange([save, cancel]); panel.Controls.Add(actions, 0, fields.Count); panel.SetColumnSpan(actions, 2); Controls.Add(panel); AcceptButton = save; CancelButton = cancel;
        save.Click += async (_, _) => await SaveAsync(); Load += async (_, _) => await LoadOptionsAsync();
    }
    private async Task LoadOptionsAsync()
    {
        await UiGuard.RunAsync(this, "SubscriptionEditor.LoadOptions", async () =>
        {
            await using var scope = _scopes.CreateAsyncScope();
            var svc = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
            var children = await svc.GetChildLookupAsync(null);
            if (children.IsFailure) { ShowError("تعذر تحميل قائمة الأطفال."); return; }
            _loadingChildren = true;
            foreach (var c in children.Value) _child.Items.Add(c);
            _loadingChildren = false;
            if (_item is null) return;
            _child.Items.Add(new ChildLookupDto(_item.ChildId, _item.ChildName));
            _child.SelectedItem = _child.Items.Cast<object>().OfType<ChildLookupDto>().First(x => x.Id == _item.ChildId);
            _start.Value = _item.PeriodStart.ToDateTime(TimeOnly.MinValue);
            _end.Value = _item.PeriodEnd.ToDateTime(TimeOnly.MinValue);
            _due.Value = _item.AmountDue;
            _statusOverride.Items.Clear(); _statusOverride.Items.AddRange(["تلقائي (حسب المدفوع)", "ملغي", "معفى"]);
            _statusOverride.SelectedIndex = _item.Status switch { SubscriptionStatus.Cancelled => 1, SubscriptionStatus.Waived => 2, _ => 0 };
            _statusOverride.Enabled = true;
            _notes.Text = _item.Notes ?? string.Empty;
            _initial.Enabled = _method.Enabled = _reference.Enabled = false;
        }, "تعذر تحميل خيارات الاشتراك.");
    }
    private async Task SearchChildrenAsync(string search)
    {
        _childSearchCancellation?.Cancel();
        _childSearchCancellation?.Dispose();
        var cancellation = _childSearchCancellation = new CancellationTokenSource();
        await UiGuard.RunAsync(this, "SubscriptionEditor.SearchChildren", async () =>
        {
            await Task.Delay(250, cancellation.Token);
            await using var scope = _scopes.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
            var result = await service.GetChildLookupAsync(search, cancellation.Token);
            if (cancellation.IsCancellationRequested || result.IsFailure || IsDisposed) return;
            var text = _child.Text;
            _loadingChildren = true;
            _child.BeginUpdate();
            _child.Items.Clear();
            foreach (var child in result.Value) _child.Items.Add(child);
            _child.Text = text;
            _child.SelectionStart = text.Length;
            _child.SelectionLength = 0;
            _child.EndUpdate();
            _loadingChildren = false;
            if (_child.Focused && result.Value.Count > 0) _child.DroppedDown = true;
        });
        _loadingChildren = false;
    }
    private async Task SaveAsync()
    {
        if (!_saveButton.Enabled) return;
        if (_child.SelectedItem is not ChildLookupDto child) { ShowError("اختر الطفل من القائمة."); return; }
        if (_end.Value.Date < _start.Value.Date) { ShowError("تاريخ نهاية الاشتراك يجب أن يكون بعد بدايته."); return; }
        if (_item is null && _initial.Value > _due.Value) { ShowError("المدفوع الآن أكبر من المستحق."); return; }
        _saveButton.Enabled = false;
        try
        {
            await UiGuard.RunAsync(this, "SubscriptionEditor.Save", async () =>
            {
                await using var scope = _scopes.CreateAsyncScope();
                var svc = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
                if (_item is null)
                {
                    var overlapConfirmed = false;
                    while (true)
                    {
                        var result = await svc.CreateSubscriptionAsync(new CreateSubscriptionDto { ChildId = child.Id,
                            PeriodStart = DateOnly.FromDateTime(_start.Value), PeriodEnd = DateOnly.FromDateTime(_end.Value), AmountDue = _due.Value, Notes = _notes.Text, StatusOverride = SelectedStatusOverride(),
                            InitialPayment = _initial.Value > 0 ? new InitialPaymentDto(_initial.Value, MethodValue(), _reference.Text, PaymentMethodNote()) : null, ConfirmOverlappingPeriod = overlapConfirmed });
                        if (result.IsFailure && result.Error.Code == "Subscriptions.OverlappingPeriod" && !overlapConfirmed)
                        {
                            if (AppDialog.Confirm(this, "فترة متقاطعة", "توجد فترة اشتراك متقاطعة لهذا الطفل. هل تريد المتابعة؟")) { overlapConfirmed = true; continue; }
                            return;
                        }
                        if (!ResultPresenter.Present(this, "الاشتراكات والمدفوعات", result)) return;
                        CreatedReceipt = result.Value.InitialPaymentReceipt;
                        DialogResult = DialogResult.OK;
                        Close();
                        return;
                    }
                }
                var reason = PromptReason();
                if (string.IsNullOrWhiteSpace(reason)) { ShowError("سبب التعديل مطلوب."); return; }
                var updated = await svc.UpdateSubscriptionAsync(_item.Id, new UpdateSubscriptionDto { PeriodStart = DateOnly.FromDateTime(_start.Value), PeriodEnd = DateOnly.FromDateTime(_end.Value), AmountDue = _due.Value, Notes = _notes.Text, Reason = reason, StatusOverride = SelectedStatusOverride() });
                if (!ResultPresenter.Present(this, "الاشتراكات والمدفوعات", updated)) return;
                DialogResult = DialogResult.OK;
                Close();
            }, "تعذر حفظ الاشتراك.");
        }
        finally { if (!IsDisposed) _saveButton.Enabled = true; }
    }
    private PaymentMethod MethodValue() => _method.Text == "نقدًا" ? PaymentMethod.Cash : _method.Text == "تحويل بنكي" ? PaymentMethod.BankTransfer : PaymentMethod.Other;
    private string? PaymentMethodNote() => MethodValue() == PaymentMethod.Other && _method.Text != "أخرى" ? $"طريقة الدفع: {_method.Text}" : null;
    private SubscriptionStatus? SelectedStatusOverride() => _statusOverride.Text switch { "ملغي" => SubscriptionStatus.Cancelled, "معفى" => SubscriptionStatus.Waived, _ => null };
    private string? PromptReason() { using var prompt = new ResponsiveForm { Text = "سبب التعديل", Width = 380, Height = 145, StartPosition = FormStartPosition.CenterParent, RightToLeft = RightToLeft.Yes }; var box = new TextBox { Left = 15, Top = 15, Width = 330 }; var ok = new Button { Text = "موافق", DialogResult = DialogResult.OK, Left = 255, Top = 55 }; prompt.Controls.AddRange([box, ok]); prompt.AcceptButton = ok; return prompt.ShowDialog(this) == DialogResult.OK ? box.Text : null; }
    private void ShowError(string message) => AppDialog.ShowInformation(this, "الاشتراكات والمدفوعات", message);
}

internal sealed class PaymentEditorForm : ResponsiveForm
{
    private readonly NumericUpDown _amount = new() { DecimalPlaces = 2, ThousandsSeparator = true, Minimum = 0.01m, Width = 240 };
    private readonly ComboBox _method = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240 };
    private readonly TextBox _reference = new() { Width = 240 }; private readonly TextBox _notes = new() { Width = 240 };
    public RecordPaymentDto? Values { get; private set; }
    public PaymentEditorForm(decimal max, IServiceScopeFactory? scopes = null)
    {
        Text = "تسجيل دفعة"; Width = 380; Height = 330; StartPosition = FormStartPosition.CenterParent; RightToLeft = RightToLeft.Yes; RightToLeftLayout = true; _amount.Maximum = Math.Max(0.01m, max); _amount.Value = _amount.Maximum; _method.Items.AddRange(["نقدًا", "تحويل بنكي", "أخرى"]); _method.SelectedIndex = 0; Shown += async (_, _) => await LookupOptions.LoadAsync(scopes, _method, "PaymentMethod", ["نقدًا", "تحويل بنكي", "أخرى"]);
        var labels = new[] { "المبلغ", "طريقة الدفع", "المرجع", "ملاحظات" }; var fields = new Control[] { _amount, _method, _reference, _notes };
        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 2, RowCount = 5 }; grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100)); grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < fields.Length; i++) { grid.Controls.Add(new Label { Text = labels[i], Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight }, 0, i); grid.Controls.Add(fields[i], 1, i); }
        var ok = new Button { Text = "حفظ", Width = 90 }; ok.Click += (_, _) => { if (_amount.Value <= 0) { AppDialog.ShowInformation(this, Text, "يجب أن يكون مبلغ الدفعة أكبر من صفر."); _amount.Focus(); return; } if (_amount.Value > max) { AppDialog.ShowInformation(this, Text, "المبلغ أكبر من المتبقي."); _amount.Focus(); return; } var method = _method.Text == "نقدًا" ? PaymentMethod.Cash : _method.Text == "تحويل بنكي" ? PaymentMethod.BankTransfer : PaymentMethod.Other; var note = method == PaymentMethod.Other && _method.Text != "أخرى" ? $"طريقة الدفع: {_method.Text}\n{_notes.Text}" : _notes.Text; Values = new RecordPaymentDto(_amount.Value, method, _reference.Text, note); DialogResult = DialogResult.OK; Close(); };
        var cancel = new Button { Text = "إلغاء", DialogResult = DialogResult.Cancel, Width = 90 }; grid.Controls.Add(ok, 0, 4); grid.Controls.Add(cancel, 1, 4); Controls.Add(grid); AcceptButton = ok; CancelButton = cancel;
    }
}


internal sealed class AbsenceFineForm : ResponsiveForm
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ComboBox _child = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260 };
    private readonly DateTimePicker _date = new() { Format = DateTimePickerFormat.Short, Width = 260 };
    private readonly TextBox _notes = new() { Width = 260 };
    public ChildLookupDto? Child => _child.SelectedItem as ChildLookupDto;
    public DateTime FineDate => _date.Value;
    public string? Notes => string.IsNullOrWhiteSpace(_notes.Text) ? null : _notes.Text.Trim();

    public AbsenceFineForm(IServiceScopeFactory scopes)
    {
        _scopes = scopes; Text = "تسجيل غرامة غياب"; Width = 480; Height = 340; StartPosition = FormStartPosition.CenterParent;
        RightToLeft = RightToLeft.Yes; RightToLeftLayout = true; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
        var fields = new (string Label, Control Control)[] { ("الطفل", _child), ("تاريخ الغرامة", _date), ("ملاحظات", _notes) };
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 2, RowCount = fields.Length + 1 };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130)); table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < fields.Length; i++) { table.Controls.Add(new Label { Text = fields[i].Label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight }, 0, i); table.Controls.Add(fields[i].Control, 1, i); }
        var save = new Button { Text = "تسجيل الغرامة", Width = 130 }; var cancel = new Button { Text = "إلغاء", Width = 90, DialogResult = DialogResult.Cancel };
        save.Click += (_, _) => { if (Child is null) { AppDialog.ShowInformation(this, Text, "اختر الطفل."); return; } DialogResult = DialogResult.OK; Close(); };
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft }; actions.Controls.AddRange([save, cancel]); table.Controls.Add(actions, 0, fields.Length); table.SetColumnSpan(actions, 2); Controls.Add(table); AcceptButton = save; CancelButton = cancel;
        Load += (_, _) => _ = UiGuard.RunAsync(this, "AbsenceFine.LoadChildren", async () =>
        {
            await using var scope = _scopes.CreateAsyncScope();
            var result = await scope.ServiceProvider.GetRequiredService<ISubscriptionService>().GetChildLookupAsync(null);
            if (result.IsSuccess)
                foreach (var child in result.Value) _child.Items.Add(child);
        }, "تعذر تحميل قائمة الأطفال.");
    }
}

