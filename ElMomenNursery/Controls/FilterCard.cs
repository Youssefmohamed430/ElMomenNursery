using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using ElMomenNursery.Styling;

namespace ElMomenNursery.Controls;

/// <summary>
/// Application-wide filter surface. A rounded white card that arranges its labelled fields
/// in a responsive, right-to-left grid (labels above inputs) and sizes itself to its content.
/// The card is presentation only: filtering logic stays in each page.
/// </summary>
public sealed class FilterCard : Panel
{
    private readonly List<Control> _blocks = [];
    private readonly List<int> _spans = [];
    private readonly List<FilterField> _fields = [];
    private bool _arranging;

    public FilterCard()
    {
        RightToLeft = RightToLeft.Yes;
        BackColor = BrandPalette.AppBackground;
        Padding = new Padding(FilterTheme.CardPadding, FilterTheme.CardVerticalPadding,
            FilterTheme.CardPadding, FilterTheme.CardVerticalPadding);
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    /// <summary>When false (custom drag layouts) the card keeps its height but does not move fields.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool AutoArrange { get; set; } = true;

    public IReadOnlyList<FilterField> Fields => _fields;

    public FilterActionGroup? ActionGroup { get; private set; }

    public int ColumnCount { get; private set; } = 1;

    public FilterField AddField(string name, string labelText, Control input, int columnSpan = 1)
    {
        var field = new FilterField(name, labelText, input) { ColumnSpan = Math.Max(1, columnSpan) };
        AddBlock(field, field.ColumnSpan);
        _fields.Add(field);
        return field;
    }

    public FilterGroup AddGroup(string name, string labelText, int columnSpan, params Control[] inputs)
    {
        var group = new FilterGroup(name, labelText, inputs);
        AddBlock(group, Math.Max(1, columnSpan));
        return group;
    }

    public FilterActionGroup AddActions(params Button[] buttons)
    {
        var group = new FilterActionGroup(buttons);
        AddBlock(group, Math.Max(1, buttons.Length));
        ActionGroup = group;
        return group;
    }

    private void AddBlock(Control block, int span)
    {
        _blocks.Add(block);
        _spans.Add(Math.Max(1, span));
        Controls.Add(block);
    }

    /// <summary>Arranges the default grid and returns the resulting card height.</summary>
    public int Arrange() => LayoutBlocks(applyPositions: true);

    /// <summary>Recomputes only the card height (used while a custom drag layout is active).</summary>
    public int UpdateHeight() => LayoutBlocks(applyPositions: false);

    protected override void OnResize(EventArgs eventArgs)
    {
        base.OnResize(eventArgs);
        if (AutoArrange) Arrange();
        else UpdateHeight();
    }

    private int LayoutBlocks(bool applyPositions)
    {
        if (_arranging) return Height;
        _arranging = true;
        try
        {
            var innerWidth = Math.Max(240, ClientSize.Width - Padding.Horizontal);
            var columns = innerWidth >= 1000 ? 6 : innerWidth >= 800 ? 4 : innerWidth >= 620 ? 3 : 2;
            ColumnCount = columns;
            var cellWidth = (innerWidth - FilterTheme.FieldGap * (columns - 1)) / columns;
            var column = 0;
            var row = 0;
            var lastRow = 0;

            for (var index = 0; index < _blocks.Count; index++)
            {
                var span = Math.Min(_spans[index], columns);
                if (column + span > columns)
                {
                    row++;
                    column = 0;
                }

                if (applyPositions)
                {
                    var slotWidth = cellWidth * span + FilterTheme.FieldGap * (span - 1);
                    var x = Padding.Left + (columns - column - span) * (cellWidth + FilterTheme.FieldGap);
                    var y = Padding.Top + row * (FilterTheme.FieldHeight + FilterTheme.FieldGap);
                    _blocks[index].SetBounds(x, y, slotWidth, FilterTheme.FieldHeight);
                    ResizeBlock(_blocks[index], slotWidth);
                }

                column += span;
                lastRow = Math.Max(lastRow, row);
                if (column >= columns)
                {
                    row++;
                    column = 0;
                }
            }

            var rows = _blocks.Count == 0 ? 0 : lastRow + 1;
            var height = rows == 0
                ? Padding.Vertical
                : Padding.Vertical + rows * FilterTheme.FieldHeight + (rows - 1) * FilterTheme.FieldGap;
            if (Height != height) Height = height;
            return Height;
        }
        finally
        {
            _arranging = false;
        }
    }

    private static void ResizeBlock(Control block, int width)
    {
        switch (block)
        {
            case FilterField field:
                field.ResizeTo(width);
                break;
            case FilterGroup group:
                group.ResizeTo(width);
                break;
            case FilterActionGroup actions:
                actions.ResizeTo(width);
                break;
        }
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        var bounds = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
        using var path = CreateRoundedRectangle(bounds, FilterTheme.CardCornerRadius);
        eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var fill = new SolidBrush(BrandPalette.Surface);
        eventArgs.Graphics.FillPath(fill, path);
        using var border = new Pen(BrandPalette.Border);
        eventArgs.Graphics.DrawPath(border, path);
        base.OnPaint(eventArgs);
    }

    private static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
