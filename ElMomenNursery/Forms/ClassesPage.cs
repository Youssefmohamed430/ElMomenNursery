using ElMomenNursery.Application.IServices;
using ElMomenNursery.Controls;
using ElMomenNursery.Styling;
using Microsoft.Extensions.DependencyInjection;
using static ElMomenNursery.Forms.MainForm;

namespace ElMomenNursery.Forms;

internal sealed class ClassesPage : UserControl
{
    public ClassesPage(IServiceScopeFactory scopeFactory, UserSession session, IExcelExportService excelExportService)
    {
        _ = scopeFactory;
        _ = session;
        _ = excelExportService;
        Dock = DockStyle.Fill;
        var page = CreateDataPage("الفصول", "إدارة الفصول غير متاحة حتى اعتماد البيانات المطلوبة.", includePrototypeBanner: false);
        var notice = new Label
        {
            Text = "سيتم إتاحة إدارة الفصول بعد تحديد بياناتها وقواعدها.",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            RightToLeft = RightToLeft.Yes,
            ForeColor = BrandPalette.MutedText
        };
        page.Controls.Add(notice);
        page.Resize += (_, _) =>
        {
            LayoutDataHeader(page);
            notice.SetBounds(34, 190, page.ClientSize.Width - 68, 110);
        };
        LayoutDataHeader(page);
        notice.SetBounds(34, 190, page.ClientSize.Width - 68, 110);
        Controls.Add(page);
    }
}
