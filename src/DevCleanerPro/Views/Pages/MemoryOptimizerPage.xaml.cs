using System.Windows; using System.Windows.Controls; using DevCleanerPro.ViewModels;
namespace DevCleanerPro.Views.Pages;
public partial class MemoryOptimizerPage : Page
{
    private readonly MemoryOptimizerViewModel _vm = new();
    public MemoryOptimizerPage() => InitializeComponent();
    private async void BtnOptimize_Click(object sender, RoutedEventArgs e)
    {
        Ring.Visibility = Visibility.Visible; BtnOptimize.IsEnabled = false;
        _vm.PropertyChanged += (_, a) => Dispatcher.Invoke(() => { TxtStatus.Text = _vm.StatusText; TxtBefore.Text = _vm.RamBefore; TxtAfter.Text = _vm.RamAfter; TxtFreed.Text = _vm.RamFreed; });
        await _vm.OptimizeAsync();
        Ring.Visibility = Visibility.Collapsed; BtnOptimize.IsEnabled = true;
    }
}
