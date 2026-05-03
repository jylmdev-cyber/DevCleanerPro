using System.Collections.ObjectModel;
using DevCleanerPro.Services;

namespace DevCleanerPro.ViewModels;

public class DnsFlushViewModel : ViewModelBase
{
    private readonly DnsFlushService _svc = new();
    private bool _isWorking;
    public ObservableCollection<string> OutputLines { get; } = new();
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
    private string _statusText = "Listo";
    public bool IsWorking { get => _isWorking; set => SetProperty(ref _isWorking, value); }

    public async Task FlushAsync()
    {
        IsWorking = true; OutputLines.Clear();
        var progress = new Progress<string>(m => { StatusText = m; OutputLines.Add($"[{DateTime.Now:HH:mm:ss}] {m}"); });
        var result = await _svc.CleanAsync(progress);
        StatusText = $"Completado — {result.ItemsCleaned} operaciones, {result.Errors} errores";
        IsWorking = false;
    }
}
