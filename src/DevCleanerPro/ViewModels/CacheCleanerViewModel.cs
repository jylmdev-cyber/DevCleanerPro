using System.Collections.ObjectModel;
using DevCleanerPro.Helpers;
using DevCleanerPro.Models;
using DevCleanerPro.Services;

namespace DevCleanerPro.ViewModels;

public class CacheCleanerViewModel : ViewModelBase
{
    private readonly CacheCleanerService _service = new();
    private string _statusText = "Pulse Analizar para detectar cachés";
    private bool _isWorking;
    private long _totalSize;

    public ObservableCollection<CacheEntry> Caches { get; } = new();
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
    public bool IsWorking { get => _isWorking; set => SetProperty(ref _isWorking, value); }
    public long TotalSize { get => _totalSize; set { SetProperty(ref _totalSize, value); OnPropertyChanged(nameof(TotalSizeText)); } }
    public string TotalSizeText => FileSizeFormatter.Format(TotalSize);

    public AsyncRelayCommand AnalyzeCommand => new(AnalyzeAsync, () => !IsWorking);
    public AsyncRelayCommand CleanCommand => new(CleanAsync, () => !IsWorking && Caches.Any(c => c.IsSelected));
    public RelayCommand SelectAllCommand => new(() => { foreach (var c in Caches) c.IsSelected = true; OnPropertyChanged(nameof(Caches)); });

    public async Task AnalyzeAsync()
    {
        IsWorking = true;
        Caches.Clear();
        var progress = new Progress<string>(msg => StatusText = msg);
        try
        {
            var entries = await _service.GetCacheEntriesAsync(progress);
            foreach (var e in entries.Where(e => e.Exists && e.Size > 0).OrderByDescending(e => e.Size))
            {
                e.IsSelected = true;
                Caches.Add(e);
            }
            TotalSize = Caches.Sum(c => c.Size);
            StatusText = $"{Caches.Count} cachés encontradas — {TotalSizeText} total";
        }
        catch (Exception ex) { StatusText = $"Error: {ex.Message}"; }
        finally { IsWorking = false; }
    }

    public async Task CleanAsync()
    {
        IsWorking = true;
        var progress = new Progress<string>(msg => StatusText = msg);
        try
        {
            var result = await _service.CleanCachesAsync(Caches.ToList(), progress);
            StatusText = $"Limpieza completa: {FileSizeFormatter.Format(result.FreedBytes)} liberados";
            await AnalyzeAsync();
        }
        catch (Exception ex) { StatusText = $"Error: {ex.Message}"; }
        finally { IsWorking = false; }
    }
}
