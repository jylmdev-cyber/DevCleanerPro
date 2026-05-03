using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using DevCleanerPro.Services;
using DevCleanerPro.ViewModels;
using Microsoft.Win32;

namespace DevCleanerPro.Views.Pages;

public partial class SettingsPage : Page
{
    private readonly SettingsService _svc = new();
    private readonly Models.AppSettings _settings;

    public SettingsPage()
    {
        InitializeComponent();
        _settings = _svc.Load();
        LoadUI();
    }

    private void LoadUI()
    {
        CmbTheme.SelectedIndex = _settings.Theme switch { "Light" => 1, "System" => 2, _ => 0 };
        CmbLanguage.SelectedIndex = _settings.Language == "en-US" ? 1 : 0;
        ChkConfirmDelete.IsChecked = _settings.ConfirmBeforeDelete;
        ChkRecycleBin.IsChecked = _settings.UseRecycleBin;
        ChkSafeMode.IsChecked = _settings.SafeModeEnabled;
        TxtDefaultPath.Text = _settings.DefaultScanPath;
        TxtThreshold.Text = _settings.LargeFileThresholdMb.ToString();
        TxtLogDays.Text = _settings.LogRetentionDays.ToString();
        ChkTray.IsChecked = _settings.MinimizeToTray;
        ChkStartup.IsChecked = _settings.StartWithWindows;
        LstExclusions.ItemsSource = _settings.ExclusionPatterns;
    }

    private void CmbTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_settings == null || !IsLoaded) return;
        string newTheme = CmbTheme.SelectedIndex switch { 1 => "Light", 2 => "System", _ => "Dark" };
        if (_settings.Theme != newTheme)
        {
            _settings.Theme = newTheme;
            DevCleanerPro.Helpers.ThemeManagerHelper.ApplyTheme(newTheme);
        }
    }

    private void BtnBrowseDefault_Click(object sender, RoutedEventArgs e)
    {
        var d = new OpenFolderDialog { Title = "Seleccionar carpeta" };
        if (d.ShowDialog() == true) TxtDefaultPath.Text = d.FolderName;
    }

    // ═══ Exclusiones ═══
    private void BtnAddExclusion_Click(object sender, RoutedEventArgs e)
    {
        var path = TxtNewExclusion.Text.Trim();
        if (string.IsNullOrEmpty(path) || _settings.ExclusionPatterns.Contains(path)) return;
        _settings.ExclusionPatterns.Add(path);
        LstExclusions.ItemsSource = null;
        LstExclusions.ItemsSource = _settings.ExclusionPatterns;
        TxtNewExclusion.Text = "";
    }

    private void BtnRemoveExclusion_Click(object sender, RoutedEventArgs e)
    {
        if (LstExclusions.SelectedItem is string sel)
        {
            _settings.ExclusionPatterns.Remove(sel);
            LstExclusions.ItemsSource = null;
            LstExclusions.ItemsSource = _settings.ExclusionPatterns;
        }
    }

    // ═══ Startup with Windows ═══
    private void ChkStartup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (key == null) return;
            if (ChkStartup.IsChecked == true)
                key.SetValue("DevCleanerPro", $"\"{Environment.ProcessPath}\" --minimized");
            else
                key.DeleteValue("DevCleanerPro", false);
        }
        catch (Exception ex) { TxtStatus.Text = $"✗ Error: {ex.Message}"; }
    }

    // ═══ Guardar ═══
    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        _settings.ConfirmBeforeDelete = ChkConfirmDelete.IsChecked == true;
        _settings.UseRecycleBin = ChkRecycleBin.IsChecked == true;
        _settings.SafeModeEnabled = ChkSafeMode.IsChecked == true;
        _settings.DefaultScanPath = TxtDefaultPath.Text;
        _settings.MinimizeToTray = ChkTray.IsChecked == true;
        _settings.StartWithWindows = ChkStartup.IsChecked == true;
        
        string newLang = CmbLanguage.SelectedIndex == 1 ? "en-US" : "es-ES";
        if (_settings.Language != newLang)
        {
            _settings.Language = newLang;
            DevCleanerPro.Helpers.LanguageManager.ChangeLanguage(newLang);
        }

        if (int.TryParse(TxtThreshold.Text, out var mb)) _settings.LargeFileThresholdMb = mb;
        if (int.TryParse(TxtLogDays.Text, out var days)) _settings.LogRetentionDays = days;
        _svc.Save(_settings);
        
        TxtStatus.Text = $"✅ Configuración guardada correctamente ({DateTime.Now:HH:mm:ss})";
    }

    // ═══ Export / Import Settings ═══
    private void BtnExportSettings_Click(object sender, RoutedEventArgs e)
    {
        var d = new SaveFileDialog { Filter = "JSON|*.json", FileName = "devcleanerpro-settings.json" };
        if (d.ShowDialog() == true)
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(d.FileName, json);
            TxtStatus.Text = $"✓ Exportado a {d.FileName}";
        }
    }

    private void BtnImportSettings_Click(object sender, RoutedEventArgs e)
    {
        var d = new OpenFileDialog { Filter = "JSON|*.json" };
        if (d.ShowDialog() == true)
        {
            try
            {
                var json = System.IO.File.ReadAllText(d.FileName);
                var imported = JsonSerializer.Deserialize<Models.AppSettings>(json);
                if (imported != null) { _svc.Save(imported); LoadUI(); TxtStatus.Text = "✓ Configuración importada"; }
            }
            catch (Exception ex) { TxtStatus.Text = $"✗ Error al importar: {ex.Message}"; }
        }
    }

    // ═══ Check Updates ═══
    private async void BtnCheckUpdate_Click(object sender, RoutedEventArgs e)
    {
        TxtStatus.Text = "Buscando actualizaciones...";
        try
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("DevCleanerPro/2.0");
            var json = await http.GetStringAsync("https://api.github.com/repos/devtools/devcleanerpro/releases/latest");
            using var doc = JsonDocument.Parse(json);
            var latest = doc.RootElement.GetProperty("tag_name").GetString() ?? "v2.0.0";
            TxtStatus.Text = latest == "v2.0.0" ? "✓ Ya tienes la última versión" : $"⬆ Nueva versión disponible: {latest}";
        }
        catch { TxtStatus.Text = "✓ No se pudo conectar — usando v2.0.0"; }
    }
}
