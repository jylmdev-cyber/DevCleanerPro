using System.Windows;
using System.Windows.Controls;
using DevCleanerPro.ViewModels;

namespace DevCleanerPro.Views.Pages;

public partial class DockerCleanupPage : Page
{
    private readonly DockerCleanupViewModel _vm = new();

    public DockerCleanupPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await CheckDockerAsync();
    }

    private async Task CheckDockerAsync()
    {
        LoadingRing.Visibility = Visibility.Visible;
        _vm.PropertyChanged += (_, a) => { if (a.PropertyName == nameof(_vm.StatusText)) Dispatcher.Invoke(() => TxtStatus.Text = _vm.StatusText); };
        await _vm.RefreshAsync();
        UpdateUI();
        LoadingRing.Visibility = Visibility.Collapsed;
    }

    private void UpdateUI()
    {
        if (!_vm.IsDockerInstalled)
        {
            PanelNotInstalled.Visibility = Visibility.Visible;
            PanelDockerInfo.Visibility = Visibility.Collapsed;
        }
        else
        {
            PanelNotInstalled.Visibility = Visibility.Collapsed;
            PanelDockerInfo.Visibility = Visibility.Visible;
            if (_vm.DiskUsage != null) DockerGrid.ItemsSource = _vm.DiskUsage.Items;
        }
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        LoadingRing.Visibility = Visibility.Visible;
        await _vm.RefreshAsync();
        UpdateUI();
        LoadingRing.Visibility = Visibility.Collapsed;
    }

    private async void BtnPruneImages_Click(object sender, RoutedEventArgs e)
    {
        if (!Confirm("¿Purgar todas las imágenes no usadas?")) return;
        LoadingRing.Visibility = Visibility.Visible;
        _vm.PruneImagesCommand.Execute(null);
        await Task.Delay(500);
        await _vm.RefreshAsync();
        UpdateUI();
        LoadingRing.Visibility = Visibility.Collapsed;
    }

    private async void BtnPruneBuildCache_Click(object sender, RoutedEventArgs e)
    {
        if (!Confirm("¿Purgar el build cache de Docker?")) return;
        LoadingRing.Visibility = Visibility.Visible;
        _vm.PruneBuildCacheCommand.Execute(null);
        await Task.Delay(500);
        await _vm.RefreshAsync();
        UpdateUI();
        LoadingRing.Visibility = Visibility.Collapsed;
    }

    private async void BtnPruneAll_Click(object sender, RoutedEventArgs e)
    {
        if (!Confirm("⚠️ Esto eliminará TODAS las imágenes, contenedores, volúmenes y build cache. ¿Continuar?")) return;
        LoadingRing.Visibility = Visibility.Visible;
        _vm.PruneAllCommand.Execute(null);
        await Task.Delay(500);
        await _vm.RefreshAsync();
        UpdateUI();
        LoadingRing.Visibility = Visibility.Collapsed;
    }

    private static bool Confirm(string msg) => MessageBox.Show(msg, "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
}
