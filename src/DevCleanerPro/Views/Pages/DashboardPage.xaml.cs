using System.Windows;
using System.Windows.Controls;
using DevCleanerPro.Helpers;
using DevCleanerPro.Services;
using DevCleanerPro.ViewModels;

namespace DevCleanerPro.Views.Pages;

public partial class DashboardPage : Page
{
    private readonly DashboardViewModel _vm;

    public DashboardPage()
    {
        InitializeComponent();
        _vm = new DashboardViewModel(new SettingsService());
        Loaded += async (_, _) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        LoadingRing.Visibility = Visibility.Visible;

        try
        {
            await _vm.RefreshAsync();

            // System Info
            TxtOs.Text = _vm.SystemInfo.OsVersion;
            TxtMachine.Text = _vm.SystemInfo.MachineName;
            TxtUser.Text = _vm.SystemInfo.UserName;
            TxtDotNet.Text = _vm.SystemInfo.DotNetVersion;
            TxtProcessor.Text = _vm.SystemInfo.ProcessorName;
            TxtRam.Text = FileSizeFormatter.FormatCompact(_vm.SystemInfo.TotalRamBytes);
            TxtCpu.Text = _vm.SystemInfo.ProcessorCount.ToString();
            TxtUptime.Text = FormatUptime(_vm.SystemInfo.Uptime);
            TxtLastScan.Text = _vm.LastScanText;
            TxtFreed.Text = _vm.LastFreedText;

            // Session stats for hero card
            var settings = new SettingsService().Load();
            if (settings.SessionFreedBytes > 0)
            {
                TxtHeroValue.Text = FileSizeFormatter.FormatCompact(settings.SessionFreedBytes);
                TxtHeroMsg.Text = $"Has liberado {FileSizeFormatter.Format(settings.SessionFreedBytes)} en esta sesión";
            }
            else if (settings.HistoricalFreedBytes > 0)
            {
                TxtHeroValue.Text = FileSizeFormatter.FormatCompact(settings.HistoricalFreedBytes);
                TxtHeroMsg.Text = $"Total histórico liberado: {FileSizeFormatter.Format(settings.HistoricalFreedBytes)}";
            }

            // Drives
            var formattedDrives = _vm.Drives.Select(d => new
            {
                d.Name, d.Label, d.UsagePercent,
                FreeSpace = FileSizeFormatter.Format(d.FreeSpace),
                TotalSize = FileSizeFormatter.Format(d.TotalSize)
            }).ToList();
            DrivesPanel.ItemsSource = formattedDrives;
        }
        catch (Exception ex) { TxtOs.Text = $"Error: {ex.Message}"; }
        finally { LoadingRing.Visibility = Visibility.Collapsed; }
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e) => await LoadDataAsync();

    private void BtnQuickClean_Click(object sender, RoutedEventArgs e)
    {
        // Navigate to QuickClean page via MainWindow
        if (Window.GetWindow(this) is MainWindow mw)
            mw.NavigationView.Navigate(typeof(QuickCleanPage));
    }

    private static string FormatUptime(TimeSpan ts)
    {
        if (ts.TotalDays >= 1) return $"{(int)ts.TotalDays}d {ts.Hours}h {ts.Minutes}m";
        if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        return $"{ts.Minutes}m {ts.Seconds}s";
    }
}
