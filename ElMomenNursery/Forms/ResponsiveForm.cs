namespace ElMomenNursery.Forms;

/// <summary>
/// Gives every application window DPI scaling and keeps it within the active monitor's work area.
/// Existing form layouts remain responsible for their content; oversized dialogs can be scrolled
/// by their own content panels instead of being placed partly off-screen.
/// </summary>
public class ResponsiveForm : Form
{
    public ResponsiveForm()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScroll = true;
        MinimumSize = new Size(420, 320);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        FitToWorkingArea();
        ResponsiveLayout.ApplyToTree(this);
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        if (IsHandleCreated && !IsDisposed)
        {
            try { BeginInvoke(() => { FitToWorkingArea(); ResponsiveLayout.ApplyToTree(this); }); }
            catch (InvalidOperationException) { }
        }
    }

    private void FitToWorkingArea()
    {
        if (!IsHandleCreated || WindowState is FormWindowState.Minimized or FormWindowState.Maximized) return;

        var workArea = Screen.FromControl(this).WorkingArea;
        var maximumWidth = Math.Max(320, workArea.Width - 16);
        var maximumHeight = Math.Max(240, workArea.Height - 16);
        var minimumWidth = Math.Min(Math.Max(420, MinimumSize.Width), maximumWidth);
        var minimumHeight = Math.Min(Math.Max(320, MinimumSize.Height), maximumHeight);
        MinimumSize = new Size(minimumWidth, minimumHeight);

        var targetWidth = Math.Min(Width, workArea.Width);
        var targetHeight = Math.Min(Height, workArea.Height);
        if (targetWidth != Width || targetHeight != Height)
            Size = new Size(targetWidth, targetHeight);

        var targetLeft = Math.Clamp(Left, workArea.Left, workArea.Right - Width);
        var targetTop = Math.Clamp(Top, workArea.Top, workArea.Bottom - Height);
        if (targetLeft != Left || targetTop != Top)
            Location = new Point(targetLeft, targetTop);
    }
}

internal static class ResponsiveLayout
{
    public static void ApplyToTree(Control root)
    {
        foreach (Control control in root.Controls)
        {
            if (control is DataGridView grid)
                ConfigureGrid(grid);
            ApplyToTree(control);
        }
    }

    private static void ConfigureGrid(DataGridView grid)
    {
        grid.ScrollBars = ScrollBars.Both;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
        grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;

        foreach (DataGridViewColumn column in grid.Columns)
        {
            if (!column.Visible) continue;
            var header = column.HeaderText;
            column.MinimumWidth = header.Contains("بيان") || header.Contains("ملاحظ") || header.Contains("مصدر") || header.Contains("موظف") || header.Contains("طفل") ? 125
                : header.Contains("تاريخ") || header == "من" || header == "إلى" || header.Contains("فترة") ? 96
                : header.Contains("مبلغ") || header.Contains("رصيد") || header.Contains("راتب") || header.Contains("إجمال") || header.Contains("مدفوع") || header.Contains("مستحق") ? 100
                : 84;
        }
    }
}
