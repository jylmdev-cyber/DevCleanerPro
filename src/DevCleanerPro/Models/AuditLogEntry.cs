namespace DevCleanerPro.Models;

/// <summary>
/// Entrada individual del log de auditoría.
/// </summary>
public class AuditLogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public long BytesFreed { get; set; }
    public int FilesDeleted { get; set; }
    public string Status { get; set; } = "OK";
    public string? Error { get; set; }
}
