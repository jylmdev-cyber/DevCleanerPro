using System.Collections.ObjectModel;
using DevCleanerPro.Helpers;
using DevCleanerPro.Services;

namespace DevCleanerPro.ViewModels;

public class VsCleanerViewModel : ViewModelBase
{
    private readonly VsCleanerService _svc = new();
    private string _statusText = "Pulse Escanear"; private bool _isWorking; private long _totalSize;

    public ObservableCollection<VsCleanerService.VsCacheItem> Items { get; } = new();
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
    public bool IsWorking { get => _isWorking; set => SetProperty(ref _isWorking, value); }
    public long TotalSize { get => _totalSize; set { SetProperty(ref _totalSize, value); OnPropertyChanged(nameof(TotalSizeText)); } }
    public string TotalSizeText => FileSizeFormatter.Format(TotalSize);

    public async Task ScanAsync()
    {
        IsWorking = true; Items.Clear();
        var items = await _svc.ScanAsync(new Progress<string>(m => StatusText = m));
        foreach (var i in items) Items.Add(i);
        TotalSize = items.Sum(i => i.Size);
        StatusText = $"{items.Count} cachés, {TotalSizeText} total";
        IsWorking = false;
    }

    public async Task CleanAsync()
    {
        IsWorking = true;
        var result = await _svc.CleanAsync(new Progress<string>(m => StatusText = m));
        StatusText = $"Liberados: {FileSizeFormatter.Format(result.FreedBytes)}, {result.Errors} errores";
        await ScanAsync();
        IsWorking = false;
    }
}
