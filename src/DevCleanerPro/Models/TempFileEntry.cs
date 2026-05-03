namespace DevCleanerPro.Models;

/// <summary>
/// Representa una categoría de archivos temporales del sistema.
/// </summary>
public class TempFileCategory
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long Size { get; set; }
    public int FileCount { get; set; }
    public bool Exists { get; set; }
    public bool IsSelected { get; set; }
    public bool RequiresAdmin { get; set; }
    public string IconGlyph { get; set; } = "\uE74D";
}
