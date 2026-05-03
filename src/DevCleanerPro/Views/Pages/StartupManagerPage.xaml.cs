using System.Windows; using System.Windows.Controls; using DevCleanerPro.ViewModels; using DevCleanerPro.Services;
namespace DevCleanerPro.Views.Pages;
public partial class StartupManagerPage : Page { private readonly StartupManagerViewModel _vm=new(); public StartupManagerPage(){InitializeComponent();Loaded+=(_, _)=>{_vm.Load();Grid.ItemsSource=_vm.Entries;TxtStatus.Text=_vm.StatusText;};}
    private void BtnLoad_Click(object s,RoutedEventArgs e){_vm.Load();Grid.ItemsSource=_vm.Entries;TxtStatus.Text=_vm.StatusText;}
    private StartupManagerService.StartupEntry? Sel=>Grid.SelectedItem as StartupManagerService.StartupEntry;
    private void BtnDisable_Click(object s,RoutedEventArgs e){if(Sel!=null){_vm.DisableEntry(Sel);Grid.ItemsSource=_vm.Entries;TxtStatus.Text=_vm.StatusText;}}
    private void BtnEnable_Click(object s,RoutedEventArgs e){if(Sel!=null){_vm.EnableEntry(Sel);Grid.ItemsSource=_vm.Entries;TxtStatus.Text=_vm.StatusText;}}
    private void BtnDelete_Click(object s,RoutedEventArgs e){if(Sel!=null&&MessageBox.Show($"¿Eliminar '{Sel.Name}'?","Confirmar",MessageBoxButton.YesNo)==MessageBoxResult.Yes){_vm.DeleteEntry(Sel);Grid.ItemsSource=_vm.Entries;TxtStatus.Text=_vm.StatusText;}}
    private void BtnOpen_Click(object s,RoutedEventArgs e){if(Sel!=null)_vm.OpenLocation(Sel);}}
