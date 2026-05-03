using System.Windows; using System.Windows.Controls; using DevCleanerPro.ViewModels;
namespace DevCleanerPro.Views.Pages;
public partial class ScheduledCleanPage : Page { private readonly ScheduledCleanViewModel _vm=new(); public ScheduledCleanPage(){InitializeComponent();Loaded+=(_, _)=>{_vm.Load();Grid.ItemsSource=_vm.Tasks;TxtStatus.Text=_vm.StatusText;};}
    private void BtnDaily_Click(object s,RoutedEventArgs e){_vm.CreatePreset(0);Grid.ItemsSource=_vm.Tasks;TxtStatus.Text=_vm.StatusText;}
    private void BtnWeekly_Click(object s,RoutedEventArgs e){_vm.CreatePreset(1);Grid.ItemsSource=_vm.Tasks;TxtStatus.Text=_vm.StatusText;}
    private void BtnRefresh_Click(object s,RoutedEventArgs e){_vm.Load();Grid.ItemsSource=_vm.Tasks;TxtStatus.Text=_vm.StatusText;}
    private void BtnDelete_Click(object s,RoutedEventArgs e){if(Grid.SelectedItem is ValueTuple<string,string,DateTime?,bool> t){_vm.Delete(t.Item1);Grid.ItemsSource=_vm.Tasks;TxtStatus.Text=_vm.StatusText;}}}
