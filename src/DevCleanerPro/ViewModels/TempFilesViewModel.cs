using System.Collections.ObjectModel;
using DevCleanerPro.Helpers;
using DevCleanerPro.Models;
using DevCleanerPro.Services;

namespace DevCleanerPro.ViewModels;

public class TempFilesViewModel : ViewModelBase
{
    private readonly TempFileService _service = new();
    private string _statusText = "Pulse Analizar para detectar archivos temporales";
    private bool _isWorking;
    private long _totalSize;

    public ObservableCollection<TempFileCategory> Categories { get; } = new();
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
    public bool IsWorking { get => _isWorking; set => SetProperty(ref _isWorking, value); }
    public long TotalSize { get => _totalSize; set { SetProperty(ref _totalSize, value); OnPropertyChanged(nameof(TotalSizeText)); } }
    public string TotalSizeText => FileSizeFormatter.Format(TotalSize);

    public AsyncRelayCommand AnalyzeCommand => new(AnalyzeAsync, () => !IsWorking);
    public AsyncRelayCommand CleanCommand => new(CleanAsync, () => !IsWorking && Categories.Any(c => c.IsSelected));

    public async Task AnalyzeAsync()
    {
        IsWorking = true;
        Categories.Clear();
        var progress = new Progress<string>(msg => StatusText = msg);
        try
        {
            var cats = await _service.GetTempCategoriesAsync(progress);
            foreach (var c in cats.Where(c => c.Exists && c.Size > 0).OrderByDescending(c => c.Size))
            {
                c.IsSelected = true;
                Categories.Add(c);
            }
            TotalSize = Categories.Sum(c => c.Size);
            StatusText = $"{Categories.Count} categorías — {TotalSizeText} total";
        }
        catch (Exception ex) { StatusText = $"Error: {ex.Message}"; }
        finally { IsWorking = false; }
    }

    public async Task CleanAsync()
    {
        IsWorking = true;
        var progress = new Progress<string>(msg => StatusText = msg);
        try
        {
            var result = await _service.CleanTempFilesAsync(Categories.ToList(), progress);
            StatusText = $"Limpieza: {FileSizeFormatter.Format(result.FreedBytes)} liberados, {result.Errors} errores";
            await AnalyzeAsync();
        }
        catch (Exception ex) { StatusText = $"Error: {ex.Message}"; }
        finally { IsWorking = false; }
    }
}
