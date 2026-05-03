using System.Collections.ObjectModel;
using Microsoft.Win32;

namespace DevCleanerPro.ViewModels;

public class EnvironmentToolsViewModel : ViewModelBase
{
    private string _statusText = "Herramientas del entorno";
    private bool _isWorking;

    public ObservableCollection<EnvVariableItem> PathEntries { get; } = new();
    public ObservableCollection<EnvVariableItem> DuplicatePaths { get; } = new();
    public ObservableCollection<EnvVariableItem> InvalidPaths { get; } = new();
    public ObservableCollection<StartupItem> StartupPrograms { get; } = new();
    public ObservableCollection<LargeFileItem> LargeFiles { get; } = new();

    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
    public bool IsWorking { get => _isWorking; set => SetProperty(ref _isWorking, value); }

    public RelayCommand AnalyzePathCommand => new(() => AnalyzePath());
    public RelayCommand LoadStartupCommand => new(() => LoadStartupPrograms());
    public AsyncRelayCommand ScanLargeFilesCommand => new(ScanLargeFilesAsync);

    public void AnalyzePath()
    {
        PathEntries.Clear(); DuplicatePaths.Clear(); InvalidPaths.Clear();
        var pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
        var entries = pathVar.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            var item = new EnvVariableItem { Name = entry, Value = Directory.Exists(entry) ? "Válido" : "No existe" };
            PathEntries.Add(item);
            if (!seen.Add(entry)) DuplicatePaths.Add(new EnvVariableItem { Name = entry, Value = "DUPLICADO" });
            if (!Directory.Exists(entry)) InvalidPaths.Add(new EnvVariableItem { Name = entry, Value = "NO EXISTE" });
        }
        StatusText = $"PATH: {entries.Length} entradas, {DuplicatePaths.Count} duplicados, {InvalidPaths.Count} inválidos";
    }

    public void LoadStartupPrograms()
    {
        StartupPrograms.Clear();
        var keys = new[] {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce"
        };
        foreach (var keyPath in keys)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(keyPath);
                if (key == null) continue;
                foreach (var name in key.GetValueNames())
                {
                    StartupPrograms.Add(new StartupItem { Name = name, Command = key.GetValue(name)?.ToString() ?? "", Source = "HKLM" });
                }
            } catch { }
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(keyPath);
                if (key == null) continue;
                foreach (var name in key.GetValueNames())
                {
                    StartupPrograms.Add(new StartupItem { Name = name, Command = key.GetValue(name)?.ToString() ?? "", Source = "HKCU" });
                }
            } catch { }
        }
        StatusText = $"{StartupPrograms.Count} programas de inicio detectados";
    }

    public async Task ScanLargeFilesAsync()
    {
        IsWorking = true;
        LargeFiles.Clear();
        StatusText = "Escaneando archivos grandes...";
        var threshold = 100L * 1024 * 1024; // 100MB
        await Task.Run(() =>
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            try
            {
                foreach (var file in new DirectoryInfo(userProfile).EnumerateFiles("*", new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = true, MaxRecursionDepth = 5, AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.System }))
                {
                    try
                    {
                        if (file.Length >= threshold)
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                LargeFiles.Add(new LargeFileItem { Path = file.FullName, Size = file.Length, LastModified = file.LastWriteTime }));
                        }
                    } catch { }
                }
            } catch { }
        });
        StatusText = $"{LargeFiles.Count} archivos grandes encontrados (>100 MB)";
        IsWorking = false;
    }
}

public class EnvVariableItem { public string Name { get; set; } = ""; public string Value { get; set; } = ""; }
public class StartupItem { public string Name { get; set; } = ""; public string Command { get; set; } = ""; public string Source { get; set; } = ""; }
public class LargeFileItem { public string Path { get; set; } = ""; public long Size { get; set; } public DateTime LastModified { get; set; } public string SizeText => Helpers.FileSizeFormatter.Format(Size); }
