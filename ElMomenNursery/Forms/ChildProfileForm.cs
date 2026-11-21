using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Domain.Enums;
using System.Diagnostics;

namespace ElMomenNursery.Forms;

internal sealed class ChildProfileForm : Form
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
        content.Controls.Add(CreateSection("بيانات الطفل وولي الأمر", $"الاسم: {child.FullName}\nالنوع: {(child.Gender == ChildGender.Boy ? "ولد" : "بنت")}\nتاريخ الميلاد: {child.DateOfBirth:dd/MM/yyyy}\nتاريخ الالتحاق: {child.EnrollmentDate:dd/MM/yyyy}\nالعنوان: {child.Address}\nالأب: {child.FatherName} — {child.FatherPhone}\nالأم: {child.MotherName} — {child.MotherPhone}\nواتساب الأب: {child.FatherWhatsApp ?? "—"}\nواتساب الأم: {child.MotherWhatsApp ?? "—"}\nملاحظات: {child.Notes ?? "—"}"), 0, 0);

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
        subscriptionsTable.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "الخطة", DataPropertyName = nameof(SubscriptionDto.SubscriptionPlanName) });
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
