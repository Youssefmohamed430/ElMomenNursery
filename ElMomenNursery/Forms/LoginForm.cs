using System.Drawing;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Styling;
using Serilog;

namespace ElMomenNursery.Forms;

public sealed class LoginForm : ResponsiveForm
{
    private static readonly ILogger _log = Log.ForContext<LoginForm>();
    private readonly IAuthService auth;
    private readonly TextBox _username = CreateTextBox("اسم المستخدم");
    private readonly TextBox _fullName = CreateTextBox("الاسم الكامل");
    private readonly TextBox _password = CreateTextBox("كلمة المرور");
    private readonly Label _nameLabel = LabelFor("الاسم الكامل");
    private readonly Label _note = new() { AutoSize = false, TextAlign = ContentAlignment.MiddleCenter };
    private readonly Button _submit = new() { Text = "دخول", Height = 48, BackColor = BrandPalette.PrimaryGreen,
        ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 11F, FontStyle.Bold), UseVisualStyleBackColor = false };
    private readonly Button _switchDatabase = new() { Height = 40, FlatStyle = FlatStyle.Flat, BackColor = Color.White,
        ForeColor = BrandPalette.PrimaryGreen, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), UseVisualStyleBackColor = false };
    private readonly bool _isMockupDatabase;
    private Action? _layoutCard;
    private bool _setupMode;

    public LoginForm(IAuthService auth, bool isMockupDatabase)
    {
        this.auth = auth;
        _isMockupDatabase = isMockupDatabase;
        _switchDatabase.Text = isMockupDatabase
            ? "القاعدة الحالية: التجريبية | التبديل للفعلية"
            : "القاعدة الحالية: الفعلية | التبديل للتجريبية";
        _switchDatabase.FlatAppearance.BorderColor = BrandPalette.Border;
        _switchDatabase.Click += SwitchDatabase;
        Text = "تسجيل الدخول - حضانة المؤمن";
        StartPosition = FormStartPosition.CenterScreen; MinimumSize = new Size(520, 670); Size = new Size(560, 700);
        BackColor = BrandPalette.AppBackground; Font = new Font("Segoe UI", 10F); RightToLeft = RightToLeft.Yes; RightToLeftLayout = true;
        var card = new Panel { BackColor = BrandPalette.Surface, Size = new Size(410, 550), Anchor = AnchorStyles.None };
        var heading = new Label { Text = "تسجيل الدخول", Font = new Font("Segoe UI", 21F, FontStyle.Bold), ForeColor = BrandPalette.Text, TextAlign = ContentAlignment.MiddleCenter };
        var subtitle = new Label { Text = "نظام إدارة حضانة المؤمن", ForeColor = BrandPalette.MutedText, TextAlign = ContentAlignment.MiddleCenter };
        var userLabel = LabelFor("اسم المستخدم"); var passLabel = LabelFor("كلمة المرور");
        _password.UseSystemPasswordChar = true;
        _submit.FlatAppearance.BorderSize = 0; _submit.Click += Submit;
        _note.Height = 48; _note.ForeColor = BrandPalette.MutedText; _note.Font = new Font("Segoe UI", 9F);
        card.Controls.AddRange([heading, subtitle, userLabel, _username, _nameLabel, _fullName, passLabel, _password, _submit, _note, _switchDatabase]);
        Controls.Add(card);
        Shown += (_, _) => _ = LoadInitialSetupAsync(heading, subtitle);
        void LayoutCard()
        {
            card.Location = new Point((ClientSize.Width - card.Width) / 2, (ClientSize.Height - card.Height) / 2);
            var w = card.ClientSize.Width - 64;
            heading.SetBounds(32, 36, w, 42); subtitle.SetBounds(32, 80, w, 32);
            userLabel.SetBounds(32, 132, w, 24); _username.SetBounds(32, 158, w, 40);
            _nameLabel.SetBounds(32, 206, w, 24); _fullName.SetBounds(32, 232, w, 40);
            passLabel.SetBounds(32, _setupMode ? 280 : 212, w, 24); _password.SetBounds(32, _setupMode ? 306 : 238, w, 40);
            _submit.SetBounds(32, _setupMode ? 365 : 300, w, 48); _note.SetBounds(32, _setupMode ? 417 : 355, w, 54);
            _switchDatabase.SetBounds(32, _setupMode ? 480 : 430, w, 40);
        }
        _layoutCard = LayoutCard;
        Resize += (_, _) => LayoutCard();
        Shown += (_, _) => LayoutCard();
    }

    private void SwitchDatabase(object? sender, EventArgs e)
    {
        var targetMode = _isMockupDatabase ? "Production" : "Mockup";
        var targetName = _isMockupDatabase ? "الفعلية" : "التجريبية";
        if (MessageBox.Show(this, $"سيتم إعادة تشغيل البرنامج على قاعدة البيانات {targetName}. هل تريد المتابعة؟",
                "تغيير قاعدة البيانات", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign) != DialogResult.Yes)
            return;

        var profilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AlMoumenNursery", "database-mode.txt");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(profilePath)!);
            File.WriteAllText(profilePath, targetMode, System.Text.Encoding.UTF8);
            _switchDatabase.Enabled = false;
            System.Windows.Forms.Application.Restart();
            Close();
        }
        catch (Exception exception)
        {
            _log.Error(exception, "Could not switch the selected local database profile");
            _note.Text = "تعذر تغيير قاعدة البيانات. تحقق من صلاحية الكتابة ثم أعد المحاولة.";
            _note.ForeColor = BrandPalette.DangerText;
        }
    }

    private async Task LoadInitialSetupAsync(Label heading, Label subtitle)
    {
        try
        {
            var result = await auth.NeedsInitialAdministratorSetupAsync();
            if (result.IsFailure) { _note.Text = result.Error.Description; return; }
            _setupMode = result.Value;
            if (IsDisposed || _note.IsDisposed || _submit.IsDisposed) return;
            Text = _setupMode ? "إعداد المسؤول الأول - حضانة المؤمن" : "تسجيل الدخول - حضانة المؤمن";
            heading.Text = _setupMode ? "إنشاء حساب المسؤول" : "تسجيل الدخول";
            subtitle.Text = _setupMode ? "اختر اسم مستخدم وكلمة مرور قوية للمسؤول الأول" : "مرحبًا بك في نظام إدارة حضانة المؤمن";
            _submit.Text = _setupMode ? "إنشاء الحساب" : "دخول";
            _fullName.Visible = _nameLabel.Visible = _setupMode;
            _note.Text = _setupMode ? "كلمة المرور 12 حرفًا على الأقل، وبها أحرف وأرقام." : "";
            _layoutCard?.Invoke();
        }
        catch (Exception exception)
        {
            _log.Error(exception, "Initial administrator setup check failed");
            if (!IsDisposed && !_note.IsDisposed)
            {
                _note.Text = "تعذر التحقق من إعداد المسؤول. أعد تشغيل التطبيق.";
                _note.ForeColor = BrandPalette.DangerText;
            }
        }
    }

    private async void Submit(object? sender, EventArgs e)
    {
        _submit.Enabled = false; _note.ForeColor = BrandPalette.MutedText;
        try
        {
            var result = _setupMode
                ? await auth.CreateInitialAdministratorAsync(_username.Text, _fullName.Text, _password.Text)
                : await auth.LoginAsync(_username.Text, _password.Text);
            if (IsDisposed) return;
            if (result.IsSuccess)
            {
                if (_setupMode)
                {
                    _note.Text = "تم إنشاء المسؤول. سجّل الدخول باستخدام بياناتك.";
                    _setupMode = false; _fullName.Visible = _nameLabel.Visible = false; _submit.Text = "دخول";
                    _layoutCard?.Invoke();
                    _password.Clear(); _password.Focus();
                    return;
                }
                DialogResult = DialogResult.OK; Close();
            }
            else
            {
                _log.Warning("Authentication operation was rejected; setupMode={SetupMode}; errorCode={ErrorCode}", _setupMode, result.Error.Code);
                _note.Text = _setupMode ? "تعذر إنشاء الحساب. تحقق من البيانات أو أعد فتح التطبيق." : "اسم المستخدم أو كلمة المرور غير صحيحة، أو الحساب غير متاح مؤقتًا.";
                _note.ForeColor = BrandPalette.DangerText;
            }
        }
        catch (Exception exception)
        {
            _log.Error(exception, "Login form operation failed; setupMode={SetupMode}", _setupMode);
            _note.Text = "تعذر إكمال العملية. أعد المحاولة."; _note.ForeColor = BrandPalette.DangerText;
        }
        finally { if (!IsDisposed && !_submit.IsDisposed) _submit.Enabled = true; }
    }

    private static Label LabelFor(string text) => new() { Text = text, ForeColor = BrandPalette.Text, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
    private static TextBox CreateTextBox(string placeholder) => new() { PlaceholderText = placeholder, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10F), RightToLeft = RightToLeft.Yes };
}
