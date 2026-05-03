using System.Windows; using System.Windows.Controls; using DevCleanerPro.ViewModels; using DevCleanerPro.Services; using Microsoft.Win32;
namespace DevCleanerPro.Views.Pages;
public partial class DiskAnalyzerPage : Page { private readonly DiskAnalyzerViewModel _vm=new(); public DiskAnalyzerPage()=>InitializeComponent();
    private void BtnBrowse_Click(object s,RoutedEventArgs e){var d=new OpenFolderDialog();if(d.ShowDialog()==true)TxtPath.Text=d.FolderName;}
    private async void BtnScan_Click(object s,RoutedEventArgs e){if(string.IsNullOrWhiteSpace(TxtPath.Text))return;_vm.ScanPath=TxtPath.Text;Ring.Visibility=Visibility.Visible;BtnScan.IsEnabled=false;_vm.PropertyChanged+=(_, a)=>Dispatcher.Invoke(()=>TxtStatus.Text=_vm.StatusText);await _vm.ScanAsync();Grid.ItemsSource=_vm.LargeFiles;Ring.Visibility=Visibility.Collapsed;BtnScan.IsEnabled=true;}
    private void BtnDelete_Click(object s,RoutedEventArgs e){if(Grid.SelectedItem is DiskAnalyzerService.FileItem f&&MessageBox.Show($"¿Eliminar {f.Path}?","Confirmar",MessageBoxButton.YesNo)==MessageBoxResult.Yes){_vm.DeleteFile(f.Path);_vm.LargeFiles.Remove(f);}}}
