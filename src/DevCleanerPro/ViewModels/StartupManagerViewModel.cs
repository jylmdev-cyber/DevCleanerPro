using System.Collections.ObjectModel;
using DevCleanerPro.Services;

namespace DevCleanerPro.ViewModels;

public class StartupManagerViewModel : ViewModelBase
{
    private readonly StartupManagerService _svc = new();
    private string _statusText = "";

    public ObservableCollection<StartupManagerService.StartupEntry> Entries { get; } = new();
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }

    public void Load()
    {
        Entries.Clear();
        foreach (var e in _svc.GetAllEntries()) Entries.Add(e);
        StatusText = $"{Entries.Count} entradas de inicio cargadas";
    }

    public void DisableEntry(StartupManagerService.StartupEntry e) { _svc.DisableEntry(e); Load(); StatusText = $"Deshabilitado: {e.Name}"; }
    public void EnableEntry(StartupManagerService.StartupEntry e) { _svc.EnableEntry(e); Load(); StatusText = $"Habilitado: {e.Name}"; }
    public void DeleteEntry(StartupManagerService.StartupEntry e) { _svc.DeleteEntry(e); Load(); StatusText = $"Eliminado: {e.Name}"; }
    public void OpenLocation(StartupManagerService.StartupEntry e) => _svc.OpenLocation(e);
}
