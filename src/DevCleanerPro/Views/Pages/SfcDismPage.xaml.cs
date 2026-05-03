using System.Windows; using System.Windows.Controls; using DevCleanerPro.ViewModels;
namespace DevCleanerPro.Views.Pages;
public partial class SfcDismPage : Page { private readonly SfcDismViewModel _vm=new(); public SfcDismPage(){InitializeComponent();OutputList.ItemsSource=_vm.OutputLines;}
    private async void BtnSfc_Click(object s,RoutedEventArgs e){SetBusy(true);_vm.PropertyChanged+=(_, a)=>Dispatcher.Invoke(()=>{TxtStatus.Text=_vm.StatusText;OutputList.ScrollIntoView(OutputList.Items[^1]);});await _vm.RunSfcAsync();SetBusy(false);}
    private async void BtnDism_Click(object s,RoutedEventArgs e){SetBusy(true);await _vm.RunDismAsync();SetBusy(false);}
    private void SetBusy(bool b){Ring.Visibility=b?Visibility.Visible:Visibility.Collapsed;BtnSfc.IsEnabled=!b;BtnDism.IsEnabled=!b;}}
