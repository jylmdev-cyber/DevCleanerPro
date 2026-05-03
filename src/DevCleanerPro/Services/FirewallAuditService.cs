using DevCleanerPro.Helpers;

namespace DevCleanerPro.Services;

/// <summary>
/// Módulo 15: Auditoría de reglas de Firewall via PowerShell (portabilidad single-file).
/// </summary>
public class FirewallAuditService
{
    public record FirewallRule(string Name, string DisplayName, string Direction, string Action,
        string Profile, string Program, bool Enabled, bool ProgramExists);

    public async Task<List<FirewallRule>> GetRulesAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        progress?.Report("Obteniendo reglas del firewall...");
        var result = await ProcessRunner.RunAsync("powershell", "-NoProfile -Command \"Get-NetFirewallRule | Select-Object Name,DisplayName,Direction,Action,Profile,Enabled | ConvertTo-Csv -NoTypeInformation\"", 30000, ct);
        if (!result.Success) return new();

        var rules = new List<FirewallRule>();
        var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var line in lines.Skip(1)) // Skip header
        {
            ct.ThrowIfCancellationRequested();
            var parts = ParseCsvLine(line);
            if (parts.Length < 6) continue;
            rules.Add(new FirewallRule(parts[0], parts[1], parts[2], parts[3], parts[4], "", parts[5] == "True", true));
        }
        progress?.Report($"{rules.Count} reglas encontradas");
        return rules;
    }

    public async Task<List<FirewallRule>> FindOrphanRulesAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        progress?.Report("Buscando reglas con programas huérfanos...");
        var result = await ProcessRunner.RunAsync("powershell", "-NoProfile -Command \"Get-NetFirewallApplicationFilter | Where-Object { $_.Program -ne 'Any' } | Select-Object Program | ConvertTo-Csv -NoTypeInformation\"", 30000, ct);
        if (!result.Success) return new();

        var orphans = new List<FirewallRule>();
        foreach (var line in result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Skip(1))
        {
            var prog = line.Trim('"');
            if (!string.IsNullOrEmpty(prog) && prog != "Any" && !File.Exists(prog))
                orphans.Add(new FirewallRule("", "", "", "", "", prog, false, false));
        }
        progress?.Report($"{orphans.Count} reglas con programas huérfanos");
        return orphans;
    }

    public async Task<bool> DisableRuleAsync(string ruleName, CancellationToken ct = default)
    {
        var r = await ProcessRunner.RunAsync("powershell", $"-NoProfile -Command \"Disable-NetFirewallRule -Name '{ruleName}'\"", 10000, ct);
        return r.Success;
    }

    public async Task<bool> EnableRuleAsync(string ruleName, CancellationToken ct = default)
    {
        var r = await ProcessRunner.RunAsync("powershell", $"-NoProfile -Command \"Enable-NetFirewallRule -Name '{ruleName}'\"", 10000, ct);
        return r.Success;
    }

    public async Task<bool> RemoveRuleAsync(string ruleName, CancellationToken ct = default)
    {
        var r = await ProcessRunner.RunAsync("powershell", $"-NoProfile -Command \"Remove-NetFirewallRule -Name '{ruleName}'\"", 10000, ct);
        return r.Success;
    }

    public async Task ExportToCsvAsync(string outputPath, CancellationToken ct = default)
    {
        await ProcessRunner.RunAsync("powershell", $"-NoProfile -Command \"Get-NetFirewallRule | Export-Csv -Path '{outputPath}' -NoTypeInformation\"", 30000, ct);
    }

    private static string[] ParseCsvLine(string line)
    {
        return line.Split(',').Select(s => s.Trim().Trim('"')).ToArray();
    }
}
