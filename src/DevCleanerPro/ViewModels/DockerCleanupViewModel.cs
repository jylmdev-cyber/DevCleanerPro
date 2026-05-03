using DevCleanerPro.Services;

namespace DevCleanerPro.ViewModels;

public class DockerCleanupViewModel : ViewModelBase
{
    private readonly DockerService _service = new();
    private string _statusText = "Verificando Docker...";
    private bool _isWorking;
    private bool _isDockerInstalled;
    private DockerDiskUsage? _diskUsage;

    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
    public bool IsWorking { get => _isWorking; set => SetProperty(ref _isWorking, value); }
    public bool IsDockerInstalled { get => _isDockerInstalled; set => SetProperty(ref _isDockerInstalled, value); }
    public DockerDiskUsage? DiskUsage { get => _diskUsage; set => SetProperty(ref _diskUsage, value); }

    public AsyncRelayCommand RefreshCommand => new(RefreshAsync);
    public AsyncRelayCommand PruneAllCommand => new(PruneAllAsync, () => IsDockerInstalled && !IsWorking);
    public AsyncRelayCommand PruneImagesCommand => new(PruneImagesAsync, () => IsDockerInstalled && !IsWorking);
    public AsyncRelayCommand PruneBuildCacheCommand => new(PruneBuildCacheAsync, () => IsDockerInstalled && !IsWorking);

    public async Task RefreshAsync()
    {
        IsWorking = true;
        IsDockerInstalled = await _service.IsDockerInstalledAsync();
        if (!IsDockerInstalled) { StatusText = "Docker no está instalado o no está en el PATH"; IsWorking = false; return; }
        StatusText = "Analizando uso de Docker...";
        DiskUsage = await _service.GetDiskUsageAsync();
        StatusText = DiskUsage != null ? $"{DiskUsage.Items.Count} categorías de Docker analizadas" : "No se pudo obtener información de Docker";
        IsWorking = false;
    }

    private async Task PruneAllAsync() { IsWorking = true; StatusText = "Purgando todo Docker..."; var r = await _service.PruneSystemAsync(); StatusText = r; await RefreshAsync(); IsWorking = false; }
    private async Task PruneImagesAsync() { IsWorking = true; StatusText = "Purgando imágenes..."; var r = await _service.PruneImagesAsync(); StatusText = r; await RefreshAsync(); IsWorking = false; }
    private async Task PruneBuildCacheAsync() { IsWorking = true; StatusText = "Purgando build cache..."; var r = await _service.PruneBuildCacheAsync(); StatusText = r; await RefreshAsync(); IsWorking = false; }
}
