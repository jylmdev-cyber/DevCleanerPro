using DevCleanerPro.Helpers;
using DevCleanerPro.Services;

namespace DevCleanerPro.ViewModels;

/// <summary>
/// Módulo 20: Quick Clean — ejecuta secuencialmente TempCleaner → MemoryOptimizer → DevPackageCleaner → DNSFlush → EventLogCleaner.
/// </summary>
public class QuickCleanViewModel : ViewModelBase
{
    private readonly AuditLogService _audit = new();
    private string _statusText = "Pulse Iniciar para limpieza rápida";
    private bool _isWorking;
    private double _progress;
    private string _currentStep = "";
    private long _totalFreed;
    private CancellationTokenSource? _cts;

    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
    public bool IsWorking { get => _isWorking; set => SetProperty(ref _isWorking, value); }
    public double Progress { get => _progress; set => SetProperty(ref _progress, value); }
    public string CurrentStep { get => _currentStep; set => SetProperty(ref _currentStep, value); }
    public string TotalFreedText => FileSizeFormatter.Format(_totalFreed);

    public async Task RunAsync()
    {
        _cts = new CancellationTokenSource();
        IsWorking = true; _totalFreed = 0; Progress = 0;

        var steps = new (string Name, ICleanerModule Module)[]
        {
            ("Archivos Temporales", new TempFileService() as ICleanerModule ?? throw new InvalidOperationException()),
            ("Memoria", new MemoryOptimizerService()),
            ("Cachés de Paquetes", new CacheCleanerService() as ICleanerModule ?? throw new InvalidOperationException()),
            ("DNS Flush", new DnsFlushService()),
            ("Event Logs", new EventLogCleanerService()),
        };

        for (int i = 0; i < steps.Length; i++)
        {
            if (_cts.Token.IsCancellationRequested) break;
            var (name, module) = steps[i];
            CurrentStep = $"Paso {i + 1}/{steps.Length}: {name}";
            StatusText = $"Ejecutando: {name}...";
            Progress = (double)i / steps.Length * 100;

            try
            {
                var result = await module.CleanAsync(new Progress<string>(m => StatusText = m), _cts.Token);
                _totalFreed += result.FreedBytes;
                _audit.Log(name, "QuickClean", result.FreedBytes, result.ItemsCleaned);
            }
            catch (Exception ex)
            {
                _audit.Log(name, "QuickClean Error", error: ex.Message);
            }
        }

        Progress = 100;
        OnPropertyChanged(nameof(TotalFreedText));
        StatusText = $"✓ Limpieza rápida completa — {TotalFreedText} liberados";
        CurrentStep = "Completado";
        IsWorking = false;
        _cts.Dispose(); _cts = null;
    }

    public void Cancel() => _cts?.Cancel();
}
