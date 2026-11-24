using System.Drawing;
using Serilog;
using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Controls;
using ElMomenNursery.Domain.Abstractions;
using ElMomenNursery.Domain.Enums;
using ElMomenNursery.Styling;
using Microsoft.Extensions.DependencyInjection;
using static ElMomenNursery.Forms.MainForm;

namespace ElMomenNursery.Forms;

internal sealed class EmployeesPage : UserControl
{
    private static readonly ILogger _log = Log.ForContext<EmployeesPage>();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly UserSession _session;
    private readonly IExcelExportService _excelExportService;

    public EmployeesPage(IServiceScopeFactory scopeFactory, UserSession session, IExcelExportService excelExportService)
    {
        _scopeFactory = scopeFactory;
        _session = session;
        _excelExportService = excelExportService;
        Dock = DockStyle.Fill;
        Controls.Add(BuildContent());
    }
    private Control BuildContent()
    {
        var page = CreateDataPage("الموظفون", "إدارة بيانات الموظفين والبحث فيها.", includePrototypeBanner: false);
        var search = CreateSearchBox("بحث بالاسم أو الرقم القومي أو الوظيفة");
        var filters = new FilterCard { Name = "employees.filters" };
        filters.AddField("employees.search", "بحث عن موظف", search, columnSpan: 3);
        var addButton = CreateActionButton("إضافة موظف", primary: true);
        var editButton = CreateActionButton("تعديل البيانات");
        var deleteButton = CreateActionButton("حذف موظف");
        var deletedButton = CreateActionButton("عرض المحذوفين");
        var restoreButton = CreateActionButton("استعادة موظف");
        var exportButton = CreateActionButton("تصدير Excel");
        exportButton.Visible = _session.HasPermission("Employees.Export");
        var canManage = _session.HasPermission("Employees.Manage");
        addButton.Visible = editButton.Visible = deleteButton.Visible = canManage;
        deletedButton.Visible = _session.HasPermission("Employees.Restore");
        restoreButton.Visible = false;
        var resultCount = new Label
        {
            AutoSize = false,
            ForeColor = BrandPalette.MutedText,
            TextAlign = ContentAlignment.MiddleRight,
            RightToLeft = RightToLeft.Yes
        };
        var grid = CreateGrid([
            "اسم الموظف", "المسمى الوظيفي", "الهاتف", "الهاتف البديل", "المؤهل", "سنة التخرج",
            "الخبرة", "الرقم القومي", "العنوان", "الحالة الاجتماعية", "تاريخ التعيين", "الراتب"
        ]);
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        var columnWidths = new[] { 190, 160, 130, 130, 160, 105, 90, 165, 230, 140, 130, 120 };
        for (var index = 0; index < grid.Columns.Count; index++)
            grid.Columns[index].Width = columnWidths[index];

        List<EmployeeDto> employees = [];
        var employeeLoadInProgress = false;
        var showingDeleted = false;
        search.TextChanged += (_, _) => RenderEmployees();
        addButton.Click += (_, _) => RunSafeEvent(async () =>
        {
            using var editor = new EmployeeEditorForm(_scopeFactory);
            if (editor.ShowDialog(this) == DialogResult.OK)
                await LoadEmployeesAsync();
        }, "Open employee creation form");
        editButton.Click += (_, _) => RunSafeEvent(async () =>
        {
            if (!TryGetSelectedEmployeeId(out var id)) return;
            try
            {
                Result<EmployeeDto> employeeResult;
                await using (var scope = _scopeFactory.CreateAsyncScope())
                    employeeResult = await scope.ServiceProvider.GetRequiredService<IEmployeeService>().GetEmployeeById(id);
                if (employeeResult.IsFailure) { ShowEmployeeError(employeeResult.Error.Description); return; }
                var employee = employeeResult.Value;
                if (page.IsDisposed) return;
                using var editor = new EmployeeEditorForm(_scopeFactory, employee);
                if (editor.ShowDialog(this) == DialogResult.OK)
                    await LoadEmployeesAsync();
            }
            catch (Exception exception)
            {
                _log.Error(exception, "Failed to open employee editor; employeeId={EmployeeId}", id);
                if (!page.IsDisposed) ShowEmployeeError("تعذر فتح بيانات الموظف. أعد المحاولة.");
            }
        }, "Open employee editor");
        exportButton.Click += async (_, _) => await GridExcelExport.SaveAsync(this, grid, _excelExportService,
            "تصدير قائمة الموظفين", "الموظفون");
        deletedButton.Click += (_, _) => RunSafeEvent(async () =>
        {
            showingDeleted = !showingDeleted;
            deletedButton.Text = showingDeleted ? "عرض الموظفين" : "عرض المحذوفين";
            restoreButton.Visible = showingDeleted && _session.HasPermission("Employees.Restore");
            addButton.Visible = editButton.Visible = deleteButton.Visible = !showingDeleted && canManage;
            filters.Enabled = !showingDeleted;
            await LoadEmployeesAsync();
        }, "Toggle deleted employees");
        restoreButton.Click += (_, _) => RunSafeEvent(async () =>
        {
            if (!TryGetSelectedEmployeeId(out var id)) return;
            await using var scope = _scopeFactory.CreateAsyncScope();
            var result = await scope.ServiceProvider.GetRequiredService<IEmployeeService>().RestoreEmployee(id);
            if (ResultPresenter.Present(this, "استعادة الموظف", result)) await LoadEmployeesAsync();
        }, "Restore employee");
        deleteButton.Click += (_, _) => RunSafeEvent(async () =>
        {
            if (!TryGetSelectedEmployeeId(out var id)) return;
            var confirm = MessageBox.Show(this,
                "هل تريد نقل سجل الموظف المحدد إلى المحذوفات؟ سيختفي من القوائم النشطة مع الاحتفاظ بسجلاته التاريخية.",
                "تأكيد حذف الموظف", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
            if (confirm != DialogResult.Yes) return;

            try
            {
                Result deleted;
                await using (var scope = _scopeFactory.CreateAsyncScope())
                    deleted = await scope.ServiceProvider.GetRequiredService<IEmployeeService>().DeleteEmployee(id);
                if (page.IsDisposed)
                {
                    _log.Debug("Employee deletion UI update cancelled: page disposed");
                    return;
                }
                if (deleted.IsFailure)
                {
                    ShowEmployeeError(deleted.Error.Description);
                    return;
                }
                ResultPresenter.Present(this, "الموظفون", deleted);
                await LoadEmployeesAsync();
            }
            catch (Exception exception)
            {
                _log.Error(exception, "Failed to delete employee; employeeId={EmployeeId}", id);
                if (!page.IsDisposed) ShowEmployeeError("تعذر نقل سجل الموظف إلى المحذوفات. أعد المحاولة.");
            }
        }, "Delete employee");

        var toolbar = new ActionToolbar { Name = "employees.actions" };
        toolbar.Add(addButton, editButton, deleteButton, deletedButton, restoreButton, exportButton);
        page.Controls.AddRange([filters, toolbar, resultCount, grid]);
        page.Resize += (_, _) => LayoutEmployeesPage();
        LayoutEmployeesPage();
        _ = RunSafeAsync(LoadEmployeesAsync, "Load employees");
        return page;

        async Task LoadEmployeesAsync()
        {
            if (employeeLoadInProgress)
            {
                _log.Debug("Skipped employee load because another load is in progress");
                return;
            }
            employeeLoadInProgress = true;
            try
            {
                Result<IReadOnlyList<EmployeeDto>> loadedResult;
                await using (var scope = _scopeFactory.CreateAsyncScope())
                {
                    var service = scope.ServiceProvider.GetRequiredService<IEmployeeService>();
                    loadedResult = showingDeleted
                        ? await service.GetDeletedEmployees()
                        : await service.GetEmployees();
                }
                if (page.IsDisposed || grid.IsDisposed || resultCount.IsDisposed)
                {
                    _log.Debug("Employee load cancelled: page disposed");
                    return;
                }
                if (loadedResult.IsFailure) { ShowEmployeeError(loadedResult.Error.Description); return; }
                var loadedEmployees = loadedResult.Value.ToList();
                employees = loadedEmployees;
                RenderEmployees();
            }
            catch (Exception exception)
            {
                _log.Error(exception, "Failed to load employees for display");
                if (!page.IsDisposed && !grid.IsDisposed && !resultCount.IsDisposed)
                {
                    employees = [];
                    grid.Rows.Clear();
                    resultCount.Text = "تعذر تحميل الموظفين";
                    ShowEmployeeError("تعذر تحميل قائمة الموظفين.");
                }
            }
            finally
            {
                employeeLoadInProgress = false;
            }
        }

        void RenderEmployees()
        {
            var query = showingDeleted ? string.Empty : search.Text.Trim();
            var visibleEmployees = string.IsNullOrEmpty(query)
                ? employees
                : employees.Where(employee =>
                    employee.FullName.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                    employee.NationalIdNumber.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                    employee.JobDesignation.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                    employee.Phone.Contains(query, StringComparison.CurrentCultureIgnoreCase)).ToList();

            grid.Rows.Clear();
            foreach (var employee in visibleEmployees)
            {
                var index = grid.Rows.Add(
                    employee.FullName,
                    employee.JobDesignation,
                    employee.Phone,
                    employee.AlternatePhone ?? string.Empty,
                    employee.Qualification ?? string.Empty,
                    employee.GraduationYear?.ToString() ?? string.Empty,
                    employee.HasExperience ? "نعم" : "لا",
                    employee.NationalIdNumber,
                    employee.Address,
                    GetMaritalStatusName(employee.MaritalStatus),
                    employee.HireDate.ToString("dd/MM/yyyy"),
                    employee.Salary.ToString("N2"));
                grid.Rows[index].Tag = employee.Id;
            }
            resultCount.Text = $"عدد الموظفين: {visibleEmployees.Count}";
        }

        bool TryGetSelectedEmployeeId(out Guid employeeId)
        {
            if (grid.SelectedRows.Count > 0 && grid.SelectedRows[0].Tag is Guid selectedId)
            {
                employeeId = selectedId;
                return true;
            }

            employeeId = Guid.Empty;
            MessageBox.Show(this, "اختر موظفًا من الجدول أولًا.", "إدارة الموظفين", MessageBoxButtons.OK,
                MessageBoxIcon.Information, MessageBoxDefaultButton.Button1,
                MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
            return false;
        }

        void LayoutEmployeesPage()
        {
            LayoutDataHeader(page);
            var width = page.ClientSize.Width - 68;
            filters.SetBounds(34, 120, width, filters.Height); filters.Arrange();
            toolbar.SetBounds(34, filters.Bottom + 10, width, toolbar.ArrangeItems(width));
            resultCount.SetBounds(34, toolbar.Bottom + 2, width, 26);
            grid.SetBounds(34, resultCount.Bottom + 4, width, Math.Max(160, page.ClientSize.Height - resultCount.Bottom - 34));
        }

        static string GetMaritalStatusName(MaritalStatus status) => status switch
        {
            MaritalStatus.Single => "أعزب/عزباء",
            MaritalStatus.Married => "متزوج/ة",
            MaritalStatus.Divorced => "مطلق/ة",
            MaritalStatus.Widowed => "أرمل/ة",
            _ => status.ToString()
        };

        void ShowEmployeeError(string message) => MessageBox.Show(this, message, "إدارة الموظفين",
            MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1,
            MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
    }
    private static async Task RunSafeAsync(Func<Task> work, string operation)
    {
        try { await work(); }
        catch (Exception exception) { _log.Error(exception, "Unhandled page load failure; operation={Operation}", operation); }
    }

    private async void RunSafeEvent(Func<Task> work, string operation)
    {
        try { await work(); }
        catch (Exception exception)
        {
            _log.Error(exception, "Unhandled employee page event failure; operation={Operation}; userId={UserId}", operation, _session.UserId);
            if (!IsDisposed) AppDialog.ShowInformation((IWin32Window?)FindForm() ?? this, "الموظفون", "تعذر إكمال العملية. أعد المحاولة.");
        }
    }
}
