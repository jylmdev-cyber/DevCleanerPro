using System.Collections.ObjectModel;
using DevCleanerPro.Models;
using DevCleanerPro.Services;

namespace DevCleanerPro.ViewModels;

public class ScheduledCleanViewModel : ViewModelBase
{
    private readonly ScheduledCleanService _svc = new();
    private string _statusText = "";

    public ObservableCollection<(string Name, string Description, DateTime? NextRun, bool Enabled)> Tasks { get; } = new();
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }

    public void Load()
    {
        Tasks.Clear();
        foreach (var t in _svc.GetTasks()) Tasks.Add(t);
        StatusText = $"{Tasks.Count} tareas programadas";
    }

    public void CreatePreset(int idx)
    {
        if (idx < 0 || idx >= ScheduledCleanService.Presets.Length) return;
        var p = ScheduledCleanService.Presets[idx];
        var days = idx == 0 ? Microsoft.Win32.TaskScheduler.DaysOfTheWeek.AllDays : Microsoft.Win32.TaskScheduler.DaysOfTheWeek.Sunday;
        var ok = _svc.CreateTask(p.Name, p.Description, days, idx == 0 ? (short)3 : (short)2, 0);
        StatusText = ok ? $"✓ Tarea '{p.Name}' creada" : "✗ Error al crear tarea";
        Load();
    }

    public void Delete(string name) { _svc.DeleteTask(name); Load(); StatusText = $"Tarea '{name}' eliminada"; }
}
