using DevCleanerPro.Helpers;
using DevCleanerPro.Models;

namespace DevCleanerPro.Services;

/// <summary>
/// Módulo 12: SFC y DISM Repair con salida en tiempo real.
/// </summary>
public class SfcDismService
{
    public async Task RunSfcAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        progress?.Report("Verificando si hay otro sfc en ejecución...");
        var check = await ProcessRunner.RunAsync("tasklist", "/FI \"IMAGENAME eq sfc.exe\"", 5000, ct);
        if (check.Output.Contains("sfc.exe", StringComparison.OrdinalIgnoreCase))
        {
            progress?.Report("⚠ Ya hay un proceso sfc.exe en ejecución");
            return;
        }

        progress?.Report("Ejecutando sfc /scannow (esto puede tardar varios minutos)...");
        await RunWithLiveOutput("sfc", "/scannow", progress, ct);
    }

    public async Task RunDismAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        progress?.Report("Ejecutando DISM /Online /Cleanup-Image /RestoreHealth...");
        await RunWithLiveOutput("DISM", "/Online /Cleanup-Image /RestoreHealth", progress, ct);
    }

    public async Task RunDismScanAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        progress?.Report("Ejecutando DISM /Online /Cleanup-Image /ScanHealth...");
        await RunWithLiveOutput("DISM", "/Online /Cleanup-Image /ScanHealth", progress, ct);
    }

    private async Task RunWithLiveOutput(string cmd, string args, IProgress<string>? progress, CancellationToken ct)
    {
        using var process = new System.Diagnostics.Process();
        process.StartInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = cmd, Arguments = args,
            RedirectStandardOutput = true, RedirectStandardError = true,
            UseShellExecute = false, CreateNoWindow = true
        };
        process.Start();
        var outputTask = Task.Run(async () =>
        {
            while (!process.StandardOutput.EndOfStream)
            {
                var line = await process.StandardOutput.ReadLineAsync(ct);
                if (!string.IsNullOrWhiteSpace(line)) progress?.Report(line);
            }
        }, ct);
        var errTask = Task.Run(async () =>
        {
            while (!process.StandardError.EndOfStream)
            {
                var line = await process.StandardError.ReadLineAsync(ct);
                if (!string.IsNullOrWhiteSpace(line)) progress?.Report($"[ERR] {line}");
            }
        }, ct);
        await Task.WhenAll(outputTask, errTask);
        await process.WaitForExitAsync(ct);
        progress?.Report($"\n--- Proceso terminado con código: {process.ExitCode} ---");
    }
}
