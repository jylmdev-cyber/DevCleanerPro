using System.Collections.ObjectModel;
using DevCleanerPro.Services;

namespace DevCleanerPro.ViewModels;

public class SfcDismViewModel : ViewModelBase
{
    private readonly SfcDismService _svc = new();
    private string _statusText = "Listo";
    private bool _isWorking;

    public ObservableCollection<string> OutputLines { get; } = new();
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
    public bool IsWorking { get => _isWorking; set => SetProperty(ref _isWorking, value); }

    public async Task RunSfcAsync()
    {
        IsWorking = true; OutputLines.Clear();
        var progress = new Progress<string>(m =>
        {
            StatusText = m;
            System.Windows.Application.Current.Dispatcher.Invoke(() => OutputLines.Add(m));
        });
        await _svc.RunSfcAsync(progress);
        IsWorking = false;
    }

    public async Task RunDismAsync()
    {
        IsWorking = true; OutputLines.Clear();
        var progress = new Progress<string>(m =>
        {
            StatusText = m;
            System.Windows.Application.Current.Dispatcher.Invoke(() => OutputLines.Add(m));
        });
        await _svc.RunDismAsync(progress);
        IsWorking = false;
    }
}
