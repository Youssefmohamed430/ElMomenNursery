using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Controls;
using ElMomenNursery.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace ElMomenNursery.Forms;

public sealed class ChildEditorForm : Form
{
    private static readonly ILogger _log = Log.ForContext<ChildEditorForm>();
    private readonly IServiceScopeFactory _scopes;
    private readonly ChildDto? _child;

    private readonly TextBox _fullName = CreateTextBox(100);
    private readonly TextBox _address = CreateTextBox(200, multiline: true);
    private readonly DateTimePicker _dateOfBirth = CreateDatePicker();
    private readonly DateTimePicker _enrollmentDate = CreateDatePicker();
    private readonly ComboBox _gender = CreateGenderPicker();
    private readonly TextBox _fatherName = CreateTextBox(100);
    private readonly TextBox _fatherOccupation = CreateTextBox(100);
    private readonly TextBox _fatherPhone = CreateTextBox(50);
    private readonly TextBox _fatherWhatsApp = CreateTextBox(50);
    private readonly TextBox _motherName = CreateTextBox(100);
    private readonly TextBox _motherOccupation = CreateTextBox(100);
    private readonly TextBox _motherPhone = CreateTextBox(50);
    private readonly TextBox _motherWhatsApp = CreateTextBox(50);
    private readonly TextBox _notes = CreateTextBox(1000, multiline: true);
    private readonly CheckBox _hasMedicalConditions = CreateCheckBox("لديه حالة طبية");
    private readonly CheckBox _previouslyAttended = CreateCheckBox("سبق له الالتحاق بحضانة");
    private readonly Button _saveButton = CreateButton("حفظ", primary: true);

    public ChildEditorForm(IServiceScopeFactory scopes, ChildDto? child = null)
    {
        _scopes = scopes;
        _child = child;
        Text = child is null ? "إضافة طفل" : "تعديل بيانات الطفل";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(820, 640);
        Size = new Size(1020, 820);
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        Font = new Font("Segoe UI", 10F);
        BackColor = Color.WhiteSmoke;
        FormBorderStyle = FormBorderStyle.Sizable;

        var content = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(14), BackColor = BackColor };
        var sections = new FlowLayoutPanel
        {
            Location = new Point(0, 0),
            Width = 950,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            RightToLeft = RightToLeft.Yes,
            BackColor = Color.Transparent,
            Padding = Padding.Empty
        };
        sections.Controls.Add(CreateSection("بيانات الطفل", table =>
        {
            AddPair(table, "اسم الطفل *", _fullName, "النوع *", _gender);
            AddPair(table, "تاريخ الميلاد *", _dateOfBirth, "تاريخ الالتحاق *", _enrollmentDate);
            AddSingle(table, "العنوان *", _address);
        }));
        sections.Controls.Add(CreateSection("بيانات الأب", table =>
        {
            AddPair(table, "اسم الأب *", _fatherName, "المهنة", _fatherOccupation);
            AddPair(table, "رقم الهاتف *", _fatherPhone, "واتساب", _fatherWhatsApp);
        }));
        sections.Controls.Add(CreateSection("بيانات الأم", table =>
        {
            AddPair(table, "اسم الأم *", _motherName, "المهنة", _motherOccupation);
            AddPair(table, "رقم الهاتف *", _motherPhone, "واتساب", _motherWhatsApp);
        }));
        sections.Controls.Add(CreateSection("بيانات إضافية", table =>
        {
            AddPair(table, "الحالة الطبية", _hasMedicalConditions, "الالتحاق السابق", _previouslyAttended);
            AddSingle(table, "ملاحظات", _notes);
        }));
        content.Controls.Add(sections);

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
        _saveButton.Click += (_, _) => RunSafeEvent(() => SaveAsync(), "Save child record");
        footer.Controls.Add(_saveButton);
        footer.Controls.Add(cancelButton);
        Controls.Add(content);
        Controls.Add(footer);
        AcceptButton = _saveButton;
        CancelButton = cancelButton;

        if (child is not null)
            Populate(child);
    }

    private void Populate(ChildDto child)
    {
        _fullName.Text = child.FullName;
        _address.Text = child.Address;
        _dateOfBirth.Value = ToDateTime(child.DateOfBirth);
        _enrollmentDate.Value = ToDateTime(child.EnrollmentDate);
        _gender.SelectedItem = _gender.Items.Cast<GenderOption>().First(option => option.Value == child.Gender);
        _fatherName.Text = child.FatherName;
        _fatherOccupation.Text = child.FatherOccupation;
        _fatherPhone.Text = child.FatherPhone;
        _fatherWhatsApp.Text = child.FatherWhatsApp;
        _motherName.Text = child.MotherName;
        _motherOccupation.Text = child.MotherOccupation;
        _motherPhone.Text = child.MotherPhone;
        _motherWhatsApp.Text = child.MotherWhatsApp;
        _notes.Text = child.Notes;
        _hasMedicalConditions.Checked = child.HasMedicalConditions;
        _previouslyAttended.Checked = child.PreviouslyAttendedNursery;
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_fullName.Text) || string.IsNullOrWhiteSpace(_address.Text) ||
            string.IsNullOrWhiteSpace(_fatherName.Text) || string.IsNullOrWhiteSpace(_fatherPhone.Text) ||
            string.IsNullOrWhiteSpace(_motherName.Text) || string.IsNullOrWhiteSpace(_motherPhone.Text))
        {
            ShowValidationError("املأ الحقول المطلوبة المعلّمة بنجمة.");
            return;
        }

        var dateOfBirth = DateOnly.FromDateTime(_dateOfBirth.Value);
        var enrollmentDate = DateOnly.FromDateTime(_enrollmentDate.Value);
        if (enrollmentDate < dateOfBirth)
        {
            ShowValidationError("تاريخ الالتحاق يجب أن يكون في يوم الميلاد أو بعده.");
            _enrollmentDate.Focus();
            return;
        }

        _saveButton.Enabled = false;
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            var childService = scope.ServiceProvider.GetRequiredService<IChildService>();
            var gender = ((GenderOption)_gender.SelectedItem!).Value;
            if (_child is null)
            {
                var result = await childService.CreateChildAsync(new CreateChildDTO
                {
                    FullName = _fullName.Text.Trim(),
                    Address = _address.Text.Trim(),
                    EnrollmentDate = enrollmentDate,
                    DateOfBirth = dateOfBirth,
                    Gender = gender,
                    FatherName = _fatherName.Text.Trim(),
                    FatherOccupation = OptionalText(_fatherOccupation.Text),
                    FatherPhone = _fatherPhone.Text.Trim(),
                    FatherWhatsApp = OptionalText(_fatherWhatsApp.Text),
                    MotherName = _motherName.Text.Trim(),
                    MotherOccupation = OptionalText(_motherOccupation.Text),
                    MotherPhone = _motherPhone.Text.Trim(),
                    MotherWhatsApp = OptionalText(_motherWhatsApp.Text),
                    Notes = OptionalText(_notes.Text),
                    HasMedicalConditions = _hasMedicalConditions.Checked,
                    PreviouslyAttendedNursery = _previouslyAttended.Checked
                });
                if (!ResultPresenter.Present(this, "الأطفال", result)) return;
            }
            else
            {
                var result = await childService.UpdateChildAsync(_child.Id, new UpdateChildDTO
                {
                    FullName = _fullName.Text.Trim(),
                    Address = _address.Text.Trim(),
                    EnrollmentDate = enrollmentDate,
                    DateOfBirth = dateOfBirth,
                    Gender = gender,
                    FatherName = _fatherName.Text.Trim(),
                    FatherOccupation = OptionalText(_fatherOccupation.Text),
                    FatherPhone = _fatherPhone.Text.Trim(),
                    FatherWhatsApp = OptionalText(_fatherWhatsApp.Text),
                    MotherName = _motherName.Text.Trim(),
                    MotherOccupation = OptionalText(_motherOccupation.Text),
                    MotherPhone = _motherPhone.Text.Trim(),
                    MotherWhatsApp = OptionalText(_motherWhatsApp.Text),
                    Notes = OptionalText(_notes.Text),
                    HasMedicalConditions = _hasMedicalConditions.Checked,
                    PreviouslyAttendedNursery = _previouslyAttended.Checked
                });
                if (!ResultPresenter.Present(this, "الأطفال", result)) return;
            }

            if (IsDisposed) return;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (DbUpdateException exception)
        {
            var details = exception.GetBaseException().Message;
            var constraintKind = details.Contains("CHECK constraint", StringComparison.OrdinalIgnoreCase) ? "CHECK"
                : details.Contains("UNIQUE constraint", StringComparison.OrdinalIgnoreCase) ? "UNIQUE"
                : details.Contains("FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase) ? "FOREIGN KEY"
                : "UNKNOWN";
            _log.Warning(exception, "Child save rejected by database constraint; childId={ChildId}; constraintKind={ConstraintKind}", _child?.Id, constraintKind);
            var message = details.Contains("CK_Children_DateOrder", StringComparison.OrdinalIgnoreCase)
                ? "يجب أن يكون تاريخ الالتحاق في يوم الميلاد أو بعده."
                : constraintKind switch
                {
                    "UNIQUE" => "تعذر حفظ البيانات لوجود سجل مكرر.",
                    "FOREIGN KEY" => "تعذر حفظ البيانات لوجود سجلات مرتبطة.",
                    _ => "تعذر حفظ بيانات الطفل. راجع البيانات ثم أعد المحاولة."
                };
            if (!IsDisposed) ShowValidationError(message);
        }
        catch (Exception exception)
        {
            _log.Error(exception, "Failed to save child record; childId={ChildId}", _child?.Id);
            if (!IsDisposed) ShowValidationError("تعذر حفظ بيانات الطفل. راجع البيانات ثم أعد المحاولة.");
        }
        finally
        {
            if (!IsDisposed)
                _saveButton.Enabled = true;
        }
    }

    private async void RunSafeEvent(Func<Task> work, string operation)
    {
        try
        {
            await work();
        }
        catch (Exception exception)
        {
            _log.Error(exception, "Unhandled child form event failure; operation={Operation}", operation);
            if (!IsDisposed) ShowValidationError("تعذر إكمال العملية. أعد المحاولة.");
        }
    }

    private static GroupBox CreateSection(string title, Action<TableLayoutPanel> build)
    {
        var group = new GroupBox
        {
            Text = title,
            Width = 940,
            Height = 120,
            Padding = new Padding(12),
            Margin = new Padding(0, 0, 0, 10),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };
        var table = new TableLayoutPanel
        {
            ColumnCount = 4,
            AutoSize = false,
            Dock = DockStyle.Fill,
            RightToLeft = RightToLeft.Yes,
            Padding = new Padding(6),
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
        build(table);
        var tableHeight = table.RowStyles.Cast<RowStyle>().Sum(style => (int)style.Height);
        group.Height = Math.Max(90, tableHeight + 42);
        group.Controls.Add(table);
        return group;
    }

    private static void AddPair(TableLayoutPanel table, string firstLabel, Control first, string secondLabel, Control second)
    {
        var row = table.RowCount++;
        var rowHeight = Math.Max(46, Math.Max(first.Height, second.Height) + 8);
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, rowHeight));
        table.Controls.Add(CreateFieldLabel(firstLabel), 0, row);
        table.Controls.Add(PrepareField(first), 1, row);
        table.Controls.Add(CreateFieldLabel(secondLabel), 2, row);
        table.Controls.Add(PrepareField(second), 3, row);
    }

    private static void AddSingle(TableLayoutPanel table, string label, Control control)
    {
        var row = table.RowCount++;
        var rowHeight = Math.Max(46, control.Height + 8);
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, rowHeight));
        table.Controls.Add(CreateFieldLabel(label), 0, row);
        table.Controls.Add(PrepareField(control), 1, row);
        table.SetColumnSpan(control, table.ColumnCount - 1);
    }

    private static Label CreateFieldLabel(string text) => new()
    {
        Text = text,
        AutoSize = false,
        Width = 120,
        Height = 40,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleRight,
        RightToLeft = RightToLeft.Yes,
        Font = new Font("Segoe UI", 9.5F)
    };

    private static Control PrepareField(Control control)
    {
        control.Margin = new Padding(4);
        if (control is TextBox or ComboBox or DateTimePicker)
            control.Dock = DockStyle.Fill;
        return control;
    }

    private static TextBox CreateTextBox(int maxLength, bool multiline = false) => new()
    {
        MaxLength = maxLength,
        Multiline = multiline,
        Height = multiline ? 64 : 34,
        ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None,
        RightToLeft = RightToLeft.Yes,
        Font = new Font("Segoe UI", 10F)
    };

    private static DateTimePicker CreateDatePicker() => new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "dd/MM/yyyy",
        Value = DateTime.Today,
        MinDate = new DateTime(1900, 1, 1),
        MaxDate = new DateTime(2100, 12, 31),
        RightToLeft = RightToLeft.Yes,
        RightToLeftLayout = true,
        Font = new Font("Segoe UI", 10F)
    };

    private static ComboBox CreateGenderPicker()
    {
        var combo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            RightToLeft = RightToLeft.Yes,
            Font = new Font("Segoe UI", 10F)
        };
        combo.Items.AddRange([new GenderOption(ChildGender.Boy, "ولد"), new GenderOption(ChildGender.Girl, "بنت")]);
        combo.SelectedIndex = 0;
        return combo;
    }

    private static CheckBox CreateCheckBox(string text) => new()
    {
        Text = text,
        AutoSize = true,
        CheckAlign = ContentAlignment.MiddleRight,
        RightToLeft = RightToLeft.Yes,
        Font = new Font("Segoe UI", 9.5F)
    };

    private static Button CreateButton(string text, bool primary = false) => new()
    {
        Text = text,
        Height = 40,
        FlatStyle = FlatStyle.Flat,
        BackColor = primary ? Color.FromArgb(44, 120, 85) : Color.White,
        ForeColor = primary ? Color.White : Color.FromArgb(35, 55, 45),
        UseVisualStyleBackColor = false,
        RightToLeft = RightToLeft.Yes,
        Font = new Font("Segoe UI", 9.5F, primary ? FontStyle.Bold : FontStyle.Regular)
    };

    private void ShowValidationError(string message) => MessageBox.Show(this, message, "مراجعة البيانات",
        MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1,
        MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

    private static DateTime ToDateTime(DateOnly date) => date.ToDateTime(TimeOnly.MinValue);
    private static string? OptionalText(string text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private sealed record GenderOption(ChildGender Value, string Name)
    {
        public override string ToString() => Name;
    }
}
