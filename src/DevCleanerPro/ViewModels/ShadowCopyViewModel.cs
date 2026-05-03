using System.Collections.ObjectModel;
using DevCleanerPro.Services;

namespace DevCleanerPro.ViewModels;

public class ShadowCopyViewModel : ViewModelBase
{
    private readonly ShadowCopyService _svc = new();
    private string _statusText = "";  private bool _isWorking;

    public ObservableCollection<ShadowCopyService.ShadowCopyItem> Items { get; } = new();
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
    public bool IsWorking { get => _isWorking; set => SetProperty(ref _isWorking, value); }

    public async Task LoadAsync()
    {
        IsWorking = true; Items.Clear();
        var items = await _svc.ListAsync();
        foreach (var i in items) Items.Add(i);
        StatusText = $"{items.Count} shadow copies encontradas";
        IsWorking = false;
    }

    public async Task DeleteAsync(string id) { IsWorking = true; await _svc.DeleteAsync(id, new Progress<string>(m => StatusText = m)); await LoadAsync(); IsWorking = false; }
    public async Task DeleteAllAsync() { IsWorking = true; await _svc.DeleteAllAsync(new Progress<string>(m => StatusText = m)); await LoadAsync(); IsWorking = false; }
    public void OpenSystemRestore() => _svc.OpenSystemRestore();
}
