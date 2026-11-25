using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace AlMoumenNursery.Bootstrapper;

internal static class Program
{
    private const string ProductName = "نظام إدارة حضانة المؤمن";
    private const string AppExe = "نظام إدارة حضانة المؤمن.exe";
    private const uint CloseWindowMessage = 0x0010;

    [STAThread]
    private static int Main()
    {
        ApplicationConfiguration.Initialize();
        var isUpdater = Path.GetFileNameWithoutExtension(Environment.ProcessPath ?? string.Empty).Equals("Update", StringComparison.OrdinalIgnoreCase);
        var operation = "بدء التثبيت";
        try
        {
            var installDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "AlMoumenNursery");
            if (!isUpdater && Directory.Exists(installDir) && File.Exists(Path.Combine(installDir, AppExe)))
            {
                if (MessageBox.Show("يوجد إصدار مثبت بالفعل. هل تريد تحديثه؟", ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return 0;
                isUpdater = true;
            }
            if (isUpdater && !WaitForApplication(installDir)) throw new InvalidOperationException("أغلق البرنامج ثم أعد تشغيل Update.exe.");

            var staging = Path.Combine(Path.GetTempPath(), "AlMoumenNursery-" + Guid.NewGuid().ToString("N"));
            operation = "فك ملفات البرنامج";
            Directory.CreateDirectory(staging);
            try
            {
                using var payload = Assembly.GetExecutingAssembly().GetManifestResourceStream("Payload.zip")
                    ?? throw new FileNotFoundException("ملفات البرنامج غير موجودة داخل حزمة التثبيت.");
                using (var archive = new ZipArchive(payload, ZipArchiveMode.Read)) archive.ExtractToDirectory(staging);
                operation = "نسخ ملفات البرنامج إلى مجلد التثبيت";
                Directory.CreateDirectory(installDir);
                foreach (var file in Directory.EnumerateFiles(staging, "*", SearchOption.AllDirectories))
                {
                    var relative = Path.GetRelativePath(staging, file);
                    var target = Path.Combine(installDir, relative);
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    File.Copy(file, target, overwrite: true);
                }
                operation = "إنشاء اختصار سطح المكتب";
                CreateShortcut(installDir);
            }
            finally { try { Directory.Delete(staging, recursive: true); } catch { } }

            MessageBox.Show(isUpdater ? "تم تحديث البرنامج بنجاح." : "تم تثبيت البرنامج بنجاح. ستجد الاختصار على سطح المكتب.", ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information,
                MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
            return 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"تعذر إكمال العملية أثناء {operation}:\n{ex.Message}", ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error,
                MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
            return 1;
        }
    }

    private static bool WaitForApplication(string directory)
    {
        var appName = Path.GetFileNameWithoutExtension(AppExe);
        var until = DateTime.UtcNow.AddSeconds(90);
        var closeRequested = false;
        while (DateTime.UtcNow < until)
        {
            var running = Process.GetProcessesByName(appName).Where(p =>
            {
                try { return string.Equals(Path.GetDirectoryName(p.MainModule?.FileName), directory, StringComparison.OrdinalIgnoreCase); }
                catch { return false; }
            }).ToArray();
            if (running.Length == 0) return true;
            if (!closeRequested)
            {
                foreach (var process in running)
                {
                    try { if (process.MainWindowHandle != IntPtr.Zero) PostMessage(process.MainWindowHandle, CloseWindowMessage, IntPtr.Zero, IntPtr.Zero); }
                    catch { }
                }
                closeRequested = true;
            }
            Thread.Sleep(1000);
        }
        return false;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam);

    private static void CreateShortcut(string directory)
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
        var path = Path.Combine(desktop, ProductName + ".lnk");
        IShellLinkW? shellLink = null;
        try
        {
            shellLink = (IShellLinkW)new ShellLink();
            shellLink.SetPath(Path.Combine(directory, AppExe));
            shellLink.SetWorkingDirectory(directory);
            shellLink.SetDescription(ProductName);
            shellLink.SetIconLocation(Path.Combine(directory, AppExe), 0);
            ((IPersistFile)shellLink).Save(path, false);
        }
        finally
        {
            if (shellLink is not null) Marshal.FinalReleaseComObject(shellLink);
        }
    }

    [ComImport]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder file, int maxPath, out IntPtr findData, uint flags);
        void GetIDList(out IntPtr itemIdList);
        void SetIDList(IntPtr itemIdList);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder name, int maxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string name);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder directory, int maxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string directory);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder arguments, int maxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string arguments);
        void GetHotkey(out short hotkey);
        void SetHotkey(short hotkey);
        void GetShowCmd(out int showCommand);
        void SetShowCmd(int showCommand);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder iconPath, int maxPath, out int iconIndex);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string iconPath, int iconIndex);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string path, uint reserved);
        void Resolve(IntPtr window, uint flags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string path);
    }

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLink { }
}
