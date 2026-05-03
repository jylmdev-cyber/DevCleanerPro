using DevCleanerPro.Helpers;
using DevCleanerPro.Models;

namespace DevCleanerPro.Services;

public class TempFileService : ICleanerModule
{
    public string Name => "Temp Cleaner";
    public string Description => "Limpia archivos temporales del sistema";
    public string IconGlyph => "\uE74D";

    private static string LocalAppData => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private static string WindowsDir => Environment.GetFolderPath(Environment.SpecialFolder.Windows);

    public async Task<long> AnalyzeAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var cats = await GetTempCategoriesAsync(progress, ct);
        return cats.Sum(c => c.Size);
    }

    public async Task<CleaningResult> CleanAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var cats = await GetTempCategoriesAsync(progress, ct);
        cats.ForEach(c => c.IsSelected = true);
        return await CleanTempFilesAsync(cats, progress, ct);
    }

    public async Task<List<TempFileCategory>> GetTempCategoriesAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var categories = GetTempDefinitions();
        await Task.Run(() =>
        {
            foreach (var cat in categories)
            {
                ct.ThrowIfCancellationRequested();
                progress?.Report($"Analizando: {cat.Name}...");
                cat.Exists = Directory.Exists(cat.Path);
                if (cat.Exists)
                {
                    cat.Size = SafeFileOps.GetDirectorySize(cat.Path);
                    try { cat.FileCount = Directory.EnumerateFiles(cat.Path, "*", new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = true, AttributesToSkip = FileAttributes.ReparsePoint }).Count(); } catch { cat.FileCount = 0; }
                }
            }
        }, ct);
        return categories;
    }

    public async Task<CleaningResult> CleanTempFilesAsync(List<TempFileCategory> selectedCategories, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var result = new CleaningResult { CategoryName = "Archivos temporales" };
        var sw = System.Diagnostics.Stopwatch.StartNew();
        foreach (var cat in selectedCategories.Where(c => c.IsSelected && c.Exists))
        {
            ct.ThrowIfCancellationRequested();
            progress?.Report($"Limpiando: {cat.Name}...");
            await Task.Run(() =>
            {
                try
                {
                    var dirInfo = new DirectoryInfo(cat.Path);
                    foreach (var file in dirInfo.EnumerateFiles("*", new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = true, AttributesToSkip = FileAttributes.ReparsePoint }))
                    {
                        try { long size = file.Length; file.Delete(); result.FreedBytes += size; result.ItemsCleaned++; } catch { result.Errors++; }
                    }
                } catch { }
            }, ct);
        }
        sw.Stop();
        result.Duration = sw.Elapsed;
        return result;
    }

    private List<TempFileCategory> GetTempDefinitions() => new()
    {
        new() { Name = "Temp del usuario", Description = "Archivos temporales del usuario actual", Path = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar), RequiresAdmin = false },
        new() { Name = "Temp de Windows", Description = "Archivos temporales del sistema", Path = Path.Combine(WindowsDir, "Temp"), RequiresAdmin = true },
        new() { Name = "Windows Update Cache", Description = "Archivos de Windows Update", Path = Path.Combine(WindowsDir, "SoftwareDistribution", "Download"), RequiresAdmin = true },
        new() { Name = "Thumbnails Cache", Description = "Miniaturas del explorador", Path = Path.Combine(LocalAppData, "Microsoft", "Windows", "Explorer"), RequiresAdmin = false },
        new() { Name = "Crash Dumps", Description = "Volcados de memoria", Path = Path.Combine(LocalAppData, "CrashDumps"), RequiresAdmin = false },
        new() { Name = "Prefetch", Description = "Datos de precarga", Path = Path.Combine(WindowsDir, "Prefetch"), RequiresAdmin = true },
    };
}
