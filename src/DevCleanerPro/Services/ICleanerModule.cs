using DevCleanerPro.Models;

namespace DevCleanerPro.Services;

/// <summary>
/// Interfaz base para todos los módulos de limpieza.
/// </summary>
public interface ICleanerModule
{
    string Name { get; }
    string Description { get; }
    string IconGlyph { get; }
    Task<long> AnalyzeAsync(IProgress<string>? progress = null, CancellationToken ct = default);
    Task<CleaningResult> CleanAsync(IProgress<string>? progress = null, CancellationToken ct = default);
}
