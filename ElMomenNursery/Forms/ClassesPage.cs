using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Controls;
using ElMomenNursery.Styling;
using Microsoft.Extensions.DependencyInjection;
using static ElMomenNursery.Forms.MainForm;

namespace ElMomenNursery.Forms;

internal sealed class ClassesPage : UserControl
{
    private readonly IServiceScopeFactory _scopes;
    public ClassesPage(IServiceScopeFactory scopeFactory, UserSession session, IExcelExportService excelExportService)
    {
        _scopes = scopeFactory;
        _ = excelExportService;
        Dock = DockStyle.Fill;
        var page = CreateDataPage("الفصول", "إدارة الفصول والمدرسين والأطفال المسجلين بها.", includePrototypeBanner: false);
        var grid = CreateGrid(["اسم الفصل", "المدرس", "عدد الأطفال"]);
        var add = CreateActionButton("إضافة فصل", true);
        var edit = CreateActionButton("تعديل");
        var delete = CreateActionButton("حذف");
        var children = CreateActionButton("إسناد الأطفال");
        var count = new Label { TextAlign = ContentAlignment.MiddleRight, ForeColor = BrandPalette.MutedText };
        add.Visible = edit.Visible = delete.Visible = children.Visible = session.HasPermission("Classes.Manage");
        add.Click += async (_, _) => { using var d = new ClassroomEditor(_scopes); if (d.ShowDialog(this) == DialogResult.OK) await Load(); };
        edit.Click += async (_, _) => { if (Selected() is not ClassroomRow row) return; using var d = new ClassroomEditor(_scopes, row.Id, row.Name, row.TeacherId); if (d.ShowDialog(this) == DialogResult.OK) await Load(row.Id); };
        delete.Click += async (_, _) =>
        {
            if (Selected() is not ClassroomRow row) return;
            if (MessageBox.Show(this, "سيتم نقل الفصل إلى المحذوفات وإنهاء إسناداته الحالية. هل تريد المتابعة؟", "حذف فصل", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign) != DialogResult.Yes) return;
            await using var scope = _scopes.CreateAsyncScope();
            var result = await scope.ServiceProvider.GetRequiredService<IClassroomService>().DeleteClassroomAsync(row.Id);
            if (!ResultPresenter.Present(this, "الفصول", result)) return;
            await Load();
        };
        children.Click += async (_, _) => { if (Selected() is not ClassroomRow row) return; using var d = new ClassroomChildrenDialog(_scopes, row.Id, row.Name); if (d.ShowDialog(this) == DialogResult.OK) await Load(row.Id); };
        page.Controls.AddRange([grid, add, edit, delete, children, count]);
        page.Resize += (_, _) => { LayoutDataHeader(page); int y = 190; add.SetBounds(page.ClientSize.Width - 179, y, 145, 42); edit.SetBounds(add.Left - 112, y, 104, 42); delete.SetBounds(edit.Left - 104, y, 96, 42); children.SetBounds(delete.Left - 150, y, 142, 42); count.SetBounds(34, 236, 280, 26); grid.SetBounds(34, 266, page.ClientSize.Width - 68, Math.Max(220, page.ClientSize.Height - 300)); };
        Controls.Add(page); _ = Load();

        ClassroomRow? Selected() => grid.SelectedRows.Count > 0 ? grid.SelectedRows[0].Tag as ClassroomRow : null;
        async Task Load(Guid? select = null)
        {
            await using var scope = _scopes.CreateAsyncScope();
            var result = await scope.ServiceProvider.GetRequiredService<IClassroomService>().GetClassroomsAsync();
            if (result.IsFailure) { AppDialog.ShowInformation(this, "الفصول", result.Error.Description); return; }
            grid.Rows.Clear(); foreach (var room in result.Value) { var id = grid.Rows.Add(room.Name, room.TeacherName ?? "غير مسند", room.ChildCount); grid.Rows[id].Tag = new ClassroomRow(room.Id, room.Name, room.TeacherId); }
            count.Text = $"عدد الفصول: {result.Value.Count}"; if (select.HasValue) foreach (DataGridViewRow r in grid.Rows) if (r.Tag is ClassroomRow cr && cr.Id == select) r.Selected = true;
        }
    }

    private sealed record ClassroomRow(Guid Id, string Name, Guid? TeacherId);

    private sealed class ClassroomEditor : Form
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly Guid? _id;
        private readonly TextBox _name = new() { Width = 300 };
        private readonly ComboBox _teacher = new() { Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
        public ClassroomEditor(IServiceScopeFactory scopes, Guid? id = null, string? name = null, Guid? teacherId = null)
        {
            _scopes = scopes; _id = id;
            Text = name == null ? "إضافة فصل" : "تعديل الفصل"; StartPosition = FormStartPosition.CenterParent; Width = 480; Height = 250; RightToLeft = RightToLeft.Yes; RightToLeftLayout = true;
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 2, RowCount = 3 };
            layout.Controls.Add(new Label { Text = "اسم الفصل", AutoSize = true }, 0, 0); layout.Controls.Add(_name, 1, 0); layout.Controls.Add(new Label { Text = "المدرس", AutoSize = true }, 0, 1); layout.Controls.Add(_teacher, 1, 1);
            var save = new Button { Text = "حفظ", Width = 100 }; var cancel = new Button { Text = "إلغاء", DialogResult = DialogResult.Cancel, Width = 100 }; layout.Controls.Add(save, 1, 2); layout.Controls.Add(cancel, 0, 2); Controls.Add(layout); _name.Text = name ?? "";
            save.Click += async (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(_name.Text)) { MessageBox.Show(this, "اكتب اسم الفصل."); return; }
                if ((_teacher.SelectedItem as TeacherOption)?.Id is not Guid teacherId) { MessageBox.Show(this, "اختر المدرس المسؤول عن الفصل."); return; }
                try
                {
                    await using var scope = _scopes.CreateAsyncScope();
                    var result = await scope.ServiceProvider.GetRequiredService<IClassroomService>().SaveClassroomAsync(_id, _name.Text, teacherId);
                    if (!ResultPresenter.Present(this, "الفصول", result)) return;
                    DialogResult = DialogResult.OK; Close();
                }
                catch (Exception exception)
                {
                    Serilog.Log.ForContext<ClassroomEditor>().Error(exception, "Classroom save failed; classroomId={ClassroomId}", _id);
                    AppDialog.ShowInformation(this, "الفصول", "تعذر حفظ الفصل وإسناد المدرس.");
                }
            };
            Shown += async (_, _) =>
            {
                await using var scope = _scopes.CreateAsyncScope(); var employees = await scope.ServiceProvider.GetRequiredService<IClassroomService>().GetTeachersAsync();
                _teacher.Items.Add(new TeacherOption(null, "اختر المدرس")); if (employees.IsSuccess) foreach (var employee in employees.Value) _teacher.Items.Add(new TeacherOption(employee.Id, employee.Name));
                _teacher.SelectedIndex = 0; for (var i = 0; i < _teacher.Items.Count; i++) if (_teacher.Items[i] is TeacherOption option && option.Id == teacherId) _teacher.SelectedIndex = i;
            };
        }
        private sealed record TeacherOption(Guid? Id, string Name) { public override string ToString() => Name; }
    }

    private sealed class ClassroomChildrenDialog : Form
    {
        private readonly IServiceScopeFactory _scopes; private readonly Guid _classroomId; private readonly CheckedListBox _children = new() { Dock = DockStyle.Fill, CheckOnClick = true };
        public ClassroomChildrenDialog(IServiceScopeFactory scopes, Guid classroomId, string classroomName)
        {
            _scopes = scopes; _classroomId = classroomId; Text = $"إسناد الأطفال — {classroomName}"; StartPosition = FormStartPosition.CenterParent; Width = 500; Height = 600; RightToLeft = RightToLeft.Yes; RightToLeftLayout = true;
            var save = new Button { Text = "حفظ الإسناد", Dock = DockStyle.Bottom, Height = 44 }; Controls.Add(_children); Controls.Add(save); Shown += async (_, _) => await LoadChildren();
            save.Click += async (_, _) =>
            {
                save.Enabled = false;
                try
                {
                    await using var scope = _scopes.CreateAsyncScope();
                    var selected = _children.CheckedItems.Cast<ChildOption>().Select(x => x.Id).ToHashSet();
                    var result = await scope.ServiceProvider.GetRequiredService<IClassroomService>().AssignChildrenAsync(_classroomId, selected);
                    if (!ResultPresenter.Present(this, "الفصول", result)) return;
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (Exception exception)
                {
                    Serilog.Log.ForContext<ClassroomChildrenDialog>().Error(exception, "Classroom child assignment failed; classroomId={ClassroomId}", _classroomId);
                    AppDialog.ShowInformation(this, "الفصول", "تعذر حفظ إسناد الأطفال. أعد المحاولة.");
                }
                finally { if (!IsDisposed) save.Enabled = true; }
            };
        }
        private async Task LoadChildren() { await using var scope = _scopes.CreateAsyncScope(); var result = await scope.ServiceProvider.GetRequiredService<IClassroomService>().GetChildrenAsync(_classroomId); if (result.IsFailure) { AppDialog.ShowInformation(this, "الفصول", result.Error.Description); return; } foreach (var child in result.Value) { var item = new ChildOption(child.Id, child.Name, child.IsAssigned); _children.Items.Add(item, item.Selected); } }
        private sealed record ChildOption(Guid Id, string Name, bool Selected) { public override string ToString() => Name; }
    }
}
