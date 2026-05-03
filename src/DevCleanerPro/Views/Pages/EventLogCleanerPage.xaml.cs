using System.Windows; using System.Windows.Controls; using DevCleanerPro.ViewModels;
namespace DevCleanerPro.Views.Pages;
public partial class EventLogCleanerPage : Page
{
    private readonly EventLogCleanerViewModel _vm = new();
    public EventLogCleanerPage() => InitializeComponent();
    private async void BtnAnalyze_Click(object s, RoutedEventArgs e)
    {
        Ring.Visibility = Visibility.Visible;
        _vm.PropertyChanged += (_, a) => Dispatcher.Invoke(() => TxtStatus.Text = _vm.StatusText);
        await _vm.AnalyzeAsync();
        UpdateUI();
        Ring.Visibility = Visibility.Collapsed;
    }
    
    private async void BtnClean_Click(object s, RoutedEventArgs e)
    {
        if (MessageBox.Show("¿Limpiar logs Application, System, Setup?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        Ring.Visibility = Visibility.Visible;
        await _vm.CleanAsync(new[] { "Application", "System", "Setup" });
        UpdateUI();
        Ring.Visibility = Visibility.Collapsed;
    }
    
    private async void BtnCleanSecurity_Click(object s, RoutedEventArgs e)
    {
        if (MessageBox.Show("⚠️ ¿Limpiar el log de SEGURIDAD? Esta acción elimina el registro de auditoría.", "Confirmar Security", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        Ring.Visibility = Visibility.Visible;
        await _vm.CleanAsync(new[] { "Security" });
        UpdateUI();
        Ring.Visibility = Visibility.Collapsed;
    }

    private void UpdateUI()
    {
        Grid.ItemsSource = _vm.Logs;
        bool hasData = _vm.Logs.Count > 0;
        BtnClean.IsEnabled = hasData;
        BtnCleanSecurity.IsEnabled = hasData;
        PanelEmptyState.Visibility = hasData ? Visibility.Collapsed : Visibility.Visible;
        Grid.Visibility = hasData ? Visibility.Visible : Visibility.Collapsed;
    }
}
