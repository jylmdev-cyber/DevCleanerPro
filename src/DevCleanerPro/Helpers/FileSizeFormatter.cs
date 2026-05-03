namespace DevCleanerPro.Helpers;

/// <summary>
/// Formateador de tamaños de archivo a formato legible.
/// </summary>
public static class FileSizeFormatter
{
    private static readonly string[] SizeSuffixes = { "B", "KB", "MB", "GB", "TB", "PB" };

    public static string Format(long bytes)
    {
        if (bytes < 0) return "0 B";
        if (bytes == 0) return "0 B";

        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < SizeSuffixes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return order == 0
            ? $"{size:N0} {SizeSuffixes[order]}"
            : $"{size:N2} {SizeSuffixes[order]}";
    }

    public static string FormatCompact(long bytes)
    {
        if (bytes < 0) return "0";
        if (bytes == 0) return "0 B";

        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < SizeSuffixes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return order == 0
            ? $"{size:N0} {SizeSuffixes[order]}"
            : $"{size:N1} {SizeSuffixes[order]}";
    }
}
