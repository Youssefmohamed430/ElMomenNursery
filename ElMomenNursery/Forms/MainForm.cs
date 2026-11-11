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

        _pageHost.Controls.Add(section == "الرئيسية" ? BuildHomePage() : BuildSectionPage(section));
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



