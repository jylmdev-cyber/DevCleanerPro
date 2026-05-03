using System.Windows; using System.Windows.Controls; using DevCleanerPro.ViewModels;
namespace DevCleanerPro.Views.Pages;
public partial class DnsFlushPage : Page { private readonly DnsFlushViewModel _vm=new(); public DnsFlushPage()=>InitializeComponent();
    private async void BtnFlush_Click(object s,RoutedEventArgs e){Ring.Visibility=Visibility.Visible;BtnFlush.IsEnabled=false;OutputList.ItemsSource=_vm.OutputLines;_vm.PropertyChanged+=(_, a)=>Dispatcher.Invoke(()=>TxtStatus.Text=_vm.StatusText);await _vm.FlushAsync();Ring.Visibility=Visibility.Collapsed;BtnFlush.IsEnabled=true;}}
