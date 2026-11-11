using ElMomenNursery.Styling;

namespace ElMomenNursery.Controls;

public sealed class FormFieldRow : UserControl
{
    public FormFieldRow(string labelText, Control editor)
    {
        Height = 44;
        MinimumSize = new Size(280, 44);
        RightToLeft = RightToLeft.Yes;
        BackColor = Color.Transparent;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            RightToLeft = RightToLeft.Yes,
            BackColor = Color.Transparent
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var label = new Label
        {
            Text = labelText,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = BrandPalette.Text,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point),
            RightToLeft = RightToLeft.Yes,
            AccessibleRole = AccessibleRole.Text
        };

        editor.Dock = DockStyle.Fill;
        editor.Margin = new Padding(6, 4, 6, 4);
        editor.Font ??= new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

        layout.Controls.Add(label, 0, 0);
        layout.Controls.Add(editor, 1, 0);
        Controls.Add(layout);
    }
}

