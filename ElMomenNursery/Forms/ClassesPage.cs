using ElMomenNursery.Application.Abstractions.Persistence;
using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Controls;
using ElMomenNursery.Domain.Entities;
using ElMomenNursery.Styling;
using Microsoft.EntityFrameworkCore;
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
            await using var scope = _scopes.CreateAsyncScope(); var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var room = await uow.Repository<Classroom>().Query(false).Include(x => x.ChildEnrollments).Include(x => x.TeacherAssignments).SingleOrDefaultAsync(x => x.Id == row.Id);
            if (room != null) { uow.Repository<Classroom>().Remove(room); var today = DateOnly.FromDateTime(DateTime.Today); foreach (var x in room.ChildEnrollments.Where(x => x.EndDate == null)) x.EndDate = today < x.StartDate ? x.StartDate : today; foreach (var x in room.TeacherAssignments.Where(x => x.EndDate == null)) x.EndDate = today < x.StartDate ? x.StartDate : today; await uow.SaveChangesAsync(); }
            await Load();
        };
        children.Click += async (_, _) => { if (Selected() is not ClassroomRow row) return; using var d = new ClassroomChildrenDialog(_scopes, row.Id, row.Name); if (d.ShowDialog(this) == DialogResult.OK) await Load(row.Id); };
        page.Controls.AddRange([grid, add, edit, delete, children, count]);
        page.Resize += (_, _) => { LayoutDataHeader(page); int y = 190; add.SetBounds(page.ClientSize.Width - 179, y, 145, 42); edit.SetBounds(add.Left - 112, y, 104, 42); delete.SetBounds(edit.Left - 104, y, 96, 42); children.SetBounds(delete.Left - 150, y, 142, 42); count.SetBounds(34, 236, 280, 26); grid.SetBounds(34, 266, page.ClientSize.Width - 68, Math.Max(220, page.ClientSize.Height - 300)); };
        Controls.Add(page); _ = Load();

        ClassroomRow? Selected() => grid.SelectedRows.Count > 0 ? grid.SelectedRows[0].Tag as ClassroomRow : null;
        async Task Load(Guid? select = null)
        {
            await using var scope = _scopes.CreateAsyncScope(); var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var rooms = await uow.Repository<Classroom>().Query().AsSplitQuery().Include(x => x.TeacherAssignments).ThenInclude(x => x.Employee).Include(x => x.ChildEnrollments).ToListAsync();
            grid.Rows.Clear(); foreach (var room in rooms.OrderBy(x => x.Name)) { var teacher = room.TeacherAssignments.Where(x => x.EndDate == null).OrderByDescending(x => x.StartDate).FirstOrDefault(); var id = grid.Rows.Add(room.Name, teacher?.Employee.FullName ?? "غير مسند", room.ChildEnrollments.Count(x => x.EndDate == null)); grid.Rows[id].Tag = new ClassroomRow(room.Id, room.Name, teacher?.EmployeeId); }
            count.Text = $"عدد الفصول: {rooms.Count}"; if (select.HasValue) foreach (DataGridViewRow r in grid.Rows) if (r.Tag is ClassroomRow cr && cr.Id == select) r.Selected = true;
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
                    await using var scope = _scopes.CreateAsyncScope(); var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var rooms = uow.Repository<Classroom>(); var now = DateOnly.FromDateTime(DateTime.Today);
                    if (_id is null) { var room = new Classroom { Name = _name.Text.Trim() }; await rooms.AddAsync(room); await uow.Repository<ClassTeacherAssignment>().AddAsync(new ClassTeacherAssignment { ClassroomId = room.Id, Classroom = room, EmployeeId = teacherId, Employee = await uow.Repository<Employee>().FirstOrDefaultAsync(x => x.Id == teacherId, asNoTracking: false) ?? throw new InvalidOperationException("الموظف غير موجود."), StartDate = now }); }
                    else { var room = await rooms.Query(false).Include(x => x.TeacherAssignments).SingleAsync(x => x.Id == _id); room.Name = _name.Text.Trim(); var current = room.TeacherAssignments.FirstOrDefault(x => x.EndDate == null); if (current?.EmployeeId != teacherId) { if (current != null) current.EndDate = now < current.StartDate ? current.StartDate : now; await uow.Repository<ClassTeacherAssignment>().AddAsync(new ClassTeacherAssignment { ClassroomId = room.Id, Classroom = room, EmployeeId = teacherId, Employee = await uow.Repository<Employee>().FirstOrDefaultAsync(x => x.Id == teacherId, asNoTracking: false) ?? throw new InvalidOperationException("الموظف غير موجود."), StartDate = now }); } }
                    await uow.SaveChangesAsync(); DialogResult = DialogResult.OK; Close();
                }
                catch (DbUpdateException exception)
                {
                    var details = exception.GetBaseException().Message;
                    var message = details.Contains("UNIQUE constraint failed: Classrooms.Name", StringComparison.OrdinalIgnoreCase)
                        ? "اسم الفصل مستخدم بالفعل. اختر اسمًا آخر."
                        : "تعذر حفظ الفصل وإسناد المدرس. أعد المحاولة، وإذا استمرت المشكلة راجع بيانات الموظف.";
                    MessageBox.Show(this, message, "الفصول", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                }
            };
            Shown += async (_, _) =>
            {
                await using var scope = _scopes.CreateAsyncScope(); var employees = await scope.ServiceProvider.GetRequiredService<IEmployeeService>().GetEmployees();
                _teacher.Items.Add(new TeacherOption(null, "اختر المدرس")); if (employees.IsSuccess) foreach (var employee in employees.Value) _teacher.Items.Add(new TeacherOption(employee.Id, employee.FullName));
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
                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var today = DateOnly.FromDateTime(DateTime.Today);
                    var selected = _children.CheckedItems.Cast<ChildOption>().Select(x => x.Id).ToHashSet();
                    var activeRows = await uow.Repository<ChildClassEnrollment>().Query(false)
                        .Where(x => x.EndDate == null && x.StartDate <= today).ToListAsync();

                    foreach (var row in activeRows.Where(x => x.ClassroomId == _classroomId && !selected.Contains(x.ChildId)))
                        row.EndDate = today < row.StartDate ? row.StartDate : today;

                    var room = await uow.Repository<Classroom>().FirstOrDefaultAsync(x => x.Id == _classroomId, asNoTracking: false)
                        ?? throw new InvalidOperationException("الفصل غير موجود.");
                    foreach (var childId in selected)
                    {
                        if (activeRows.Any(x => x.ChildId == childId && x.ClassroomId == _classroomId))
                            continue;

                        foreach (var previous in activeRows.Where(x => x.ChildId == childId && x.ClassroomId != _classroomId))
                            previous.EndDate = today < previous.StartDate ? previous.StartDate : today;

                        var child = await uow.Repository<Child>().FirstOrDefaultAsync(x => x.Id == childId, asNoTracking: false)
                            ?? throw new InvalidOperationException("الطفل غير موجود.");
                        var enrollmentRepository = uow.Repository<ChildClassEnrollment>();
                        var sameDayEnrollment = await enrollmentRepository.FirstOrDefaultAsync(
                            x => x.ChildId == childId && x.ClassroomId == _classroomId && x.StartDate == today,
                            asNoTracking: false);
                        if (sameDayEnrollment is not null)
                            sameDayEnrollment.EndDate = null;
                        else
                            await enrollmentRepository.AddAsync(new ChildClassEnrollment
                        {
                            ChildId = childId, ClassroomId = _classroomId, StartDate = today, Child = child, Classroom = room
                        });
                    }

                    await uow.SaveChangesAsync();
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (DbUpdateException)
                {
                    MessageBox.Show(this, "تعذر حفظ إسناد الأطفال. أعد المحاولة.", "الفصول", MessageBoxButtons.OK, MessageBoxIcon.Error,
                        MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                }
                finally { if (!IsDisposed) save.Enabled = true; }
            };
        }
        private async Task LoadChildren() { await using var scope = _scopes.CreateAsyncScope(); var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>(); var active = await uow.Repository<ChildClassEnrollment>().Query().Where(x => x.ClassroomId == _classroomId && x.EndDate == null).Select(x => x.ChildId).ToListAsync(); var kids = await uow.Repository<Child>().Query().OrderBy(x => x.FullName).ToListAsync(); foreach (var child in kids) { var item = new ChildOption(child.Id, child.FullName, active.Contains(child.Id)); _children.Items.Add(item, item.Selected); } }
        private sealed record ChildOption(Guid Id, string Name, bool Selected) { public override string ToString() => Name; }
    }
}
