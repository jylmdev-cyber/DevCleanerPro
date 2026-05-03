using System.Windows; using System.Windows.Controls; using DevCleanerPro.ViewModels;
namespace DevCleanerPro.Views.Pages;
public partial class QuickCleanPage : Page { private readonly QuickCleanViewModel _vm=new(); public QuickCleanPage()=>InitializeComponent();
    private async void BtnStart_Click(object s,RoutedEventArgs e){BtnStart.IsEnabled=false;BtnCancel.IsEnabled=true;_vm.PropertyChanged+=(_, a)=>Dispatcher.Invoke(()=>{TxtStatus.Text=_vm.StatusText;TxtStep.Text=_vm.CurrentStep;ProgressBar.Value=_vm.Progress;TxtCurrent.Text=_vm.CurrentStep;TxtFreed.Text=_vm.TotalFreedText;});await _vm.RunAsync();BtnStart.IsEnabled=true;BtnCancel.IsEnabled=false;}
    private void BtnCancel_Click(object s,RoutedEventArgs e){_vm.Cancel();BtnCancel.IsEnabled=false;TxtStatus.Text="Cancelando...";}}
