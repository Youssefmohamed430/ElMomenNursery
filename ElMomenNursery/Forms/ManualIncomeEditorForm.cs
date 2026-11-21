using System.Drawing;
using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Controls;
using ElMomenNursery.Styling;
using Microsoft.Extensions.DependencyInjection;

namespace ElMomenNursery.Forms;

public sealed class ManualIncomeEditorForm : Form
{
    private readonly IServiceScopeFactory? _scopes;
    private readonly FinanceTransactionDto? _transaction;

    private readonly DateTimePicker _date = new() { Format = DateTimePickerFormat.Short, RightToLeft = RightToLeft.Yes };
    private readonly ComboBox _source = new() { DropDownStyle = ComboBoxStyle.DropDown, RightToLeft = RightToLeft.Yes };
    private readonly TextBox _description = new() { MaxLength = 200, RightToLeft = RightToLeft.Yes };
    private readonly NumericUpDown _amount = new() { DecimalPlaces = 2, ThousandsSeparator = true, Minimum = 0.01m, Maximum = 10_000_000m, RightToLeft = RightToLeft.Yes };
    private readonly TextBox _reference = new() { MaxLength = 100, RightToLeft = RightToLeft.Yes };
    private readonly TextBox _notes = new() { MaxLength = 500, Multiline = true, Height = 60, RightToLeft = RightToLeft.Yes };

    private readonly Button _save = MainForm.CreateActionButton("حفظ", true);

    public ManualIncomeEditorForm() : this(null, null) { }

    public ManualIncomeEditorForm(IServiceScopeFactory? scopes, FinanceTransactionDto? transaction = null)
    {
        _scopes = scopes;
        _transaction = transaction;

        Text = transaction is null ? "تسجيل إيراد يدوي" : "تعديل إيراد يدوي";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(520, 520);
        Size = new Size(580, 560);
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        Font = new Font("Segoe UI", 10F);
        BackColor = BrandPalette.AppBackground;

        _source.Items.AddRange(["تبرع", "منحة", "دعم خارجي", "نشاط ترفيهي إضافي", "رحلة", "إيراد متنوع", "أخرى"]);
        _source.SelectedIndex = 0;
        _date.Value = DateTime.Today;

        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24),
            ColumnCount = 2,
            RowCount = 6,
            RightToLeft = RightToLeft.Yes,
            BackColor = BrandPalette.Surface
        };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddField(fields, "تاريخ الإيراد *", _date, 0, 50);
        AddField(fields, "مصدر الإيراد *", _source, 1, 50);
        AddField(fields, "البيان / الوصف *", _description, 2, 50);
        AddField(fields, "المبلغ (ج.م) *", _amount, 3, 50);
        AddField(fields, "المرجع / رقم الإيصال", _reference, 4, 50);
        AddField(fields, "ملاحظات", _notes, 5, 80);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 64,
            FlowDirection = FlowDirection.LeftToRight,
            RightToLeft = RightToLeft.Yes,
            Padding = new Padding(16)
        };
        _save.Width = 120;
        var cancel = MainForm.CreateActionButton("إلغاء");
        cancel.Width = 100;
        cancel.DialogResult = DialogResult.Cancel;

        _save.Click += async (_, _) => await SaveAsync();
        footer.Controls.AddRange([_save, cancel]);

        Controls.Add(fields);
        Controls.Add(footer);

        AcceptButton = _save;
        CancelButton = cancel;

        if (transaction is not null)
        {
            _date.Value = transaction.Date.ToDateTime(TimeOnly.MinValue);
            _source.Text = transaction.Category;
            _description.Text = transaction.Description;
            _amount.Value = Math.Max(0.01m, transaction.Amount);
            _reference.Text = transaction.Reference ?? string.Empty;
        }
    }

    private static void AddField(TableLayoutPanel table, string label, Control control, int row, int height)
    {
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        table.Controls.Add(new Label { Text = label, TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill, RightToLeft = RightToLeft.Yes }, 0, row);
        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(4);
        table.Controls.Add(control, 1, row);
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_description.Text))
        {
            AppDialog.ShowInformation(this, "تنبيه", "يرجى كتابة بيان الإيراد.");
            _description.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(_source.Text))
        {
            AppDialog.ShowInformation(this, "تنبيه", "يرجى تحديد مصدر الإيراد.");
            _source.Focus();
            return;
        }

        if (_amount.Value <= 0)
        {
            AppDialog.ShowInformation(this, "تنبيه", "المبلغ يجب أن يكون أكبر من الصفر.");
            _amount.Focus();
            return;
        }

        if (_scopes is null)
        {
            DialogResult = DialogResult.OK;
            Close();
            return;
        }

        _save.Enabled = false;
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<IFinanceService>();

            if (_transaction is null)
            {
                var dto = new CreateManualIncomeDto
                {
                    IncomeDate = DateOnly.FromDateTime(_date.Value),
                    Source = _source.Text.Trim(),
                    Amount = _amount.Value,
                    Description = _description.Text.Trim(),
                    Reference = string.IsNullOrWhiteSpace(_reference.Text) ? null : _reference.Text.Trim(),
                    Notes = string.IsNullOrWhiteSpace(_notes.Text) ? null : _notes.Text.Trim()
                };

                var res = await service.CreateManualIncomeAsync(dto);
                if (!ResultPresenter.Present(this, "المقبوضات", res))
                {
                    _save.Enabled = true;
                    return;
                }
            }
            else
            {
                var dto = new UpdateManualIncomeDto
                {
                    IncomeDate = DateOnly.FromDateTime(_date.Value),
                    Source = _source.Text.Trim(),
                    Amount = _amount.Value,
                    Description = _description.Text.Trim(),
                    Reference = string.IsNullOrWhiteSpace(_reference.Text) ? null : _reference.Text.Trim(),
                    Notes = string.IsNullOrWhiteSpace(_notes.Text) ? null : _notes.Text.Trim()
                };

                var res = await service.UpdateManualIncomeAsync(_transaction.Id, dto);
                if (!ResultPresenter.Present(this, "المقبوضات", res))
                {
                    _save.Enabled = true;
                    return;
                }
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception)
        {
            _save.Enabled = true;
            AppDialog.ShowInformation(this, "خطأ", "حدث خطأ أثناء حفظ الإيراد. يرجى المحاولة مرة أخرى.");
        }
    }
}

