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
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var result = await scope.ServiceProvider.GetRequiredService<ILookupService>().GetAsync(category);
                if (result.IsSuccess) values = result.Value.Select(x => x.Name);
            }
            catch { }
        }
        combo.Items.AddRange(values.Cast<object>().ToArray());
        var oldIndex = string.IsNullOrWhiteSpace(old) ? -1 : combo.FindStringExact(old);
        if (oldIndex >= 0) combo.SelectedIndex = oldIndex;
        else if (combo.Items.Count > 0) combo.SelectedIndex = 0;
    }
}
