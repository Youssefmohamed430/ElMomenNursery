using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Domain.Enums;
using System.Diagnostics;
using System.Drawing;

namespace ElMomenNursery.Forms;

internal sealed class ChildProfileForm : ResponsiveForm
{
    public ChildProfileForm(ChildDto child, IReadOnlyList<ChildAttachmentDto> attachments, IReadOnlyList<SubscriptionDto> subscriptions)
    {
        Text = $"ملف الطفل - {child.FullName}";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(900, 720);
        MinimumSize = new Size(760, 580);
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        Font = new Font("Segoe UI", 10F);

        var content = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(16), AutoScroll = true };
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var personalPhoto = attachments.FirstOrDefault(x => x.Type == AttachmentType.PersonalPhoto);
        var details = CreateChildDetails(child, personalPhoto);
        content.Controls.Add(details, 0, 0);

        var docs = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
        foreach (var type in new[] { AttachmentType.PersonalPhoto, AttachmentType.BirthCertificate, AttachmentType.GuardianIdentityCard })
        {
            var attachment = attachments.FirstOrDefault(x => x.Type == type);
            var label = type switch { AttachmentType.PersonalPhoto => "صورة الطفل", AttachmentType.BirthCertificate => "شهادة الميلاد", _ => "بطاقة ولي الأمر" };
            var button = new Button { Text = attachment is null ? $"{label}: غير مرفق" : $"فتح {label} ({attachment.OriginalFileName})", AutoSize = true, Enabled = attachment is not null, Height = 38, Margin = new Padding(5) };
            if (attachment is not null) button.Click += (_, _) => OpenAttachment(attachment.RelativePath);
            docs.Controls.Add(button);
        }
        var docsSection = new GroupBox { Text = "الوثائق", AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(10) };
        docsSection.Controls.Add(docs);
        content.Controls.Add(docsSection, 0, 1);

        var subscriptionsTable = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AutoGenerateColumns = false, RightToLeft = RightToLeft.Yes, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        subscriptionsTable.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "من", DataPropertyName = nameof(SubscriptionDto.PeriodStart) });
        subscriptionsTable.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "إلى", DataPropertyName = nameof(SubscriptionDto.PeriodEnd) });
        subscriptionsTable.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "المطلوب", DataPropertyName = nameof(SubscriptionDto.AmountDue), DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        subscriptionsTable.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "المدفوع", DataPropertyName = nameof(SubscriptionDto.PaidAmount), DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        subscriptionsTable.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "المتبقي", DataPropertyName = nameof(SubscriptionDto.RemainingAmount), DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        subscriptionsTable.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "الحالة", DataPropertyName = nameof(SubscriptionDto.Status) });
        subscriptionsTable.DataSource = subscriptions.OrderByDescending(x => x.PeriodStart).ToList();
        var subscriptionSection = new GroupBox { Text = "الاشتراكات", Dock = DockStyle.Fill, Padding = new Padding(10) };
        subscriptionSection.Controls.Add(subscriptionsTable);
        content.Controls.Add(subscriptionSection, 0, 2);
        Controls.Add(content);
        FormClosed += (_, _) => { var picture = details.Controls.OfType<TableLayoutPanel>().SelectMany(x => x.Controls.OfType<PictureBox>()).FirstOrDefault(); picture?.Image?.Dispose(); };
    }

    private static GroupBox CreateChildDetails(ChildDto child, ChildAttachmentDto? photo)
    {
        var section = new GroupBox { Text = "بيانات الطفل وولي الأمر", Dock = DockStyle.Top, Height = 300, Padding = new Padding(12), Margin = new Padding(0,0,0,10) };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(4) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        var detailsText = !child.IsRegistrationComplete
            ? $"الاسم: {child.FullName}\nهذا تسجيل مبدئي. لم تُستكمل البيانات الشخصية بعد.\nيمكن استكماله من زر تعديل البيانات؛ الاشتراكات والفواتير المرتبطة ستظل على هذا السجل."
            : $"الاسم: {child.FullName}\nالنوع: {(child.Gender == ChildGender.Boy ? "ولد" : "بنت")}\nتاريخ الميلاد: {child.DateOfBirth:dd/MM/yyyy}\nتاريخ الالتحاق: {child.EnrollmentDate:dd/MM/yyyy}\nالعنوان: {child.Address}\nالأب: {child.FatherName} — {child.FatherPhone}\nالأم: {child.MotherName} — {child.MotherPhone}\nواتساب الأب: {child.FatherWhatsApp ?? "—"}\nواتساب الأم: {child.MotherWhatsApp ?? "—"}\nملاحظات: {child.Notes ?? "—"}";
        var text = new Label { Text = detailsText, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(8), AutoEllipsis = true };
        var picture = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White, Margin = new Padding(12, 4, 4, 4) };
        if (photo is not null)
        {
            var path = ResolvePath(photo.RelativePath);
            try
            {
                if (File.Exists(path))
                {
                    using var image = Image.FromFile(path); picture.Image = new Bitmap(image);
                    picture.Cursor = Cursors.Hand; picture.Click += (_, _) => ShowPhoto(picture.Image, child.FullName);
                }
                else picture.AccessibleDescription = "تعذر العثور على صورة الطفل";
            }
            catch { picture.AccessibleDescription = "تعذر فتح صورة الطفل"; }
        }
        layout.Controls.Add(text, 0, 0); layout.Controls.Add(picture, 1, 0); section.Controls.Add(layout); return section;
    }

    private static void ShowPhoto(Image? image, string childName)
    {
        if (image is null) return;
        using var viewer = new Form { Text = $"صورة الطفل - {childName}", Size = new Size(900, 700), MinimumSize = new Size(480, 360), StartPosition = FormStartPosition.CenterParent, BackColor = Color.White };
        viewer.Controls.Add(new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, Image = image });
        viewer.ShowDialog();
    }

    private static string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path) || File.Exists(path)) return path;
        return Path.Combine(AppContext.BaseDirectory, path);
    }

    private static GroupBox CreateSection(string title, string value) => new()
    {
        Text = title, Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(12), Margin = new Padding(0, 0, 0, 10),
        Controls = { new Label { Text = value, AutoSize = true, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(6), RightToLeft = RightToLeft.Yes } }
    };

    private static void OpenAttachment(string path)
    {
        if (!File.Exists(path))
        {
            MessageBox.Show("الملف غير موجود في المسار المسجل.", "الوثائق", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
            return;
        }
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
}
