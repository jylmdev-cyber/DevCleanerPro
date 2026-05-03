using System.Windows;
using System.Windows.Controls;
using DevCleanerPro.ViewModels;
using DevCleanerPro.Services;

namespace DevCleanerPro.Views.Pages;

public partial class ProcessMonitorPage : Page
{
    private readonly ProcessMonitorViewModel _vm = new();

    public ProcessMonitorPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        TxtStatus.Text = "Cargando procesos...";
        await _vm.RefreshAsync();
        Grid.ItemsSource = _vm.Processes;
        TxtStatus.Text = _vm.StatusText;
    }

    private async void BtnRefresh_Click(object s, RoutedEventArgs e) => await RefreshAsync();

    private void TxtFilter_Changed(object s, TextChangedEventArgs e)
    {
        _vm.Filter = TxtFilter.Text;
        _vm.ApplyFilter();
        Grid.ItemsSource = _vm.Processes;
        TxtStatus.Text = _vm.StatusText;
    }

    private async void BtnKill_Click(object s, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is ProcessMonitorService.ProcessItem p &&
            MessageBox.Show($"¿Kill proceso {p.Name} (PID {p.Pid})?", "Confirmar", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        {
            _vm.Kill(p.Pid);
            await RefreshAsync();
        }
    }

    private async void BtnKillTree_Click(object s, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is ProcessMonitorService.ProcessItem p &&
            MessageBox.Show($"¿Kill tree {p.Name}?", "Confirmar", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        {
            _vm.KillTree(p.Pid);
            await RefreshAsync();
        }
    }
}
