using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Controls;
using ElMomenNursery.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using static ElMomenNursery.Forms.MainForm;

namespace ElMomenNursery.Forms;

internal sealed class ChildRegistrationForm : ResponsiveForm
{
    private readonly IServiceScopeFactory _scopes;
    private readonly UserSession _session;
    private readonly TextBox _name = new() { Dock = DockStyle.Fill, MaxLength = 100, PlaceholderText = "اسم الطفل فقط" };
    private readonly DateTimePicker _start = new() { Format = DateTimePickerFormat.Short, Dock = DockStyle.Fill };
    private readonly DateTimePicker _end = new() { Format = DateTimePickerFormat.Short, Dock = DockStyle.Fill };
    private readonly NumericUpDown _due = new() { Minimum = 0, Maximum = 10000000, DecimalPlaces = 2, ThousandsSeparator = true, Dock = DockStyle.Fill };
    private readonly NumericUpDown _paid = new() { Minimum = 0, Maximum = 10000000, DecimalPlaces = 2, ThousandsSeparator = true, Dock = DockStyle.Fill };
    private bool _paidManuallySet;
    private bool _syncingPaid;
    private readonly ComboBox _method = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly CheckBox _addSubscription = new() { Text = "إنشاء اشتراك وتسجيل دفعة الآن", AutoSize = true, Checked = true };
    private readonly CheckBox _addStoreInvoice = new() { Text = "إضافة فاتورة أصناف من المخزن بعد حفظ التسجيل", AutoSize = true, Checked = true };

    public ChildRegistrationForm(IServiceScopeFactory scopes, UserSession session)
    {
        _scopes = scopes; _session = session; Text = "تسجيل طفل جديد"; StartPosition = FormStartPosition.CenterParent;
        Size = new Size(650, 470); MinimumSize = new Size(560, 420); RightToLeft = RightToLeft.Yes; RightToLeftLayout = true;
        Font = new Font("Segoe UI", 10F); BackColor = Color.WhiteSmoke;
        var today = DateTime.Today; _start.Value = today; _end.Value = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
        _method.Items.AddRange(["نقدًا", "تحويل بنكي", "أخرى"]); _method.SelectedIndex = 0;
        _paid.Enabled = session.HasPermission("Payments.Create");
        _due.ValueChanged += (_, _) =>
        {
            if (_paidManuallySet || !_paid.Enabled) return;
            _syncingPaid = true; _paid.Value = Math.Min(_paid.Maximum, _due.Value); _syncingPaid = false;
        };
        _paid.ValueChanged += (_, _) => { if (!_syncingPaid) _paidManuallySet = true; };
        var canCreateSubscription = session.HasPermission("Subscriptions.Manage");
        var canCreateStoreInvoice = session.HasPermission("Payments.Create");
        _addSubscription.Visible = canCreateSubscription;
        _addStoreInvoice.Visible = canCreateStoreInvoice;
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 2, AutoScroll = true };
        table.RowCount = 2 + (canCreateSubscription ? 6 : 0) + (canCreateStoreInvoice ? 1 : 0);
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 185)); table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Add("اسم الطفل", _name);
        if (canCreateSubscription)
        {
            Add("", _addSubscription); Add("بداية الاشتراك", _start); Add("نهاية الاشتراك", _end);
            Add("قيمة الاشتراك", _due); Add("المبلغ المدفوع (افتراضيًا كامل الاشتراك)", _paid); Add("طريقة الدفع", _method);
        }
        if (canCreateStoreInvoice) Add("", _addStoreInvoice);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        var save = CreateActionButton("حفظ التسجيل", true); var cancel = CreateActionButton("إلغاء"); cancel.DialogResult = DialogResult.Cancel;
        actions.Controls.AddRange([cancel, save]); table.RowStyles.Add(new RowStyle(SizeType.Absolute, 52)); table.Controls.Add(actions, 1, table.RowCount - 1); Controls.Add(table); CancelButton = cancel;
        _addSubscription.CheckedChanged += (_, _) => SetSubscriptionFields(_addSubscription.Checked);
        save.Click += async (_, _) => await SaveAsync();

        void Add(string label, Control input)
        {
            var row = table.Controls.Count / 2; table.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            table.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight }, 0, row);
            table.Controls.Add(input, 1, row);
        }
    }

    private void SetSubscriptionFields(bool enabled)
    {
        foreach (var control in new Control[] { _start, _end, _due, _paid, _method }) control.Enabled = enabled;
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_name.Text)) { AppDialog.ShowInformation(this, Text, "اكتب اسم الطفل."); _name.Focus(); return; }
        if (_addSubscription.Checked && _session.HasPermission("Subscriptions.Manage") && _start.Value.Date > _end.Value.Date)
        { AppDialog.ShowInformation(this, Text, "نهاية الاشتراك يجب أن تكون بعد بدايته."); return; }
        if (_paid.Value > _due.Value) { AppDialog.ShowInformation(this, Text, "المبلغ المدفوع لا يمكن أن يزيد على قيمة الاشتراك."); return; }
        await UiGuard.RunAsync(this, "Child.RegisterPending", async () =>
        {
            await using var scope = _scopes.CreateAsyncScope();
            var childResult = await scope.ServiceProvider.GetRequiredService<IChildService>().CreatePendingRegistrationAsync(_name.Text);
            if (childResult.IsFailure) { AppDialog.ShowInformation(this, Text, childResult.Error.Description); return; }
            var shouldSubscribe = _addSubscription.Checked && _session.HasPermission("Subscriptions.Manage");
            if (shouldSubscribe)
            {
                InitialPaymentDto? payment = _paid.Value > 0
                    ? new InitialPaymentDto(_paid.Value, _method.SelectedIndex switch { 1 => PaymentMethod.BankTransfer, 2 => PaymentMethod.Other, _ => PaymentMethod.Cash })
                    : null;
                var subscription = await scope.ServiceProvider.GetRequiredService<ISubscriptionService>().CreateSubscriptionAsync(new CreateSubscriptionDto
                {
                    ChildId = childResult.Value.Id,
                    PeriodStart = DateOnly.FromDateTime(_start.Value), PeriodEnd = DateOnly.FromDateTime(_end.Value),
                    AmountDue = _due.Value, InitialPayment = payment
                });
                if (subscription.IsFailure)
                {
                    AppDialog.ShowInformation(this, Text, $"تم حفظ التسجيل المبدئي باسم الطفل، لكن تعذر تسجيل الاشتراك: {subscription.Error.Description}");
                    DialogResult = DialogResult.OK; Close(); return;
                }
            }
            if (_addStoreInvoice.Checked && _session.HasPermission("Payments.Create"))
            {
                using var invoice = new CashierInvoiceForm(_scopes, childResult.Value.Id, childResult.Value.FullName);
                if (invoice.ShowDialog(this) != DialogResult.OK)
                    AppDialog.ShowInformation(this, Text, shouldSubscribe
                        ? "تم حفظ التسجيل والاشتراك. لم يتم إصدار فاتورة مخزن."
                        : "تم حفظ التسجيل المبدئي. لم يتم إصدار فاتورة مخزن.");
            }
            DialogResult = DialogResult.OK; Close();
        }, "تعذر تسجيل الطفل مبدئيًا.");
    }
}
