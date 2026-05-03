using System.Diagnostics;

namespace DevCleanerPro.Helpers;

/// <summary>
/// Ejecutor de procesos externos con captura de salida.
/// </summary>
public static class ProcessRunner
{
    public static async Task<ProcessResult> RunAsync(
        string fileName,
        string arguments,
        int timeoutMs = 30000,
        CancellationToken ct = default)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(ct);
            var errorTask = process.StandardError.ReadToEndAsync(ct);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(timeoutMs);

            await process.WaitForExitAsync(timeoutCts.Token);

            return new ProcessResult
            {
                ExitCode = process.ExitCode,
                Output = await outputTask,
                Error = await errorTask,
                Success = process.ExitCode == 0
            };
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            return new ProcessResult { Success = false, Error = "Operación cancelada o timeout" };
        }
        catch (Exception ex)
        {
            return new ProcessResult { Success = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// Verifica si un ejecutable está disponible en el PATH (síncrono, usa Task.Run para evitar deadlock).
    /// </summary>
    public static bool IsCommandAvailable(string command)
    {
        try
        {
            return Task.Run(async () =>
            {
                var result = await RunAsync("where", command, 5000);
                return result.Success && !string.IsNullOrWhiteSpace(result.Output);
            }).GetAwaiter().GetResult();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Verifica si un ejecutable está disponible en el PATH (asíncrono, preferido).
    /// </summary>
    public static async Task<bool> IsCommandAvailableAsync(string command)
    {
        try
        {
            var result = await RunAsync("where", command, 5000);
            return result.Success && !string.IsNullOrWhiteSpace(result.Output);
        }
        catch
        {
            return false;
        }
    }
}

public class ProcessResult
{
    public int ExitCode { get; set; }
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public bool Success { get; set; }
}
