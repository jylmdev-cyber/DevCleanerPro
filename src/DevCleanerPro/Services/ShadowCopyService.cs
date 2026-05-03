using System.Management;
using DevCleanerPro.Helpers;

namespace DevCleanerPro.Services;

/// <summary>
/// Módulo 14: Gestor de Shadow Copies (puntos de restauración).
/// </summary>
public class ShadowCopyService
{
    public record ShadowCopyItem(string Id, string Volume, DateTime InstallDate, string DeviceObject);

    public async Task<List<ShadowCopyItem>> ListAsync(CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            var items = new List<ShadowCopyItem>();
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ShadowCopy");
                foreach (ManagementObject obj in searcher.Get())
                {
                    ct.ThrowIfCancellationRequested();
                    items.Add(new ShadowCopyItem(
                        obj["ID"]?.ToString() ?? "",
                        obj["VolumeName"]?.ToString() ?? "",
                        ManagementDateTimeConverter.ToDateTime(obj["InstallDate"]?.ToString() ?? ""),
                        obj["DeviceObject"]?.ToString() ?? ""));
                }
            }
            catch { }
            return items;
        }, ct);
    }

    public async Task<bool> DeleteAsync(string shadowId, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        progress?.Report($"Eliminando shadow copy: {shadowId}...");
        var result = await ProcessRunner.RunAsync("vssadmin", $"delete shadows /Shadow={shadowId} /Quiet", 30000, ct);
        return result.Success;
    }

    public async Task<string> DeleteAllAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        progress?.Report("Eliminando todas las shadow copies...");
        var result = await ProcessRunner.RunAsync("vssadmin", "delete shadows /all /Quiet", 60000, ct);
        return result.Success ? result.Output : result.Error;
    }

    public void OpenSystemRestore()
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "SystemPropertiesProtection.exe", UseShellExecute = true
        });
    }
}
