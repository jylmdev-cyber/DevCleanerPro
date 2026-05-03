using System.Windows; using System.Windows.Controls; using DevCleanerPro.ViewModels;
namespace DevCleanerPro.Views.Pages;
public partial class AuditLogPage : Page { private readonly AuditLogViewModel _vm=new(); public AuditLogPage(){InitializeComponent();Loaded+=(_, _)=>{_vm.LoadToday();Grid.ItemsSource=_vm.Entries;TxtStatus.Text=_vm.StatusText;};}
    private void BtnToday_Click(object s,RoutedEventArgs e){_vm.LoadToday();Grid.ItemsSource=_vm.Entries;TxtStatus.Text=_vm.StatusText;}
    private void BtnWeek_Click(object s,RoutedEventArgs e){_vm.LoadWeek();Grid.ItemsSource=_vm.Entries;TxtStatus.Text=_vm.StatusText;}
    private void BtnExport_Click(object s,RoutedEventArgs e){var d=new Microsoft.Win32.SaveFileDialog{Filter="CSV|*.csv",FileName="audit_log.csv"};if(d.ShowDialog()==true){_vm.ExportCsv(d.FileName);TxtStatus.Text=_vm.StatusText;}}
    private void BtnPurge_Click(object s,RoutedEventArgs e){if(MessageBox.Show("¿Eliminar logs de más de 30 días?","Confirmar",MessageBoxButton.YesNo)==MessageBoxResult.Yes){_vm.Purge(30);TxtStatus.Text=_vm.StatusText;}}}
