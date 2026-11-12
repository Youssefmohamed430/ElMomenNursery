using System.Drawing;
using ElMomenNursery.Styling;

namespace ElMomenNursery.Forms;

public sealed class LoginForm : Form
{
    public LoginForm()
    {
        Text = "تسجيل الدخول - حضانة المؤمن";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(520, 600);
        Size = new Size(560, 650);
        BackColor = BrandPalette.AppBackground;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;

        var card = new Panel
        {
            BackColor = BrandPalette.Surface,
            Size = new Size(400, 440),
            Padding = new Padding(32),
            Anchor = AnchorStyles.None
        };

        var logo = new PictureBox
        {
            Size = new Size(92, 92),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };
        var logoPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Branding", "AlMoumenNurseryLogo.jpeg");
        if (File.Exists(logoPath))
        {
            using var image = Image.FromFile(logoPath);
            logo.Image = new Bitmap(image);
        }

        var heading = new Label
        {
            Text = "تسجيل الدخول",
            Font = new Font("Segoe UI", 21F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = BrandPalette.Text,
            TextAlign = ContentAlignment.MiddleCenter
        };
        var subtitle = new Label
        {
            Text = "مرحبًا بك في نظام إدارة حضانة المؤمن",
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = BrandPalette.MutedText,
            TextAlign = ContentAlignment.MiddleCenter
        };
        var userLabel = CreateFieldLabel("اسم المستخدم");
        var userName = CreateTextBox("اكتب اسم المستخدم");
        var passwordLabel = CreateFieldLabel("كلمة المرور");
        var password = CreateTextBox("اكتب كلمة المرور");
        password.UseSystemPasswordChar = true;

        var signIn = new Button
        {
            Text = "دخول",
            Height = 48,
            BackColor = BrandPalette.PrimaryGreen,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        signIn.FlatAppearance.BorderSize = 0;
        signIn.FlatAppearance.MouseOverBackColor = BrandPalette.PrimaryGreenHover;
        signIn.Click += (_, _) => DialogResult = DialogResult.OK;

        var note = new Label
        {
            Text = "الدخول مؤقت — لا يتم التحقق من البيانات حاليًا",
            Height = 28,
            ForeColor = BrandPalette.MutedText,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point),
            TextAlign = ContentAlignment.MiddleCenter
        };

        card.Controls.AddRange([logo, heading, subtitle, userLabel, userName, passwordLabel, password, signIn, note]);
        Controls.Add(card);
        LayoutCard();
        Resize += (_, _) => LayoutCard();

        void LayoutCard()
        {
            card.Location = new Point((ClientSize.Width - card.Width) / 2, (ClientSize.Height - card.Height) / 2);
            var width = card.ClientSize.Width - 64;
            logo.SetBounds((card.ClientSize.Width - 92) / 2, 25, 92, 92);
            heading.SetBounds(32, 125, width, 40);
            subtitle.SetBounds(32, 168, width, 30);
            userLabel.SetBounds(32, 215, width, 24);
            userName.SetBounds(32, 241, width, 42);
            passwordLabel.SetBounds(32, 296, width, 24);
            password.SetBounds(32, 322, width, 42);
            signIn.SetBounds(32, 379, width, 48);
            note.SetBounds(32, 433, width, 28);
        }
    }

    private static Label CreateFieldLabel(string text) => new()
    {
        Text = text,
        ForeColor = BrandPalette.Text,
        Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point),
        TextAlign = ContentAlignment.MiddleLeft,
        RightToLeft = RightToLeft.Yes
    };

    private static TextBox CreateTextBox(string placeholder) => new()
    {
        PlaceholderText = placeholder,
        BorderStyle = BorderStyle.FixedSingle,
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
        RightToLeft = RightToLeft.Yes
    };

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var picture in Controls.OfType<Panel>().SelectMany(panel => panel.Controls.OfType<PictureBox>()))
                picture.Image?.Dispose();
        }
        base.Dispose(disposing);
    }
}
