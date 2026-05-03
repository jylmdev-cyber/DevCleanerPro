using System.Collections.ObjectModel;
using DevCleanerPro.Helpers;
using DevCleanerPro.Services;

namespace DevCleanerPro.ViewModels;

public class DiskAnalyzerViewModel : ViewModelBase
{
    private readonly DiskAnalyzerService _svc = new();
    private string _statusText = "Seleccione una carpeta para analizar";
    private bool _isWorking;
    private string _scanPath = "";

    public ObservableCollection<DiskAnalyzerService.FileItem> LargeFiles { get; } = new();
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
    public bool IsWorking { get => _isWorking; set => SetProperty(ref _isWorking, value); }
    public string ScanPath { get => _scanPath; set => SetProperty(ref _scanPath, value); }

    public async Task ScanAsync(long minSizeMb = 50)
    {
        if (string.IsNullOrWhiteSpace(ScanPath) || !Directory.Exists(ScanPath)) { StatusText = "Ruta inválida"; return; }
        IsWorking = true; LargeFiles.Clear();
        var progress = new Progress<string>(m => StatusText = m);
        var files = await _svc.ScanLargestFilesAsync(ScanPath, 50, minSizeMb * 1024 * 1024, progress);
        foreach (var f in files) LargeFiles.Add(f);
        StatusText = $"{files.Count} archivos encontrados (>{minSizeMb} MB)";
        IsWorking = false;
    }

    public bool DeleteFile(string path)
    {
        try { File.Delete(path); return true; } catch { return false; }
    }
}
