using System.Drawing;
using System.Text.Json;
using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Controls;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Domain.Enums;
using ElMomenNursery.Styling;

namespace ElMomenNursery.Forms;

public sealed class MainForm : Form
{
    private const int SidebarWidth = 258;
    private Image? _logoImage;
    private readonly Panel _pageHost = new() { Dock = DockStyle.Fill, AutoScroll = true, BackColor = BrandPalette.AppBackground };
    private readonly Dictionary<string, NavigationButton> _navigationButtons = new(StringComparer.Ordinal);

    private readonly UserSession _session;
    private readonly Func<Task> _logout;
    private readonly IAccountManagementService _accountManagement;
    private readonly IChildService _childService;

    public MainForm(UserSession session, Func<Task> logout, IAccountManagementService accountManagement, IChildService childService)
    {
        _session = session;
        _logout = logout;
        _accountManagement = accountManagement;
        _childService = childService;
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
            using var sourceImage = Image.FromFile(logoPath);
            _logoImage = new Bitmap(sourceImage);
            logo.Image = _logoImage;
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
        logout.Click += async (_, _) => { logout.Enabled = false; await _logout(); Close(); };
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
            "الفصول",
            "الحضور",
            "الاشتراكات والمدفوعات",
            "المخزن",
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
            MessageBox.Show("ليست لديك صلاحية لفتح هذا القسم.", "صلاحية غير متاحة", MessageBoxButtons.OK, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
            return;
        }
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

        var page = section switch
        {
            "الرئيسية" => BuildHomePage(),
            "الأطفال" => BuildChildrenPage(),
            "الفصول" => BuildClassesPage(),
            "إدارة المستخدمين" => new UserManagementForm(_accountManagement, _session),
            _ => BuildSectionPage(section)
        };
        _pageHost.Controls.Add(page);
        _pageHost.ResumeLayout(performLayout: true);
    }

    private bool CanViewSection(string section) => section switch
    {
        "الرئيسية" => true,
        "الأطفال" => _session.HasPermission("Students.View"),
        "الفصول" => _session.HasPermission("Classes.View"),
        "الحضور" => _session.HasPermission("Attendance.View"),
        "الموظفون" => _session.HasPermission("Employees.View"),
        "الاشتراكات والمدفوعات" => _session.HasPermission("Subscriptions.View") || _session.HasPermission("Payments.View"),
        "المخزن" => _session.HasPermission("Inventory.View"),
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

    private Control BuildChildrenPage()
    {
        var page = CreateDataPage("الأطفال", "استعرض الأطفال وادمج أكثر من فلتر للوصول إلى المجموعة المطلوبة.", includePrototypeBanner: false);
        var addButton = CreateActionButton("إضافة طفل", primary: true);
        var editButton = CreateActionButton("تعديل البيانات");
        var detailsButton = CreateActionButton("عرض الملف");
        var exportButton = CreateActionButton("تصدير القائمة");
        addButton.Visible = _session.HasPermission("Students.Create");
        editButton.Visible = _session.HasPermission("Students.Edit");
        detailsButton.Visible = _session.HasPermission("Students.View");
        exportButton.Visible = _session.HasPermission("Students.View");
        addButton.Name = "actions.add";
        editButton.Name = "actions.edit";
        detailsButton.Name = "actions.details";
        exportButton.Name = "actions.export";
        addButton.Width = 150;
        editButton.Width = 140;
        detailsButton.Width = 142;
        exportButton.Width = 142;
        var actions = new Panel
        {
            BackColor = Color.Transparent,
            Padding = Padding.Empty
        };

        var filterPanel = new Panel
        {
            AutoScroll = true,
            RightToLeft = RightToLeft.Yes,
            BackColor = BrandPalette.Surface,
            Padding = new Padding(10, 6, 10, 6),
            BorderStyle = BorderStyle.FixedSingle
        };
        var draggableControls = new Dictionary<string, Control>(StringComparer.Ordinal);
        var filterItems = new List<Control>();
        var search = CreateSearchBox("اسم الطفل");
        var genderFilter = CreateFilterCombo(["الكل", "ولد", "بنت"], 116);
        var dateOfBirthFrom = CreateOptionalDatePicker();
        var dateOfBirthTo = CreateOptionalDatePicker();
        var enrollmentFrom = CreateOptionalDatePicker();
        var enrollmentTo = CreateOptionalDatePicker();
        var classroomFilter = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5F),
            RightToLeft = RightToLeft.Yes,
            Width = 150,
            Height = 34
        };
        classroomFilter.Items.Add("كل الفصول");
        classroomFilter.SelectedIndex = 0;
        var paymentFilter = CreateFilterCombo(["كل الحالات", "دفعوا", "مدفعوش"], 142);
        var resultCount = new Label
        {
            AutoSize = false,
            Width = 280,
            Height = 28,
            ForeColor = BrandPalette.MutedText,
            TextAlign = ContentAlignment.MiddleRight,
            RightToLeft = RightToLeft.Yes
        };

        AddFilterField("filter.search", "اسم الطفل", search, 180);
        AddFilterField("filter.gender", "النوع", genderFilter, 128);
        AddFilterField("filter.birth-from", "الميلاد من", dateOfBirthFrom, 142);
        AddFilterField("filter.birth-to", "الميلاد إلى", dateOfBirthTo, 142);
        AddFilterField("filter.enrollment-from", "الالتحاق من", enrollmentFrom, 142);
        AddFilterField("filter.enrollment-to", "الالتحاق إلى", enrollmentTo, 142);
        AddFilterField("filter.classroom", "الفصل الحالي", classroomFilter, 158);
        AddFilterField("filter.payment", "الاشتراك الساري", paymentFilter, 150);

        var applyButton = CreateActionButton("تطبيق", primary: true);
        applyButton.Name = "filter.apply";
        applyButton.Width = 96;
        var clearButton = CreateActionButton("مسح الفلاتر");
        clearButton.Name = "filter.clear";
        clearButton.Width = 112;
        AddFilterButton(applyButton);
        AddFilterButton(clearButton);

        var customizeButton = CreateActionButton("تخصيص الشاشة");
        customizeButton.Name = "layout.customize";
        customizeButton.Width = 150;
        var resetLayoutButton = CreateActionButton("استعادة الافتراضي");
        resetLayoutButton.Name = "layout.reset";
        resetLayoutButton.Width = 150;
        resetLayoutButton.Visible = false;
        actions.Controls.AddRange([addButton, editButton, detailsButton, exportButton, customizeButton, resetLayoutButton]);

        var grid = CreateGrid([
            "المعرف", "اسم الطفل", "العنوان", "تاريخ الالتحاق", "تاريخ الميلاد", "النوع",
            "اسم الأب", "مهنة الأب", "هاتف الأب", "واتساب الأب", "اسم الأم", "مهنة الأم",
            "هاتف الأم", "واتساب الأم", "ملاحظات", "حالة طبية", "سبق له الالتحاق",
            "تاريخ الإضافة", "آخر تعديل"
        ]);
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        grid.Name = "children.grid";
        var columnWidths = new[] { 265, 170, 220, 140, 140, 90, 150, 140, 140, 140, 150, 140, 140, 140, 240, 120, 150, 160, 160 };
        for (var columnIndex = 0; columnIndex < grid.Columns.Count; columnIndex++)
            grid.Columns[columnIndex].Width = columnWidths[columnIndex];
        resultCount.Name = "children.count";

        RegisterDraggable(addButton);
        RegisterDraggable(editButton);
        RegisterDraggable(detailsButton);
        RegisterDraggable(exportButton);
        RegisterDraggable(grid);
        RegisterDraggable(resultCount);

        var isCustomizing = false;
        var savedLayout = LoadSavedLayout();
        var hasCustomLayout = savedLayout.Count > 0;
        AttachDraggableControls();

        addButton.Click += async (_, _) =>
        {
            if (isCustomizing) return;
            using var editor = new ChildEditorForm(_childService);
            if (editor.ShowDialog(this) == DialogResult.OK)
                await LoadChildrenAsync();
        };
        editButton.Click += async (_, _) =>
        {
            if (isCustomizing) return;
            if (grid.SelectedRows.Count == 0 || grid.SelectedRows[0].Tag is not Guid childId)
            {
                MessageBox.Show("اختر طفلًا من الجدول أولًا.", "تعديل بيانات طفل", MessageBoxButtons.OK,
                    MessageBoxIcon.Information, MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                return;
            }

            var child = await _childService.GetChildByIdAsync(childId);
            using var editor = new ChildEditorForm(_childService, child);
            if (editor.ShowDialog(this) == DialogResult.OK)
                await LoadChildrenAsync();
        };
        detailsButton.Click += (_, _) => { if (!isCustomizing) ShowComingSoon("ملف الطفل"); };
        exportButton.Click += (_, _) => { if (!isCustomizing) ShowComingSoon("تصدير القائمة"); };

        applyButton.Click += async (_, _) => { if (!isCustomizing) await LoadChildrenAsync(); };
        clearButton.Click += async (_, _) =>
        {
            if (isCustomizing) return;
            search.Clear();
            genderFilter.SelectedIndex = 0;
            dateOfBirthFrom.Checked = false;
            dateOfBirthTo.Checked = false;
            enrollmentFrom.Checked = false;
            enrollmentTo.Checked = false;
            classroomFilter.SelectedIndex = 0;
            paymentFilter.SelectedIndex = 0;
            await LoadChildrenAsync();
        };
        customizeButton.Click += async (_, _) =>
        {
            if (!isCustomizing)
            {
                isCustomizing = true;
                customizeButton.Text = "حفظ التخطيط";
                resetLayoutButton.Visible = true;
                resultCount.Text = "وضع التخصيص: اسحب العناصر ثم احفظ.";
                SetDragAppearance(true);
                return;
            }

            if (!SaveCurrentLayout()) return;
            savedLayout = ReadCurrentLayout();
            hasCustomLayout = true;
            isCustomizing = false;
            customizeButton.Text = "تخصيص الشاشة";
            resetLayoutButton.Visible = false;
            SetDragAppearance(false);
            await LoadChildrenAsync();
        };
        resetLayoutButton.Click += (_, _) =>
        {
            try
            {
                if (File.Exists(GetLayoutFilePath())) File.Delete(GetLayoutFilePath());
                savedLayout.Clear();
                hasCustomLayout = false;
                isCustomizing = false;
                customizeButton.Text = "تخصيص الشاشة";
                resetLayoutButton.Visible = false;
                SetDragAppearance(false);
                LayoutChildrenPage();
                _ = LoadChildrenAsync();
            }
            catch (Exception exception)
            {
                ShowLayoutError(exception.Message);
            }
        };

        page.Controls.AddRange([filterPanel, actions, resultCount, grid]);
        page.Resize += (_, _) => LayoutChildrenPage();
        LayoutChildrenPage();
        _ = LoadChildrenAsync();
        return page;

        void AddFilterField(string controlName, string labelText, Control control, int width)
        {
            var group = new Panel { Name = controlName, Width = width, Height = 56, BackColor = Color.Transparent, Margin = new Padding(3, 0, 3, 0) };
            var label = new Label
            {
                Text = labelText,
                Location = new Point(0, 0),
                Width = width,
                Height = 20,
                ForeColor = BrandPalette.Text,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
                RightToLeft = RightToLeft.Yes
            };
            control.Location = new Point(0, 21);
            control.Width = width;
            control.Height = 32;
            group.Controls.Add(control);
            group.Controls.Add(label);
            filterPanel.Controls.Add(group);
            filterItems.Add(group);
            RegisterDraggable(group);
        }

        void AddFilterButton(Button button)
        {
            button.Height = 40;
            button.Margin = new Padding(5, 18, 5, 0);
            filterPanel.Controls.Add(button);
            filterItems.Add(button);
            RegisterDraggable(button);
        }

        async Task LoadChildrenAsync()
        {
            applyButton.Enabled = false;
            clearButton.Enabled = false;
            resultCount.Text = "جارٍ تحميل الأطفال...";
            try
            {
                if (classroomFilter.Items.Count == 1)
                {
                    var classrooms = await _childService.GetClassroomsForFilterAsync();
                    foreach (var classroom in classrooms)
                        classroomFilter.Items.Add(classroom);
                }

                var gender = genderFilter.SelectedIndex switch
                {
                    1 => ChildGender.Boy,
                    2 => ChildGender.Girl,
                    _ => (ChildGender?)null
                };
                var classroomId = (classroomFilter.SelectedItem as ClassroomFilterOptionDto)?.Id;
                var hasPaidSubscription = paymentFilter.SelectedIndex switch
                {
                    1 => true,
                    2 => false,
                    _ => (bool?)null
                };
                var filter = new ChildFilter
                {
                    SearchText = search.Text,
                    Gender = gender,
                    DateOfBirthFrom = dateOfBirthFrom.Checked ? DateOnly.FromDateTime(dateOfBirthFrom.Value) : null,
                    DateOfBirthTo = dateOfBirthTo.Checked ? DateOnly.FromDateTime(dateOfBirthTo.Value) : null,
                    EnrollmentDateFrom = enrollmentFrom.Checked ? DateOnly.FromDateTime(enrollmentFrom.Value) : null,
                    EnrollmentDateTo = enrollmentTo.Checked ? DateOnly.FromDateTime(enrollmentTo.Value) : null,
                    ClassroomId = classroomId,
                    HasPaidSubscription = hasPaidSubscription
                };
                var children = await _childService.GetChildrenAsync(filter);
                grid.Rows.Clear();
                foreach (var child in children)
                {
                    var index = grid.Rows.Add(
                        child.Id,
                        child.FullName,
                        child.Address,
                        child.EnrollmentDate.ToString("dd/MM/yyyy"),
                        child.DateOfBirth.ToString("dd/MM/yyyy"),
                        child.Gender == ChildGender.Boy ? "ولد" : "بنت",
                        child.FatherName,
                        child.FatherOccupation ?? string.Empty,
                        child.FatherPhone,
                        child.FatherWhatsApp ?? string.Empty,
                        child.MotherName,
                        child.MotherOccupation ?? string.Empty,
                        child.MotherPhone,
                        child.MotherWhatsApp ?? string.Empty,
                        child.Notes ?? string.Empty,
                        child.HasMedicalConditions ? "نعم" : "لا",
                        child.PreviouslyAttendedNursery ? "نعم" : "لا",
                        child.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                        child.UpdatedAt?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? string.Empty);
                    grid.Rows[index].Tag = child.Id;
                }
                resultCount.Text = $"عدد الأطفال: {children.Count}";
            }
            catch (Exception exception)
            {
                resultCount.Text = "تعذر تحميل البيانات";
                MessageBox.Show($"تعذر تحميل قائمة الأطفال.\n{exception.Message}", "خطأ في تحميل البيانات",
                    MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
            }
            finally
            {
                applyButton.Enabled = true;
                clearButton.Enabled = true;
            }
        }

        void LayoutChildrenPage()
        {
            LayoutDataHeader(page);
            var leftMargin = 34;
            var rightMargin = 34;
            filterPanel.SetBounds(leftMargin, 120, Math.Max(300, page.ClientSize.Width - leftMargin - rightMargin), 72);
            actions.SetBounds(leftMargin, 204, Math.Max(300, page.ClientSize.Width - leftMargin - rightMargin), 44);
            resultCount.SetBounds(leftMargin, 252, 280, 26);
            var gridTop = 282;
            grid.SetBounds(leftMargin, gridTop, Math.Max(300, page.ClientSize.Width - leftMargin - rightMargin), Math.Max(220, page.ClientSize.Height - gridTop - 24));
            if (!isCustomizing)
            {
                ArrangeDefaultFilterItems();
                ArrangeDefaultActions();
                if (hasCustomLayout)
                    ApplySavedLayout();
            }
            ArrangePinnedActions();
        }

        void RegisterDraggable(Control control) => draggableControls[control.Name] = control;

        void ArrangeDefaultFilterItems()
        {
            var x = 10;
            foreach (var item in filterItems)
            {
                item.SetBounds(x, Math.Max(0, (filterPanel.ClientSize.Height - item.Height) / 2), item.Width, item.Height);
                x += item.Width + 6;
            }
            filterPanel.AutoScrollMinSize = new Size(x + 10, 0);
        }

        void ArrangeDefaultActions()
        {
            var right = actions.ClientSize.Width - 8;
            foreach (var button in new[] { addButton, editButton, detailsButton, exportButton })
            {
                right -= button.Width;
                button.SetBounds(right, 1, button.Width, 42);
                right -= 8;
            }
        }

        void ArrangePinnedActions()
        {
            customizeButton.SetBounds(8, 1, customizeButton.Width, 42);
            resetLayoutButton.SetBounds(166, 1, resetLayoutButton.Width, 42);
        }

        void ApplySavedLayout()
        {
            foreach (var (name, bounds) in savedLayout)
            {
                if (bounds.Length != 4 || !draggableControls.TryGetValue(name, out var control) || control.Parent is null)
                    continue;
                var parent = control.Parent;
                var width = Math.Clamp(bounds[2], 30, Math.Max(30, parent.ClientSize.Width));
                var height = Math.Clamp(bounds[3], 24, Math.Max(24, parent.ClientSize.Height));
                var x = Math.Clamp(bounds[0], 0, Math.Max(0, parent.ClientSize.Width - width));
                var y = Math.Clamp(bounds[1], 0, Math.Max(0, parent.ClientSize.Height - height));
                control.SetBounds(x, y, width, height);
            }
        }

        void AttachDraggableControls()
        {
            foreach (var root in draggableControls.Values)
                AttachDragHandler(root, root);
        }

        void AttachDragHandler(Control root, Control target)
        {
            var dragging = false;
            var offset = Point.Empty;
            target.MouseDown += (_, eventArgs) =>
            {
                if (!isCustomizing || eventArgs.Button != MouseButtons.Left || root.Parent is null) return;
                var point = root.Parent.PointToClient(target.PointToScreen(eventArgs.Location));
                offset = new Point(point.X - root.Left, point.Y - root.Top);
                dragging = true;
            };
            target.MouseMove += (_, eventArgs) =>
            {
                if (!dragging || !isCustomizing || root.Parent is null) return;
                var point = root.Parent.PointToClient(target.PointToScreen(eventArgs.Location));
                var minX = root.Parent == actions
                    ? Math.Min(resetLayoutButton.Visible ? resetLayoutButton.Right + 8 : customizeButton.Right + 8,
                        Math.Max(0, actions.ClientSize.Width - root.Width))
                    : 0;
                var minY = root == grid ? Math.Min(282, Math.Max(0, page.ClientSize.Height - root.Height)) : 0;
                var x = Math.Clamp(point.X - offset.X, minX, Math.Max(minX, root.Parent.ClientSize.Width - root.Width));
                var y = Math.Clamp(point.Y - offset.Y, minY, Math.Max(minY, root.Parent.ClientSize.Height - root.Height));
                root.Location = new Point(x, y);
            };
            target.MouseUp += (_, _) => dragging = false;
            foreach (Control child in target.Controls)
                AttachDragHandler(root, child);
        }

        void SetDragAppearance(bool enabled)
        {
            foreach (var root in draggableControls.Values)
                SetCursor(root, enabled ? Cursors.SizeAll : Cursors.Default);
        }

        static void SetCursor(Control control, Cursor cursor)
        {
            control.Cursor = cursor;
            foreach (Control child in control.Controls)
                SetCursor(child, cursor);
        }

        bool SaveCurrentLayout()
        {
            try
            {
                var directory = Path.GetDirectoryName(GetLayoutFilePath())!;
                Directory.CreateDirectory(directory);
                File.WriteAllText(GetLayoutFilePath(), JsonSerializer.Serialize(ReadCurrentLayout()));
                return true;
            }
            catch (Exception exception)
            {
                ShowLayoutError(exception.Message);
                return false;
            }
        }

        Dictionary<string, int[]> ReadCurrentLayout() => draggableControls.ToDictionary(
            pair => pair.Key,
            pair => new[] { pair.Value.Left, pair.Value.Top, pair.Value.Width, pair.Value.Height },
            StringComparer.Ordinal);

        Dictionary<string, int[]> LoadSavedLayout()
        {
            try
            {
                if (!File.Exists(GetLayoutFilePath())) return new Dictionary<string, int[]>(StringComparer.Ordinal);
                return JsonSerializer.Deserialize<Dictionary<string, int[]>>(File.ReadAllText(GetLayoutFilePath()))
                    ?? new Dictionary<string, int[]>(StringComparer.Ordinal);
            }
            catch
            {
                return new Dictionary<string, int[]>(StringComparer.Ordinal);
            }
        }

        static string GetLayoutFilePath() => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AlMoumenNursery", "child-screen-layout.json");

        void ShowLayoutError(string detail) => MessageBox.Show(this, $"تعذر حفظ تخطيط الشاشة.\n{detail}",
            "تخصيص شاشة الأطفال", MessageBoxButtons.OK, MessageBoxIcon.Error,
            MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

        static ComboBox CreateFilterCombo(string[] options, int width)
        {
            var combo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F),
                RightToLeft = RightToLeft.Yes,
                Width = width,
                Height = 34
            };
            combo.Items.AddRange(options);
            combo.SelectedIndex = 0;
            return combo;
        }

        static DateTimePicker CreateOptionalDatePicker() => new()
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            ShowCheckBox = true,
            Checked = false,
            Font = new Font("Segoe UI", 8.5F),
            Width = 142,
            Height = 30
        };
    }

    private Control BuildClassesPage()
    {
        var page = CreateDataPage("الفصول", "استعرض الفصول والقائمة الحالية. البيانات المعروضة تجريبية.");
        var search = CreateSearchBox("بحث باسم الفصل");
        var addButton = CreateActionButton("إضافة فصل", primary: true);
        var editButton = CreateActionButton("تعديل الفصل");
        addButton.Visible = editButton.Visible = _session.HasPermission("Classes.Manage");
        var rosterButton = CreateActionButton("عرض قائمة الأطفال");
        var printButton = CreateActionButton("طباعة القائمة");
        var grid = CreateGrid(["اسم الفصل", "المعلمة", "عدد الأطفال", "الحالة"]);
        grid.Rows.Add("فصل الفراشات", "—", "١٢", "متاح");
        grid.Rows.Add("فصل الزهور", "—", "١٠", "متاح");
        grid.Rows.Add("فصل النجوم", "—", "٨", "متاح");
        search.TextChanged += (_, _) => FilterRows(grid, search.Text);
        addButton.Click += (_, _) => ShowComingSoon("إضافة فصل");
        editButton.Click += (_, _) => ShowComingSoon("تعديل بيانات الفصل");
        rosterButton.Click += (_, _) => ShowComingSoon("قائمة الأطفال بالفصل");
        printButton.Click += (_, _) => ShowComingSoon("طباعة قائمة الفصل");

        page.Controls.AddRange([search, addButton, editButton, rosterButton, printButton, grid]);
        page.Resize += (_, _) => LayoutClassesPage();
        LayoutClassesPage();
        return page;

        void LayoutClassesPage()
        {
            LayoutDataHeader(page);
            var y = 190;
            search.SetBounds(34, y, Math.Max(230, page.ClientSize.Width - 68 - 650), 42);
            addButton.SetBounds(page.ClientSize.Width - 34 - 145, y, 145, 42);
            editButton.SetBounds(addButton.Left - 140 - 8, y, 140, 42);
            rosterButton.SetBounds(editButton.Left - 180 - 8, y, 180, 42);
            printButton.SetBounds(rosterButton.Left - 145 - 8, y, 145, 42);
            grid.SetBounds(34, 248, page.ClientSize.Width - 68, Math.Max(220, page.ClientSize.Height - 282));
        }
    }

    private static Panel CreateDataPage(string titleText, string subtitleText, bool includePrototypeBanner = true)
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

    private static void LayoutDataHeader(Panel page)
    {
        if (page.Controls.Count < 2) return;
        page.Controls[0].SetBounds(34, 28, page.ClientSize.Width - 68, 46);
        page.Controls[1].SetBounds(34, 78, page.ClientSize.Width - 68, 30);
        if (page.Controls.Count > 2 && page.Controls[2] is FeedbackBanner)
        {
            page.Controls[2].SetBounds(34, 116, page.ClientSize.Width - 68, 62);
        }
    }

    private static TextBox CreateSearchBox(string placeholder) => new()
    {
        PlaceholderText = placeholder,
        BorderStyle = BorderStyle.FixedSingle,
        Font = new Font("Segoe UI", 10F),
        RightToLeft = RightToLeft.Yes
    };

    private static Button CreateActionButton(string text, bool primary = false)
    {
        var button = new Button
        {
            Text = text,
            Height = 42,
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? BrandPalette.PrimaryGreen : BrandPalette.Surface,
            ForeColor = primary ? Color.White : BrandPalette.Text,
            Font = new Font("Segoe UI", 9.5F, primary ? FontStyle.Bold : FontStyle.Regular),
            Cursor = Cursors.Hand,
            RightToLeft = RightToLeft.Yes,
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderColor = primary ? BrandPalette.PrimaryGreen : BrandPalette.Border;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = primary ? BrandPalette.PrimaryGreenHover : BrandPalette.SoftGreen;
        return button;
    }

    private static DataGridView CreateGrid(string[] columns)
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



