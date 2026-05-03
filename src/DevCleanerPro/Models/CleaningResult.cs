namespace DevCleanerPro.Models;

/// <summary>
/// Resultado de una operación de limpieza.
/// </summary>
public class CleaningResult
{
    public string CategoryName { get; set; } = string.Empty;
    public long FreedBytes { get; set; }
    public int ItemsCleaned { get; set; }
    public int Errors { get; set; }
    public List<string> ErrorMessages { get; set; } = new();
    public TimeSpan Duration { get; set; }
    public bool Success => Errors == 0;
}

/// <summary>
/// Resultado agregado de múltiples limpiezas.
/// </summary>
public class CleaningSummary
{
    public long TotalFreedBytes { get; set; }
    public int TotalItemsCleaned { get; set; }
    public int TotalErrors { get; set; }
    public List<CleaningResult> Results { get; set; } = new();
    public TimeSpan TotalDuration { get; set; }
}
