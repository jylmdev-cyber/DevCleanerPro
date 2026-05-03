using System.Collections.ObjectModel;
using DevCleanerPro.Services;

namespace DevCleanerPro.ViewModels;

public class RegistryCleanerViewModel : ViewModelBase
{
    private readonly RegistryCleanerService _svc = new();
    private string _statusText = "Pulse Escanear para analizar el registro";
    private bool _isWorking;

    public ObservableCollection<RegistryCleanerService.RegistryItem> Items { get; } = new();
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
    public bool IsWorking { get => _isWorking; set => SetProperty(ref _isWorking, value); }

    public async Task ScanAsync()
    {
        IsWorking = true; Items.Clear();
        var progress = new Progress<string>(m => StatusText = m);
        var items = await _svc.ScanAsync(progress);
        foreach (var i in items) Items.Add(i);
        StatusText = $"{items.Count} entradas encontradas ({items.Count(i => i.IsOrphan)} huérfanas)";
        IsWorking = false;
    }

    public async Task CleanAsync()
    {
        IsWorking = true;
        var progress = new Progress<string>(m => StatusText = m);
        var result = await _svc.CleanSelectedAsync(Items.Where(i => i.IsOrphan).ToList(), progress);
        StatusText = $"{result.ItemsCleaned} eliminados, {result.Errors} errores";
        await ScanAsync();
        IsWorking = false;
    }
}
