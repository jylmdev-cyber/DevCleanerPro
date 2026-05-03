using System.Windows;
using System.Windows.Controls;
using DevCleanerPro.ViewModels;

namespace DevCleanerPro.Views.Pages;

public partial class TempFilesPage : Page
{
    private readonly TempFilesViewModel _vm = new();
    public TempFilesPage() => InitializeComponent();

    private async void BtnAnalyze_Click(object sender, RoutedEventArgs e)
    {
        SetWorking(true);
        EmptyState.Visibility = Visibility.Collapsed;
        _vm.PropertyChanged += (_, a) => { if (a.PropertyName == nameof(_vm.StatusText)) Dispatcher.Invoke(() => TxtStatus.Text = _vm.StatusText); };
        await _vm.AnalyzeAsync();
        TempGrid.ItemsSource = _vm.Categories;
        TxtCatCount.Text = _vm.Categories.Count.ToString();
        TxtTotalSize.Text = _vm.TotalSizeText;
        BtnClean.IsEnabled = _vm.Categories.Count > 0;
        EmptyState.Visibility = _vm.Categories.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        SetWorking(false);
    }

    private async void BtnClean_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("¿Eliminar los archivos temporales seleccionados?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        SetWorking(true);
        await _vm.CleanAsync();
        TempGrid.ItemsSource = _vm.Categories;
        TxtCatCount.Text = _vm.Categories.Count.ToString();
        TxtTotalSize.Text = _vm.TotalSizeText;
        SetWorking(false);
    }

    private void SetWorking(bool w)
    {
        LoadingRing.Visibility = w ? Visibility.Visible : Visibility.Collapsed;
        BtnAnalyze.IsEnabled = !w;
        BtnClean.IsEnabled = !w;
    }
}
