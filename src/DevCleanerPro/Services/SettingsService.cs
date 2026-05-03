using System.Text.Json;
using DevCleanerPro.Models;

namespace DevCleanerPro.Services;

/// <summary>
/// Servicio de persistencia de configuración en formato JSON.
/// Almacena el archivo de settings junto al ejecutable para mantener la portabilidad.
/// </summary>
public class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "devcleanerpro-settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private AppSettings? _cachedSettings;

    public AppSettings Load()
    {
        if (_cachedSettings != null) return _cachedSettings;

        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                _cachedSettings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }
            else
            {
                _cachedSettings = new AppSettings();
            }
        }
        catch
        {
            _cachedSettings = new AppSettings();
        }

        return _cachedSettings;
    }

    public void Save(AppSettings settings)
    {
        try
        {
            _cachedSettings = settings;
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Silently fail if we can't write settings
        }
    }

    public void UpdateLastScan(long totalFreed)
    {
        var settings = Load();
        settings.LastScanDate = DateTime.Now;
        settings.LastTotalFreed = totalFreed;
        Save(settings);
    }
}
