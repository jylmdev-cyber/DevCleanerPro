using System.Windows;
using System.Windows.Controls;
using DevCleanerPro.Helpers;
using DevCleanerPro.ViewModels;

namespace DevCleanerPro.Views.Pages;

public partial class CacheCleanerPage : Page
{
    private readonly CacheCleanerViewModel _vm = new();

    public CacheCleanerPage() => InitializeComponent();

    private async void BtnAnalyze_Click(object sender, RoutedEventArgs e)
    {
        SetWorking(true);
        _vm.PropertyChanged += (_, a) => { if (a.PropertyName == nameof(_vm.StatusText)) Dispatcher.Invoke(() => TxtStatus.Text = _vm.StatusText); };
        await _vm.AnalyzeAsync();
        CacheGrid.ItemsSource = _vm.Caches;
        TxtCacheCount.Text = _vm.Caches.Count.ToString();
        TxtTotalSize.Text = _vm.TotalSizeText;
        BtnClean.IsEnabled = _vm.Caches.Count > 0;
        SetWorking(false);
    }

    private async void BtnClean_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("¿Eliminar las cachés seleccionadas?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        SetWorking(true);
        await _vm.CleanAsync();
        CacheGrid.ItemsSource = _vm.Caches;
        TxtCacheCount.Text = _vm.Caches.Count.ToString();
        TxtTotalSize.Text = _vm.TotalSizeText;
        SetWorking(false);
    }

    private void SetWorking(bool working)
    {
        LoadingRing.Visibility = working ? Visibility.Visible : Visibility.Collapsed;
        BtnAnalyze.IsEnabled = !working;
        BtnClean.IsEnabled = !working;
    }
}
