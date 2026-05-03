using System.Windows; using System.Windows.Controls; using DevCleanerPro.ViewModels;
namespace DevCleanerPro.Views.Pages;
public partial class VsCleanerPage : Page { private readonly VsCleanerViewModel _vm=new(); public VsCleanerPage()=>InitializeComponent();
    private async void BtnScan_Click(object s,RoutedEventArgs e){Ring.Visibility=Visibility.Visible;_vm.PropertyChanged+=(_, a)=>Dispatcher.Invoke(()=>{TxtStatus.Text=_vm.StatusText;TxtTotal.Text=_vm.TotalSizeText;});await _vm.ScanAsync();Grid.ItemsSource=_vm.Items;BtnClean.IsEnabled=_vm.Items.Count>0;Ring.Visibility=Visibility.Collapsed;}
    private async void BtnClean_Click(object s,RoutedEventArgs e){if(MessageBox.Show("¿Limpiar todas las cachés de VS/VS Code?","Confirmar",MessageBoxButton.YesNo)==MessageBoxResult.Yes){Ring.Visibility=Visibility.Visible;await _vm.CleanAsync();Grid.ItemsSource=_vm.Items;Ring.Visibility=Visibility.Collapsed;}}}
