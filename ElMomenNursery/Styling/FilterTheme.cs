using System.Drawing;

namespace ElMomenNursery.Styling;

/// <summary>
/// Single source of truth for the application-wide filter design system: spacing,
/// control sizes, typography, and the factory methods used by every filter section.
/// </summary>
internal static class FilterTheme
{
    // Card
    internal const int CardCornerRadius = 12;
    internal const int CardPadding = 18;
    internal const int CardVerticalPadding = 16;

    // Field grid
    internal const int FieldGap = 14;
    internal const int FieldHeight = 58;
    internal const int LabelHeight = 18;
    internal const int InputTop = 24;
    internal const int InputHeight = 32;

    // Actions
    internal const int ButtonHeight = 36;
    internal const int ButtonGap = 10;
    internal const int MinButtonWidth = 96;

    internal static Font CreateLabelFont() => new("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);

    internal static Font CreateInputFont() => new("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point);

    internal static Label CreateLabel(string text) => new()
    {
        Text = text,
        Height = LabelHeight,
        ForeColor = BrandPalette.MutedText,
        Font = CreateLabelFont(),
        // In an RTL container WinForms mirrors label alignment, so MiddleLeft renders at the physical right edge.
        TextAlign = ContentAlignment.MiddleLeft,
        RightToLeft = RightToLeft.Yes,
        AutoEllipsis = true
    };

    internal static TextBox CreateTextInput(string placeholder = "") => new()
    {
        BorderStyle = BorderStyle.FixedSingle,
        BackColor = BrandPalette.Surface,
        Font = CreateInputFont(),
        RightToLeft = RightToLeft.Yes,
        PlaceholderText = placeholder
    };

    internal static ComboBox CreateComboBox(params string[] options)
    {
        var combo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = BrandPalette.Surface,
            Font = CreateInputFont(),
            RightToLeft = RightToLeft.Yes
        };
        if (options.Length > 0)
        {
            combo.Items.AddRange(options);
            combo.SelectedIndex = 0;
        }
        return combo;
    }

    internal static DateTimePicker CreateDatePicker(bool optional = false)
    {
        var picker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            ShowCheckBox = optional,
            Checked = !optional,
            Font = CreateInputFont(),
            RightToLeft = RightToLeft.Yes
        };
        return picker;
    }

    internal static Button CreatePrimaryButton(string text)
    {
        var button = BrandControls.ActionButton(text, primary: true);
        button.Height = ButtonHeight;
        return button;
    }

    internal static Button CreateSecondaryButton(string text)
    {
        var button = BrandControls.ActionButton(text);
        button.Height = ButtonHeight;
        button.FlatAppearance.BorderColor = BrandPalette.FilterBorderStrong;
        return button;
    }

    /// <summary>Standard height for operational buttons shown in page toolbars.</summary>
    internal const int ToolbarHeight = 42;
}
