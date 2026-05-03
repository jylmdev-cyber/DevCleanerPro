namespace DevCleanerPro.Models;

/// <summary>
/// Representa una ubicación de caché de un gestor de paquetes.
/// </summary>
public class CacheEntry
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long Size { get; set; }
    public bool Exists { get; set; }
    public bool IsSelected { get; set; }
    public string IconGlyph { get; set; } = "\uE74D";
    public DateTime? LastModified { get; set; }
}
