using Microsoft.Win32;

namespace DevCleanerPro.Services;

/// <summary>
/// Módulo 10: Gestor de programas de inicio.
/// </summary>
public class StartupManagerService
{
    public record StartupEntry(string Name, string Command, string Source, string KeyPath, bool IsEnabled, string? Publisher);

    private static readonly (string Path, string Source, RegistryKey Root)[] RegistryPaths =
    {
        (@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "HKCU", Registry.CurrentUser),
        (@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "HKLM", Registry.LocalMachine),
    };

    public List<StartupEntry> GetAllEntries()
    {
        var entries = new List<StartupEntry>();
        foreach (var (path, source, root) in RegistryPaths)
        {
            try
            {
                using var key = root.OpenSubKey(path);
                if (key == null) continue;
                foreach (var name in key.GetValueNames())
                {
                    var cmd = key.GetValue(name)?.ToString() ?? "";
                    bool enabled = !name.StartsWith("~disabled~");
                    var displayName = enabled ? name : name.Replace("~disabled~", "");
                    entries.Add(new StartupEntry(displayName, cmd, source, path, enabled, null));
                }
            }
            catch { }
        }
        // Shell startup folders
        var folders = new[]
        {
            (Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup)), "AllUsers"),
            (Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup)), "User"),
        };
        foreach (var (folder, source) in folders)
        {
            if (!Directory.Exists(folder)) continue;
            foreach (var file in Directory.GetFiles(folder))
                entries.Add(new StartupEntry(Path.GetFileNameWithoutExtension(file), file, source, folder, true, null));
        }
        return entries;
    }

    public bool DisableEntry(StartupEntry entry)
    {
        if (entry.Source is "User" or "AllUsers") return false; // Folder-based, just rename
        try
        {
            var root = entry.Source == "HKCU" ? Registry.CurrentUser : Registry.LocalMachine;
            using var key = root.OpenSubKey(entry.KeyPath, writable: true);
            if (key == null) return false;
            var val = key.GetValue(entry.Name);
            key.DeleteValue(entry.Name);
            key.SetValue($"~disabled~{entry.Name}", val!);
            return true;
        }
        catch { return false; }
    }

    public bool EnableEntry(StartupEntry entry)
    {
        try
        {
            var root = entry.Source == "HKCU" ? Registry.CurrentUser : Registry.LocalMachine;
            using var key = root.OpenSubKey(entry.KeyPath, writable: true);
            if (key == null) return false;
            var disabledName = $"~disabled~{entry.Name}";
            var val = key.GetValue(disabledName);
            if (val == null) return false;
            key.DeleteValue(disabledName);
            key.SetValue(entry.Name, val);
            return true;
        }
        catch { return false; }
    }

    public bool DeleteEntry(StartupEntry entry)
    {
        if (entry.Source is "User" or "AllUsers")
        {
            try { File.Delete(entry.Command); return true; } catch { return false; }
        }
        try
        {
            var root = entry.Source == "HKCU" ? Registry.CurrentUser : Registry.LocalMachine;
            using var key = root.OpenSubKey(entry.KeyPath, writable: true);
            key?.DeleteValue(entry.Name, throwOnMissingValue: false);
            key?.DeleteValue($"~disabled~{entry.Name}", throwOnMissingValue: false);
            return true;
        }
        catch { return false; }
    }

    public void OpenLocation(StartupEntry entry)
    {
        var path = entry.Command.Trim().Trim('"');
        var idx = path.IndexOf(".exe ", StringComparison.OrdinalIgnoreCase);
        if (idx > 0) path = path[..(idx + 4)];
        if (File.Exists(path))
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{path}\"");
        else if (Directory.Exists(entry.KeyPath))
            System.Diagnostics.Process.Start("explorer.exe", entry.KeyPath);
    }
}
