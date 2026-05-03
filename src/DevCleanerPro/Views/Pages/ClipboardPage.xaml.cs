using System.Windows; using System.Windows.Controls; using DevCleanerPro.ViewModels;
namespace DevCleanerPro.Views.Pages;
public partial class ClipboardPage : Page { private readonly ClipboardViewModel _vm=new(); public ClipboardPage(){InitializeComponent();Loaded+=(_, _)=>{_vm.Load();ChkHistory.IsChecked=_vm.HistoryEnabled;TxtStatus.Text=_vm.StatusText;};}
    private void BtnClear_Click(object s,RoutedEventArgs e){_vm.Clear();TxtStatus.Text=_vm.StatusText;}
    private void ChkHistory_Click(object s,RoutedEventArgs e){_vm.HistoryEnabled=ChkHistory.IsChecked==true;TxtStatus.Text=_vm.StatusText;}
    private void BtnSettings_Click(object s,RoutedEventArgs e)=>_vm.OpenSettings();}
