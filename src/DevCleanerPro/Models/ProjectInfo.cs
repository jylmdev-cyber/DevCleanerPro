namespace DevCleanerPro.Models;

/// <summary>
/// Representa un proyecto de desarrollo detectado en el sistema.
/// </summary>
public class ProjectInfo
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public ProjectType Type { get; set; }
    public long TotalSize { get; set; }
    public long CleanableSize { get; set; }
    public List<CleanableFolder> CleanableFolders { get; set; } = new();
    public bool IsSelected { get; set; } = true;
    public DateTime LastModified { get; set; }

    public string TypeIcon => Type switch
    {
        ProjectType.DotNet => "⚙️",
        ProjectType.NodeJs => "📦",
        ProjectType.Rust => "🦀",
        ProjectType.Java => "☕",
        ProjectType.Python => "🐍",
        ProjectType.Go => "🐹",
        _ => "📂"
    };

    public string TypeName => Type switch
    {
        ProjectType.DotNet => ".NET",
        ProjectType.NodeJs => "Node.js",
        ProjectType.Rust => "Rust",
        ProjectType.Java => "Java/Maven/Gradle",
        ProjectType.Python => "Python",
        ProjectType.Go => "Go",
        _ => "Desconocido"
    };
}

public class CleanableFolder
{
    public string Path { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long Size { get; set; }
    public bool IsSelected { get; set; } = true;
}

public enum ProjectType
{
    Unknown,
    DotNet,
    NodeJs,
    Rust,
    Java,
    Python,
    Go
}
