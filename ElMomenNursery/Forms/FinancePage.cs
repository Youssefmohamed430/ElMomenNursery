using System.Drawing;
using System.Globalization;
using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Controls;
using ElMomenNursery.Styling;
using Microsoft.Extensions.DependencyInjection;

namespace ElMomenNursery.Forms;

public sealed class FinancePage : UserControl
{
    private readonly IServiceScopeFactory? _scopes;
    private readonly UserSession? _session;
    private readonly IExcelExportService? _excel;

    private readonly DateTimePicker _fromPicker = FilterTheme.CreateDatePicker(optional: true);
    private readonly DateTimePicker _toPicker = FilterTheme.CreateDatePicker(optional: true);

    private readonly Button _quickToday = MainForm.CreateActionButton("اليوم");
    private readonly Button _quickThisMonth = MainForm.CreateActionButton("هذا الشهر", true);
    private readonly Button _quickLastMonth = MainForm.CreateActionButton("الشهر السابق");
    private readonly Button _quickAll = MainForm.CreateActionButton("الكل");

    private readonly Label _incomeKpiValue = new() { Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = BrandPalette.PrimaryGreen, AutoSize = true };
    private readonly Label _incomeKpiSub = new() { Font = new Font("Segoe UI", 8.5F), ForeColor = BrandPalette.MutedText, AutoSize = true };

    private readonly Label _expenseKpiValue = new() { Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = BrandPalette.DangerText, AutoSize = true };
    private readonly Label _expenseKpiSub = new() { Font = new Font("Segoe UI", 8.5F), ForeColor = BrandPalette.MutedText, AutoSize = true };

    private readonly Label _netKpiValue = new() { Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = BrandPalette.PrimaryGreen, AutoSize = true };
    private readonly Label _netKpiSub = new() { Font = new Font("Segoe UI", 8.5F), ForeColor = BrandPalette.MutedText, AutoSize = true };
    private Panel? _netKpiCard;

    private readonly DataGridView _incomeGrid;
    private readonly DataGridView _expenseGrid;

    private readonly Button _addIncomeBtn = MainForm.CreateActionButton("إضافة إيراد يدوي", true);
    private readonly Button _editIncomeBtn = MainForm.CreateActionButton("تعديل");
    private readonly Button _deleteIncomeBtn = MainForm.CreateActionButton("حذف");
    private readonly Button _exportIncomeBtn = MainForm.CreateActionButton("تصدير Excel");
    private readonly Label _incomeSummaryLabel = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };

    private readonly Button _addExpenseBtn = MainForm.CreateActionButton("إضافة مصروف خارجي", true);
    private readonly Button _editExpenseBtn = MainForm.CreateActionButton("تعديل");
    private readonly Button _deleteExpenseBtn = MainForm.CreateActionButton("حذف");
    private readonly Button _exportExpenseBtn = MainForm.CreateActionButton("تصدير Excel");
    private readonly Label _expenseSummaryLabel = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };

    private List<FinanceTransactionDto> _incomeRows = [];
    private List<FinanceTransactionDto> _expenseRows = [];
    private int _loadVersion;

    public FinancePage() : this(null, null, null) { }

    public FinancePage(IServiceScopeFactory? scopes, UserSession? session, IExcelExportService? excel)
    {
        _scopes = scopes;
        _session = session;
        _excel = excel;

        Dock = DockStyle.Fill;
        RightToLeft = RightToLeft.Yes;
        BackColor = BrandPalette.AppBackground;

        _incomeGrid = MainForm.CreateGrid(["التاريخ", "النوع", "المصدر / الطرف", "البيان", "المبلغ", "المرجع", "بواسطة"]);
        _expenseGrid = MainForm.CreateGrid(["التاريخ", "النوع", "الفئة", "البيان", "المبلغ", "المرجع", "بواسطة"]);

        // Set default filter to current month
        var today = DateTime.Today;
        _fromPicker.Value = new DateTime(today.Year, today.Month, 1);
        _toPicker.Value = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));

        Controls.Add(BuildLayout());

        WireEvents();

        Load += async (_, _) => await LoadDataAsync();
    }

    private Control BuildLayout()
    {
        var mainContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16), AutoScroll = true };

        // 1. Title Bar
        var titleLabel = new Label
        {
            Text = "الماليات والمصروفات",
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            ForeColor = BrandPalette.Text,
            AutoSize = true,
            Location = new Point(0, 0)
        };
        var subtitleLabel = new Label
        {
            Text = "متابعة شاملة للمقبوضات، الاشتراكات، مبيعات المخزن، الرواتب، والمصروفات الخارجية مع إحصائيات الأرصدة.",
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = BrandPalette.MutedText,
            AutoSize = true,
            Location = new Point(0, 34)
        };

        var headerPanel = new Panel { Dock = DockStyle.Top, Height = 66 };
        headerPanel.Controls.Add(titleLabel);
        headerPanel.Controls.Add(subtitleLabel);

        // 2. Filter Bar
        var filterPanel = BuildFilterBar();

        // 3. KPI Cards
        var kpiPanel = BuildKpiRow();

        // 4. TabControl
        var tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            RightToLeft = RightToLeft.Yes,
            RightToLeftLayout = true,
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold)
        };

        var incomeTabPage = new TabPage("المقبوضات والإيرادات") { BackColor = BrandPalette.AppBackground };
        incomeTabPage.Controls.Add(BuildIncomeTabContent());

        var expenseTabPage = new TabPage("المصروفات والرواتب") { BackColor = BrandPalette.AppBackground };
        expenseTabPage.Controls.Add(BuildExpenseTabContent());

        tabControl.TabPages.Add(incomeTabPage);
        tabControl.TabPages.Add(expenseTabPage);

        // Assemble overall layout
        var topSection = new Panel { Dock = DockStyle.Top, AutoSize = true };
        var topFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Width = Math.Max(320, mainContainer.ClientSize.Width - mainContainer.Padding.Horizontal)
        };

        void ResizeTopSection()
        {
            var width = Math.Max(320, mainContainer.ClientSize.Width - mainContainer.Padding.Horizontal);
            topFlow.Width = headerPanel.Width = filterPanel.Width = kpiPanel.Width = width;
            if (filterPanel is FilterCard filterCard) filterCard.Arrange();
        }
        mainContainer.Resize += (_, _) => ResizeTopSection();
        ResizeTopSection();

        topFlow.Controls.Add(headerPanel);
        topFlow.Controls.Add(filterPanel);
        topFlow.Controls.Add(kpiPanel);
        topSection.Controls.Add(topFlow);

        var bodyPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 0) };
        bodyPanel.Controls.Add(tabControl);

        mainContainer.Controls.Add(bodyPanel);
        mainContainer.Controls.Add(topSection);

        return mainContainer;
    }

    private Control BuildFilterBar()
    {
        var filterBox = new FilterCard { Name = "finance.filters", Margin = new Padding(0, 4, 0, 8) };
        filterBox.AddGroup("finance.period", "الفترة من / إلى", 2, _fromPicker, _toPicker);
        filterBox.AddGroup("finance.quick-period", "فترة سريعة", 3, _quickToday, _quickThisMonth, _quickLastMonth, _quickAll);
        var applyBtn = FilterTheme.CreatePrimaryButton("تطبيق");
        var refreshBtn = FilterTheme.CreateSecondaryButton("تحديث");
        filterBox.AddActions(applyBtn, refreshBtn);

        applyBtn.Click += async (_, _) => await LoadDataAsync();
        refreshBtn.Click += async (_, _) => await LoadDataAsync();

        filterBox.Resize += (_, _) => filterBox.Arrange();
        filterBox.Arrange();
        return filterBox;
    }

    private Control BuildKpiRow()
    {
        var table = new TableLayoutPanel
        {
            Height = 110,
            ColumnCount = 3,
            RowCount = 1,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 6, 0, 10),
            Padding = new Padding(0)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));

        // Income Card
        var incomeCard = CreateCard("إجمالي المقبوضات", _incomeKpiValue, _incomeKpiSub, BrandPalette.SoftGreen, BrandPalette.PrimaryGreen);
        // Expense Card
        var expenseCard = CreateCard("إجمالي المصروفات", _expenseKpiValue, _expenseKpiSub, BrandPalette.SoftRed, BrandPalette.DangerText);
        // Net Card
        _netKpiCard = CreateCard("صافي الرصيد (الفائض)", _netKpiValue, _netKpiSub, BrandPalette.SoftGreen, BrandPalette.PrimaryGreen);

        table.Controls.Add(incomeCard, 0, 0);
        table.Controls.Add(expenseCard, 1, 0);
        table.Controls.Add(_netKpiCard, 2, 0);

        return table;
    }

    private static Panel CreateCard(string title, Label valLabel, Label subLabel, Color bgColor, Color borderIndicatorColor)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = bgColor,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(12),
            Margin = new Padding(4)
        };

        var titleLabel = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = borderIndicatorColor,
            Dock = DockStyle.Top,
            Height = 24
        };

        valLabel.Dock = DockStyle.Top;
        valLabel.Height = 36;
        valLabel.Text = "0.00 ج.م";

        subLabel.Dock = DockStyle.Bottom;
        subLabel.Height = 22;
        subLabel.Text = "جاري التحميل...";

        panel.Controls.Add(valLabel);
        panel.Controls.Add(titleLabel);
        panel.Controls.Add(subLabel);

        return panel;
    }

    private Control BuildIncomeTabContent()
    {
        var container = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8), BackColor = BrandPalette.AppBackground };

        var actionBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        _addIncomeBtn.Height = 38;
        _editIncomeBtn.Height = 38;
        _deleteIncomeBtn.Height = 38;
        _exportIncomeBtn.Height = 38;

        _editIncomeBtn.Enabled = false;
        _deleteIncomeBtn.Enabled = false;

        var canManageIncome = _session?.HasPermission("Finance.ManageIncome") == true;
        _addIncomeBtn.Visible = canManageIncome;
        _editIncomeBtn.Visible = canManageIncome;
        _deleteIncomeBtn.Visible = canManageIncome;
        _exportIncomeBtn.Visible = _session?.HasPermission("Finance.Export") == true;

        actionBar.Controls.AddRange([_addIncomeBtn, _editIncomeBtn, _deleteIncomeBtn, _exportIncomeBtn]);

        var bottomSummary = new Panel { Dock = DockStyle.Bottom, Height = 34, BackColor = BrandPalette.Surface, Padding = new Padding(8, 4, 8, 4) };
        bottomSummary.Controls.Add(_incomeSummaryLabel);

        var gridPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 6, 0, 6) };
        _incomeGrid.Dock = DockStyle.Fill;
        gridPanel.Controls.Add(_incomeGrid);

        container.Controls.Add(gridPanel);
        container.Controls.Add(actionBar);
        container.Controls.Add(bottomSummary);

        return container;
    }

    private Control BuildExpenseTabContent()
    {
        var container = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8), BackColor = BrandPalette.AppBackground };

        var actionBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        _addExpenseBtn.Height = 38;
        _editExpenseBtn.Height = 38;
        _deleteExpenseBtn.Height = 38;
        _exportExpenseBtn.Height = 38;

        _editExpenseBtn.Enabled = false;
        _deleteExpenseBtn.Enabled = false;

        var canManageExpense = _session?.HasPermission("Finance.ManageExpenses") == true || _session?.HasPermission("Expenses.Manage") == true;
        _addExpenseBtn.Visible = canManageExpense;
        _editExpenseBtn.Visible = canManageExpense;
        _deleteExpenseBtn.Visible = canManageExpense;
        _exportExpenseBtn.Visible = _session?.HasPermission("Finance.Export") == true;

        actionBar.Controls.AddRange([_addExpenseBtn, _editExpenseBtn, _deleteExpenseBtn, _exportExpenseBtn]);

        var bottomSummary = new Panel { Dock = DockStyle.Bottom, Height = 34, BackColor = BrandPalette.Surface, Padding = new Padding(8, 4, 8, 4) };
        bottomSummary.Controls.Add(_expenseSummaryLabel);

        var gridPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 6, 0, 6) };
        _expenseGrid.Dock = DockStyle.Fill;
        gridPanel.Controls.Add(_expenseGrid);

        container.Controls.Add(gridPanel);
        container.Controls.Add(actionBar);
        container.Controls.Add(bottomSummary);

        return container;
    }

    private void WireEvents()
    {
        // Quick Filters
        _quickToday.Click += async (_, _) =>
        {
            _fromPicker.Checked = true;
            _toPicker.Checked = true;
            _fromPicker.Value = DateTime.Today;
            _toPicker.Value = DateTime.Today;
            await LoadDataAsync();
        };

        _quickThisMonth.Click += async (_, _) =>
        {
            var now = DateTime.Today;
            _fromPicker.Checked = true;
            _toPicker.Checked = true;
            _fromPicker.Value = new DateTime(now.Year, now.Month, 1);
            _toPicker.Value = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));
            await LoadDataAsync();
        };

        _quickLastMonth.Click += async (_, _) =>
        {
            var now = DateTime.Today.AddMonths(-1);
            _fromPicker.Checked = true;
            _toPicker.Checked = true;
            _fromPicker.Value = new DateTime(now.Year, now.Month, 1);
            _toPicker.Value = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));
            await LoadDataAsync();
        };

        _quickAll.Click += async (_, _) =>
        {
            _fromPicker.Checked = false;
            _toPicker.Checked = false;
            await LoadDataAsync();
        };

        // Income Actions
        _incomeGrid.SelectionChanged += (_, _) => UpdateIncomeActions();
        _incomeGrid.KeyDown += async (_, e) => { if (e.KeyCode == Keys.F5) { e.Handled = true; await LoadDataAsync(); } };

        _addIncomeBtn.Click += async (_, _) =>
        {
            using var form = new ManualIncomeEditorForm(_scopes);
            if (form.ShowDialog(FindForm()) == DialogResult.OK)
                await LoadDataAsync();
        };

        _editIncomeBtn.Click += async (_, _) =>
        {
            var selected = SelectedIncome();
            if (selected is null || !selected.IsEditable) return;
            using var form = new ManualIncomeEditorForm(_scopes, selected);
            if (form.ShowDialog(FindForm()) == DialogResult.OK)
                await LoadDataAsync();
        };

        _deleteIncomeBtn.Click += async (_, _) =>
        {
            var selected = SelectedIncome();
            if (selected is null || !selected.IsEditable) return;
            if (!AppDialog.Confirm(this, "تأكيد الحذف", $"هل أنت متأكد من حذف الإيراد اليدوي '{selected.Description}' بقيمة {selected.Amount:N2} ج.م؟"))
                return;

            if (_scopes is null) return;
            try
            {
                await using var scope = _scopes.CreateAsyncScope();
                var svc = scope.ServiceProvider.GetRequiredService<IFinanceService>();
                var res = await svc.DeleteManualIncomeAsync(selected.Id);
                if (ResultPresenter.Present(this, "المقبوضات", res))
                    await LoadDataAsync();
            }
            catch (Exception)
            {
                AppDialog.ShowInformation(this, "خطأ", "تعذر حذف الإيراد.");
            }
        };

        _exportIncomeBtn.Click += async (_, _) =>
        {
            if (_excel is not null)
                await GridExcelExport.SaveAsync((IWin32Window?)FindForm() ?? this, _incomeGrid, _excel, "تصدير المقبوضات", "المقبوضات");
        };

        // Expense Actions
        _expenseGrid.SelectionChanged += (_, _) => UpdateExpenseActions();
        _expenseGrid.KeyDown += async (_, e) => { if (e.KeyCode == Keys.F5) { e.Handled = true; await LoadDataAsync(); } };

        _addExpenseBtn.Click += async (_, _) =>
        {
            using var form = new ExpenseEditorForm(_scopes);
            if (form.ShowDialog(FindForm()) == DialogResult.OK)
                await LoadDataAsync();
        };

        _editExpenseBtn.Click += async (_, _) =>
        {
            var selected = SelectedExpense();
            if (selected is null || !selected.IsEditable) return;
            using var form = new ExpenseEditorForm(_scopes, selected);
            if (form.ShowDialog(FindForm()) == DialogResult.OK)
                await LoadDataAsync();
        };

        _deleteExpenseBtn.Click += async (_, _) =>
        {
            var selected = SelectedExpense();
            if (selected is null || !selected.IsEditable) return;
            if (!AppDialog.Confirm(this, "تأكيد الإلغاء", $"هل أنت متأكد من إلغاء المصروف '{selected.Description}' بقيمة {selected.Amount:N2} ج.م؟"))
                return;

            if (_scopes is null) return;
            try
            {
                await using var scope = _scopes.CreateAsyncScope();
                var svc = scope.ServiceProvider.GetRequiredService<IFinanceService>();
                var res = await svc.DeleteExpenseAsync(selected.Id);
                if (ResultPresenter.Present(this, "المصروفات", res))
                    await LoadDataAsync();
            }
            catch (Exception)
            {
                AppDialog.ShowInformation(this, "خطأ", "تعذر إلغاء المصروف.");
            }
        };

        _exportExpenseBtn.Click += async (_, _) =>
        {
            if (_excel is not null)
                await GridExcelExport.SaveAsync((IWin32Window?)FindForm() ?? this, _expenseGrid, _excel, "تصدير المصروفات", "المصروفات");
        };
    }

    private async Task LoadDataAsync()
    {
        var currentVersion = Interlocked.Increment(ref _loadVersion);

        FinanceDateFilter filter = new()
        {
            From = _fromPicker.Checked ? DateOnly.FromDateTime(_fromPicker.Value) : null,
            To = _toPicker.Checked ? DateOnly.FromDateTime(_toPicker.Value) : null
        };

        if (_scopes is null) return;

        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<IFinanceService>();

            var summaryTask = service.GetSummaryAsync(filter);
            var incomeTask = service.GetIncomeTransactionsAsync(filter);
            var expenseTask = service.GetExpenseTransactionsAsync(filter);

            await Task.WhenAll(summaryTask, incomeTask, expenseTask);

            if (IsDisposed || currentVersion != _loadVersion) return;

            var summaryRes = await summaryTask;
            var incomeRes = await incomeTask;
            var expenseRes = await expenseTask;

            if (summaryRes.IsSuccess)
                RenderSummary(summaryRes.Value);

            if (incomeRes.IsSuccess)
            {
                _incomeRows = incomeRes.Value.ToList();
                RenderIncomeGrid();
            }

            if (expenseRes.IsSuccess)
            {
                _expenseRows = expenseRes.Value.ToList();
                RenderExpenseGrid();
            }
        }
        catch (Exception)
        {
            if (!IsDisposed && currentVersion == _loadVersion)
                AppDialog.ShowInformation(this, "خطأ", "تعذر تحميل البيانات المالية.");
        }
    }

    private void RenderSummary(FinanceSummaryDto s)
    {
        _incomeKpiValue.Text = $"{s.TotalIncome:N2} ج.م";
        _incomeKpiSub.Text = $"اشتراكات: {s.TotalSubscriptionIncome:N2} | مبيعات: {s.TotalInventorySaleIncome:N2} | إيرادات أخرى: {s.TotalManualIncome:N2}";

        _expenseKpiValue.Text = $"{s.TotalExpenses:N2} ج.م";
        _expenseKpiSub.Text = $"رواتب: {s.TotalPayrollExpense:N2} | مصاريف خارجية: {s.TotalManualExpense:N2}";

        _netKpiValue.Text = $"{(s.NetBalance >= 0 ? "+" : "")}{s.NetBalance:N2} ج.م";
        if (s.NetBalance >= 0)
        {
            _netKpiValue.ForeColor = BrandPalette.PrimaryGreen;
            if (_netKpiCard is not null) _netKpiCard.BackColor = BrandPalette.SoftGreen;
            _netKpiSub.Text = "فائض مالي (المقبوضات تفوق المصروفات)";
        }
        else
        {
            _netKpiValue.ForeColor = BrandPalette.DangerText;
            if (_netKpiCard is not null) _netKpiCard.BackColor = BrandPalette.SoftRed;
            _netKpiSub.Text = "عجز مالي مؤقت (المصروفات تفوق المقبوضات)";
        }
    }

    private void RenderIncomeGrid()
    {
        _incomeGrid.Rows.Clear();
        foreach (var r in _incomeRows)
        {
            var i = _incomeGrid.Rows.Add(
                r.Date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                r.TypeLabel,
                r.Category,
                r.Description,
                $"{r.Amount:N2} ج.م",
                r.Reference ?? "—",
                r.RecordedBy ?? "—"
            );
            _incomeGrid.Rows[i].Tag = r;
        }

        var total = _incomeRows.Sum(x => x.Amount);
        _incomeSummaryLabel.Text = $"عدد حركات المقبوضات: {_incomeRows.Count}  |  إجمالي المقبوضات: {total:N2} ج.م";
        UpdateIncomeActions();
    }

    private void RenderExpenseGrid()
    {
        _expenseGrid.Rows.Clear();
        foreach (var r in _expenseRows)
        {
            var i = _expenseGrid.Rows.Add(
                r.Date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                r.TypeLabel,
                r.Category,
                r.Description,
                $"{r.Amount:N2} ج.م",
                r.Reference ?? "—",
                r.RecordedBy ?? "—"
            );
            _expenseGrid.Rows[i].Tag = r;
        }

        var total = _expenseRows.Sum(x => x.Amount);
        _expenseSummaryLabel.Text = $"عدد حركات المصروفات: {_expenseRows.Count}  |  إجمالي المصروفات: {total:N2} ج.م";
        UpdateExpenseActions();
    }

    private FinanceTransactionDto? SelectedIncome() =>
        _incomeGrid.SelectedRows.Count == 0 ? null : _incomeGrid.SelectedRows[0].Tag as FinanceTransactionDto;

    private FinanceTransactionDto? SelectedExpense() =>
        _expenseGrid.SelectedRows.Count == 0 ? null : _expenseGrid.SelectedRows[0].Tag as FinanceTransactionDto;

    private void UpdateIncomeActions()
    {
        var row = SelectedIncome();
        var canManage = _session?.HasPermission("Finance.ManageIncome") == true;
        _editIncomeBtn.Enabled = canManage && row is { IsEditable: true };
        _deleteIncomeBtn.Enabled = canManage && row is { IsEditable: true };
    }

    private void UpdateExpenseActions()
    {
        var row = SelectedExpense();
        var canManage = _session?.HasPermission("Finance.ManageExpenses") == true || _session?.HasPermission("Expenses.Manage") == true;
        _editExpenseBtn.Enabled = canManage && row is { IsEditable: true };
        _deleteExpenseBtn.Enabled = canManage && row is { IsEditable: true };
    }
}
