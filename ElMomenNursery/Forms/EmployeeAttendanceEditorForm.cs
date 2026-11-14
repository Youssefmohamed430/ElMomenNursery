using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Domain.Enums;

namespace ElMomenNursery.Forms;

public sealed class EmployeeAttendanceEditorForm : Form
{
    private readonly IEmployeeAttendanceService _attendanceService;
    private readonly EmployeeDailyAttendanceDto _row;
    private readonly Guid _currentUserId;
    private readonly ComboBox _status = CreateStatusPicker();
    private readonly DateTimePicker _checkIn = CreateTimePicker();
    private readonly DateTimePicker _checkOut = CreateTimePicker();
    private readonly TextBox _notes = new() { Multiline = true, Height = 72, MaxLength = 500, ScrollBars = ScrollBars.Vertical, RightToLeft = RightToLeft.Yes };
    private readonly Button _saveButton = CreateButton("حفظ", primary: true);

    public EmployeeAttendanceEditorForm(
        IEmployeeAttendanceService attendanceService,
        EmployeeDailyAttendanceDto row,
        Guid currentUserId)
    {
        _attendanceService = attendanceService;
        _row = row;
        _currentUserId = currentUserId;
        Text = row.IsMarked ? "تصحيح حضور الموظف" : "تسجيل حضور يدوي";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(650, 440);
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        Font = new Font("Segoe UI", 10F);
        BackColor = Color.WhiteSmoke;

        var title = new Label
        {
            Text = $"{row.EmployeeName} — {row.JobDesignation}\nالتاريخ: {row.AttendanceDate:dd/MM/yyyy}",
            Dock = DockStyle.Top,
            Height = 76,
            Padding = new Padding(16),
            TextAlign = ContentAlignment.MiddleRight,
            RightToLeft = RightToLeft.Yes,
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.FromArgb(35, 78, 58)
        };
        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(20, 8, 20, 8),
            RightToLeft = RightToLeft.Yes
        };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        fields.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        AddField(fields, 0, "الحالة *", _status);
        AddField(fields, 1, "وقت الحضور", _checkIn);
        AddField(fields, 2, "وقت الانصراف", _checkOut);
        AddField(fields, 3, "ملاحظات", _notes);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 62,
            FlowDirection = FlowDirection.LeftToRight,
            RightToLeft = RightToLeft.Yes,
            WrapContents = false,
            Padding = new Padding(12, 8, 12, 8),
            BackColor = Color.White
        };
        _saveButton.Width = 130;
        var cancelButton = CreateButton("إلغاء");
        cancelButton.Width = 130;
        cancelButton.DialogResult = DialogResult.Cancel;
        _saveButton.Click += async (_, _) => await SaveAsync();
        footer.Controls.Add(_saveButton);
        footer.Controls.Add(cancelButton);
        Controls.Add(fields);
        Controls.Add(footer);
        Controls.Add(title);
        AcceptButton = _saveButton;
        CancelButton = cancelButton;

        Populate();
    }

    private void Populate()
    {
        _status.SelectedItem = _status.Items.Cast<AttendanceStatusOption>()
            .First(option => option.Value == (_row.Status ?? AttendanceStatus.Present));
        if (_row.CheckInTime.HasValue)
        {
            _checkIn.Value = _row.CheckInTime.Value.ToLocalTime().DateTime;
            _checkIn.Checked = true;
        }
        else
        {
            _checkIn.Value = DateTime.Now;
            _checkIn.Checked = !_row.IsMarked && _row.AttendanceDate == DateOnly.FromDateTime(DateTime.Today);
        }

        if (_row.CheckOutTime.HasValue)
        {
            _checkOut.Value = _row.CheckOutTime.Value.ToLocalTime().DateTime;
            _checkOut.Checked = true;
        }
        else
        {
            _checkOut.Value = DateTime.Now;
            _checkOut.Checked = false;
        }
        _notes.Text = _row.Notes ?? string.Empty;
    }

    private async Task SaveAsync()
    {
        var checkIn = ToDateTimeOffset(_checkIn, _row.AttendanceDate);
        var checkOut = ToDateTimeOffset(_checkOut, _row.AttendanceDate);
        if (checkIn.HasValue && checkOut.HasValue && checkOut.Value < checkIn.Value)
        {
            ShowError("وقت الانصراف لا يمكن أن يسبق وقت الحضور.");
            return;
        }

        _saveButton.Enabled = false;
        try
        {
            var status = ((AttendanceStatusOption)_status.SelectedItem!).Value;
            if (_row.AttendanceId is Guid attendanceId)
            {
                await _attendanceService.UpdateEmployeeAttendanceAsync(attendanceId, new UpdateEmployeeAttendanceDto
                {
                    AttendanceDate = _row.AttendanceDate,
                    CheckInTime = checkIn,
                    CheckOutTime = checkOut,
                    Status = status,
                    RecordedByUserId = _currentUserId,
                    Notes = OptionalText(_notes.Text)
                });
            }
            else
            {
                await _attendanceService.MarkManualAttendanceAsync(new CreateEmployeeAttendanceDto
                {
                    EmployeeId = _row.EmployeeId,
                    AttendanceDate = _row.AttendanceDate,
                    CheckInTime = checkIn,
                    CheckOutTime = checkOut,
                    Status = status,
                    EntryMethod = AttendanceEntryMethod.Manual,
                    RecordedByUserId = _currentUserId,
                    Notes = OptionalText(_notes.Text)
                });
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (InvalidOperationException)
        {
            ShowError("تم تسجيل الحضور لهذا الموظف في هذا التاريخ بالفعل.");
        }
        catch
        {
            ShowError("تعذر حفظ سجل الحضور. راجع البيانات وحاول مرة أخرى.");
        }
        finally
        {
            if (!IsDisposed) _saveButton.Enabled = true;
        }
    }

    private static void AddField(TableLayoutPanel table, int row, string labelText, Control control)
    {
        var label = new Label
        {
            Text = labelText,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            RightToLeft = RightToLeft.Yes
        };
        control.Margin = new Padding(4);
        if (control is not TextBox { Multiline: true }) control.Dock = DockStyle.Fill;
        table.Controls.Add(label, 0, row);
        table.Controls.Add(control, 1, row);
    }

    private static ComboBox CreateStatusPicker()
    {
        var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, RightToLeft = RightToLeft.Yes };
        combo.Items.AddRange(Enum.GetValues<AttendanceStatus>().Select(status => new AttendanceStatusOption(status, status switch
        {
            AttendanceStatus.Present => "حاضر",
            AttendanceStatus.Absent => "غائب",
            AttendanceStatus.Late => "متأخر",
            AttendanceStatus.Excused => "غياب بعذر",
            _ => status.ToString()
        })).ToArray());
        combo.SelectedIndex = 0;
        return combo;
    }

    private static DateTimePicker CreateTimePicker() => new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "HH:mm",
        ShowUpDown = true,
        ShowCheckBox = true,
        Checked = false,
        RightToLeft = RightToLeft.Yes
    };

    private static DateTimeOffset? ToDateTimeOffset(DateTimePicker picker, DateOnly date)
    {
        if (!picker.Checked) return null;
        var time = TimeOnly.FromDateTime(picker.Value);
        var localDateTime = date.ToDateTime(time, DateTimeKind.Unspecified);
        var offset = TimeZoneInfo.Local.GetUtcOffset(localDateTime);
        return new DateTimeOffset(localDateTime, offset);
    }

    private static Button CreateButton(string text, bool primary = false) => new()
    {
        Text = text,
        Height = 40,
        FlatStyle = FlatStyle.Flat,
        BackColor = primary ? Color.FromArgb(44, 120, 85) : Color.White,
        ForeColor = primary ? Color.White : Color.FromArgb(35, 55, 45),
        UseVisualStyleBackColor = false,
        RightToLeft = RightToLeft.Yes
    };

    private void ShowError(string message) => MessageBox.Show(this, message, "حضور الموظفين",
        MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1,
        MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

    private static string? OptionalText(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record AttendanceStatusOption(AttendanceStatus Value, string Name)
    {
        public override string ToString() => Name;
    }
}
