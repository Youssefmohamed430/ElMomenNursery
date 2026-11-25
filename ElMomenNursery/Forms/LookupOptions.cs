using ElMomenNursery.Application.IServices;
using Microsoft.Extensions.DependencyInjection;
namespace ElMomenNursery.Forms;
internal static class LookupOptions
{
    public static async Task LoadAsync(IServiceScopeFactory? scopes, ComboBox combo, string category, string[] fallback)
    {
        var old = combo.Text;
        combo.Items.Clear();
        var values = fallback.AsEnumerable();
        if (scopes is not null)
        {
            try { await using var scope = scopes.CreateAsyncScope(); var result = await scope.ServiceProvider.GetRequiredService<ILookupService>().GetAsync(category); if (result.IsSuccess && result.Value.Count > 0) values = result.Value.Select(x => x.Name).Concat(fallback.Where(x => !result.Value.Any(y => y.Name == x))); }
            catch { }
        }
        combo.Items.AddRange(values.Cast<object>().ToArray());
        if (!string.IsNullOrWhiteSpace(old)) combo.Text = old; else if (combo.Items.Count > 0) combo.SelectedIndex = 0;
    }
}
