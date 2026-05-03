using System.Text.Json;
using DevCleanerPro.Models;

namespace DevCleanerPro.Services;

/// <summary>
/// Servicio transversal de auditoría. Registra todas las acciones de limpieza en JSON.
/// </summary>
public class AuditLogService
{
    private static readonly string LogDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DevCleanerPro", "logs");

    private static readonly JsonSerializerOptions Opts = new() { WriteIndented = true };

    public AuditLogService()
    {
        Directory.CreateDirectory(LogDir);
    }

    public void Log(string module, string action, long bytesFreed = 0, int filesDeleted = 0, string? error = null)
    {
        var entry = new AuditLogEntry
        {
            Module = module, Action = action, BytesFreed = bytesFreed,
            FilesDeleted = filesDeleted, Status = error == null ? "OK" : "ERROR", Error = error
        };
        try
        {
            var file = Path.Combine(LogDir, $"{DateTime.Now:yyyy-MM-dd}.json");
            var entries = LoadDay(DateTime.Now);
            entries.Add(entry);
            File.WriteAllText(file, JsonSerializer.Serialize(entries, Opts));
        }
        catch { }
    }

    public List<AuditLogEntry> LoadDay(DateTime date)
    {
        var file = Path.Combine(LogDir, $"{date:yyyy-MM-dd}.json");
        if (!File.Exists(file)) return new();
        try { return JsonSerializer.Deserialize<List<AuditLogEntry>>(File.ReadAllText(file), Opts) ?? new(); }
        catch { return new(); }
    }

    public List<AuditLogEntry> LoadRange(DateTime from, DateTime to)
    {
        var all = new List<AuditLogEntry>();
        for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
            all.AddRange(LoadDay(d));
        return all;
    }

    public List<string> GetLogFiles() =>
        Directory.Exists(LogDir) ? Directory.GetFiles(LogDir, "*.json").OrderByDescending(f => f).ToList() : new();

    public void PurgeOlderThan(int days)
    {
        var cutoff = DateTime.Now.AddDays(-days);
        foreach (var f in Directory.GetFiles(LogDir, "*.json"))
        {
            if (File.GetCreationTime(f) < cutoff)
                try { File.Delete(f); } catch { }
        }
    }

    public void ExportToCsv(string outputPath, DateTime from, DateTime to)
    {
        var entries = LoadRange(from, to);
        var lines = new List<string> { "Timestamp,Module,Action,BytesFreed,FilesDeleted,Status,Error" };
        lines.AddRange(entries.Select(e =>
            $"\"{e.Timestamp:O}\",\"{e.Module}\",\"{e.Action}\",{e.BytesFreed},{e.FilesDeleted},\"{e.Status}\",\"{e.Error ?? ""}\""));
        File.WriteAllLines(outputPath, lines);
    }
}
