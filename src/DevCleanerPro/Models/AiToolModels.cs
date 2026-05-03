using System.ComponentModel;

namespace DevCleanerPro.Models;

/// <summary>
/// Información de una herramienta de IA detectada en el sistema.
/// </summary>
public class AiToolInfo : INotifyPropertyChanged
{
    public string Name { get; set; } = "";
    public string ProcessName { get; set; } = "";
    public bool IsInstalled { get; set; }
    public bool IsRunning { get; set; }
    public string? InstalledVersion { get; set; }
    public long TotalSize { get; set; }
    public long SafeSize { get; set; }    // Eliminable sin riesgo
    public long UnsafeSize { get; set; }  // Requiere confirmación
    public int ModelCount { get; set; }
    public List<AiCacheItem> Items { get; set; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>
/// Un elemento individual de caché/log/modelo de una herramienta de IA.
/// </summary>
public class AiCacheItem : INotifyPropertyChanged
{
    private bool _isSelected;

    public string ToolName { get; set; } = "";
    public string Category { get; set; } = "";       // Cache, Logs, Models, Workspace, Crashpad, etc.
    public string Description { get; set; } = "";
    public string Path { get; set; } = "";
    public long Size { get; set; }
    public bool IsSafe { get; set; }                  // true = se puede eliminar sin riesgo
    public bool IsOrphan { get; set; }                // workspace huérfano, blob sin manifest, etc.
    public string? ProjectPath { get; set; }          // Para workspace items
    public bool ProjectExists { get; set; }           // Para workspace items
    public DateTime? LastModified { get; set; }
    public bool IsSelected { get => _isSelected; set { _isSelected = value; PropertyChanged?.Invoke(this, new(nameof(IsSelected))); } }

    public event PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>
/// Información de un modelo LLM (Ollama, LM Studio, HuggingFace).
/// </summary>
public class LlmModelInfo
{
    public string Source { get; set; } = "";           // Ollama, LMStudio, HuggingFace
    public string Name { get; set; } = "";
    public string? Tag { get; set; }                   // :7b, :latest, etc.
    public string? Quantization { get; set; }          // Q4_K_M, Q8_0, BF16, etc.
    public long Size { get; set; }
    public DateTime? LastAccessed { get; set; }
    public string Path { get; set; } = "";
    public string? CliCommand { get; set; }            // "ollama rm nombre:tag"
}
