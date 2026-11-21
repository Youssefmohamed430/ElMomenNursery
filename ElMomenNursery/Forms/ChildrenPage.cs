using System.Drawing;
using System.Text.Json;
using Serilog;
using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Controls;
using ElMomenNursery.Domain.Abstractions;
using ElMomenNursery.Domain.Enums;
using ElMomenNursery.Styling;
using Microsoft.Extensions.DependencyInjection;
using static ElMomenNursery.Forms.MainForm;

namespace ElMomenNursery.Forms;

internal sealed class ChildrenPage : UserControl
{
    private static readonly ILogger _log = Log.ForContext<ChildrenPage>();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly UserSession _session;
    private readonly IExcelExportService _excelExportService;

    public ChildrenPage(IServiceScopeFactory scopeFactory, UserSession session, IExcelExportService excelExportService)
    {
        _scopeFactory = scopeFactory;
        _session = session;
        _excelExportService = excelExportService;
        Dock = DockStyle.Fill;
        Controls.Add(BuildContent());
    }
    private Control BuildContent()
    {
        var page = CreateDataPage("الأطفال", "استعرض الأطفال وادمج أكثر من فلتر للوصول إلى المجموعة المطلوبة.", includePrototypeBanner: false);
        var addButton = CreateActionButton("إضافة طفل", primary: true);
        var editButton = CreateActionButton("تعديل البيانات");
        var deleteButton = CreateActionButton("حذف طفل");
        var detailsButton = CreateActionButton("عرض الملف");
        var exportButton = CreateActionButton("تصدير القائمة");
        addButton.Visible = _session.HasPermission("Students.Create");
        editButton.Visible = _session.HasPermission("Students.Edit");
        deleteButton.Visible = _session.HasPermission("Students.Delete");
        detailsButton.Visible = _session.HasPermission("Students.View");
        exportButton.Visible = _session.HasPermission("Students.Export");
        addButton.Name = "actions.add";
        editButton.Name = "actions.edit";
        deleteButton.Name = "actions.delete";
        detailsButton.Name = "actions.details";
        exportButton.Name = "actions.export";
        addButton.Width = 150;
        editButton.Width = 140;
        deleteButton.Width = 126;
        detailsButton.Width = 142;
        exportButton.Width = 142;
        var actions = new Panel
        {
            BackColor = Color.Transparent,
            Padding = Padding.Empty
        };

        var filterPanel = new Panel
        {
            AutoScroll = true,
            RightToLeft = RightToLeft.Yes,
            BackColor = BrandPalette.Surface,
            Padding = new Padding(10, 6, 10, 6),
            BorderStyle = BorderStyle.FixedSingle
        };
        var draggableControls = new Dictionary<string, Control>(StringComparer.Ordinal);
        var filterItems = new List<Control>();
        var search = CreateSearchBox("اسم الطفل");
        var genderFilter = CreateFilterCombo(["الكل", "ولد", "بنت"], 116);
        var dateOfBirthFrom = CreateOptionalDatePicker();
        var dateOfBirthTo = CreateOptionalDatePicker();
        var enrollmentFrom = CreateOptionalDatePicker();
        var enrollmentTo = CreateOptionalDatePicker();
        var classroomFilter = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5F),
            RightToLeft = RightToLeft.Yes,
            Width = 150,
            Height = 34
        };
        classroomFilter.Items.Add("كل الفصول");
        classroomFilter.SelectedIndex = 0;
        var paymentFilter = CreateFilterCombo(["كل الحالات", "دفعوا", "مدفعوش"], 142);
        var resultCount = new Label
        {
            AutoSize = false,
            Width = 280,
            Height = 28,
            ForeColor = BrandPalette.MutedText,
            TextAlign = ContentAlignment.MiddleRight,
            RightToLeft = RightToLeft.Yes
        };

        AddFilterField("filter.search", "اسم الطفل", search, 180);
        AddFilterField("filter.gender", "النوع", genderFilter, 128);
        AddFilterField("filter.birth-from", "الميلاد من", dateOfBirthFrom, 142);
        AddFilterField("filter.birth-to", "الميلاد إلى", dateOfBirthTo, 142);
        AddFilterField("filter.enrollment-from", "الالتحاق من", enrollmentFrom, 142);
        AddFilterField("filter.enrollment-to", "الالتحاق إلى", enrollmentTo, 142);
        AddFilterField("filter.classroom", "الفصل الحالي", classroomFilter, 158);
        AddFilterField("filter.payment", "الاشتراك الساري", paymentFilter, 150);

        var applyButton = CreateActionButton("تطبيق", primary: true);
        applyButton.Name = "filter.apply";
        applyButton.Width = 96;
        var clearButton = CreateActionButton("مسح الفلاتر");
        clearButton.Name = "filter.clear";
        clearButton.Width = 112;
        AddFilterButton(applyButton);
        AddFilterButton(clearButton);

        var customizeButton = CreateActionButton("تخصيص الشاشة");
        customizeButton.Name = "layout.customize";
        customizeButton.Width = 150;
        var resetLayoutButton = CreateActionButton("استعادة الافتراضي");
        resetLayoutButton.Name = "layout.reset";
        resetLayoutButton.Width = 150;
        resetLayoutButton.Visible = false;
        actions.Controls.AddRange([addButton, editButton, deleteButton, detailsButton, exportButton, customizeButton, resetLayoutButton]);

        var grid = CreateGrid([
            "المعرف", "اسم الطفل", "العنوان", "تاريخ الالتحاق", "تاريخ الميلاد", "النوع",
            "اسم الأب", "مهنة الأب", "هاتف الأب", "واتساب الأب", "اسم الأم", "مهنة الأم",
            "هاتف الأم", "واتساب الأم", "ملاحظات", "حالة طبية", "سبق له الالتحاق",
            "تاريخ الإضافة", "آخر تعديل"
        ]);
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        grid.Name = "children.grid";
        var columnWidths = new[] { 265, 170, 220, 140, 140, 90, 150, 140, 140, 140, 150, 140, 140, 140, 240, 120, 150, 160, 160 };
        for (var columnIndex = 0; columnIndex < grid.Columns.Count; columnIndex++)
            grid.Columns[columnIndex].Width = columnWidths[columnIndex];
        resultCount.Name = "children.count";

        RegisterDraggable(addButton);
        RegisterDraggable(editButton);
        RegisterDraggable(deleteButton);
        RegisterDraggable(detailsButton);
        RegisterDraggable(exportButton);
        RegisterDraggable(grid);
        RegisterDraggable(resultCount);

        var isCustomizing = false;
        var savedLayout = LoadSavedLayout();
        var hasCustomLayout = savedLayout.Count > 0;
        var loadInProgress = false;
        AttachDraggableControls();

        addButton.Click += (_, _) => RunSafeEvent(async () =>
        {
            if (isCustomizing) return;
            using var editor = new ChildEditorForm(_scopeFactory);
            if (editor.ShowDialog(this) == DialogResult.OK)
                await LoadChildrenAsync();
        }, "Open child creation form");
        editButton.Click += (_, _) => RunSafeEvent(async () =>
        {
            if (isCustomizing) return;
            if (grid.SelectedRows.Count == 0 || grid.SelectedRows[0].Tag is not Guid childId)
            {
                MessageBox.Show("اختر طفلًا من الجدول أولًا.", "تعديل بيانات طفل", MessageBoxButtons.OK,
                    MessageBoxIcon.Information, MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                return;
            }

            try
            {
                Result<ChildDto> childResult;
                await using (var scope = _scopeFactory.CreateAsyncScope())
                    childResult = await scope.ServiceProvider.GetRequiredService<IChildService>().GetChildByIdAsync(childId);
                if (childResult.IsFailure) { AppDialog.ShowInformation(this, "تعديل بيانات طفل", childResult.Error.Description); return; }
                var child = childResult.Value;
                if (page.IsDisposed)
                {
                    _log.Debug("Child editor opening cancelled: page disposed");
                    return;
                }
                using var editor = new ChildEditorForm(_scopeFactory, child);
                if (editor.ShowDialog(this) == DialogResult.OK)
                    await LoadChildrenAsync();
            }
            catch (KeyNotFoundException exception)
            {
                if (page.IsDisposed) return;
                _log.Warning(exception, "Selected child was not found; childId={ChildId}", childId);
                MessageBox.Show(this, "لم يتم العثور على سجل الطفل. حدّث القائمة.", "تعديل بيانات طفل", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                await LoadChildrenAsync();
            }
            catch (Exception exception)
            {
                _log.Error(exception, "Failed to open child editor; childId={ChildId}", childId);
                if (page.IsDisposed) return;
                MessageBox.Show(this, "تعذر فتح سجل الطفل.", "تعديل بيانات طفل", MessageBoxButtons.OK,
                    MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
            }
        }, "Open child editor");
        deleteButton.Click += (_, _) => RunSafeEvent(async () =>
        {
            if (isCustomizing) return;
            if (grid.SelectedRows.Count == 0 || grid.SelectedRows[0].Tag is not Guid childId)
            {
                MessageBox.Show(this, "اختر طفلًا من الجدول أولًا.", "حذف سجل طفل", MessageBoxButtons.OK,
                    MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                return;
            }
            var confirm = MessageBox.Show(this,
                "هل تريد نقل سجل الطفل المحدد إلى المحذوفات؟ سيختفي من القوائم النشطة مع الاحتفاظ بسجلاته التاريخية.",
                "تأكيد حذف الطفل", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2,
                MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
            if (confirm != DialogResult.Yes) return;
            try
            {
                Result deleted;
                await using (var scope = _scopeFactory.CreateAsyncScope())
                    deleted = await scope.ServiceProvider.GetRequiredService<IChildService>().DeleteChildAsync(childId);
                if (page.IsDisposed)
                {
                    _log.Debug("Child deletion UI update cancelled: page disposed");
                    return;
                }
                if (deleted.IsFailure)
                {
                    AppDialog.ShowInformation(this, "حذف سجل طفل", deleted.Error.Description);
                    await LoadChildrenAsync();
                    return;
                }
                ResultPresenter.Present(this, "حذف سجل طفل", deleted);
                await LoadChildrenAsync();
            }
            catch (Exception exception)
            {
                _log.Warning(exception, "Child deletion could not be completed; childId={ChildId}", childId);
                if (page.IsDisposed) return;
                MessageBox.Show(this, "تعذر نقل سجل الطفل إلى المحذوفات. أعد المحاولة.", "تعذر حذف الطفل",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
            }
        }, "Delete child");
        detailsButton.Click += (_, _) => RunSafeEvent(async () =>
        {
            if (isCustomizing) return;
            if (grid.SelectedRows.Count == 0 || grid.SelectedRows[0].Tag is not Guid childId)
            {
                AppDialog.ShowInformation(this, "ملف الطفل", "اختر طفلًا من الجدول أولًا.");
                return;
            }
            await using var scope = _scopeFactory.CreateAsyncScope();
            var childService = scope.ServiceProvider.GetRequiredService<IChildService>();
            var child = await childService.GetChildByIdAsync(childId);
            var attachments = await childService.GetChildAttachmentsAsync(childId);
            var subscriptions = await scope.ServiceProvider.GetRequiredService<ISubscriptionService>().GetSubscriptionsForChildAsync(childId);
            if (child.IsFailure) { AppDialog.ShowInformation(this, "ملف الطفل", child.Error.Description); return; }
            if (attachments.IsFailure) { AppDialog.ShowInformation(this, "ملف الطفل", attachments.Error.Description); return; }
            if (subscriptions.IsFailure) { AppDialog.ShowInformation(this, "ملف الطفل", subscriptions.Error.Description); return; }
            using var profile = new ChildProfileForm(child.Value, attachments.Value, subscriptions.Value);
            profile.ShowDialog(this);
        }, "Open child profile");
        exportButton.Click += async (_, _) =>
        {
            if (!isCustomizing) await GridExcelExport.SaveAsync(this, grid, _excelExportService, "تصدير قائمة الأطفال", "الأطفال");
        };

        applyButton.Click += (_, _) => RunSafeEvent(async () => { if (!isCustomizing) await LoadChildrenAsync(); }, "Apply child filters");
        clearButton.Click += (_, _) => RunSafeEvent(async () =>
        {
            if (isCustomizing) return;
            search.Clear();
            genderFilter.SelectedIndex = 0;
            dateOfBirthFrom.Checked = false;
            dateOfBirthTo.Checked = false;
            enrollmentFrom.Checked = false;
            enrollmentTo.Checked = false;
            classroomFilter.SelectedIndex = 0;
            paymentFilter.SelectedIndex = 0;
            await LoadChildrenAsync();
        }, "Clear child filters");
        customizeButton.Click += (_, _) => RunSafeEvent(async () =>
        {
            if (!isCustomizing)
            {
                isCustomizing = true;
                customizeButton.Text = "حفظ التخطيط";
                resetLayoutButton.Visible = true;
                resultCount.Text = "وضع التخصيص: اسحب العناصر ثم احفظ.";
                SetDragAppearance(true);
                return;
            }

            if (!SaveCurrentLayout()) return;
            savedLayout = ReadCurrentLayout();
            hasCustomLayout = true;
            isCustomizing = false;
            customizeButton.Text = "تخصيص الشاشة";
            resetLayoutButton.Visible = false;
            SetDragAppearance(false);
            await LoadChildrenAsync();
        }, "Customize child page");
        resetLayoutButton.Click += (_, _) =>
        {
            try
            {
                if (File.Exists(GetLayoutFilePath())) File.Delete(GetLayoutFilePath());
                savedLayout.Clear();
                hasCustomLayout = false;
                isCustomizing = false;
                customizeButton.Text = "تخصيص الشاشة";
                resetLayoutButton.Visible = false;
                SetDragAppearance(false);
                LayoutChildrenPage();
                _ = RunSafeAsync(LoadChildrenAsync, "Load children after layout reset");
            }
            catch (Exception exception)
            {
                _log.Error(exception, "Failed to reset child screen layout");
                ShowLayoutError();
            }
        };

        page.Controls.AddRange([filterPanel, actions, resultCount, grid]);
        page.Resize += (_, _) => LayoutChildrenPage();
        LayoutChildrenPage();
        _ = RunSafeAsync(LoadChildrenAsync, "Load children");
        return page;

        void AddFilterField(string controlName, string labelText, Control control, int width)
        {
            var group = new Panel { Name = controlName, Width = width, Height = 56, BackColor = Color.Transparent, Margin = new Padding(3, 0, 3, 0) };
            var label = new Label
            {
                Text = labelText,
                Location = new Point(0, 0),
                Width = width,
                Height = 20,
                ForeColor = BrandPalette.Text,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
                RightToLeft = RightToLeft.Yes
            };
            control.Location = new Point(0, 21);
            control.Width = width;
            control.Height = 32;
            group.Controls.Add(control);
            group.Controls.Add(label);
            filterPanel.Controls.Add(group);
            filterItems.Add(group);
            RegisterDraggable(group);
        }

        void AddFilterButton(Button button)
        {
            button.Height = 40;
            button.Margin = new Padding(5, 18, 5, 0);
            filterPanel.Controls.Add(button);
            filterItems.Add(button);
            RegisterDraggable(button);
        }

        async Task LoadChildrenAsync()
        {
            if (loadInProgress)
            {
                _log.Debug("Skipped child load because another load is in progress");
                return;
            }
            loadInProgress = true;
            applyButton.Enabled = false;
            clearButton.Enabled = false;
            resultCount.Text = "جارٍ تحميل الأطفال...";
            try
            {
                if (classroomFilter.Items.Count == 1)
                {
                    Result<IReadOnlyList<ClassroomFilterOptionDto>> classrooms;
                    await using (var scope = _scopeFactory.CreateAsyncScope())
                        classrooms = await scope.ServiceProvider.GetRequiredService<IChildService>().GetClassroomsForFilterAsync();
                    if (page.IsDisposed || classroomFilter.IsDisposed)
                    {
                        _log.Debug("Child load cancelled: page disposed");
                        return;
                    }
                    if (classrooms.IsFailure) { AppDialog.ShowInformation(this, "الأطفال", classrooms.Error.Description); return; }
                    foreach (var classroom in classrooms.Value)
                        classroomFilter.Items.Add(classroom);
                }

                var gender = genderFilter.SelectedIndex switch
                {
                    1 => ChildGender.Boy,
                    2 => ChildGender.Girl,
                    _ => (ChildGender?)null
                };
                var classroomId = (classroomFilter.SelectedItem as ClassroomFilterOptionDto)?.Id;
                var hasPaidSubscription = paymentFilter.SelectedIndex switch
                {
                    1 => true,
                    2 => false,
                    _ => (bool?)null
                };
                var filter = new ChildFilter
                {
                    SearchText = search.Text,
                    Gender = gender,
                    DateOfBirthFrom = dateOfBirthFrom.Checked ? DateOnly.FromDateTime(dateOfBirthFrom.Value) : null,
                    DateOfBirthTo = dateOfBirthTo.Checked ? DateOnly.FromDateTime(dateOfBirthTo.Value) : null,
                    EnrollmentDateFrom = enrollmentFrom.Checked ? DateOnly.FromDateTime(enrollmentFrom.Value) : null,
                    EnrollmentDateTo = enrollmentTo.Checked ? DateOnly.FromDateTime(enrollmentTo.Value) : null,
                    ClassroomId = classroomId,
                    HasPaidSubscription = hasPaidSubscription
                };
                Result<IReadOnlyList<ChildDto>> childrenResult;
                await using (var scope = _scopeFactory.CreateAsyncScope())
                    childrenResult = await scope.ServiceProvider.GetRequiredService<IChildService>().GetChildrenAsync(filter);
                if (page.IsDisposed || grid.IsDisposed || resultCount.IsDisposed)
                {
                    _log.Debug("Child load cancelled: page disposed");
                    return;
                }
                if (childrenResult.IsFailure)
                {
                    resultCount.Text = childrenResult.Error.Description;
                    AppDialog.ShowInformation(this, "الأطفال", childrenResult.Error.Description);
                    return;
                }
                var children = childrenResult.Value;
                grid.Rows.Clear();
                foreach (var child in children)
                {
                    var index = grid.Rows.Add(
                        child.Id,
                        child.FullName,
                        child.Address,
                        child.EnrollmentDate.ToString("dd/MM/yyyy"),
                        child.DateOfBirth.ToString("dd/MM/yyyy"),
                        child.Gender == ChildGender.Boy ? "ولد" : "بنت",
                        child.FatherName,
                        child.FatherOccupation ?? string.Empty,
                        child.FatherPhone,
                        child.FatherWhatsApp ?? string.Empty,
                        child.MotherName,
                        child.MotherOccupation ?? string.Empty,
                        child.MotherPhone,
                        child.MotherWhatsApp ?? string.Empty,
                        child.Notes ?? string.Empty,
                        child.HasMedicalConditions ? "نعم" : "لا",
                        child.PreviouslyAttendedNursery ? "نعم" : "لا",
                        child.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                        child.UpdatedAt?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? string.Empty);
                    grid.Rows[index].Tag = child.Id;
                }
                resultCount.Text = $"عدد الأطفال: {children.Count}";
            }
            catch (Exception exception)
            {
                _log.Error(exception, "Failed to load children for display");
                if (!page.IsDisposed && !resultCount.IsDisposed)
                {
                    resultCount.Text = "تعذر تحميل البيانات";
                    MessageBox.Show(this, "تعذر تحميل قائمة الأطفال. أعد المحاولة.", "خطأ في تحميل البيانات",
                        MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                }
            }
            finally
            {
                loadInProgress = false;
                if (!page.IsDisposed && !applyButton.IsDisposed && !clearButton.IsDisposed)
                {
                    applyButton.Enabled = true;
                    clearButton.Enabled = true;
                }
            }
        }

        void LayoutChildrenPage()
        {
            LayoutDataHeader(page);
            var leftMargin = 34;
            var rightMargin = 34;
            filterPanel.SetBounds(leftMargin, 120, Math.Max(300, page.ClientSize.Width - leftMargin - rightMargin), 72);
            actions.SetBounds(leftMargin, 204, Math.Max(300, page.ClientSize.Width - leftMargin - rightMargin), 44);
            resultCount.SetBounds(leftMargin, 252, 280, 26);
            var gridTop = 282;
            grid.SetBounds(leftMargin, gridTop, Math.Max(300, page.ClientSize.Width - leftMargin - rightMargin), Math.Max(220, page.ClientSize.Height - gridTop - 24));
            if (!isCustomizing)
            {
                ArrangeDefaultFilterItems();
                ArrangeDefaultActions();
                if (hasCustomLayout)
                    ApplySavedLayout();
            }
            ArrangePinnedActions();
        }

        void RegisterDraggable(Control control) => draggableControls[control.Name] = control;

        void ArrangeDefaultFilterItems()
        {
            var x = 10;
            foreach (var item in filterItems)
            {
                item.SetBounds(x, Math.Max(0, (filterPanel.ClientSize.Height - item.Height) / 2), item.Width, item.Height);
                x += item.Width + 6;
            }
            filterPanel.AutoScrollMinSize = new Size(x + 10, 0);
        }

        void ArrangeDefaultActions()
        {
            var right = actions.ClientSize.Width - 8;
            foreach (var button in new[] { addButton, editButton, deleteButton, detailsButton, exportButton })
            {
                right -= button.Width;
                button.SetBounds(right, 1, button.Width, 42);
                right -= 8;
            }
        }

        void ArrangePinnedActions()
        {
            customizeButton.SetBounds(8, 1, customizeButton.Width, 42);
            resetLayoutButton.SetBounds(166, 1, resetLayoutButton.Width, 42);
        }

        void ApplySavedLayout()
        {
            foreach (var (name, bounds) in savedLayout)
            {
                if (bounds.Length != 4 || !draggableControls.TryGetValue(name, out var control) || control.Parent is null)
                    continue;
                var parent = control.Parent;
                var width = Math.Clamp(bounds[2], 30, Math.Max(30, parent.ClientSize.Width));
                var height = Math.Clamp(bounds[3], 24, Math.Max(24, parent.ClientSize.Height));
                var x = Math.Clamp(bounds[0], 0, Math.Max(0, parent.ClientSize.Width - width));
                var y = Math.Clamp(bounds[1], 0, Math.Max(0, parent.ClientSize.Height - height));
                control.SetBounds(x, y, width, height);
            }
        }

        void AttachDraggableControls()
        {
            foreach (var root in draggableControls.Values)
                AttachDragHandler(root, root);
        }

        void AttachDragHandler(Control root, Control target)
        {
            var dragging = false;
            var offset = Point.Empty;
            target.MouseDown += (_, eventArgs) =>
            {
                if (!isCustomizing || eventArgs.Button != MouseButtons.Left || root.Parent is null) return;
                var point = root.Parent.PointToClient(target.PointToScreen(eventArgs.Location));
                offset = new Point(point.X - root.Left, point.Y - root.Top);
                dragging = true;
            };
            target.MouseMove += (_, eventArgs) =>
            {
                if (!dragging || !isCustomizing || root.Parent is null) return;
                var point = root.Parent.PointToClient(target.PointToScreen(eventArgs.Location));
                var minX = root.Parent == actions
                    ? Math.Min(resetLayoutButton.Visible ? resetLayoutButton.Right + 8 : customizeButton.Right + 8,
                        Math.Max(0, actions.ClientSize.Width - root.Width))
                    : 0;
                var minY = root == grid ? Math.Min(282, Math.Max(0, page.ClientSize.Height - root.Height)) : 0;
                var x = Math.Clamp(point.X - offset.X, minX, Math.Max(minX, root.Parent.ClientSize.Width - root.Width));
                var y = Math.Clamp(point.Y - offset.Y, minY, Math.Max(minY, root.Parent.ClientSize.Height - root.Height));
                root.Location = new Point(x, y);
            };
            target.MouseUp += (_, _) => dragging = false;
            foreach (Control child in target.Controls)
                AttachDragHandler(root, child);
        }

        void SetDragAppearance(bool enabled)
        {
            foreach (var root in draggableControls.Values)
                SetCursor(root, enabled ? Cursors.SizeAll : Cursors.Default);
        }

        static void SetCursor(Control control, Cursor cursor)
        {
            control.Cursor = cursor;
            foreach (Control child in control.Controls)
                SetCursor(child, cursor);
        }

        bool SaveCurrentLayout()
        {
            try
            {
                var directory = Path.GetDirectoryName(GetLayoutFilePath())!;
                Directory.CreateDirectory(directory);
                File.WriteAllText(GetLayoutFilePath(), JsonSerializer.Serialize(ReadCurrentLayout()));
                return true;
            }
            catch (Exception exception)
            {
                _log.Error(exception, "Failed to save child screen layout");
                ShowLayoutError();
                return false;
            }
        }

        Dictionary<string, int[]> ReadCurrentLayout() => draggableControls.ToDictionary(
            pair => pair.Key,
            pair => new[] { pair.Value.Left, pair.Value.Top, pair.Value.Width, pair.Value.Height },
            StringComparer.Ordinal);

        Dictionary<string, int[]> LoadSavedLayout()
        {
            try
            {
                if (!File.Exists(GetLayoutFilePath())) return new Dictionary<string, int[]>(StringComparer.Ordinal);
                return JsonSerializer.Deserialize<Dictionary<string, int[]>>(File.ReadAllText(GetLayoutFilePath()))
                    ?? new Dictionary<string, int[]>(StringComparer.Ordinal);
            }
            catch (Exception exception)
            {
                _log.Warning(exception, "Failed to read saved child screen layout; using default layout");
                return new Dictionary<string, int[]>(StringComparer.Ordinal);
            }
        }

        static string GetLayoutFilePath() => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AlMoumenNursery", "child-screen-layout.json");

        void ShowLayoutError() => MessageBox.Show(this, "تعذر حفظ تخطيط الشاشة. أعد المحاولة.",
            "تخصيص شاشة الأطفال", MessageBoxButtons.OK, MessageBoxIcon.Error,
            MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

        static ComboBox CreateFilterCombo(string[] options, int width)
        {
            var combo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F),
                RightToLeft = RightToLeft.Yes,
                Width = width,
                Height = 34
            };
            combo.Items.AddRange(options);
            combo.SelectedIndex = 0;
            return combo;
        }

        static DateTimePicker CreateOptionalDatePicker() => new()
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            ShowCheckBox = true,
            Checked = false,
            Font = new Font("Segoe UI", 8.5F),
            Width = 142,
            Height = 30
        };
    }
    private static async Task RunSafeAsync(Func<Task> work, string operation)
    {
        try { await work(); }
        catch (Exception exception) { _log.Error(exception, "Unhandled page load failure; operation={Operation}", operation); }
    }

    private async void RunSafeEvent(Func<Task> work, string operation)
    {
        try { await work(); }
        catch (Exception exception)
        {
            _log.Error(exception, "Unhandled child page event failure; operation={Operation}; userId={UserId}", operation, _session.UserId);
            if (!IsDisposed) AppDialog.ShowInformation((IWin32Window?)FindForm() ?? this, "الأطفال", "تعذر إكمال العملية. أعد المحاولة.");
        }
    }
}
