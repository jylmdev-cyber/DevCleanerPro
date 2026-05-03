using System.Windows;
using System.Windows.Controls;
using DevCleanerPro.Helpers;
using DevCleanerPro.ViewModels;
using Microsoft.Win32;

namespace DevCleanerPro.Views.Pages;

public partial class ProjectScannerPage : Page
{
    private readonly ProjectScannerViewModel _vm = new();

    public ProjectScannerPage()
    {
        InitializeComponent();
    }

    private void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Seleccionar carpeta raíz de proyectos" };
        if (dialog.ShowDialog() == true)
        {
            TxtPath.Text = dialog.FolderName;
        }
    }

    private async void BtnScan_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtPath.Text)) return;
        _vm.ScanPath = TxtPath.Text;
        SetScanning(true);
        try
        {
            _vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(_vm.StatusText))
                    Dispatcher.Invoke(() => TxtStatus.Text = _vm.StatusText);
            };
            await _vm.ScanAsync();
            UpdateUI();
        }
        finally { SetScanning(false); }
    }

    private async void BtnClean_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("¿Está seguro de eliminar las carpetas seleccionadas?\nEsta acción no se puede deshacer.", "Confirmar limpieza", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;
        SetScanning(true);
        try
        {
            await _vm.CleanAsync();
            UpdateUI();
        }
        finally { SetScanning(false); }
    }

    private void UpdateUI()
    {
        ResultsGrid.ItemsSource = _vm.Projects;
        TxtProjectCount.Text = _vm.ProjectCount.ToString();
        TxtCleanableSize.Text = FileSizeFormatter.FormatCompact(_vm.TotalCleanable);
        
        bool hasData = _vm.Projects.Count > 0;
        BtnClean.IsEnabled = hasData;
        PanelEmptyState.Visibility = hasData ? Visibility.Collapsed : Visibility.Visible;
        ResultsGrid.Visibility = hasData ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        _vm.CancelCommand.Execute(null);
    }

    private void SetScanning(bool scanning)
    {
        LoadingRing.Visibility = scanning ? Visibility.Visible : Visibility.Collapsed;
        BtnCancel.Visibility = scanning ? Visibility.Visible : Visibility.Collapsed;
        BtnScan.IsEnabled = !scanning;
        BtnClean.IsEnabled = !scanning;
    }
}
