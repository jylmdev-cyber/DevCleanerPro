using DevCleanerPro.Helpers;
using DevCleanerPro.Services;

namespace DevCleanerPro.ViewModels;

public class MemoryOptimizerViewModel : ViewModelBase
{
    private readonly MemoryOptimizerService _svc = new();
    private string _statusText = "Pulse Optimizar para liberar RAM";
    private bool _isWorking;
    private string _ramBefore = "—", _ramAfter = "—", _ramFreed = "—";

    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
    public bool IsWorking { get => _isWorking; set => SetProperty(ref _isWorking, value); }
    public string RamBefore { get => _ramBefore; set => SetProperty(ref _ramBefore, value); }
    public string RamAfter { get => _ramAfter; set => SetProperty(ref _ramAfter, value); }
    public string RamFreed { get => _ramFreed; set => SetProperty(ref _ramFreed, value); }

    public async Task OptimizeAsync()
    {
        IsWorking = true;
        var gcBefore = GC.GetGCMemoryInfo();
        RamBefore = $"{gcBefore.TotalAvailableMemoryBytes / 1024 / 1024} MB";
        var progress = new Progress<string>(msg => StatusText = msg);
        var result = await _svc.CleanAsync(progress);
        var gcAfter = GC.GetGCMemoryInfo();
        RamAfter = $"{gcAfter.TotalAvailableMemoryBytes / 1024 / 1024} MB";
        RamFreed = FileSizeFormatter.Format(result.FreedBytes);
        StatusText = $"Optimización completa — {result.ItemsCleaned} procesos procesados";
        IsWorking = false;
    }
}
