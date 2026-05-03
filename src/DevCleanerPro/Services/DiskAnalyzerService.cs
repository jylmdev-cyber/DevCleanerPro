using DevCleanerPro.Helpers;

namespace DevCleanerPro.Services;

/// <summary>
/// Módulo 11: Analizador de disco con escaneo asíncrono.
/// </summary>
public class DiskAnalyzerService
{
    public record FileItem(string Path, long Size, DateTime LastModified, string Extension);

    public async Task<List<FileItem>> ScanLargestFilesAsync(
        string rootPath, int topN, long minSizeBytes,
        IProgress<string>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            var files = new List<FileItem>();
            ScanDir(new DirectoryInfo(rootPath), files, minSizeBytes, progress, ct, 0, 10);
            return files.OrderByDescending(f => f.Size).Take(topN).ToList();
        }, ct);
    }

    private void ScanDir(DirectoryInfo dir, List<FileItem> results, long minSize,
        IProgress<string>? progress, CancellationToken ct, int depth, int maxDepth)
    {
        if (depth > maxDepth) return;
        ct.ThrowIfCancellationRequested();
        try
        {
            progress?.Report($"Escaneando: {dir.FullName}");
            foreach (var file in dir.EnumerateFiles("*", new EnumerationOptions
                { IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint }))
            {
                try
                {
                    if (file.Length >= minSize)
                        results.Add(new FileItem(file.FullName, file.Length, file.LastWriteTime, file.Extension.ToLowerInvariant()));
                }
                catch { }
            }
            foreach (var sub in dir.EnumerateDirectories("*", new EnumerationOptions
                { IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint }))
            {
                ScanDir(sub, results, minSize, progress, ct, depth + 1, maxDepth);
            }
        }
        catch { }
    }

    public async Task<Dictionary<string, long>> GetFolderSizesAsync(
        string rootPath, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            var sizes = new Dictionary<string, long>();
            try
            {
                foreach (var dir in new DirectoryInfo(rootPath).EnumerateDirectories("*", new EnumerationOptions
                    { IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint }))
                {
                    ct.ThrowIfCancellationRequested();
                    progress?.Report($"Calculando: {dir.Name}");
                    sizes[dir.FullName] = SafeFileOps.GetDirectorySize(dir.FullName);
                }
            }
            catch { }
            return sizes;
        }, ct);
    }

    /// <summary>
    /// Obtiene información de todas las unidades del sistema (para el Dashboard).
    /// </summary>
    public List<Models.DriveInfoModel> GetDriveInfo()
    {
        return DriveInfo.GetDrives()
            .Where(d => d.IsReady)
            .Select(d => new Models.DriveInfoModel
            {
                Name = d.Name,
                Label = d.VolumeLabel,
                DriveType = d.DriveType.ToString(),
                FileSystem = d.DriveFormat,
                TotalSize = d.TotalSize,
                FreeSpace = d.AvailableFreeSpace,
            })
            .ToList();
    }
}
