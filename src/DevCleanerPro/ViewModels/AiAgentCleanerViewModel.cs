using System.Collections.ObjectModel;
using DevCleanerPro.Helpers;
using DevCleanerPro.Models;
using DevCleanerPro.Services;

namespace DevCleanerPro.ViewModels;

public class AiAgentCleanerViewModel : ViewModelBase
{
    private readonly AiAgentCleanerService _svc = new();
    private readonly AuditLogService _audit = new();
    private string _statusText = "Pulse Escanear para detectar herramientas de IA";
    private bool _isWorking;
    private long _totalSize, _safeSize, _unsafeSize;

    public ObservableCollection<AiToolInfo> Tools { get; } = new();
    public ObservableCollection<AiCacheItem> AllItems { get; } = new();
    public ObservableCollection<LlmModelInfo> Models { get; } = new();
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
    public bool IsWorking { get => _isWorking; set => SetProperty(ref _isWorking, value); }
    public long TotalSize { get => _totalSize; set { SetProperty(ref _totalSize, value); OnPropertyChanged(nameof(TotalSizeText)); } }
    public long SafeSize { get => _safeSize; set { SetProperty(ref _safeSize, value); OnPropertyChanged(nameof(SafeSizeText)); } }
    public long UnsafeSize { get => _unsafeSize; set { SetProperty(ref _unsafeSize, value); OnPropertyChanged(nameof(UnsafeSizeText)); } }
    public string TotalSizeText => FileSizeFormatter.Format(TotalSize);
    public string SafeSizeText => FileSizeFormatter.Format(SafeSize);
    public string UnsafeSizeText => FileSizeFormatter.Format(UnsafeSize);

    public async Task ScanAsync()
    {
        IsWorking = true; Tools.Clear(); AllItems.Clear(); Models.Clear();
        var progress = new Progress<string>(m => StatusText = m);
        var tools = await _svc.ScanAllAsync(progress);
        foreach (var t in tools) { Tools.Add(t); foreach (var i in t.Items) AllItems.Add(i); }
        TotalSize = tools.Sum(t => t.TotalSize);
        SafeSize = tools.Sum(t => t.SafeSize);
        UnsafeSize = tools.Sum(t => t.UnsafeSize);
        var models = await _svc.GetAllModelsAsync();
        foreach (var m in models) Models.Add(m);
        StatusText = $"{tools.Count} herramientas detectadas — {FileSizeFormatter.Format(TotalSize)} total ({FileSizeFormatter.Format(SafeSize)} seguro)";
        IsWorking = false;
    }

    public void SelectAllSafe() { foreach (var i in AllItems.Where(i => i.IsSafe)) i.IsSelected = true; }
    public void SelectOrphans() { foreach (var i in AllItems.Where(i => i.IsOrphan)) i.IsSelected = true; }
    public void DeselectAll() { foreach (var i in AllItems) i.IsSelected = false; }

    public async Task CleanSelectedAsync()
    {
        IsWorking = true;
        var selected = AllItems.Where(i => i.IsSelected).ToList();
        if (selected.Count == 0) { StatusText = "No hay elementos seleccionados"; IsWorking = false; return; }
        // Verificar herramientas en ejecución
        var runningTools = selected.Select(i => i.ToolName).Distinct().Where(t => _svc.IsToolRunning(t)).ToList();
        if (runningTools.Count > 0) StatusText = $"⚠ Herramientas en ejecución: {string.Join(", ", runningTools)} — continúa bajo tu responsabilidad";
        var progress = new Progress<string>(m => StatusText = m);
        var result = await _svc.CleanSelectedAsync(selected, progress);
        _audit.Log(Name, "AiClean", result.FreedBytes, result.ItemsCleaned);
        StatusText = $"✓ {FileSizeFormatter.Format(result.FreedBytes)} liberados — {result.ItemsCleaned} elementos, {result.Errors} errores";
        await ScanAsync();
        IsWorking = false;
    }

    public async Task DeleteModelAsync(LlmModelInfo model)
    {
        IsWorking = true;
        if (!string.IsNullOrEmpty(model.CliCommand))
        {
            StatusText = $"Ejecutando: {model.CliCommand}";
            var parts = model.CliCommand.Split(' ', 3);
            var r = await Helpers.ProcessRunner.RunAsync(parts[0], string.Join(' ', parts.Skip(1)), 30000);
            StatusText = r.Success ? $"✓ Modelo {model.Name} eliminado" : $"✗ {r.Error}";
        }
        else if (Directory.Exists(model.Path))
        {
            await SafeFileOps.DeleteDirectoryAsync(model.Path, null);
            StatusText = $"✓ Modelo {model.Name} eliminado";
        }
        else if (File.Exists(model.Path))
        {
            try { File.Delete(model.Path); StatusText = $"✓ {model.Name} eliminado"; } catch (Exception ex) { StatusText = $"✗ {ex.Message}"; }
        }
        _audit.Log("AI Models", $"Eliminar {model.Source}/{model.Name}", model.Size);
        Models.Remove(model);
        IsWorking = false;
    }

    private string Name => "AI Agent Cleaner";
}
