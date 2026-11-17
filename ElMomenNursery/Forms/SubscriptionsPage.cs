using System.Diagnostics;
using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Domain.Enums;
using ElMomenNursery.Domain.Abstractions;
using ElMomenNursery.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace ElMomenNursery.Forms;

public sealed class SubscriptionsPage : UserControl
{
    private readonly IServiceScopeFactory _scopes;
    private readonly UserSession _session;
    private readonly IExcelExportService _excel;
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RightToLeft = RightToLeft.Yes, RowHeadersVisible = false };
    private readonly TextBox _search = new() { Width = 170, PlaceholderText = "بحث باسم الطفل", RightToLeft = RightToLeft.Yes };
    private readonly ComboBox _status = new() { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _plan = new() { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly DateTimePicker _from = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", ShowCheckBox = true, Width = 135 };
    private readonly DateTimePicker _to = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", ShowCheckBox = true, Width = 135 };
    private readonly CheckBox _balance = new() { Text = "عليه متبقي فقط", AutoSize = true };
    private readonly Label _summary = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };
    private readonly Button _add = Button("إضافة اشتراك");
    private readonly Button _edit = Button("تعديل");
    private readonly Button _pay = Button("تسجيل دفعة");
    private readonly Button _cancel = Button("إلغاء الاشتراك");
    private readonly Button _delete = Button("حذف");
    private readonly Button _receipt = Button("طباعة / إعادة طباعة إيصال");
    private readonly Button _export = Button("تصدير Excel");
    private List<SubscriptionDto> _rows = [];
    private int _loadVersion;

    public SubscriptionsPage(IServiceScopeFactory scopes, UserSession session, IExcelExportService excel)
    {
        _scopes = scopes; _session = session; _excel = excel; Dock = DockStyle.Fill; Padding = new Padding(10); RightToLeft = RightToLeft.Yes; BackColor = Color.White;
        _status.Items.AddRange(["الكل", "مستحق", "مدفوع جزئيًا", "مدفوع", "ملغي", "معفى"]); _status.SelectedIndex = 0;
        _plan.Items.Add(new PlanChoice(null, "كل الخطط")); _plan.SelectedIndex = 0;
        _grid.Columns.AddRange([new DataGridViewTextBoxColumn { HeaderText = "الطفل" }, new DataGridViewTextBoxColumn { HeaderText = "الخطة" },
            new DataGridViewTextBoxColumn { HeaderText = "من" }, new DataGridViewTextBoxColumn { HeaderText = "إلى" }, new DataGridViewTextBoxColumn { HeaderText = "المستحق" },
            new DataGridViewTextBoxColumn { HeaderText = "المدفوع" }, new DataGridViewTextBoxColumn { HeaderText = "المتبقي" }, new DataGridViewTextBoxColumn { HeaderText = "الحالة" },
            new DataGridViewTextBoxColumn { HeaderText = "ملاحظات" }, new DataGridViewTextBoxColumn { HeaderText = "تاريخ الإضافة" }, new DataGridViewTextBoxColumn { Visible = false }]);
        _add.Visible = session.HasPermission("Subscriptions.Manage"); _edit.Visible = _cancel.Visible = _delete.Visible = session.HasPermission("Subscriptions.Manage");
        _pay.Visible = session.HasPermission("Payments.Create"); _receipt.Visible = session.HasPermission("Payments.View"); _export.Visible = session.HasPermission("Subscriptions.Export");
        var filter = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = true, AutoScroll = true };
        var apply = Button("تطبيق"); var clear = Button("مسح الفلاتر"); var refresh = Button("تحديث");
        filter.Controls.AddRange([_search, _status, _plan, _from, _to, _balance, apply, clear, refresh, _add, _edit, _pay, _cancel, _delete, _receipt, _export]);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 102)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(filter, 0, 0); layout.Controls.Add(_summary, 0, 1); layout.Controls.Add(_grid, 0, 2); Controls.Add(layout);
        apply.Click += async (_, _) => await LoadAsync(); refresh.Click += async (_, _) => await LoadAsync();
        clear.Click += async (_, _) => { _search.Clear(); _status.SelectedIndex = 0; _plan.SelectedIndex = 0; _from.Checked = _to.Checked = false; _balance.Checked = false; await LoadAsync(); };
        _add.Click += async (_, _) => await EditAsync(null); _edit.Click += async (_, _) => { if (Selected() is { } row) await EditAsync(row); };
        _cancel.Click += async (_, _) => await CancelAsync(); _delete.Click += async (_, _) => await DeleteAsync();
        _pay.Click += async (_, _) => await RecordPaymentAsync(); _receipt.Click += async (_, _) => await PrintAsync();
        _export.Click += async (_, _) => await GridExcelExport.SaveAsync((IWin32Window?)FindForm() ?? this, _grid, _excel, "تصدير الاشتراكات", "الاشتراكات");
        _grid.SelectionChanged += (_, _) => UpdateActions(); _grid.KeyDown += async (_, e) => { if (e.KeyCode == Keys.F5) { e.Handled = true; await LoadAsync(); } };
        Load += async (_, _) => { await LoadPlansAsync(); await LoadAsync(); };
    }

    private async Task LoadPlansAsync()
    {
        try { await using var s = _scopes.CreateAsyncScope(); var svc = s.ServiceProvider.GetRequiredService<ISubscriptionService>(); var res = await svc.GetPlansAsync();
            if (res.IsFailure) return; foreach (var plan in res.Value) _plan.Items.Add(new PlanChoice(plan.Id, plan.Name)); }
        catch { ShowError("تعذر تحميل خطط الاشتراك."); }
    }

    private async Task LoadAsync()
    {
        var version = Interlocked.Increment(ref _loadVersion); var selected = Selected()?.Id;
        try
        {
            var status = _status.SelectedItem?.ToString() switch { "مستحق" => SubscriptionStatus.Pending, "مدفوع جزئيًا" => SubscriptionStatus.PartiallyPaid, "مدفوع" => SubscriptionStatus.Paid, "ملغي" => SubscriptionStatus.Cancelled, "معفى" => SubscriptionStatus.Waived, _ => (SubscriptionStatus?)null };
            await using var scope = _scopes.CreateAsyncScope(); var svc = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
            var result = await svc.GetSubscriptionsAsync(new SubscriptionFilter { SearchText = _search.Text, Status = status, PlanId = (_plan.SelectedItem as PlanChoice)?.Id,
                PeriodFrom = _from.Checked ? DateOnly.FromDateTime(_from.Value) : null, PeriodTo = _to.Checked ? DateOnly.FromDateTime(_to.Value) : null, OnlyWithBalance = _balance.Checked });
            if (IsDisposed || version != _loadVersion) return;
            if (result.IsFailure) { ShowError(result.Error.Description); return; } _rows = result.Value.ToList(); Render(selected);
        }
        catch { if (!IsDisposed && version == _loadVersion) ShowError("تعذر تحميل الاشتراكات."); }
    }

    private void Render(Guid? keep = null)
    {
        _grid.Rows.Clear(); foreach (var row in _rows)
        {
            var i = _grid.Rows.Add(row.ChildName, row.SubscriptionPlanName ?? "—", row.PeriodStart.ToString("dd/MM/yyyy"), row.PeriodEnd.ToString("dd/MM/yyyy"),
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
        try { await using var scope = _scopes.CreateAsyncScope(); var svc = scope.ServiceProvider.GetRequiredService<ISubscriptionService>(); var res = await svc.CancelSubscriptionAsync(row.Id, reason);
            if (!ResultPresenter.Present(this, "إلغاء الاشتراك", res)) return; await LoadAsync(); }
        catch { ShowError("تعذر إلغاء الاشتراك."); }
    }

    private async Task DeleteAsync()
    {
        if (Selected() is not { } row) return;
        if (row.PaidAmount > 0) { ShowError(SubscriptionErrors.HasPayments.Description); return; }
        if (!AppDialog.Confirm(this, "تأكيد الحذف", "سيتم حذف الاشتراك المحدد. هل تريد المتابعة؟")) return;
        try { await using var scope = _scopes.CreateAsyncScope(); var svc = scope.ServiceProvider.GetRequiredService<ISubscriptionService>(); var res = await svc.DeleteSubscriptionAsync(row.Id);
            if (!ResultPresenter.Present(this, "حذف الاشتراك", res)) return; await LoadAsync(); }
        catch { ShowError("تعذر حذف الاشتراك."); }
    }

    private async Task RecordPaymentAsync()
    {
        if (Selected() is not { } row) return; using var form = new PaymentEditorForm(row.RemainingAmount);
        if (form.ShowDialog(FindForm()) != DialogResult.OK || form.Values is null) return;
        try { await using var scope = _scopes.CreateAsyncScope(); var svc = scope.ServiceProvider.GetRequiredService<ISubscriptionService>(); var result = await svc.RecordPaymentAsync(row.Id, form.Values);
            if (!ResultPresenter.Present(this, "تسجيل دفعة", result)) return; await LoadAsync(); await CreateAndOpenReceiptAsync(result.Value, false); }
        catch { ShowError("تعذر تسجيل الدفعة."); }
    }

    private async Task PrintAsync()
    {
        if (Selected() is not { } row) return;
        try { await using var scope = _scopes.CreateAsyncScope(); var svc = scope.ServiceProvider.GetRequiredService<ISubscriptionService>(); var payments = await svc.GetPaymentsForSubscriptionAsync(row.Id);
            if (payments.IsFailure) { ShowError(payments.Error.Description); return; } if (payments.Value.Count == 0) { ShowError("لا توجد إيصالات لهذا الاشتراك."); return; }
            var chosen = payments.Value.Count == 1 ? payments.Value[0] : SelectReceipt(payments.Value);
            if (chosen is null) return;
            await CreateAndOpenReceiptAsync(chosen, true); }
        catch { ShowError("تعذر تجهيز الإيصال."); }
    }

    private async Task CreateAndOpenReceiptAsync(PaymentReceiptDto receipt, bool copy)
    {
        try { await using var scope = _scopes.CreateAsyncScope(); var pdf = scope.ServiceProvider.GetRequiredService<IReceiptPdfService>(); var fileResult = await pdf.CreateReceiptFileAsync(receipt, copy);
            if (!ResultPresenter.Present(this, "الإيصال", fileResult)) return;
            var path = fileResult.Value;
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            if (AppDialog.Confirm(this, "حفظ نسخة", "هل تريد حفظ نسخة من الإيصال في موقع تختاره؟"))
            {
                using var dialog = new SaveFileDialog { Filter = "ملف PDF (*.pdf)|*.pdf", DefaultExt = "pdf", AddExtension = true, FileName = Path.GetFileName(path), OverwritePrompt = true };
                if (dialog.ShowDialog(this) == DialogResult.OK) File.Copy(path, dialog.FileName, true);
            } }
        catch { ShowError("تم حفظ الدفعة، لكن تعذر فتح الإيصال. يمكنك إعادة طباعته لاحقًا."); }
    }

    private SubscriptionDto? Selected() => _grid.SelectedRows.Count == 0 ? null : _rows.FirstOrDefault(x => x.Id == _grid.SelectedRows[0].Tag as Guid?);
    private void UpdateActions() { var row = Selected(); _edit.Enabled = _cancel.Enabled = _delete.Enabled = row is not null; _pay.Enabled = row is { RemainingAmount: > 0 }; _receipt.Enabled = row is not null; }
    private void ShowError(string message) => AppDialog.ShowInformation((IWin32Window?)FindForm() ?? this, "الاشتراكات والمدفوعات", message);
    private static string StatusName(SubscriptionStatus s) => s switch { SubscriptionStatus.Pending => "مستحق", SubscriptionStatus.PartiallyPaid => "مدفوع جزئيًا", SubscriptionStatus.Paid => "مدفوع", SubscriptionStatus.Cancelled => "ملغي", SubscriptionStatus.Waived => "معفى", _ => s.ToString() };
    private static Button Button(string text) => new() { Text = text, AutoSize = true, Height = 36, UseVisualStyleBackColor = true };
    private static string? Prompt(string title)
    {
        using var form = new Form { Text = title, Width = 430, Height = 170, StartPosition = FormStartPosition.CenterParent, RightToLeft = RightToLeft.Yes, RightToLeftLayout = true, FormBorderStyle = FormBorderStyle.FixedDialog, MinimizeBox = false, MaximizeBox = false };
        var box = new TextBox { Left = 18, Top = 20, Width = 375, RightToLeft = RightToLeft.Yes }; var ok = Button("موافق"); ok.DialogResult = DialogResult.OK; ok.SetBounds(286, 65, 105, 34); var cancel = Button("إلغاء"); cancel.DialogResult = DialogResult.Cancel; cancel.SetBounds(174,65,105,34);
        form.Controls.AddRange([box, ok, cancel]); form.AcceptButton = ok; form.CancelButton = cancel; return form.ShowDialog() == DialogResult.OK ? box.Text : null;
    }
    private PaymentReceiptDto? SelectReceipt(IReadOnlyList<PaymentReceiptDto> receipts)
    {
        using var dialog = new Form { Text = "اختيار إيصال", Width = 460, Height = 190, StartPosition = FormStartPosition.CenterParent, RightToLeft = RightToLeft.Yes, RightToLeftLayout = true, FormBorderStyle = FormBorderStyle.FixedDialog, MinimizeBox = false, MaximizeBox = false };
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
    private sealed record PlanChoice(Guid? Id, string Name) { public override string ToString() => Name; }
}

internal sealed class SubscriptionEditorForm : Form
{
    private readonly IServiceScopeFactory _scopes; private readonly UserSession _session; private readonly SubscriptionDto? _item;
    private readonly ComboBox _child = new() { DropDownStyle = ComboBoxStyle.DropDown, AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems, Width = 300 };
    private readonly ComboBox _plan = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
    private readonly DateTimePicker _start = new() { Format = DateTimePickerFormat.Short, Width = 300 };
    private readonly DateTimePicker _end = new() { Format = DateTimePickerFormat.Short, Width = 300 };
    private readonly NumericUpDown _due = new() { Maximum = 10000000, DecimalPlaces = 2, ThousandsSeparator = true, Width = 300 };
    private readonly NumericUpDown _initial = new() { Maximum = 10000000, DecimalPlaces = 2, ThousandsSeparator = true, Width = 300 };
    private readonly ComboBox _method = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
    private readonly TextBox _reference = new() { Width = 300 }; private readonly TextBox _notes = new() { Width = 300 };
    private CancellationTokenSource? _childSearchCancellation;
    private bool _loadingChildren;
    public PaymentReceiptDto? CreatedReceipt { get; private set; }
    public SubscriptionEditorForm(IServiceScopeFactory scopes, UserSession session, SubscriptionDto? item)
    {
        _scopes = scopes; _session = session; _item = item; Text = item is null ? "إضافة اشتراك" : "تعديل اشتراك"; Width = 470; Height = 610; StartPosition = FormStartPosition.CenterParent; RightToLeft = RightToLeft.Yes; RightToLeftLayout = true; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
        _method.Items.AddRange(["نقدًا", "تحويل بنكي", "أخرى"]); _method.SelectedIndex = 0; _start.Value = DateTime.Today; _end.Value = DateTime.Today.AddMonths(1);
        _child.TextUpdate += async (_, _) => { if (!_loadingChildren && _item is null) await SearchChildrenAsync(_child.Text); };
        _child.Enabled = item is null;
        var fields = new List<(string, Control)> { ("الطفل", _child), ("الخطة (اختيارية)", _plan), ("من", _start), ("إلى", _end), ("المستحق", _due) };
        if (session.HasPermission("Payments.Create")) fields.AddRange([("المدفوع الآن", _initial), ("طريقة الدفع", _method), ("المرجع", _reference)]);
        fields.Add(("ملاحظات", _notes));
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 2, RowCount = fields.Count + 1, AutoScroll = true }; panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110)); panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < fields.Count; i++) { panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); panel.Controls.Add(new Label { Text = fields[i].Item1, TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, i); fields[i].Item2.Anchor = AnchorStyles.Left | AnchorStyles.Right; panel.Controls.Add(fields[i].Item2, 1, i); }
        var save = new Button { Text = "حفظ", DialogResult = DialogResult.None, Width = 110 }; var cancel = new Button { Text = "إلغاء", DialogResult = DialogResult.Cancel, Width = 110 }; var actions = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill }; actions.Controls.AddRange([save, cancel]); panel.Controls.Add(actions, 0, fields.Count); panel.SetColumnSpan(actions, 2); Controls.Add(panel); AcceptButton = save; CancelButton = cancel;
        _plan.SelectedIndexChanged += (_, _) => { if (_plan.SelectedItem is SubscriptionPlanOptionDto p && _item is null) _due.Value = Math.Min(_due.Maximum, p.DefaultFee); };
        save.Click += async (_, _) => await SaveAsync(); Load += async (_, _) => await LoadOptionsAsync();
    }
    private async Task LoadOptionsAsync()
    {
        try { await using var scope = _scopes.CreateAsyncScope(); var svc = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
            var children = await svc.GetChildLookupAsync(null); var plans = await svc.GetPlansAsync(); if (children.IsFailure || plans.IsFailure) { ShowError("تعذر تحميل بيانات الاختيار."); return; }
            _loadingChildren = true; foreach (var c in children.Value) _child.Items.Add(c); _loadingChildren = false;
            _plan.Items.Add(new PlanOption(null, "بدون خطة")); foreach (var p in plans.Value) _plan.Items.Add(p);
            _plan.SelectedIndex = 0;
            if (_item is null) return;
            _child.Items.Add(new ChildLookupDto(_item.ChildId, _item.ChildName)); _child.SelectedItem = _child.Items.Cast<object>().OfType<ChildLookupDto>().First(x => x.Id == _item.ChildId);
            _start.Value = _item.PeriodStart.ToDateTime(TimeOnly.MinValue); _end.Value = _item.PeriodEnd.ToDateTime(TimeOnly.MinValue); _due.Value = _item.AmountDue; _notes.Text = _item.Notes ?? string.Empty;
            for (var i=0;i<_plan.Items.Count;i++) if (_plan.Items[i] is SubscriptionPlanOptionDto p && p.Id == _item.SubscriptionPlanId) _plan.SelectedIndex=i;
            _initial.Enabled = _method.Enabled = _reference.Enabled = false;
        } catch { _loadingChildren = false; ShowError("تعذر تحميل خيارات الاشتراك."); }
    }
    private async Task SearchChildrenAsync(string search)
    {
        _childSearchCancellation?.Cancel();
        _childSearchCancellation?.Dispose();
        var cancellation = _childSearchCancellation = new CancellationTokenSource();
        try
        {
            await Task.Delay(250, cancellation.Token);
            await using var scope = _scopes.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
            var result = await service.GetChildLookupAsync(search, cancellation.Token);
            if (cancellation.IsCancellationRequested || result.IsFailure || IsDisposed) return;
            var text = _child.Text;
            _loadingChildren = true;
            _child.BeginUpdate(); _child.Items.Clear();
            foreach (var child in result.Value) _child.Items.Add(child);
            _child.Text = text; _child.SelectionStart = text.Length; _child.SelectionLength = 0;
            _child.EndUpdate(); _loadingChildren = false;
            if (_child.Focused && result.Value.Count > 0) _child.DroppedDown = true;
        }
        catch (OperationCanceledException) { }
        catch { _loadingChildren = false; }
    }
    private async Task SaveAsync()
    {
        if (_child.SelectedItem is not ChildLookupDto child) { ShowError("اختر الطفل من القائمة."); return; }
        if (_end.Value.Date < _start.Value.Date) { ShowError("تاريخ نهاية الاشتراك يجب أن يكون بعد بدايته."); return; }
        if (_item is null && _initial.Value > _due.Value) { ShowError("المدفوع الآن أكبر من المستحق."); return; }
        try { await using var scope = _scopes.CreateAsyncScope(); var svc = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
            if (_item is null)
            {
                var overlapConfirmed = false;
                while (true)
                {
                    var result = await svc.CreateSubscriptionAsync(new CreateSubscriptionDto { ChildId = child.Id, SubscriptionPlanId = (_plan.SelectedItem as SubscriptionPlanOptionDto)?.Id,
                        PeriodStart = DateOnly.FromDateTime(_start.Value), PeriodEnd = DateOnly.FromDateTime(_end.Value), AmountDue = _due.Value, Notes = _notes.Text,
                        InitialPayment = _initial.Value > 0 ? new InitialPaymentDto(_initial.Value, MethodValue(), _reference.Text) : null, ConfirmOverlappingPeriod = overlapConfirmed });
                    if (result.IsFailure && result.Error.Code == "Subscriptions.OverlappingPeriod" && !overlapConfirmed)
                    { if (AppDialog.Confirm(this, "فترة متقاطعة", "توجد فترة اشتراك متقاطعة لهذا الطفل. هل تريد المتابعة؟")) { overlapConfirmed = true; continue; } return; }
                    if (!ResultPresenter.Present(this, "الاشتراكات والمدفوعات", result)) return; CreatedReceipt = result.Value.InitialPaymentReceipt; DialogResult = DialogResult.OK; Close(); return;
                }
            }
            var reason = PromptReason(); if (string.IsNullOrWhiteSpace(reason)) { ShowError("سبب التعديل مطلوب."); return; }
            var updated = await svc.UpdateSubscriptionAsync(_item.Id, new UpdateSubscriptionDto { SubscriptionPlanId = (_plan.SelectedItem as SubscriptionPlanOptionDto)?.Id, PeriodStart = DateOnly.FromDateTime(_start.Value), PeriodEnd = DateOnly.FromDateTime(_end.Value), AmountDue = _due.Value, Notes = _notes.Text, Reason = reason });
            if (!ResultPresenter.Present(this, "الاشتراكات والمدفوعات", updated)) return; DialogResult = DialogResult.OK; Close();
        } catch { ShowError("تعذر حفظ الاشتراك."); }
    }
    private PaymentMethod MethodValue() => _method.SelectedIndex switch { 0 => PaymentMethod.Cash, 1 => PaymentMethod.BankTransfer, _ => PaymentMethod.Other };
    private string? PromptReason() { using var prompt = new Form { Text = "سبب التعديل", Width = 380, Height = 145, StartPosition = FormStartPosition.CenterParent, RightToLeft = RightToLeft.Yes }; var box = new TextBox { Left = 15, Top = 15, Width = 330 }; var ok = new Button { Text = "موافق", DialogResult = DialogResult.OK, Left = 255, Top = 55 }; prompt.Controls.AddRange([box, ok]); prompt.AcceptButton = ok; return prompt.ShowDialog(this) == DialogResult.OK ? box.Text : null; }
    private void ShowError(string message) => AppDialog.ShowInformation(this, "الاشتراكات والمدفوعات", message);
    private sealed record PlanOption(Guid? Id, string Name) { public override string ToString() => Name; }
}

internal sealed class PaymentEditorForm : Form
{
    private readonly NumericUpDown _amount = new() { DecimalPlaces = 2, ThousandsSeparator = true, Minimum = 0.01m, Width = 240 };
    private readonly ComboBox _method = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240 };
    private readonly TextBox _reference = new() { Width = 240 }; private readonly TextBox _notes = new() { Width = 240 };
    public RecordPaymentDto? Values { get; private set; }
    public PaymentEditorForm(decimal max)
    {
        Text = "تسجيل دفعة"; Width = 380; Height = 330; StartPosition = FormStartPosition.CenterParent; RightToLeft = RightToLeft.Yes; RightToLeftLayout = true; _amount.Maximum = Math.Max(0.01m, max); _amount.Value = _amount.Maximum; _method.Items.AddRange(["نقدًا", "تحويل بنكي", "أخرى"]); _method.SelectedIndex = 0;
        var labels = new[] { "المبلغ", "طريقة الدفع", "المرجع", "ملاحظات" }; var fields = new Control[] { _amount, _method, _reference, _notes };
        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 2, RowCount = 5 }; grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100)); grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < fields.Length; i++) { grid.Controls.Add(new Label { Text = labels[i], Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight }, 0, i); grid.Controls.Add(fields[i], 1, i); }
        var ok = new Button { Text = "حفظ", Width = 90 }; ok.Click += (_, _) => { if (_amount.Value > max) { AppDialog.ShowInformation(this, Text, "المبلغ أكبر من المتبقي."); return; } Values = new RecordPaymentDto(_amount.Value, _method.SelectedIndex switch { 0 => PaymentMethod.Cash, 1 => PaymentMethod.BankTransfer, _ => PaymentMethod.Other }, _reference.Text, _notes.Text); DialogResult = DialogResult.OK; Close(); };
        var cancel = new Button { Text = "إلغاء", DialogResult = DialogResult.Cancel, Width = 90 }; grid.Controls.Add(ok, 0, 4); grid.Controls.Add(cancel, 1, 4); Controls.Add(grid); AcceptButton = ok; CancelButton = cancel;
    }
}
