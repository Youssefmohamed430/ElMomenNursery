using System.Drawing;
using ElMomenNursery.Domain.Abstractions;
using ElMomenNursery.Domain.Enums;
using ElMomenNursery.Styling;

namespace ElMomenNursery.Forms;

internal sealed record AttendanceEditorValues(AttendanceStatus Status, DateTimeOffset? CheckIn, DateTimeOffset? CheckOut,
    string? Notes, string? Reason);

internal sealed class AttendanceEditorForm : ResponsiveForm
{
    private readonly DateOnly _date;
    private readonly bool _correction;
    private readonly Func<AttendanceEditorValues, Task<Error?>> _save;
    private readonly ComboBox _status = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, RightToLeft = RightToLeft.Yes };
    private readonly DateTimePicker _checkIn = TimePicker();
    private readonly DateTimePicker _checkOut = TimePicker();
    private readonly TextBox _notes = new() { Multiline = true, MaxLength = 500, ScrollBars = ScrollBars.Vertical, RightToLeft = RightToLeft.Yes, Dock = DockStyle.Fill };
    private readonly TextBox _reason = new() { Multiline = true, MaxLength = 500, ScrollBars = ScrollBars.Vertical, RightToLeft = RightToLeft.Yes, Dock = DockStyle.Fill };
    private readonly Button _submit = new() { Text = "حفظ", DialogResult = DialogResult.None, Width = 120, Height = 38 };

    // Visual Studio's WinForms designer needs a parameterless constructor to preview this form.
    // The preview uses sample values and a no-op save callback; normal app flows use the full constructor below.
    public AttendanceEditorForm()
        : this("معاينة", "اسم الطفل", DateOnly.FromDateTime(DateTime.Today), correction: false,
            status: AttendanceStatus.Present, checkIn: null, checkOut: null, notes: null,
            save: _ => Task.FromResult<Error?>(null))
    {
    }

    public AttendanceEditorForm(string title, string subject, DateOnly date, bool correction,
        AttendanceStatus? status, DateTimeOffset? checkIn, DateTimeOffset? checkOut, string? notes,
        Func<AttendanceEditorValues, Task<Error?>> save)
    {
        _date = date; _correction = correction; _save = save;
        Text = correction ? $"تصحيح حضور {title}" : $"تسجيل حضور {title}";
        StartPosition = FormStartPosition.CenterParent; MinimumSize = new Size(540, 500); Size = new Size(620, 560);
        RightToLeft = RightToLeft.Yes; RightToLeftLayout = true; BackColor = BrandPalette.AppBackground;
        Font = new Font("Segoe UI", 10F); AutoScaleMode = AutoScaleMode.Dpi;
        _status.Items.AddRange(Enum.GetValues<AttendanceStatus>().Select(x => new StatusItem(x, AttendanceLabels.Status(x))).ToArray());
        _status.SelectedItem = _status.Items.Cast<StatusItem>().First(x => x.Value == (status ?? AttendanceStatus.Present));
        SetTime(_checkIn, checkIn); SetTime(_checkOut, checkOut); _notes.Text = notes ?? string.Empty;
        _reason.Visible = correction;

        var heading = new Label { Text = $"{subject}\nالتاريخ: {date:dd/MM/yyyy}", Dock = DockStyle.Top, Height = 70, Padding = new Padding(18), TextAlign = ContentAlignment.MiddleRight, ForeColor = BrandPalette.PrimaryGreen, Font = new Font("Segoe UI", 12, FontStyle.Bold) };
        var fields = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = correction ? 5 : 4, Padding = new Padding(18), RightToLeft = RightToLeft.Yes };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 135)); fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        fields.RowStyles.Add(new RowStyle(SizeType.Percent, correction ? 50 : 100)); if (correction) fields.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        AddField(fields, 0, "الحالة", _status); AddField(fields, 1, "وقت الحضور", _checkIn); AddField(fields, 2, "وقت الانصراف", _checkOut); AddField(fields, 3, "ملاحظات", _notes);
        if (correction) AddField(fields, 4, "سبب التصحيح *", _reason);
        var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 56, FlowDirection = FlowDirection.LeftToRight, RightToLeft = RightToLeft.Yes, Padding = new Padding(12) };
        var cancel = new Button { Text = "إلغاء", DialogResult = DialogResult.Cancel, Width = 105, Height = 36 };
        _submit.Click += async (_, _) => await SaveAsync(); footer.Controls.Add(_submit); footer.Controls.Add(cancel);
        Controls.Add(fields); Controls.Add(footer); Controls.Add(heading); AcceptButton = _submit; CancelButton = cancel;
        _status.SelectedIndexChanged += (_, _) => UpdateTimes(); UpdateTimes();
    }

    private async Task SaveAsync()
    {
        if (_correction && string.IsNullOrWhiteSpace(_reason.Text)) { ShowError("سبب التصحيح مطلوب."); return; }
        _submit.Enabled = false;
        try
        {
            var selected = (StatusItem)_status.SelectedItem!;
            var error = await _save(new AttendanceEditorValues(selected.Value, ReadTime(_checkIn), ReadTime(_checkOut), Clean(_notes.Text), Clean(_reason.Text)));
            if (IsDisposed) return;
            if (error is not null) { ShowError(error.Description); return; }
            DialogResult = DialogResult.OK; Close();
        }
        catch (UnauthorizedAccessException) { ShowError("لا تملك الصلاحية اللازمة لإتمام العملية."); }
        catch { ShowError("تعذر حفظ سجل الحضور. راجع البيانات ثم أعد المحاولة."); }
        finally { if (!IsDisposed) _submit.Enabled = true; }
    }

    private void UpdateTimes()
    {
        var status = ((StatusItem)_status.SelectedItem!).Value;
        var enabled = status is AttendanceStatus.Present or AttendanceStatus.Late;
        _checkIn.Enabled = _checkOut.Enabled = enabled;
        if (!enabled) { _checkIn.Checked = false; _checkOut.Checked = false; }
    }
    private DateTimeOffset? ReadTime(DateTimePicker picker)
    {
        if (!picker.Enabled || !picker.Checked) return null;
        var local = DateTime.SpecifyKind(_date.ToDateTime(TimeOnly.FromDateTime(picker.Value)), DateTimeKind.Unspecified);
        return new DateTimeOffset(local, TimeZoneInfo.Local.GetUtcOffset(local));
    }
    private static void SetTime(DateTimePicker picker, DateTimeOffset? value) { if (value.HasValue) { picker.Value = value.Value.ToLocalTime().DateTime; picker.Checked = true; } }
    private static DateTimePicker TimePicker() => new() { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm", ShowUpDown = true, ShowCheckBox = true, Checked = false, RightToLeft = RightToLeft.Yes };
    private static void AddField(TableLayoutPanel table, int row, string text, Control control) { table.Controls.Add(new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, RightToLeft = RightToLeft.Yes }, 0, row); control.Margin = new Padding(4); table.Controls.Add(control, 1, row); }
    private void ShowError(string message) { if (!IsDisposed) MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign); }
    private static string? Clean(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private sealed record StatusItem(AttendanceStatus Value, string Name) { public override string ToString() => Name; }
}
