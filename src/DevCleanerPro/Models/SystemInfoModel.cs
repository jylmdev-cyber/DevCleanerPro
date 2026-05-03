namespace DevCleanerPro.Models;

/// <summary>
/// Información del sistema para el Dashboard.
/// </summary>
public class SystemInfoModel
{
    public string OsVersion { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string DotNetVersion { get; set; } = string.Empty;
    public string ProcessorName { get; set; } = string.Empty;
    public int ProcessorCount { get; set; }
    public long TotalRamBytes { get; set; }
    public long AvailableRamBytes { get; set; }
    public TimeSpan Uptime { get; set; }
}

/// <summary>
/// Información de un disco/unidad.
/// </summary>
public class DriveInfoModel
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string DriveType { get; set; } = string.Empty;
    public string FileSystem { get; set; } = string.Empty;
    public long TotalSize { get; set; }
    public long FreeSpace { get; set; }
    public long UsedSpace => TotalSize - FreeSpace;
    public double UsagePercent => TotalSize > 0 ? (double)UsedSpace / TotalSize * 100 : 0;
}

/// <summary>
/// Configuración de la aplicación (persistida en JSON).
/// </summary>
public class AppSettings
{
    // Apariencia
    public string Theme { get; set; } = "Dark";
    public string Language { get; set; } = "es-ES";

    // Comportamiento
    public string DefaultScanPath { get; set; } = string.Empty;
    public bool ConfirmBeforeDelete { get; set; } = true;
    public bool UseRecycleBin { get; set; } = false;
    public bool SafeModeEnabled { get; set; } = true;
    public int LargeFileThresholdMb { get; set; } = 100;

    // Exclusiones
    public List<string> ExclusionPatterns { get; set; } = new();

    // Logs / Retención
    public int LogRetentionDays { get; set; } = 30;

    // System Tray
    public bool MinimizeToTray { get; set; } = false;
    public bool StartWithWindows { get; set; } = false;

    // Estadísticas
    public DateTime? LastScanDate { get; set; }
    public long LastTotalFreed { get; set; }
    public long SessionFreedBytes { get; set; }
    public long HistoricalFreedBytes { get; set; }
}
