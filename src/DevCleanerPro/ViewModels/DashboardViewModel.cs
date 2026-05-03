using DevCleanerPro.Helpers;
using DevCleanerPro.Models;
using DevCleanerPro.Services;

namespace DevCleanerPro.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private readonly SystemInfoService _sysInfoService = new();
    private readonly DiskAnalyzerService _diskService = new();
    private readonly SettingsService _settingsService;

    private SystemInfoModel _systemInfo = new();
    private List<DriveInfoModel> _drives = new();
    private string _statusText = "Listo";
    private bool _isLoading;
    private string _lastScanText = "Nunca";
    private string _lastFreedText = "—";

    public DashboardViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
    }

    public SystemInfoModel SystemInfo { get => _systemInfo; set => SetProperty(ref _systemInfo, value); }
    public List<DriveInfoModel> Drives { get => _drives; set => SetProperty(ref _drives, value); }
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public string LastScanText { get => _lastScanText; set => SetProperty(ref _lastScanText, value); }
    public string LastFreedText { get => _lastFreedText; set => SetProperty(ref _lastFreedText, value); }
    public AsyncRelayCommand RefreshCommand { get; }

    public async Task RefreshAsync()
    {
        IsLoading = true;
        StatusText = "Cargando información del sistema...";
        try
        {
            SystemInfo = await Task.Run(() => _sysInfoService.GetSystemInfo());
            Drives = await Task.Run(() => _diskService.GetDriveInfo());
            var settings = _settingsService.Load();
            LastScanText = settings.LastScanDate?.ToString("dd/MM/yyyy HH:mm") ?? "Nunca";
            LastFreedText = settings.LastTotalFreed > 0 ? FileSizeFormatter.Format(settings.LastTotalFreed) : "—";
            StatusText = "Información actualizada";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally { IsLoading = false; }
    }
}
