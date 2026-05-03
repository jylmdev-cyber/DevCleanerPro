using System.Diagnostics;
using DevCleanerPro.Models;

namespace DevCleanerPro.Services;

/// <summary>
/// Módulo 6: Limpieza de Event Logs del sistema.
/// </summary>
public class EventLogCleanerService : ICleanerModule
{
    public string Name => "Event Log Cleaner";
    public string Description => "Limpia logs de Application, System, Security y Setup";
    public string IconGlyph => "\uE7BA";

    private static readonly string[] LogNames = { "Application", "System", "Security", "Setup" };

    public record EventLogInfo(string Name, long EntryCount, long SizeKb, bool Accessible);

    public async Task<List<EventLogInfo>> GetLogInfoAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            var list = new List<EventLogInfo>();
            foreach (var name in LogNames)
            {
                ct.ThrowIfCancellationRequested();
                progress?.Report($"Analizando: {name}...");
                try
                {
                    using var log = new EventLog(name);
                    long size = 0;
                    try
                    {
                        var logFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
                            "winevt", "Logs", $"{name}.evtx");
                        if (File.Exists(logFile)) size = new FileInfo(logFile).Length / 1024;
                    }
                    catch { }
                    list.Add(new EventLogInfo(name, log.Entries.Count, size, true));
                }
                catch { list.Add(new EventLogInfo(name, 0, 0, false)); }
            }
            return list;
        }, ct);
    }

    public Task<long> AnalyzeAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        return Task.FromResult(0L);
    }

    public async Task<CleaningResult> CleanAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        return await CleanLogsAsync(LogNames.Where(n => n != "Security").ToArray(), progress, ct);
    }

    public async Task<CleaningResult> CleanLogsAsync(string[] logNames, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var result = new CleaningResult { CategoryName = Name };
        var sw = Stopwatch.StartNew();
        await Task.Run(() =>
        {
            foreach (var name in logNames)
            {
                ct.ThrowIfCancellationRequested();
                progress?.Report($"Limpiando log: {name}...");
                try
                {
                    var r = Helpers.ProcessRunner.RunAsync("wevtutil", $"cl {name}", 10000, ct).GetAwaiter().GetResult();
                    if (r.Success) { result.ItemsCleaned++; progress?.Report($"✓ {name} limpiado"); }
                    else { result.Errors++; result.ErrorMessages.Add($"{name}: {r.Error}"); }
                }
                catch (Exception ex) { result.Errors++; result.ErrorMessages.Add($"{name}: {ex.Message}"); }
            }
        }, ct);
        sw.Stop();
        result.Duration = sw.Elapsed;
        return result;
    }
}
