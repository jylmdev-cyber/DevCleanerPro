using System.Collections.ObjectModel;
using DevCleanerPro.Services;

namespace DevCleanerPro.ViewModels;

public class EventLogCleanerViewModel : ViewModelBase
{
    private readonly EventLogCleanerService _svc = new();
    private string _statusText = "Pulse Analizar para ver los logs";
    private bool _isWorking;

    public ObservableCollection<EventLogCleanerService.EventLogInfo> Logs { get; } = new();
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
    public bool IsWorking { get => _isWorking; set => SetProperty(ref _isWorking, value); }

    public async Task AnalyzeAsync()
    {
        IsWorking = true; Logs.Clear();
        var progress = new Progress<string>(m => StatusText = m);
        var logs = await _svc.GetLogInfoAsync(progress);
        foreach (var l in logs) Logs.Add(l);
        StatusText = $"{logs.Count} logs analizados";
        IsWorking = false;
    }

    public async Task CleanAsync(string[] logNames)
    {
        IsWorking = true;
        var progress = new Progress<string>(m => StatusText = m);
        var result = await _svc.CleanLogsAsync(logNames, progress);
        StatusText = $"{result.ItemsCleaned} logs limpiados, {result.Errors} errores";
        await AnalyzeAsync();
        IsWorking = false;
    }
}
