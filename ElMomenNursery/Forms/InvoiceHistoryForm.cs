using System.Drawing;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Controls;
using ElMomenNursery.Styling;
using Microsoft.Extensions.DependencyInjection;

namespace ElMomenNursery.Forms;

internal sealed class InvoiceHistoryForm : ResponsiveForm
{
    private readonly IServiceScopeFactory _scopes;
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
        AllowUserToDeleteRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RightToLeft = RightToLeft.Yes, RowHeadersVisible = false };
    private readonly TextBox _details = new() { Dock = DockStyle.Bottom, Height = 150, Multiline = true, ReadOnly = true,
        ScrollBars = ScrollBars.Vertical, RightToLeft = RightToLeft.Yes };

    public InvoiceHistoryForm(IServiceScopeFactory scopes)
    {
        _scopes = scopes; Text = "سجل الفواتير"; Size = new Size(1050, 650); MinimumSize = new Size(800, 480);
        StartPosition = FormStartPosition.CenterParent; RightToLeft = RightToLeft.Yes; RightToLeftLayout = true;
        BackColor = BrandPalette.AppBackground;
        _grid.Columns.AddRange([
            new DataGridViewTextBoxColumn { HeaderText = "رقم الفاتورة" }, new DataGridViewTextBoxColumn { HeaderText = "التاريخ" },
            new DataGridViewTextBoxColumn { HeaderText = "العميل" }, new DataGridViewTextBoxColumn { HeaderText = "الإجمالي" },
            new DataGridViewTextBoxColumn { HeaderText = "الحالة" }, new DataGridViewTextBoxColumn { HeaderText = "طريقة الدفع" },
            new DataGridViewTextBoxColumn { HeaderText = "مرجع الدفع" }
        ]);
        Controls.Add(_grid); Controls.Add(_details);
        _grid.SelectionChanged += (_, _) => ShowDetails();
        Load += async (_, _) => await LoadInvoicesAsync();
    }

    private async Task LoadInvoicesAsync()
    {
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            var result = await scope.ServiceProvider.GetRequiredService<IInvoiceService>().GetInvoicesAsync();
            if (result.IsFailure) { ResultPresenter.Present(this, "الفواتير", result); return; }
            foreach (var invoice in result.Value)
            {
                var row = _grid.Rows.Add(invoice.InvoiceNumber, invoice.InvoiceDate.ToString("dd/MM/yyyy"), invoice.CustomerName,
                    invoice.TotalAmount.ToString("N2") + " ج.م", StatusName(invoice.Status), invoice.PaymentMethod.ToString(), invoice.PaymentReference ?? "—");
                _grid.Rows[row].Tag = invoice;
            }
            if (_grid.Rows.Count > 0) _grid.Rows[0].Selected = true;
        }
        catch (Exception ex)
        {
            Serilog.Log.ForContext<InvoiceHistoryForm>().Error(ex, "Invoice history load failed");
            AppDialog.ShowInformation(this, "الفواتير", "تعذر تحميل سجل الفواتير.");
        }
    }

    private void ShowDetails()
    {
        if (_grid.SelectedRows.Count == 0 || _grid.SelectedRows[0].Tag is not ElMomenNursery.Application.DTOs.InvoiceListItemDto invoice) return;
        _details.Text = string.Join(Environment.NewLine, invoice.Lines.Select(x =>
            $"{x.Description} — {x.Quantity} × {x.UnitPrice:N2} = {x.Total:N2} ج.م"));
    }

    private static string StatusName(ElMomenNursery.Domain.Enums.InvoiceStatus status) => status switch
    {
        ElMomenNursery.Domain.Enums.InvoiceStatus.Draft => "مسودة",
        ElMomenNursery.Domain.Enums.InvoiceStatus.Posted => "مكتملة",
        ElMomenNursery.Domain.Enums.InvoiceStatus.Voided => "ملغاة",
        _ => status.ToString()
    };
}
