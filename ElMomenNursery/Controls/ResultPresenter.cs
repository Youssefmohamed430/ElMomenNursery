using ElMomenNursery.Domain.Abstractions;

namespace ElMomenNursery.Controls;

internal static class ResultPresenter
{
    public static bool Present(IWin32Window owner, string title, Result result)
    {
        if (result.IsFailure)
        {
            AppDialog.ShowInformation(owner, title, result.Error.Description);
            return false;
        }

        if (!string.IsNullOrWhiteSpace(result.Message)) AppDialog.ShowInformation(owner, title, result.Message);
        return true;
    }
}
