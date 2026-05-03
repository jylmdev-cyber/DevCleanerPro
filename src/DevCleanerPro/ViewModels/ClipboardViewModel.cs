using DevCleanerPro.Services;

namespace DevCleanerPro.ViewModels;

public class ClipboardViewModel : ViewModelBase
{
    private readonly ClipboardService _svc = new();
    private string _statusText = ""; private bool _historyEnabled;

    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
    public bool HistoryEnabled { get => _historyEnabled; set { if (SetProperty(ref _historyEnabled, value)) _svc.SetHistoryEnabled(value); } }

    public void Load() { HistoryEnabled = _svc.IsHistoryEnabled(); StatusText = $"Historial: {(HistoryEnabled ? "Habilitado" : "Deshabilitado")}"; }
    public void Clear() { _svc.ClearClipboard(); StatusText = "✓ Portapapeles vaciado"; }
    public void OpenSettings() => _svc.OpenClipboardHistory();
}
