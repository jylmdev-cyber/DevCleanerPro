using System.Windows;
using System.Windows.Controls;
using DevCleanerPro.ViewModels;

namespace DevCleanerPro.Views.Pages;

public partial class EnvironmentToolsPage : Page
{
    private readonly EnvironmentToolsViewModel _vm = new();
    public EnvironmentToolsPage() => InitializeComponent();

    private void BtnAnalyzePath_Click(object sender, RoutedEventArgs e)
    {
        _vm.AnalyzePath();
        PathGrid.ItemsSource = _vm.PathEntries;
        DuplicatesList.ItemsSource = _vm.DuplicatePaths;
        InvalidsList.ItemsSource = _vm.InvalidPaths;
        TxtPathStatus.Text = _vm.StatusText;
    }

    private void BtnLoadStartup_Click(object sender, RoutedEventArgs e)
    {
        _vm.LoadStartupPrograms();
        StartupGrid.ItemsSource = _vm.StartupPrograms;
        TxtStartupStatus.Text = _vm.StatusText;
    }

    private async void BtnScanLargeFiles_Click(object sender, RoutedEventArgs e)
    {
        BtnScanLarge.IsEnabled = false;
        LargeFilesRing.Visibility = Visibility.Visible;
        LargeFilesGrid.ItemsSource = _vm.LargeFiles;
        _vm.PropertyChanged += (_, a) => { if (a.PropertyName == nameof(_vm.StatusText)) Dispatcher.Invoke(() => TxtLargeStatus.Text = _vm.StatusText); };
        await _vm.ScanLargeFilesAsync();
        LargeFilesRing.Visibility = Visibility.Collapsed;
        BtnScanLarge.IsEnabled = true;
    }
}
