using ElMomenNursery.Controls;

namespace ElMomenNursery.Forms;

internal static class ExportColumnPicker
{
    public static int[]? Pick(IWin32Window owner, IReadOnlyList<string> headers)
    {
        using var form = new ResponsiveForm
        {
            Text = "اختيار أعمدة التصدير", Width = 420, Height = 520,
            MinimumSize = new Size(340, 360), RightToLeft = RightToLeft.Yes,
            RightToLeftLayout = true, StartPosition = FormStartPosition.CenterParent
        };
        var list = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, RightToLeft = RightToLeft.Yes };
        foreach (var header in headers) list.Items.Add(header, true);
        var all = new Button { Text = "تحديد الكل/إلغاء الكل", AutoSize = true };
        all.Click += (_, _) =>
        {
            var check = Enumerable.Range(0, list.Items.Count).Any(i => !list.GetItemChecked(i));
            for (var i = 0; i < list.Items.Count; i++) list.SetItemChecked(i, check);
        };
        var ok = new Button { Text = "موافق", DialogResult = DialogResult.OK, AutoSize = true };
        var cancel = new Button { Text = "إلغاء", DialogResult = DialogResult.Cancel, AutoSize = true };
        var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, FlowDirection = FlowDirection.LeftToRight, RightToLeft = RightToLeft.Yes };
        footer.Controls.AddRange([ok, cancel, all]);
        form.Controls.Add(list); form.Controls.Add(footer); form.AcceptButton = ok; form.CancelButton = cancel;
        while (form.ShowDialog(owner) == DialogResult.OK)
        {
            var selected = Enumerable.Range(0, list.Items.Count).Where(list.GetItemChecked).ToArray();
            if (selected.Length > 0) return selected;
            AppDialog.ShowInformation(form, "اختيار أعمدة التصدير", "حدد عمودًا واحدًا على الأقل.");
        }
        return null;
    }
}
