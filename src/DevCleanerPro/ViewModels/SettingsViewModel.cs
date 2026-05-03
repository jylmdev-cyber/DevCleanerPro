using DevCleanerPro.Models;
using DevCleanerPro.Services;
using Wpf.Ui.Appearance;

namespace DevCleanerPro.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private AppSettings _settings = new();
    private string _statusText = "";
    private int _selectedThemeIndex;

    public SettingsViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        _settings = _settingsService.Load();
        _selectedThemeIndex = _settings.Theme switch { "Light" => 1, "System" => 2, _ => 0 };
        SaveCommand = new RelayCommand(Save);
    }

    public AppSettings Settings { get => _settings; set => SetProperty(ref _settings, value); }
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
    public int SelectedThemeIndex
    {
        get => _selectedThemeIndex;
        set
        {
            if (SetProperty(ref _selectedThemeIndex, value))
            {
                _settings.Theme = value switch { 1 => "Light", 2 => "System", _ => "Dark" };
                ApplyTheme();
            }
        }
    }

    public bool ConfirmBeforeDelete
    {
        get => _settings.ConfirmBeforeDelete;
        set { _settings.ConfirmBeforeDelete = value; OnPropertyChanged(); }
    }

    public bool UseRecycleBin
    {
        get => _settings.UseRecycleBin;
        set { _settings.UseRecycleBin = value; OnPropertyChanged(); }
    }

    public string DefaultScanPath
    {
        get => _settings.DefaultScanPath;
        set { _settings.DefaultScanPath = value; OnPropertyChanged(); }
    }

    public RelayCommand SaveCommand { get; }

    public void Save()
    {
        _settingsService.Save(_settings);
        StatusText = "Configuración guardada ✓";
    }

    public void ApplyTheme()
    {
        var theme = _settings.Theme switch
        {
            "Light" => ApplicationTheme.Light,
            "System" => ApplicationTheme.Unknown,
            _ => ApplicationTheme.Dark
        };
        ApplicationThemeManager.Apply(theme);
    }
}
