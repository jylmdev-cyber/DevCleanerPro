using DevCleanerPro.Helpers;
using DevCleanerPro.Models;

namespace DevCleanerPro.Services;

/// <summary>
/// Servicio para escanear recursivamente una carpeta raíz y detectar
/// proyectos de desarrollo con sus carpetas limpiables.
/// </summary>
public class ProjectScannerService
{
    // Mapeo de archivos indicadores → tipo de proyecto
    private static readonly Dictionary<string, ProjectType> ProjectIndicators = new()
    {
        { "*.sln", ProjectType.DotNet },
        { "*.csproj", ProjectType.DotNet },
        { "*.fsproj", ProjectType.DotNet },
        { "package.json", ProjectType.NodeJs },
        { "Cargo.toml", ProjectType.Rust },
        { "pom.xml", ProjectType.Java },
        { "build.gradle", ProjectType.Java },
        { "go.mod", ProjectType.Go },
        { "requirements.txt", ProjectType.Python },
        { "pyproject.toml", ProjectType.Python },
        { "setup.py", ProjectType.Python },
    };

    // Carpetas limpiables por tipo de proyecto
    private static readonly Dictionary<ProjectType, string[]> CleanableFoldersByType = new()
    {
        { ProjectType.DotNet, new[] { "bin", "obj", ".vs", "packages", "TestResults" } },
        { ProjectType.NodeJs, new[] { "node_modules", "dist", "build", ".next", ".nuxt", ".output", "coverage" } },
        { ProjectType.Rust, new[] { "target" } },
        { ProjectType.Java, new[] { "target", "build", ".gradle" } },
        { ProjectType.Python, new[] { "__pycache__", ".venv", "venv", "env", ".tox", "dist", "build", "*.egg-info" } },
        { ProjectType.Go, new[] { "vendor" } },
    };

    /// <summary>
    /// Escanea un directorio raíz buscando proyectos de desarrollo.
    /// </summary>
    public async Task<List<ProjectInfo>> ScanAsync(
        string rootPath,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        var projects = new List<ProjectInfo>();

        await Task.Run(() =>
        {
            ScanDirectory(rootPath, projects, progress, ct, maxDepth: 6, currentDepth: 0);
        }, ct);

        return projects;
    }

    private void ScanDirectory(
        string path,
        List<ProjectInfo> results,
        IProgress<string>? progress,
        CancellationToken ct,
        int maxDepth,
        int currentDepth)
    {
        ct.ThrowIfCancellationRequested();

        if (currentDepth > maxDepth) return;
        if (!Directory.Exists(path)) return;

        // Detectar tipo de proyecto en este directorio
        var detectedType = DetectProjectType(path);

        if (detectedType != ProjectType.Unknown)
        {
            progress?.Report($"Proyecto encontrado: {path}");

            var project = new ProjectInfo
            {
                Name = Path.GetFileName(path),
                Path = path,
                Type = detectedType,
                LastModified = Directory.GetLastWriteTime(path)
            };

            // Buscar carpetas limpiables
            var cleanableFolders = FindCleanableFolders(path, detectedType);
            project.CleanableFolders = cleanableFolders;
            project.CleanableSize = cleanableFolders.Sum(f => f.Size);

            if (project.CleanableSize > 0)
            {
                results.Add(project);
            }

            return; // No escanear subdirectorios de un proyecto detectado
        }

        // Si no es un proyecto, seguir escaneando subdirectorios
        try
        {
            foreach (var dir in Directory.EnumerateDirectories(path))
            {
                ct.ThrowIfCancellationRequested();

                var dirName = Path.GetFileName(dir);

                // Saltar directorios conocidos que no contienen proyectos
                if (ShouldSkipDirectory(dirName)) continue;

                progress?.Report($"Escaneando: {dir}");
                ScanDirectory(dir, results, progress, ct, maxDepth, currentDepth + 1);
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (DirectoryNotFoundException) { }
    }

    private ProjectType DetectProjectType(string path)
    {
        try
        {
            foreach (var (pattern, type) in ProjectIndicators)
            {
                if (pattern.StartsWith("*."))
                {
                    if (Directory.EnumerateFiles(path, pattern, SearchOption.TopDirectoryOnly).Any())
                        return type;
                }
                else
                {
                    if (File.Exists(Path.Combine(path, pattern)))
                        return type;
                }
            }
        }
        catch { }

        return ProjectType.Unknown;
    }

    private List<CleanableFolder> FindCleanableFolders(string projectPath, ProjectType type)
    {
        var folders = new List<CleanableFolder>();

        if (!CleanableFoldersByType.TryGetValue(type, out var folderNames))
            return folders;

        foreach (var folderName in folderNames)
        {
            try
            {
                // Buscar la carpeta directamente y en subdirectorios (para bin/obj en proyectos multi-csproj)
                var matches = Directory.EnumerateDirectories(projectPath, folderName, new EnumerationOptions
                {
                    IgnoreInaccessible = true,
                    RecurseSubdirectories = true,
                    MaxRecursionDepth = 4,
                    AttributesToSkip = FileAttributes.ReparsePoint
                });

                foreach (var match in matches)
                {
                    var size = SafeFileOps.GetDirectorySize(match);
                    if (size > 0)
                    {
                        folders.Add(new CleanableFolder
                        {
                            Name = Path.GetFileName(match),
                            Path = match,
                            Size = size,
                            IsSelected = true
                        });
                    }
                }
            }
            catch { }
        }

        return folders;
    }

    private static bool ShouldSkipDirectory(string name)
    {
        return name.StartsWith('.') ||
               name.Equals("node_modules", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("target", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("__pycache__", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("$Recycle.Bin", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("Windows", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("Program Files", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("Program Files (x86)", StringComparison.OrdinalIgnoreCase);
    }
}
