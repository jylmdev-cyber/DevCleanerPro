using System.Collections.ObjectModel;
using DevCleanerPro.Helpers;
using DevCleanerPro.Models;
using DevCleanerPro.Services;

namespace DevCleanerPro.ViewModels;

public class ProjectScannerViewModel : ViewModelBase
{
    private readonly ProjectScannerService _scanner = new();
    private string _scanPath = "";
    private string _statusText = "Seleccione una carpeta para escanear";
    private bool _isScanning;
    private long _totalCleanable;
    private int _projectCount;
    private CancellationTokenSource? _cts;

    public ObservableCollection<ProjectInfo> Projects { get; } = new();

    public string ScanPath { get => _scanPath; set => SetProperty(ref _scanPath, value); }
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
    public bool IsScanning { get => _isScanning; set => SetProperty(ref _isScanning, value); }
    public long TotalCleanable { get => _totalCleanable; set { SetProperty(ref _totalCleanable, value); OnPropertyChanged(nameof(TotalCleanableText)); } }
    public string TotalCleanableText => FileSizeFormatter.Format(TotalCleanable);
    public int ProjectCount { get => _projectCount; set => SetProperty(ref _projectCount, value); }

    public AsyncRelayCommand ScanCommand => new(ScanAsync, () => !IsScanning && !string.IsNullOrEmpty(ScanPath));
    public AsyncRelayCommand CleanCommand => new(CleanAsync, () => !IsScanning && Projects.Any(p => p.IsSelected));
    public RelayCommand CancelCommand => new(() => _cts?.Cancel(), () => IsScanning);
    public RelayCommand SelectAllCommand => new(() => { foreach (var p in Projects) p.IsSelected = true; OnPropertyChanged(nameof(Projects)); });
    public RelayCommand DeselectAllCommand => new(() => { foreach (var p in Projects) p.IsSelected = false; OnPropertyChanged(nameof(Projects)); });

    public async Task ScanAsync()
    {
        if (string.IsNullOrWhiteSpace(ScanPath) || !Directory.Exists(ScanPath)) { StatusText = "Ruta inválida"; return; }
        _cts = new CancellationTokenSource();
        IsScanning = true;
        Projects.Clear();
        TotalCleanable = 0;
        ProjectCount = 0;
        var progress = new Progress<string>(msg => StatusText = msg);
        try
        {
            var results = await _scanner.ScanAsync(ScanPath, progress, _cts.Token);
            foreach (var p in results.OrderByDescending(p => p.CleanableSize)) Projects.Add(p);
            TotalCleanable = results.Sum(p => p.CleanableSize);
            ProjectCount = results.Count;
            StatusText = $"Escaneo completo: {ProjectCount} proyectos, {TotalCleanableText} recuperable";
        }
        catch (OperationCanceledException) { StatusText = "Escaneo cancelado"; }
        catch (Exception ex) { StatusText = $"Error: {ex.Message}"; }
        finally { IsScanning = false; _cts?.Dispose(); _cts = null; }
    }

    public async Task CleanAsync()
    {
        var selected = Projects.Where(p => p.IsSelected).ToList();
        if (!selected.Any()) return;
        _cts = new CancellationTokenSource();
        IsScanning = true;
        long totalFreed = 0;
        int errors = 0;
        var progress = new Progress<string>(msg => StatusText = msg);
        try
        {
            foreach (var project in selected)
            {
                foreach (var folder in project.CleanableFolders.Where(f => f.IsSelected))
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    StatusText = $"Limpiando: {project.Name}/{folder.Name}...";
                    var (freed, err) = await SafeFileOps.DeleteDirectoryAsync(folder.Path, progress, _cts.Token);
                    totalFreed += freed;
                    errors += err;
                }
            }
            StatusText = $"Limpieza completa: {FileSizeFormatter.Format(totalFreed)} liberados, {errors} errores";
            await ScanAsync(); // Rescanear
        }
        catch (OperationCanceledException) { StatusText = "Limpieza cancelada"; }
        catch (Exception ex) { StatusText = $"Error: {ex.Message}"; }
        finally { IsScanning = false; _cts?.Dispose(); _cts = null; }
    }
}
