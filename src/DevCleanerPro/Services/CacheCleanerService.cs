using DevCleanerPro.Helpers;
using DevCleanerPro.Models;

namespace DevCleanerPro.Services;

/// <summary>
/// Servicio para detectar y limpiar cachés de gestores de paquetes.
/// </summary>
public class CacheCleanerService
{
    private static string UserProfile => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private static string AppData => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    private static string LocalAppData => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    /// <summary>
    /// Obtiene todas las ubicaciones de caché conocidas con su tamaño.
    /// </summary>
    public async Task<List<CacheEntry>> GetCacheEntriesAsync(
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        var entries = GetCacheDefinitions();

        await Task.Run(() =>
        {
            foreach (var entry in entries)
            {
                ct.ThrowIfCancellationRequested();
                progress?.Report($"Analizando: {entry.Name}...");

                entry.Exists = Directory.Exists(entry.Path);
                if (entry.Exists)
                {
                    entry.Size = SafeFileOps.GetDirectorySize(entry.Path);
                    try
                    {
                        entry.LastModified = Directory.GetLastWriteTime(entry.Path);
                    }
                    catch { }
                }
            }
        }, ct);

        return entries;
    }

    /// <summary>
    /// Limpia las cachés seleccionadas.
    /// </summary>
    public async Task<CleaningResult> CleanCachesAsync(
        List<CacheEntry> selectedCaches,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        var result = new CleaningResult { CategoryName = "Cachés de paquetes" };
        var sw = System.Diagnostics.Stopwatch.StartNew();

        foreach (var cache in selectedCaches.Where(c => c.IsSelected && c.Exists))
        {
            ct.ThrowIfCancellationRequested();
            progress?.Report($"Limpiando: {cache.Name}...");

            var (freed, errors) = await SafeFileOps.DeleteDirectoryAsync(cache.Path, progress, ct);
            result.FreedBytes += freed;
            result.ItemsCleaned++;
            result.Errors += errors;
        }

        sw.Stop();
        result.Duration = sw.Elapsed;
        return result;
    }

    private List<CacheEntry> GetCacheDefinitions()
    {
        var goPath = Environment.GetEnvironmentVariable("GOPATH") ?? Path.Combine(UserProfile, "go");

        return new List<CacheEntry>
        {
            new()
            {
                Name = "NuGet — Paquetes globales",
                Description = "Paquetes NuGet descargados globalmente",
                Path = Path.Combine(UserProfile, ".nuget", "packages"),
                IconGlyph = "\uE74D"
            },
            new()
            {
                Name = "NuGet — HTTP Cache",
                Description = "Caché HTTP de NuGet v3",
                Path = Path.Combine(LocalAppData, "NuGet", "v3-cache"),
                IconGlyph = "\uE74D"
            },
            new()
            {
                Name = "npm — Cache",
                Description = "Paquetes npm descargados en caché",
                Path = Path.Combine(AppData, "npm-cache"),
                IconGlyph = "\uE74D"
            },
            new()
            {
                Name = "Yarn — Cache",
                Description = "Caché del gestor de paquetes Yarn",
                Path = Path.Combine(LocalAppData, "Yarn", "Cache"),
                IconGlyph = "\uE74D"
            },
            new()
            {
                Name = "pnpm — Store",
                Description = "Content-addressable store de pnpm",
                Path = Path.Combine(LocalAppData, "pnpm-store"),
                IconGlyph = "\uE74D"
            },
            new()
            {
                Name = "pip — Cache",
                Description = "Caché de paquetes Python pip",
                Path = Path.Combine(LocalAppData, "pip", "cache"),
                IconGlyph = "\uE74D"
            },
            new()
            {
                Name = "Gradle — Caches",
                Description = "Caché de dependencias Gradle",
                Path = Path.Combine(UserProfile, ".gradle", "caches"),
                IconGlyph = "\uE74D"
            },
            new()
            {
                Name = "Maven — Repository",
                Description = "Repositorio local de Maven (.m2)",
                Path = Path.Combine(UserProfile, ".m2", "repository"),
                IconGlyph = "\uE74D"
            },
            new()
            {
                Name = "Go — Module Cache",
                Description = "Caché de módulos Go",
                Path = Path.Combine(goPath, "pkg", "mod", "cache"),
                IconGlyph = "\uE74D"
            },
            new()
            {
                Name = "Cargo — Registry Cache",
                Description = "Caché del registro de Cargo (Rust)",
                Path = Path.Combine(UserProfile, ".cargo", "registry", "cache"),
                IconGlyph = "\uE74D"
            },
            new()
            {
                Name = "Composer — Cache",
                Description = "Caché de paquetes PHP Composer",
                Path = Path.Combine(LocalAppData, "Composer", "cache"),
                IconGlyph = "\uE74D"
            },
        };
    }
}
