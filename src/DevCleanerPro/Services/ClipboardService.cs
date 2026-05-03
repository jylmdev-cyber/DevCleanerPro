using Microsoft.Win32;

namespace DevCleanerPro.Services;

/// <summary>
/// Módulo 17: Gestión del portapapeles e historial.
/// </summary>
public class ClipboardService
{
    private const string HistoryRegPath = @"SOFTWARE\Microsoft\Clipboard";

    public void ClearClipboard()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
            System.Windows.Clipboard.Clear());
    }

    public bool IsHistoryEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(HistoryRegPath);
            return key?.GetValue("EnableClipboardHistory") is int val && val == 1;
        }
        catch { return false; }
    }

    public bool SetHistoryEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(HistoryRegPath);
            key.SetValue("EnableClipboardHistory", enabled ? 1 : 0, RegistryValueKind.DWord);
            return true;
        }
        catch { return false; }
    }

    public void OpenClipboardHistory()
    {
        // Simular Win+V
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = "ms-settings:clipboard",
            UseShellExecute = true
        });
    }
}
