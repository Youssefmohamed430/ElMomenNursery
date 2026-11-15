using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Domain.Enums;
using Serilog;

namespace ElMomenNursery.Forms;

public sealed class EmployeeEditorForm : Form
{
    private readonly IEmployeeService _employeeService;
    private readonly EmployeeDto? _employee;

    private readonly TextBox _fullName = CreateTextBox(150);
    private readonly TextBox _qualification = CreateTextBox(120);
    private readonly TextBox _graduationYear = CreateTextBox(4);
    private readonly ComboBox _maritalStatus = CreateMaritalStatusPicker();
    private readonly TextBox _nationalId = CreateTextBox(50);
    private readonly TextBox _phone = CreateTextBox(50);
    private readonly TextBox _alternatePhone = CreateTextBox(50);
    private readonly TextBox _address = CreateTextBox(250, multiline: true);
    private readonly TextBox _jobDesignation = CreateTextBox(100);
    private readonly DateTimePicker _hireDate = CreateDatePicker();
    private readonly TextBox _salary = CreateTextBox(30);
    private readonly CheckBox _hasExperience = new() { Text = "لديه خبرة", AutoSize = true, RightToLeft = RightToLeft.Yes };
    private readonly Button _saveButton = CreateButton("حفظ", primary: true);

    public EmployeeEditorForm(IEmployeeService employeeService, EmployeeDto? employee = null)
    {
        _employeeService = employeeService;
        _employee = employee;
        Text = employee is null ? "إضافة موظف" : "تعديل بيانات الموظف";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(820, 620);
        Size = new Size(1020, 760);
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        Font = new Font("Segoe UI", 10F);
        BackColor = Color.WhiteSmoke;

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
            BackColor = Color.Transparent
        };
        sections.Controls.Add(CreateSection("البيانات الشخصية", table =>
        {
            AddPair(table, "الاسم الكامل *", _fullName, "المؤهل", _qualification);
            AddPair(table, "سنة التخرج", _graduationYear, "الحالة الاجتماعية *", _maritalStatus);
        }));
        sections.Controls.Add(CreateSection("بيانات التواصل والهوية", table =>
        {
            AddPair(table, "الرقم القومي *", _nationalId, "الهاتف *", _phone);
            AddPair(table, "هاتف بديل", _alternatePhone, "العنوان *", _address);
        }));
        sections.Controls.Add(CreateSection("بيانات العمل", table =>
        {
            AddPair(table, "المسمى الوظيفي *", _jobDesignation, "تاريخ التعيين *", _hireDate);
            AddPair(table, "الراتب *", _salary, "الخبرة", _hasExperience);
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
        _saveButton.Click += async (_, _) => await SaveAsync();
        footer.Controls.Add(_saveButton);
        footer.Controls.Add(cancelButton);
        Controls.Add(content);
        Controls.Add(footer);
        AcceptButton = _saveButton;
        CancelButton = cancelButton;

        if (employee is not null) Populate(employee);
    }

    private void Populate(EmployeeDto employee)
    {
        _fullName.Text = employee.FullName;
        _qualification.Text = employee.Qualification;
        _graduationYear.Text = employee.GraduationYear?.ToString() ?? string.Empty;
        _maritalStatus.SelectedItem = _maritalStatus.Items.Cast<MaritalStatusOption>().First(option => option.Value == employee.MaritalStatus);
        _nationalId.Text = employee.NationalIdNumber;
        _phone.Text = employee.Phone;
        _alternatePhone.Text = employee.AlternatePhone;
        _address.Text = employee.Address;
        _jobDesignation.Text = employee.JobDesignation;
        _hireDate.Value = employee.HireDate.ToDateTime(TimeOnly.MinValue);
        _salary.Text = employee.Salary.ToString("0.00");
        _hasExperience.Checked = employee.HasExperience;
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_fullName.Text) || string.IsNullOrWhiteSpace(_nationalId.Text) ||
            string.IsNullOrWhiteSpace(_phone.Text) || string.IsNullOrWhiteSpace(_address.Text) ||
            string.IsNullOrWhiteSpace(_jobDesignation.Text))
        {
            ShowError("املأ الحقول المطلوبة المعلّمة بنجمة.");
            return;
        }

        if (!decimal.TryParse(_salary.Text, out var salary) || salary < 0)
        {
            ShowError("أدخل راتبًا صحيحًا يساوي صفرًا أو أكثر.");
            _salary.Focus();
            return;
        }

        int? graduationYear = null;
        if (!string.IsNullOrWhiteSpace(_graduationYear.Text))
        {
            if (!int.TryParse(_graduationYear.Text, out var year))
            {
                ShowError("أدخل سنة تخرج صحيحة أو اتركها فارغة.");
                _graduationYear.Focus();
                return;
            }
            graduationYear = year;
        }

        _saveButton.Enabled = false;
        try
        {
            var maritalStatus = ((MaritalStatusOption)_maritalStatus.SelectedItem!).Value;
            if (_employee is null)
            {
                await _employeeService.CreateEmployee(new CreateEmployeeDto
                {
                    FullName = _fullName.Text.Trim(),
                    Qualification = OptionalText(_qualification.Text),
                    GraduationYear = graduationYear,
                    HasExperience = _hasExperience.Checked,
                    NationalIdNumber = _nationalId.Text.Trim(),
                    Address = _address.Text.Trim(),
                    MaritalStatus = maritalStatus,
                    Phone = _phone.Text.Trim(),
                    AlternatePhone = OptionalText(_alternatePhone.Text),
                    JobDesignation = _jobDesignation.Text.Trim(),
                    HireDate = DateOnly.FromDateTime(_hireDate.Value),
                    Salary = salary
                });
            }
            else
            {
                await _employeeService.UpdateEmployee(_employee.Id, new UpdateEmployeeDto
                {
                    FullName = _fullName.Text.Trim(),
                    Qualification = _qualification.Text.Trim(),
                    GraduationYear = graduationYear,
                    HasExperience = _hasExperience.Checked,
                    NationalIdNumber = _nationalId.Text.Trim(),
                    Address = _address.Text.Trim(),
                    MaritalStatus = maritalStatus,
                    Phone = _phone.Text.Trim(),
                    AlternatePhone = _alternatePhone.Text.Trim(),
                    JobDesignation = _jobDesignation.Text.Trim(),
                    HireDate = DateOnly.FromDateTime(_hireDate.Value),
                    Salary = salary
                });
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (InvalidOperationException exception)
        {
            Log.Warning(exception, "Failed to save employee because of a business rule; employeeId={EmployeeId}", _employee?.Id);
            ShowError("الرقم القومي مسجل لموظف آخر.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to save employee; employeeId={EmployeeId}", _employee?.Id);
            ShowError("تعذر حفظ بيانات الموظف. راجع القيم المدخلة، وتأكد أن الرقم القومي غير مسجل من قبل.");
        }
        finally
        {
            if (!IsDisposed) _saveButton.Enabled = true;
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
            Font = new Font("Segoe UI", 9.5F)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
        build(table);
        group.Height = Math.Max(90, table.RowStyles.Cast<RowStyle>().Sum(style => (int)style.Height) + 42);
        group.Controls.Add(table);
        return group;
    }

    private static void AddPair(TableLayoutPanel table, string firstLabel, Control first, string secondLabel, Control second)
    {
        var row = table.RowCount++;
        var height = Math.Max(46, Math.Max(first.Height, second.Height) + 8);
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        table.Controls.Add(CreateLabel(firstLabel), 0, row);
        table.Controls.Add(PrepareField(first), 1, row);
        table.Controls.Add(CreateLabel(secondLabel), 2, row);
        table.Controls.Add(PrepareField(second), 3, row);
    }

    private static Label CreateLabel(string text) => new()
    {
        Text = text,
        Width = 120,
        Height = 40,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleRight,
        RightToLeft = RightToLeft.Yes
    };

    private static Control PrepareField(Control control)
    {
        control.Margin = new Padding(4);
        if (control is TextBox or ComboBox or DateTimePicker) control.Dock = DockStyle.Fill;
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

    private static ComboBox CreateMaritalStatusPicker()
    {
        var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, RightToLeft = RightToLeft.Yes, Font = new Font("Segoe UI", 10F) };
        combo.Items.AddRange(Enum.GetValues<MaritalStatus>().Select(value => new MaritalStatusOption(value, value switch
        {
            MaritalStatus.Single => "أعزب/عزباء",
            MaritalStatus.Married => "متزوج/ة",
            MaritalStatus.Divorced => "مطلق/ة",
            MaritalStatus.Widowed => "أرمل/ة",
            _ => value.ToString()
        })).ToArray());
        combo.SelectedIndex = 0;
        return combo;
    }

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

    private void ShowError(string message) => MessageBox.Show(this, message, "مراجعة بيانات الموظف",
        MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1,
        MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

    private static string? OptionalText(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record MaritalStatusOption(MaritalStatus Value, string Name)
    {
        public override string ToString() => Name;
    }
}
