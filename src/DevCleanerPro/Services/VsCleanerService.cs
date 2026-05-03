using Microsoft.Win32;
using DevCleanerPro.Helpers;
using DevCleanerPro.Models;

namespace DevCleanerPro.Services;

/// <summary>
/// Módulo 13: Limpiador de cachés de Visual Studio y VS Code.
/// </summary>
public class VsCleanerService : ICleanerModule
{
    public string Name => "VS Cleaner";
    public string Description => "Limpia cachés de Visual Studio, VS Code, y archivos .suo/.user";
    public string IconGlyph => "\uE70F";

    private static string LocalAppData => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private static string AppData => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

    public record VsCacheItem(string Name, string Path, long Size, bool Exists);

    public async Task<List<VsCacheItem>> ScanAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            var items = new List<VsCacheItem>();
            // VS ComponentModelCache
            var vsBase = Path.Combine(LocalAppData, "Microsoft", "VisualStudio");
            if (Directory.Exists(vsBase))
            {
                foreach (var dir in Directory.GetDirectories(vsBase))
                {
                    var cmc = Path.Combine(dir, "ComponentModelCache");
                    if (Directory.Exists(cmc))
                        items.Add(new VsCacheItem($"VS {Path.GetFileName(dir)} ComponentModelCache", cmc, SafeFileOps.GetDirectorySize(cmc), true));
                }
            }
            // VS Code Cache
            var vsCodePaths = new[]
            {
                (Path.Combine(AppData, "Code", "Cache"), "VS Code — Cache"),
                (Path.Combine(AppData, "Code", "CachedData"), "VS Code — CachedData"),
                (Path.Combine(AppData, "Code", "CachedExtensionVSIXs"), "VS Code — CachedExtensionVSIXs"),
                (Path.Combine(AppData, "Code", "logs"), "VS Code — Logs"),
            };
            foreach (var (path, name) in vsCodePaths)
            {
                progress?.Report($"Escaneando: {name}");
                if (Directory.Exists(path))
                    items.Add(new VsCacheItem(name, path, SafeFileOps.GetDirectorySize(path), true));
            }
            return items;
        }, ct);
    }

    public Task<long> AnalyzeAsync(IProgress<string>? progress = null, CancellationToken ct = default)
        => Task.FromResult(0L);

    public async Task<CleaningResult> CleanAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var result = new CleaningResult { CategoryName = Name };
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var items = await ScanAsync(progress, ct);
        foreach (var item in items.Where(i => i.Exists))
        {
            ct.ThrowIfCancellationRequested();
            progress?.Report($"Limpiando: {item.Name}...");
            var (freed, err) = await SafeFileOps.DeleteDirectoryAsync(item.Path, null, ct);
            result.FreedBytes += freed;
            result.ItemsCleaned++;
            result.Errors += err;
        }
        sw.Stop();
        result.Duration = sw.Elapsed;
        return result;
    }

    public List<string> DetectInstalledVersions()
    {
        var versions = new List<string>();
        var vsBase = Path.Combine(LocalAppData, "Microsoft", "VisualStudio");
        if (Directory.Exists(vsBase))
            versions.AddRange(Directory.GetDirectories(vsBase).Select(Path.GetFileName).Where(n => n != null)!);
        if (Directory.Exists(Path.Combine(AppData, "Code")))
            versions.Add("VS Code");
        return versions;
    }
}
