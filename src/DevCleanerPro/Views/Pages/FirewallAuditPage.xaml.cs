using System.Windows; using System.Windows.Controls; using DevCleanerPro.ViewModels;
namespace DevCleanerPro.Views.Pages;
public partial class FirewallAuditPage : Page { private readonly FirewallAuditViewModel _vm=new(); public FirewallAuditPage()=>InitializeComponent();
    private async void BtnLoad_Click(object s,RoutedEventArgs e){Ring.Visibility=Visibility.Visible;_vm.PropertyChanged+=(_, a)=>Dispatcher.Invoke(()=>TxtStatus.Text=_vm.StatusText);await _vm.LoadAsync();Grid.ItemsSource=_vm.Rules;Ring.Visibility=Visibility.Collapsed;}
    private async void BtnOrphans_Click(object s,RoutedEventArgs e){Ring.Visibility=Visibility.Visible;await _vm.FindOrphansAsync();Grid.ItemsSource=_vm.Orphans;Ring.Visibility=Visibility.Collapsed;}
    private async void BtnExport_Click(object s,RoutedEventArgs e){var d=new Microsoft.Win32.SaveFileDialog{Filter="CSV|*.csv",FileName="firewall_rules.csv"};if(d.ShowDialog()==true){await _vm.ExportAsync(d.FileName);TxtStatus.Text=$"Exportado a {d.FileName}";}}}
