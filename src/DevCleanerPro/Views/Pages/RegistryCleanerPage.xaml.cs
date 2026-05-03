using System.Windows; using System.Windows.Controls; using DevCleanerPro.ViewModels;
namespace DevCleanerPro.Views.Pages;
public partial class RegistryCleanerPage : Page { private readonly RegistryCleanerViewModel _vm=new(); public RegistryCleanerPage()=>InitializeComponent();
    private async void BtnScan_Click(object s, RoutedEventArgs e)
    {
        Ring.Visibility = Visibility.Visible;
        _vm.PropertyChanged += (_, a) => Dispatcher.Invoke(() => TxtStatus.Text = _vm.StatusText);
        await _vm.ScanAsync();
        UpdateUI();
        Ring.Visibility = Visibility.Collapsed;
    }
    
    private async void BtnClean_Click(object s, RoutedEventArgs e)
    {
        if (MessageBox.Show("¿Eliminar entradas huérfanas del registro?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        Ring.Visibility = Visibility.Visible;
        await _vm.CleanAsync();
        UpdateUI();
        Ring.Visibility = Visibility.Collapsed;
    }

    private void UpdateUI()
    {
        Grid.ItemsSource = _vm.Items;
        bool hasData = _vm.Items.Count > 0;
        BtnClean.IsEnabled = _vm.Items.Any(i => i.IsOrphan);
        PanelEmptyState.Visibility = hasData ? Visibility.Collapsed : Visibility.Visible;
        Grid.Visibility = hasData ? Visibility.Visible : Visibility.Collapsed;
    }
}
