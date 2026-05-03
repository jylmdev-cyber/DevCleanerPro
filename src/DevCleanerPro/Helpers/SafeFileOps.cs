namespace DevCleanerPro.Helpers;

/// <summary>
/// Operaciones de archivos seguras con manejo de errores integrado.
/// </summary>
public static class SafeFileOps
{
    /// <summary>
    /// Calcula el tamaño de un directorio de forma segura y recursiva.
    /// </summary>
    public static long GetDirectorySize(string path)
    {
        if (!Directory.Exists(path)) return 0;

        long totalSize = 0;

        try
        {
            var dirInfo = new DirectoryInfo(path);

            foreach (var file in dirInfo.EnumerateFiles("*", new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = true,
                AttributesToSkip = FileAttributes.ReparsePoint
            }))
            {
                try
                {
                    totalSize += file.Length;
                }
                catch
                {
                    // Archivo inaccesible, saltar
                }
            }
        }
        catch
        {
            // Directorio inaccesible
        }

        return totalSize;
    }

    /// <summary>
    /// Calcula el tamaño de un directorio de forma asíncrona.
    /// </summary>
    public static Task<long> GetDirectorySizeAsync(string path, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            return GetDirectorySize(path);
        }, ct);
    }

    /// <summary>
    /// Elimina un directorio de forma segura, retornando el espacio liberado.
    /// </summary>
    public static (long freedBytes, int errors) DeleteDirectory(string path, IProgress<string>? progress = null)
    {
        if (!Directory.Exists(path)) return (0, 0);

        long freedBytes = 0;
        int errors = 0;

        try
        {
            var dirInfo = new DirectoryInfo(path);

            // Primero calcular tamaño
            foreach (var file in dirInfo.EnumerateFiles("*", new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = true,
                AttributesToSkip = FileAttributes.ReparsePoint
            }))
            {
                try
                {
                    long size = file.Length;
                    // Quitar atributo de solo lectura si existe
                    if (file.Attributes.HasFlag(FileAttributes.ReadOnly))
                        file.Attributes &= ~FileAttributes.ReadOnly;

                    file.Delete();
                    freedBytes += size;
                    progress?.Report($"Eliminado: {file.FullName}");
                }
                catch
                {
                    errors++;
                }
            }

            // Luego eliminar directorios vacíos (de abajo hacia arriba)
            foreach (var dir in dirInfo.EnumerateDirectories("*", new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = true,
                AttributesToSkip = FileAttributes.ReparsePoint
            }).Reverse())
            {
                try
                {
                    if (!dir.EnumerateFileSystemInfos().Any())
                        dir.Delete();
                }
                catch
                {
                    errors++;
                }
            }

            // Finalmente eliminar el directorio raíz
            try
            {
                if (Directory.Exists(path) && !Directory.EnumerateFileSystemEntries(path).Any())
                    Directory.Delete(path);
            }
            catch
            {
                errors++;
            }
        }
        catch
        {
            errors++;
        }

        return (freedBytes, errors);
    }

    /// <summary>
    /// Elimina un directorio de forma asíncrona.
    /// </summary>
    public static Task<(long freedBytes, int errors)> DeleteDirectoryAsync(
        string path, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            return DeleteDirectory(path, progress);
        }, ct);
    }

    /// <summary>
    /// Verifica si un directorio existe y es accesible.
    /// </summary>
    public static bool IsDirectoryAccessible(string path)
    {
        try
        {
            if (!Directory.Exists(path)) return false;
            // Intentar acceder al directorio para verificar permisos
            Directory.EnumerateFileSystemEntries(path).GetEnumerator().MoveNext();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
