using DevCleanerPro.Helpers;
using DevCleanerPro.Models;

namespace DevCleanerPro.Services;

/// <summary>
/// Módulo 7: DNS Flush y limpieza de caché de red.
/// </summary>
public class DnsFlushService : ICleanerModule
{
    public string Name => "DNS Flush";
    public string Description => "Limpia caché DNS, NetBIOS y reinicia servicio DNS Client";
    public string IconGlyph => "\uE968";

    public Task<long> AnalyzeAsync(IProgress<string>? progress = null, CancellationToken ct = default)
        => Task.FromResult(0L);

    public async Task<CleaningResult> CleanAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var result = new CleaningResult { CategoryName = Name };
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // 1. ipconfig /flushdns
        progress?.Report("Ejecutando ipconfig /flushdns...");
        var r1 = await ProcessRunner.RunAsync("ipconfig", "/flushdns", 10000, ct);
        result.ItemsCleaned++;
        progress?.Report(r1.Success ? $"✓ {r1.Output.Trim()}" : $"✗ {r1.Error}");
        if (!r1.Success) result.Errors++;

        // 2. nbtstat -R (NetBIOS cache)
        progress?.Report("Ejecutando nbtstat -R...");
        var r2 = await ProcessRunner.RunAsync("nbtstat", "-R", 10000, ct);
        result.ItemsCleaned++;
        progress?.Report(r2.Success ? "✓ Caché NetBIOS limpiada" : $"✗ {r2.Error}");
        if (!r2.Success) result.Errors++;

        // 3. Reiniciar servicio DnsCache si está atascado
        progress?.Report("Reiniciando servicio DNS Client...");
        var r3 = await ProcessRunner.RunAsync("net", "stop DnsCache", 10000, ct);
        await Task.Delay(500, ct);
        var r4 = await ProcessRunner.RunAsync("net", "start DnsCache", 10000, ct);
        result.ItemsCleaned++;
        progress?.Report(r4.Success ? "✓ Servicio DNS Client reiniciado" : "⚠ No se pudo reiniciar (puede estar protegido)");

        sw.Stop();
        result.Duration = sw.Elapsed;
        return result;
    }
}
