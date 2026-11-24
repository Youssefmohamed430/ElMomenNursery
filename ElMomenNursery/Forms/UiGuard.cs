using System.Diagnostics;
using ElMomenNursery.Controls;
using Serilog;

namespace ElMomenNursery.Forms;

public static class UiGuard
{
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(UiGuard));

    public static async Task RunAsync(
        IWin32Window? owner,
        string operation,
        Func<Task> work,
        string? userMessage = null)
    {
        try
        {
            await work();
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation - do not treat as an unexpected error
        }
        catch (Exception exception)
        {
            Log.Error(exception, "UI operation failed; operation={Operation}", operation);
            ShowError(owner, userMessage ?? "تعذر إكمال العملية. أعد المحاولة.");
        }
    }

    public static async Task<T?> RunAsync<T>(
        IWin32Window? owner,
        string operation,
        Func<Task<T>> work,
        string? userMessage = null,
        T? fallback = default)
    {
        try
        {
            return await work();
        }
        catch (OperationCanceledException)
        {
            return fallback;
        }
        catch (Exception exception)
        {
            Log.Error(exception, "UI operation failed; operation={Operation}", operation);
            ShowError(owner, userMessage ?? "تعذر إكمال العملية. أعد المحاولة.");
            return fallback;
        }
    }

    public static void Run(
        IWin32Window? owner,
        string operation,
        Action work,
        string? userMessage = null)
    {
        try
        {
            work();
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        catch (Exception exception)
        {
            Log.Error(exception, "UI operation failed; operation={Operation}", operation);
            ShowError(owner, userMessage ?? "تعذر إكمال العملية. أعد المحاولة.");
        }
    }

    private static void ShowError(IWin32Window? owner, string message)
    {
        if (owner is Control { IsDisposed: true } or Control { Disposing: true })
            return;

        try
        {
            if (owner is Control control && control.InvokeRequired)
            {
                control.BeginInvoke(new Action(() => ShowDialogCore(owner, message)));
            }
            else
            {
                ShowDialogCore(owner, message);
            }
        }
        catch (Exception)
        {
            // Prevent crashing if the host control is closing
        }
    }

    private static void ShowDialogCore(IWin32Window? owner, string message)
    {
        if (owner is Control { IsDisposed: true } or Control { Disposing: true })
            return;

        if (owner is not null)
        {
            AppDialog.ShowInformation(owner, "خطأ", message);
        }
        else
        {
            MessageBox.Show(message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
        }
    }
}
