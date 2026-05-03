using System.Management;
using System.Runtime.InteropServices;
using DevCleanerPro.Models;

namespace DevCleanerPro.Services;

/// <summary>
/// Servicio para obtener información del sistema operativo, hardware y runtime.
/// </summary>
public class SystemInfoService
{
    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetPhysicallyInstalledSystemMemory(out long totalMemoryInKilobytes);

    public SystemInfoModel GetSystemInfo()
    {
        var info = new SystemInfoModel
        {
            OsVersion = $"{Environment.OSVersion.VersionString}",
            MachineName = Environment.MachineName,
            UserName = Environment.UserName,
            DotNetVersion = Environment.Version.ToString(),
            ProcessorCount = Environment.ProcessorCount,
            Uptime = TimeSpan.FromMilliseconds(Environment.TickCount64)
        };

        // RAM total
        if (GetPhysicallyInstalledSystemMemory(out long totalKb))
        {
            info.TotalRamBytes = totalKb * 1024;
        }

        // RAM disponible
        try
        {
            var gcInfo = GC.GetGCMemoryInfo();
            info.AvailableRamBytes = (long)gcInfo.TotalAvailableMemoryBytes;
        }
        catch { }

        // Nombre del procesador via WMI
        try
        {
            using var searcher = new ManagementObjectSearcher("select Name from Win32_Processor");
            foreach (var obj in searcher.Get())
            {
                info.ProcessorName = obj["Name"]?.ToString()?.Trim() ?? "Desconocido";
                break;
            }
        }
        catch
        {
            info.ProcessorName = $"Procesador ({info.ProcessorCount} núcleos)";
        }

        return info;
    }
}
