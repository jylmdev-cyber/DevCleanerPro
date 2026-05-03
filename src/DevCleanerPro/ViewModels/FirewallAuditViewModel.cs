using System.Collections.ObjectModel;
using DevCleanerPro.Services;

namespace DevCleanerPro.ViewModels;

public class FirewallAuditViewModel : ViewModelBase
{
    private readonly FirewallAuditService _svc = new();
    private string _statusText = ""; private bool _isWorking;

    public ObservableCollection<FirewallAuditService.FirewallRule> Rules { get; } = new();
    public ObservableCollection<FirewallAuditService.FirewallRule> Orphans { get; } = new();
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
    public bool IsWorking { get => _isWorking; set => SetProperty(ref _isWorking, value); }

    public async Task LoadAsync()
    {
        IsWorking = true; Rules.Clear();
        var rules = await _svc.GetRulesAsync(new Progress<string>(m => StatusText = m));
        foreach (var r in rules) Rules.Add(r);
        StatusText = $"{rules.Count} reglas cargadas";
        IsWorking = false;
    }

    public async Task FindOrphansAsync()
    {
        IsWorking = true; Orphans.Clear();
        var orphans = await _svc.FindOrphanRulesAsync(new Progress<string>(m => StatusText = m));
        foreach (var r in orphans) Orphans.Add(r);
        IsWorking = false;
    }

    public async Task ExportAsync(string path) => await _svc.ExportToCsvAsync(path);
}
