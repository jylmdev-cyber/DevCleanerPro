using Microsoft.Win32;
using DevCleanerPro.Models;

namespace DevCleanerPro.Services;

/// <summary>
/// Módulo 8: Limpiador de registro en modo seguro.
/// Solo entradas conocidas (MUICache, UserAssist, Run keys huérfanos).
/// </summary>
public class RegistryCleanerService : ICleanerModule
{
    public string Name => "Registry Cleaner";
    public string Description => "Limpia MUICache, UserAssist, y Run keys huérfanos (HKCU)";
    public string IconGlyph => "\uE74C";

    public record RegistryItem(string Category, string KeyPath, string ValueName, string Value, bool IsOrphan);

    public Task<long> AnalyzeAsync(IProgress<string>? progress = null, CancellationToken ct = default) => Task.FromResult(0L);

    public async Task<List<RegistryItem>> ScanAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            var items = new List<RegistryItem>();
            items.AddRange(ScanMuiCache(progress, ct));
            items.AddRange(ScanRunKeys(progress, ct));
            items.AddRange(ScanUserAssist(progress, ct));
            return items;
        }, ct);
    }

    private List<RegistryItem> ScanMuiCache(IProgress<string>? progress, CancellationToken ct)
    {
        var items = new List<RegistryItem>();
        const string path = @"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache";
        progress?.Report("Escaneando MUICache...");
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(path);
            if (key == null) return items;
            foreach (var name in key.GetValueNames())
            {
                ct.ThrowIfCancellationRequested();
                if (name.Contains(".exe", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains(".msc", StringComparison.OrdinalIgnoreCase))
                {
                    var exePath = name.Split(".FriendlyAppName")[0].Split(".ApplicationCompany")[0];
                    bool orphan = !File.Exists(exePath);
                    if (orphan)
                        items.Add(new RegistryItem("MUICache", path, name, key.GetValue(name)?.ToString() ?? "", orphan));
                }
            }
        }
        catch { }
        progress?.Report($"MUICache: {items.Count} entradas huérfanas");
        return items;
    }

    private List<RegistryItem> ScanRunKeys(IProgress<string>? progress, CancellationToken ct)
    {
        var items = new List<RegistryItem>();
        const string path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        progress?.Report("Escaneando Run keys...");
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(path);
            if (key == null) return items;
            foreach (var name in key.GetValueNames())
            {
                ct.ThrowIfCancellationRequested();
                var val = key.GetValue(name)?.ToString() ?? "";
                var exePath = ExtractPath(val);
                bool orphan = !string.IsNullOrEmpty(exePath) && !File.Exists(exePath);
                if (orphan)
                    items.Add(new RegistryItem("Run Key (HKCU)", path, name, val, true));
            }
        }
        catch { }
        progress?.Report($"Run keys: {items.Count} huérfanos");
        return items;
    }

    private List<RegistryItem> ScanUserAssist(IProgress<string>? progress, CancellationToken ct)
    {
        var items = new List<RegistryItem>();
        const string basePath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\UserAssist";
        progress?.Report("Escaneando UserAssist...");
        try
        {
            using var baseKey = Registry.CurrentUser.OpenSubKey(basePath);
            if (baseKey == null) return items;
            foreach (var subName in baseKey.GetSubKeyNames())
            {
                ct.ThrowIfCancellationRequested();
                using var countKey = baseKey.OpenSubKey($@"{subName}\Count");
                if (countKey == null) continue;
                int count = countKey.GetValueNames().Length;
                if (count > 0)
                    items.Add(new RegistryItem("UserAssist", $@"{basePath}\{subName}\Count", $"{count} entradas", "", false));
            }
        }
        catch { }
        progress?.Report($"UserAssist: {items.Count} subclaves con historial");
        return items;
    }

    public async Task<CleaningResult> CleanAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        return await CleanSelectedAsync(null, progress, ct);
    }

    public async Task<CleaningResult> CleanSelectedAsync(List<RegistryItem>? selectedItems = null, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var result = new CleaningResult { CategoryName = Name };
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var items = selectedItems ?? await ScanAsync(progress, ct);

        await Task.Run(() =>
        {
            foreach (var item in items.Where(i => i.IsOrphan))
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    using var key = Registry.CurrentUser.OpenSubKey(item.KeyPath, writable: true);
                    if (key != null && !string.IsNullOrEmpty(item.ValueName) && !item.ValueName.Contains("entradas"))
                    {
                        key.DeleteValue(item.ValueName, throwOnMissingValue: false);
                        result.ItemsCleaned++;
                        progress?.Report($"✓ Eliminado: {item.ValueName}");
                    }
                }
                catch (Exception ex) { result.Errors++; result.ErrorMessages.Add(ex.Message); }
            }
        }, ct);
        sw.Stop();
        result.Duration = sw.Elapsed;
        return result;
    }

    private static string ExtractPath(string value)
    {
        var v = value.Trim().Trim('"');
        var spaceIdx = v.IndexOf(".exe ", StringComparison.OrdinalIgnoreCase);
        if (spaceIdx > 0) v = v[..(spaceIdx + 4)];
        return v;
    }
}
