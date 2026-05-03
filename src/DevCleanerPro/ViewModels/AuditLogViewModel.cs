using System.Collections.ObjectModel;
using DevCleanerPro.Models;
using DevCleanerPro.Services;

namespace DevCleanerPro.ViewModels;

public class AuditLogViewModel : ViewModelBase
{
    private readonly AuditLogService _svc = new();
    private string _statusText = "";

    public ObservableCollection<AuditLogEntry> Entries { get; } = new();
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }

    public void LoadToday() => LoadRange(DateTime.Today, DateTime.Today);
    public void LoadWeek() => LoadRange(DateTime.Today.AddDays(-7), DateTime.Today);

    public void LoadRange(DateTime from, DateTime to)
    {
        Entries.Clear();
        foreach (var e in _svc.LoadRange(from, to).OrderByDescending(e => e.Timestamp)) Entries.Add(e);
        StatusText = $"{Entries.Count} entradas ({from:dd/MM} — {to:dd/MM})";
    }

    public void ExportCsv(string path) { _svc.ExportToCsv(path, DateTime.Today.AddDays(-30), DateTime.Today); StatusText = $"Exportado a {path}"; }
    public void Purge(int days) { _svc.PurgeOlderThan(days); StatusText = $"Logs de más de {days} días eliminados"; }
}
