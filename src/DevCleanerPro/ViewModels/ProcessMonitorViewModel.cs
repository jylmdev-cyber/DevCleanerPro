using System.Collections.ObjectModel;
using System.Diagnostics;
using DevCleanerPro.Services;

namespace DevCleanerPro.ViewModels;

public class ProcessMonitorViewModel : ViewModelBase
{
    private readonly ProcessMonitorService _svc = new();
    private string _statusText = ""; private string _filter = "";
    private List<ProcessMonitorService.ProcessItem> _allProcesses = new();

    public ObservableCollection<ProcessMonitorService.ProcessItem> Processes { get; } = new();
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
    public string Filter { get => _filter; set => SetProperty(ref _filter, value); }

    public async Task RefreshAsync()
    {
        _allProcesses = await _svc.GetProcessesAsync();
        ApplyFilter();
    }

    public void ApplyFilter()
    {
        Processes.Clear();
        var filtered = string.IsNullOrWhiteSpace(Filter) ? _allProcesses :
            _allProcesses.Where(p => p.Name.Contains(Filter, StringComparison.OrdinalIgnoreCase) || p.Pid.ToString() == Filter).ToList();
        foreach (var p in filtered) Processes.Add(p);
        StatusText = $"{Processes.Count} procesos (de {_allProcesses.Count} totales)";
    }

    public bool Kill(int pid) { var r = _svc.KillProcess(pid); return r; }
    public bool KillTree(int pid) { var r = _svc.KillProcessTree(pid); return r; }
    public bool SetPriority(int pid, ProcessPriorityClass priority) => _svc.SetPriority(pid, priority);
}
