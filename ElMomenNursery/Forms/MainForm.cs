using System.Drawing;
using ElMomenNursery.Controls;
using ElMomenNursery.Styling;

namespace ElMomenNursery.Forms;

public sealed class MainForm : Form
{
    private const int SidebarWidth = 258;
    private Image? _logoImage;
    private readonly Panel _pageHost = new() { Dock = DockStyle.Fill, AutoScroll = true, BackColor = BrandPalette.AppBackground };
    private readonly Dictionary<string, NavigationButton> _navigationButtons = new(StringComparer.Ordinal);

    public MainForm()
    {
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
            "المخزن"
        };

        foreach (var section in sections)
        {
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
            _ => BuildSectionPage(section)
        };
        _pageHost.Controls.Add(page);
        _pageHost.ResumeLayout(performLayout: true);
    }

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
        var page = CreateDataPage("الأطفال", "ابحث عن طفل أو استعرض القائمة. بيانات الجدول للعرض التجريبي فقط.");
        var search = CreateSearchBox("بحث باسم الطفل");
        var classFilter = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10F),
            RightToLeft = RightToLeft.Yes
        };
        classFilter.Items.AddRange(["كل الفصول", "فصل الفراشات", "فصل الزهور"]);
        classFilter.SelectedIndex = 0;
        var addButton = CreateActionButton("إضافة طفل", primary: true);
        var editButton = CreateActionButton("تعديل البيانات");
        var detailsButton = CreateActionButton("عرض الملف");
        var exportButton = CreateActionButton("تصدير القائمة");
        var grid = CreateGrid(["اسم الطفل", "الفصل", "تاريخ الالتحاق", "الحالة"]);
        grid.Rows.Add("آدم محمد", "فصل الفراشات", "١٢/٠٩/٢٠٢٦", "نشط");
        grid.Rows.Add("ليلى أحمد", "فصل الزهور", "١٥/٠٩/٢٠٢٦", "نشط");
        grid.Rows.Add("يوسف محمود", "فصل الفراشات", "٠١/١٠/٢٠٢٦", "نشط");
        search.TextChanged += (_, _) => FilterRows(grid, search.Text);
        addButton.Click += (_, _) => ShowComingSoon("إضافة طفل");
        editButton.Click += (_, _) => ShowComingSoon("تعديل بيانات الطفل");
        detailsButton.Click += (_, _) => ShowComingSoon("ملف الطفل");
        exportButton.Click += (_, _) => ShowComingSoon("تصدير القائمة");

        page.Controls.AddRange([search, classFilter, addButton, editButton, detailsButton, exportButton, grid]);
        page.Resize += (_, _) => LayoutChildrenPage();
        LayoutChildrenPage();
        return page;

        void LayoutChildrenPage()
        {
            LayoutDataHeader(page);
            var y = 190;
            search.SetBounds(34, y, Math.Max(220, page.ClientSize.Width - 68 - 730), 42);
            classFilter.SetBounds(search.Right + 10, y, 170, 42);
            addButton.SetBounds(page.ClientSize.Width - 34 - 150, y, 150, 42);
            editButton.SetBounds(addButton.Left - 150 - 8, y, 150, 42);
            detailsButton.SetBounds(editButton.Left - 142 - 8, y, 142, 42);
            exportButton.SetBounds(detailsButton.Left - 142 - 8, y, 142, 42);
            grid.SetBounds(34, 248, page.ClientSize.Width - 68, Math.Max(220, page.ClientSize.Height - 282));
        }
    }

    private Control BuildClassesPage()
    {
        var page = CreateDataPage("الفصول", "استعرض الفصول والقائمة الحالية. البيانات المعروضة تجريبية.");
        var search = CreateSearchBox("بحث باسم الفصل");
        var addButton = CreateActionButton("إضافة فصل", primary: true);
        var editButton = CreateActionButton("تعديل الفصل");
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

    private static Panel CreateDataPage(string titleText, string subtitleText)
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
        page.Controls.Add(new FeedbackBanner("نسخة تجريبية", "الأزرار للعرض فقط، ولا يتم حفظ أو تعديل أي بيانات حاليًا.", MessageTone.Information));
        page.Resize += (_, _) => LayoutDataHeader(page);
        return page;
    }

    private static void LayoutDataHeader(Panel page)
    {
        if (page.Controls.Count < 3) return;
        page.Controls[0].SetBounds(34, 28, page.ClientSize.Width - 68, 46);
        page.Controls[1].SetBounds(34, 78, page.ClientSize.Width - 68, 30);
        page.Controls[2].SetBounds(34, 116, page.ClientSize.Width - 68, 62);
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



