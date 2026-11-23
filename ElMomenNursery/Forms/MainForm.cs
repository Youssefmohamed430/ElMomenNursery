using System.Drawing;
using System.Text.Json;
using Serilog;
using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Controls;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Domain.Enums;
using ElMomenNursery.Styling;
using Microsoft.Extensions.DependencyInjection;

namespace ElMomenNursery.Forms;

public sealed class MainForm : Form
{
    private static readonly ILogger _log = Log.ForContext<MainForm>();
    private const int SidebarWidth = 258;
    private Image? _logoImage;
    private readonly Panel _pageHost = new() { Dock = DockStyle.Fill, AutoScroll = true, BackColor = BrandPalette.AppBackground };
    private readonly Dictionary<string, NavigationButton> _navigationButtons = new(StringComparer.Ordinal);

    private readonly UserSession _session;
    private readonly Func<Task> _logout;
    private readonly IExcelExportService _excelExportService;
    private readonly IServiceScopeFactory _scopeFactory;

    public MainForm(UserSession session, Func<Task> logout, IExcelExportService excelExportService, IServiceScopeFactory scopeFactory)
    {
        _session = session;
        _logout = logout;
        _excelExportService = excelExportService;
        _scopeFactory = scopeFactory;
        Text = "نظام إدارة حضانة المؤمن";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1024, 700);
        Size = new Size(1440, 900);
        BackColor = BrandPalette.AppBackground;
        ForeColor = BrandPalette.Text;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        RightToLeft = RightToLeft.Yes;
        // RTL text and child controls are enabled, while root positions are set explicitly below.
        RightToLeftLayout = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        DoubleBuffered = true;

        var mainArea = new Panel { BackColor = BrandPalette.AppBackground, RightToLeft = RightToLeft.Yes };
        mainArea.Controls.Add(_pageHost);
        mainArea.Controls.Add(BuildHeader());

        var sidebar = BuildSidebar();
        Controls.Add(mainArea);
        Controls.Add(sidebar);
        ApplyRootLayout(mainArea, sidebar);
        SizeChanged += (_, _) => ApplyRootLayout(mainArea, sidebar);

        Navigate("الرئيسية");
    }

    private void ApplyRootLayout(Control mainArea, Control sidebar)
    {
        var sidebarWidth = Math.Min(SidebarWidth, ClientSize.Width / 3);
        var mainWidth = ClientSize.Width - sidebarWidth;

        mainArea.SetBounds(0, 0, mainWidth, ClientSize.Height);
        sidebar.SetBounds(mainWidth, 0, sidebarWidth, ClientSize.Height);
    }

    private Control BuildHeader()
    {
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 94,
            BackColor = BrandPalette.Surface,
            Padding = new Padding(22, 10, 24, 10),
            RightToLeft = RightToLeft.Yes
        };

        var brandFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            RightToLeft = RightToLeft.No,
            BackColor = Color.Transparent
        };

        var logo = new PictureBox
        {
            Width = 70,
            Height = 70,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 12, 0),
            AccessibleName = "شعار حضانة المؤمن"
        };

        var logoPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Branding", "AlMoumenNurseryLogo.jpeg");
        if (File.Exists(logoPath))
        {
            try
            {
                using var sourceImage = Image.FromFile(logoPath);
                _logoImage = new Bitmap(sourceImage);
                logo.Image = _logoImage;
            }
            catch (Exception exception)
            {
                _log.Warning(exception, "Unable to load nursery logo image");
            }
        }
        else
        {
            _log.Warning("Nursery logo image was not found");
        }

        var brandText = new Panel { Width = 330, Height = 68, Margin = new Padding(0, 1, 0, 0) };
        var brandName = new Label
        {
            Text = "نظام إدارة حضانة المؤمن",
            Dock = DockStyle.Top,
            Height = 36,
            Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = BrandPalette.PrimaryGreen,
            TextAlign = ContentAlignment.MiddleLeft,
            RightToLeft = RightToLeft.Yes
        };
        var brandSubtitle = new Label
        {
            Text = "إدارة يومية واضحة وسهلة",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = BrandPalette.MutedText,
            TextAlign = ContentAlignment.MiddleLeft,
            RightToLeft = RightToLeft.Yes
        };

        brandText.Controls.Add(brandSubtitle);
        brandText.Controls.Add(brandName);
        brandFlow.Controls.Add(logo);
        brandFlow.Controls.Add(brandText);
        header.Controls.Add(brandFlow);

        var account = new FlowLayoutPanel { Dock = DockStyle.Left, Width = 320, FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false, BackColor = Color.Transparent, Padding = new Padding(0, 15, 0, 0) };
        var who = new Label { AutoSize = false, Width = 175, Height = 54, Text = $"{_session.FullName}\n{string.Join("، ", _session.Roles)}",
            TextAlign = ContentAlignment.MiddleRight, ForeColor = BrandPalette.Text, Font = new Font("Segoe UI", 9F, FontStyle.Bold), RightToLeft = RightToLeft.Yes };
        var logout = CreateActionButton("تسجيل الخروج");
        logout.Width = 120; logout.Height = 40;
        logout.Click += (_, _) => RunSafeEvent(async () =>
        {
            logout.Enabled = false;
            try
            {
                await _logout();
                Close();
            }
            catch (Exception exception)
            {
                _log.Error(exception, "Logout failed; userId={UserId}", _session.UserId);
                if (!logout.IsDisposed) logout.Enabled = true;
                MessageBox.Show(this, "تعذر تسجيل الخروج. أعد المحاولة.", "تسجيل الخروج", MessageBoxButtons.OK,
                    MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
            }
        }, "Logout");
        account.Controls.Add(who); account.Controls.Add(logout);
        header.Controls.Add(account);

        var divider = new Panel { Dock = DockStyle.Bottom, Height = 3, BackColor = BrandPalette.LogoGreen };
        header.Controls.Add(divider);
        return header;
    }

    private Control BuildSidebar()
    {
        var sidebar = new Panel
        {
            Width = SidebarWidth,
            BackColor = BrandPalette.Surface,
            Padding = new Padding(14, 18, 14, 10),
            RightToLeft = RightToLeft.Yes
        };

        var menuTitle = new Label
        {
            Text = "القائمة الرئيسية",
            Dock = DockStyle.Top,
            Height = 48,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = BrandPalette.MutedText,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point),
            RightToLeft = RightToLeft.Yes
        };

        var menu = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            RightToLeft = RightToLeft.Yes,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 8, 0, 0)
        };

        // Admin-only settings are introduced with authentication and permission enforcement.
        var sections = new[]
        {
            "الرئيسية",
            "الأطفال",
            "الموظفون",
            "الرواتب",
            "الفصول",
            "الحضور",
            "الاشتراكات والمدفوعات",
            "المخزن",
            "الماليات",
            "إدارة المستخدمين"
        };

        foreach (var section in sections)
        {
            if (!CanViewSection(section)) continue;
            var button = new NavigationButton(section) { Width = SidebarWidth - 30 };
            button.Click += (_, _) => Navigate(section);
            _navigationButtons.Add(section, button);
            menu.Controls.Add(button);
        }

        var footer = new Label
        {
            Text = "حضانة المؤمن الصغيرة",
            Dock = DockStyle.Bottom,
            Height = 42,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = BrandPalette.MutedText,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
            RightToLeft = RightToLeft.Yes
        };

        sidebar.Controls.Add(menu);
        sidebar.Controls.Add(footer);
        sidebar.Controls.Add(menuTitle);
        return sidebar;
    }

    private void Navigate(string section)
    {
        if (!CanViewSection(section))
        {
            _log.Warning("Section access denied; section={Section}; userId={UserId}", section, _session.UserId);
            MessageBox.Show("ليست لديك صلاحية لفتح هذا القسم.", "صلاحية غير متاحة", MessageBoxButtons.OK, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
            return;
        }
        _log.Information("Section opened; section={Section}; userId={UserId}", section, _session.UserId);
        foreach (var (name, button) in _navigationButtons)
        {
            button.SetSelected(string.Equals(name, section, StringComparison.Ordinal));
        }

        _pageHost.SuspendLayout();
        while (_pageHost.Controls.Count > 0)
        {
            var oldPage = _pageHost.Controls[0];
            _pageHost.Controls.RemoveAt(0);
            oldPage.Dispose();
        }

        try
        {
            var page = section switch
            {
                "الرئيسية" => BuildHomePage(),
                "الأطفال" => new ChildrenPage(_scopeFactory, _session, _excelExportService),
                "الموظفون" => new EmployeesPage(_scopeFactory, _session, _excelExportService),
                "الرواتب" => new PayrollPage(_scopeFactory),
                "الحضور" => new AttendancePage(_scopeFactory, _session, _excelExportService),
                "الاشتراكات والمدفوعات" => new SubscriptionsPage(_scopeFactory, _session, _excelExportService),
                "المخزن" => new InventoryPage(_scopeFactory, _session, _excelExportService),
                "الماليات" => new FinancePage(_scopeFactory, _session, _excelExportService),
                "الفصول" => new ClassesPage(_scopeFactory, _session, _excelExportService),
                "إدارة المستخدمين" => new UserManagementForm(_scopeFactory, _session, _excelExportService),
                _ => BuildSectionPage(section)
            };
            _pageHost.Controls.Add(page);
            _pageHost.ResumeLayout(performLayout: true);
        }
        catch (Exception exception)
        {
            _log.Error(exception, "Failed to build page; section={Section}; userId={UserId}", section, _session.UserId);
            _pageHost.ResumeLayout(performLayout: true);
            MessageBox.Show(this, "تعذر فتح هذا القسم. أعد المحاولة.", "تعذر فتح القسم", MessageBoxButtons.OK,
                MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
        }
    }

    private bool CanViewSection(string section) => section switch
    {
        "الرئيسية" => true,
        "الأطفال" => _session.HasPermission("Students.View"),
        "الفصول" => _session.HasPermission("Classes.View"),
        "الحضور" => _session.HasPermission("Attendance.View") || _session.HasPermission("EmployeeAttendance.View"),
        "الموظفون" => _session.HasPermission("Employees.View"),
        "الرواتب" => _session.HasPermission("Payroll.Manage"),
        "الاشتراكات والمدفوعات" => _session.HasPermission("Subscriptions.View") || _session.HasPermission("Payments.View"),
        "المخزن" => _session.HasPermission("Inventory.View"),
        "الماليات" => _session.HasPermission("Finance.View") || _session.HasPermission("Payments.View") || _session.HasPermission("Expenses.View"),
        "إدارة المستخدمين" => _session.HasPermission("Users.View"),
        _ => true
    };

    private Control BuildHomePage()
    {
        var page = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BrandPalette.AppBackground,
            Padding = new Padding(34, 28, 34, 28),
            RightToLeft = RightToLeft.Yes
        };

        var title = CreateHeading("أهلًا بك في نظام حضانة المؤمن", 22F);
        var subtitle = new Label
        {
            Text = "اختر القسم الذي تريد العمل فيه.",
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = BrandPalette.MutedText,
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
            RightToLeft = RightToLeft.Yes
        };
        var quickActionsTitle = CreateHeading("اختصارات سريعة", 13F);

        var shortcuts = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = true,
            AutoScroll = true,
            RightToLeft = RightToLeft.No,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 0, 0, 4)
        };
        shortcuts.Controls.Add(CreateShortcut("الأطفال", "فتح قسم الأطفال", "الأطفال"));
        shortcuts.Controls.Add(CreateShortcut("الحضور", "فتح قسم الحضور", "الحضور"));
        shortcuts.Controls.Add(CreateShortcut("الاشتراكات والمدفوعات", "متابعة الاشتراكات والمدفوعات", "الاشتراكات والمدفوعات"));
        shortcuts.Controls.Add(CreateShortcut("الماليات", "متابعة المقبوضات والمصروفات والأرصدة", "الماليات"));
        shortcuts.Controls.Add(CreateShortcut("الفصول", "عرض الفصول وإدارتها", "الفصول"));
        shortcuts.Controls.Add(CreateShortcut("المخزن", "فتح قسم المخزن", "المخزن"));
        shortcuts.Controls.Add(CreateShortcut("الموظفون", "فتح قسم الموظفين", "الموظفون"));

        var guidance = new FeedbackBanner(
            "طريقة الاستخدام",
            "يمكنك الوصول إلى الأقسام من القائمة الجانبية أو من الاختصارات هنا.",
            MessageTone.Information);

        page.Controls.Add(title);
        page.Controls.Add(subtitle);
        page.Controls.Add(quickActionsTitle);
        page.Controls.Add(shortcuts);
        page.Controls.Add(guidance);

        void LayoutHomePage()
        {
            var contentWidth = Math.Max(300, page.ClientSize.Width - 68);
            title.SetBounds(34, 28, contentWidth, 46);
            subtitle.SetBounds(34, 78, contentWidth, 30);
            quickActionsTitle.SetBounds(34, 126, contentWidth, 34);
            shortcuts.SetBounds(26, 166, Math.Max(300, page.ClientSize.Width - 52), 300);
            guidance.SetBounds(34, 480, contentWidth, 76);
        }

        page.Resize += (_, _) => LayoutHomePage();
        LayoutHomePage();
        return page;
    }

    private Control BuildSectionPage(string section)
    {
        var page = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BrandPalette.AppBackground,
            Padding = new Padding(34, 28, 34, 28),
            RightToLeft = RightToLeft.Yes
        };

        var title = CreateHeading(section, 21F);
        var subtitle = new Label
        {
            Text = "يمكنك الرجوع إلى القائمة الرئيسية لاختيار قسم آخر.",
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = BrandPalette.MutedText,
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
            RightToLeft = RightToLeft.Yes
        };
        var contentArea = new Panel { BackColor = Color.Transparent };
        var emptyState = new EmptyStatePanel("لا توجد بيانات للعرض الآن", "ستظهر معلومات هذا القسم هنا.")
        {
            Size = new Size(620, 170)
        };

        void CenterEmptyState()
        {
            emptyState.Left = Math.Max(0, (contentArea.ClientSize.Width - emptyState.Width) / 2);
            emptyState.Top = Math.Max(0, (contentArea.ClientSize.Height - emptyState.Height) / 2);
        }

        contentArea.Resize += (_, _) => CenterEmptyState();
        contentArea.Controls.Add(emptyState);
        page.Controls.Add(title);
        page.Controls.Add(subtitle);
        page.Controls.Add(contentArea);

        void LayoutSectionPage()
        {
            var contentWidth = Math.Max(300, page.ClientSize.Width - 68);
            title.SetBounds(34, 28, contentWidth, 46);
            subtitle.SetBounds(34, 78, contentWidth, 30);
            contentArea.SetBounds(34, 122, contentWidth, Math.Max(250, page.ClientSize.Height - 150));
            CenterEmptyState();
        }

        page.Resize += (_, _) => LayoutSectionPage();
        LayoutSectionPage();
        return page;
    }

    internal static Panel CreateDataPage(string titleText, string subtitleText, bool includePrototypeBanner = true)
    {
        var page = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BrandPalette.AppBackground,
            Padding = new Padding(34, 28, 34, 28),
            RightToLeft = RightToLeft.Yes
        };
        page.Controls.Add(CreateHeading(titleText, 21F));
        page.Controls.Add(new Label
        {
            Text = subtitleText,
            ForeColor = BrandPalette.MutedText,
            Font = new Font("Segoe UI", 10F),
            TextAlign = ContentAlignment.MiddleLeft,
            RightToLeft = RightToLeft.Yes
        });
        if (includePrototypeBanner)
        {
            page.Controls.Add(new FeedbackBanner("نسخة تجريبية", "الأزرار للعرض فقط، ولا يتم حفظ أو تعديل أي بيانات حاليًا.", MessageTone.Information));
        }
        page.Resize += (_, _) => LayoutDataHeader(page);
        return page;
    }

    internal static void LayoutDataHeader(Panel page)
    {
        if (page.Controls.Count < 2) return;
        page.Controls[0].SetBounds(34, 28, page.ClientSize.Width - 68, 46);
        page.Controls[1].SetBounds(34, 78, page.ClientSize.Width - 68, 30);
        if (page.Controls.Count > 2 && page.Controls[2] is FeedbackBanner)
        {
            page.Controls[2].SetBounds(34, 116, page.ClientSize.Width - 68, 62);
        }
    }

    internal static TextBox CreateSearchBox(string placeholder) => new()
    {
        PlaceholderText = placeholder,
        BorderStyle = BorderStyle.FixedSingle,
        Font = new Font("Segoe UI", 10F),
        RightToLeft = RightToLeft.Yes
    };

    internal static Button CreateActionButton(string text, bool primary = false) =>
        Styling.BrandControls.ActionButton(text, primary);

    internal static DataGridView CreateGrid(string[] columns)
    {
        var grid = new DataGridView
        {
            BackgroundColor = BrandPalette.Surface,
            BorderStyle = BorderStyle.FixedSingle,
            GridColor = BrandPalette.Border,
            RightToLeft = RightToLeft.Yes,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            EnableHeadersVisualStyles = false,
            Font = new Font("Segoe UI", 10F),
            ColumnHeadersHeight = 46,
            RowTemplate = { Height = 42 }
        };
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = BrandPalette.SoftGreen,
            ForeColor = BrandPalette.Text,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleRight
        };
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = BrandPalette.Surface,
            ForeColor = BrandPalette.Text,
            SelectionBackColor = BrandPalette.SoftGreen,
            SelectionForeColor = BrandPalette.Text,
            Alignment = DataGridViewContentAlignment.MiddleRight,
            Padding = new Padding(10, 0, 10, 0)
        };
        foreach (var column in columns)
        {
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = column, SortMode = DataGridViewColumnSortMode.Automatic });
        }
        return grid;
    }

    private static void FilterRows(DataGridView grid, string query)
    {
        foreach (DataGridViewRow row in grid.Rows)
        {
            row.Visible = string.IsNullOrWhiteSpace(query)
                || row.Cells.Cast<DataGridViewCell>().Any(cell => cell.Value?.ToString()?.Contains(query, StringComparison.CurrentCultureIgnoreCase) == true);
        }
    }

    private void ShowComingSoon(string action)
    {
        MessageBox.Show($"زر «{action}» للعرض فقط في هذه المرحلة، وسيتم تفعيله لاحقًا.", "قريبًا", MessageBoxButtons.OK, MessageBoxIcon.Information,
            MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
    }

    private Button CreateShortcut(string title, string description, string section)
    {
        var button = new Button
        {
            Text = $"{title}\n{description}",
            Width = 276,
            Height = 112,
            Margin = new Padding(8),
            Padding = new Padding(16, 10, 16, 10),
            TextAlign = ContentAlignment.MiddleLeft,
            RightToLeft = RightToLeft.Yes,
            BackColor = BrandPalette.Surface,
            ForeColor = BrandPalette.Text,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            AccessibleName = title,
            AccessibleDescription = description,
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderColor = BrandPalette.Border;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = BrandPalette.SoftGreen;
        button.Click += (_, _) => Navigate(section);
        return button;
    }

    private static async Task RunSafeAsync(Func<Task> work, string operation)
    {
        try
        {
            await work();
        }
        catch (Exception exception)
        {
            _log.Error(exception, "Unhandled page load failure; operation={Operation}", operation);
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
            _log.Error(exception, "Unhandled main form event failure; operation={Operation}; userId={UserId}", operation, _session.UserId);
            if (!IsDisposed)
                MessageBox.Show(this, "تعذر إكمال العملية. أعد المحاولة.", "تعذر إكمال العملية", MessageBoxButtons.OK,
                    MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _logoImage?.Dispose();
        }
        base.Dispose(disposing);
    }

    private static Label CreateHeading(string text, float size)
    {
        return new Label
        {
            Text = text,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = BrandPalette.Text,
            Font = new Font("Segoe UI", size, FontStyle.Bold, GraphicsUnit.Point),
            RightToLeft = RightToLeft.Yes
        };
    }
}
