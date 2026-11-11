using ElMomenNursery.Styling;

namespace ElMomenNursery.Controls;

public sealed class FieldValidationHint : Label
{
    public FieldValidationHint()
    {
        Height = 24;
        AutoSize = false;
        Visible = false;
        TextAlign = ContentAlignment.MiddleLeft;
        ForeColor = BrandPalette.DangerText;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        RightToLeft = RightToLeft.Yes;
        AccessibleRole = AccessibleRole.Alert;
    }

    public void SetMessage(string? message)
    {
        Text = message ?? string.Empty;
        Visible = !string.IsNullOrWhiteSpace(message);
    }
}

