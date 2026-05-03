using System.Windows; using System.Windows.Controls; using DevCleanerPro.ViewModels; using DevCleanerPro.Services;
namespace DevCleanerPro.Views.Pages;
public partial class ShadowCopyPage : Page { private readonly ShadowCopyViewModel _vm=new(); public ShadowCopyPage()=>InitializeComponent();
    private async void BtnLoad_Click(object s,RoutedEventArgs e){Ring.Visibility=Visibility.Visible;_vm.PropertyChanged+=(_, a)=>Dispatcher.Invoke(()=>TxtStatus.Text=_vm.StatusText);await _vm.LoadAsync();Grid.ItemsSource=_vm.Items;Ring.Visibility=Visibility.Collapsed;}
    private async void BtnDelete_Click(object s,RoutedEventArgs e){if(Grid.SelectedItem is ShadowCopyService.ShadowCopyItem sc&&MessageBox.Show($"¿Eliminar shadow copy?","Confirmar",MessageBoxButton.YesNo)==MessageBoxResult.Yes){Ring.Visibility=Visibility.Visible;await _vm.DeleteAsync(sc.Id);Grid.ItemsSource=_vm.Items;Ring.Visibility=Visibility.Collapsed;}}
    private async void BtnDeleteAll_Click(object s,RoutedEventArgs e){if(MessageBox.Show("⚠️ ¿Eliminar TODAS las shadow copies?","Confirmar",MessageBoxButton.YesNo,MessageBoxImage.Warning)==MessageBoxResult.Yes){Ring.Visibility=Visibility.Visible;await _vm.DeleteAllAsync();Grid.ItemsSource=_vm.Items;Ring.Visibility=Visibility.Collapsed;}}
    private void BtnOpenRestore_Click(object s,RoutedEventArgs e)=>_vm.OpenSystemRestore();}
