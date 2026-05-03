using System.Windows;
using System.Windows.Controls;
using DevCleanerPro.Models;
using DevCleanerPro.ViewModels;

namespace DevCleanerPro.Views.Pages;

public partial class AiAgentCleanerPage : Page
{
    private readonly AiAgentCleanerViewModel _vm = new();

    public AiAgentCleanerPage() => InitializeComponent();

    private async void BtnScan_Click(object sender, RoutedEventArgs e)
    {
        Ring.Visibility = Visibility.Visible;
        BtnClean.IsEnabled = false;
        _vm.PropertyChanged += (_, _) => Dispatcher.Invoke(() =>
        {
            TxtStatus.Text = _vm.StatusText;
            TxtTotal.Text = _vm.TotalSizeText;
            TxtSafe.Text = _vm.SafeSizeText;
            TxtUnsafe.Text = _vm.UnsafeSizeText;
            TxtToolCount.Text = _vm.Tools.Count.ToString();
        });
        await _vm.ScanAsync();
        GridItems.ItemsSource = _vm.AllItems;
        GridModels.ItemsSource = _vm.Models;
        GridTools.ItemsSource = _vm.Tools;
        bool hasData = _vm.AllItems.Count > 0;
        BtnClean.IsEnabled = hasData;
        PanelEmptyState.Visibility = hasData ? Visibility.Collapsed : Visibility.Visible;
        GridItems.Visibility = hasData ? Visibility.Visible : Visibility.Collapsed;
        Ring.Visibility = Visibility.Collapsed;
    }

    private void BtnSelectSafe_Click(object sender, RoutedEventArgs e) => _vm.SelectAllSafe();
    private void BtnSelectOrphans_Click(object sender, RoutedEventArgs e) => _vm.SelectOrphans();
    private void BtnDeselectAll_Click(object sender, RoutedEventArgs e) => _vm.DeselectAll();

    private async void BtnClean_Click(object sender, RoutedEventArgs e)
    {
        var selectedCount = _vm.AllItems.Count(i => i.IsSelected);
        if (selectedCount == 0) { TxtStatus.Text = "No hay elementos seleccionados"; return; }
        var unsafeCount = _vm.AllItems.Count(i => i.IsSelected && !i.IsSafe);
        var msg = unsafeCount > 0
            ? $"Se eliminarán {selectedCount} elementos ({unsafeCount} requieren confirmación). ¿Continuar?"
            : $"Se eliminarán {selectedCount} elementos seguros. ¿Continuar?";
        if (MessageBox.Show(msg, "Confirmar limpieza AI", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        Ring.Visibility = Visibility.Visible;
        BtnClean.IsEnabled = false;
        await _vm.CleanSelectedAsync();
        Ring.Visibility = Visibility.Collapsed;
        BtnClean.IsEnabled = true;
    }

    private async void BtnDeleteModel_Click(object sender, RoutedEventArgs e)
    {
        if (GridModels.SelectedItem is not LlmModelInfo model) return;
        if (MessageBox.Show($"¿Eliminar modelo {model.Source}/{model.Name}?\nTamaño: {model.Size / 1024 / 1024} MB", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        Ring.Visibility = Visibility.Visible;
        await _vm.DeleteModelAsync(model);
        Ring.Visibility = Visibility.Collapsed;
    }
}
