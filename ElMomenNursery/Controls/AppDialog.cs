using ElMomenNursery.Styling;
using ElMomenNursery.Forms;

namespace ElMomenNursery.Controls;

public static class AppDialog
{
    public static void ShowInformation(IWin32Window owner, string title, string message)
    {
        using var dialog = new MessageDialog(title, message, isConfirmation: false);
        dialog.ShowDialog(owner);
    }

    public static bool Confirm(IWin32Window owner, string title, string message)
    {
        using var dialog = new MessageDialog(title, message, isConfirmation: true);
        return dialog.ShowDialog(owner) == DialogResult.OK;
    }

    private sealed class MessageDialog : ResponsiveForm
    {
        public MessageDialog(string title, string message, bool isConfirmation)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ShowIcon = false;
            ClientSize = new Size(500, isConfirmation ? 230 : 190);
            BackColor = BrandPalette.Surface;
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = false;
            AutoScaleMode = AutoScaleMode.Dpi;

            var heading = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 48,
                Padding = new Padding(20, 8, 20, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = BrandPalette.PrimaryGreen,
                RightToLeft = RightToLeft.Yes
            };

            var body = new Label
            {
                Text = message,
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 4, 20, 12),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = BrandPalette.Text,
                RightToLeft = RightToLeft.Yes
            };

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 58,
                Padding = new Padding(16, 8, 16, 8),
                FlowDirection = FlowDirection.RightToLeft,
                RightToLeft = RightToLeft.No,
                WrapContents = false
            };

            var accept = new Button
            {
                Text = isConfirmation ? "تأكيد" : "موافق",
                Width = 112,
                Height = 38,
                DialogResult = DialogResult.OK,
                RightToLeft = RightToLeft.Yes,
                BackColor = BrandPalette.PrimaryGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                AccessibleName = isConfirmation ? "تأكيد" : "موافق"
            };
            accept.FlatAppearance.BorderSize = 0;
            accept.FlatAppearance.MouseOverBackColor = BrandPalette.PrimaryGreenHover;

            buttons.Controls.Add(accept);
            AcceptButton = accept;

            if (isConfirmation)
            {
                var cancel = new Button
                {
                    Text = "إلغاء",
                    Width = 100,
                    Height = 38,
                    DialogResult = DialogResult.Cancel,
                    RightToLeft = RightToLeft.Yes,
                    BackColor = BrandPalette.Surface,
                    ForeColor = BrandPalette.Text,
                    FlatStyle = FlatStyle.Flat,
                    UseVisualStyleBackColor = false,
                    AccessibleName = "إلغاء"
                };
                cancel.FlatAppearance.BorderColor = BrandPalette.Border;
                cancel.FlatAppearance.BorderSize = 1;
                buttons.Controls.Add(cancel);
                CancelButton = cancel;
            }

            Controls.Add(body);
            Controls.Add(buttons);
            Controls.Add(heading);
        }
    }
}




