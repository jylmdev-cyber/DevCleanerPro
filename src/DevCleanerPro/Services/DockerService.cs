using DevCleanerPro.Helpers;

namespace DevCleanerPro.Services;

public class DockerService
{
    public bool IsDockerInstalled => ProcessRunner.IsCommandAvailable("docker");

    public async Task<bool> IsDockerInstalledAsync() => await ProcessRunner.IsCommandAvailableAsync("docker");

    public async Task<DockerDiskUsage?> GetDiskUsageAsync(CancellationToken ct = default)
    {
        if (!await IsDockerInstalledAsync()) return null;
        var result = await ProcessRunner.RunAsync("docker", "system df --format \"{{.Type}}|{{.TotalCount}}|{{.Size}}|{{.Reclaimable}}\"", 15000, ct);
        if (!result.Success) return null;

        var usage = new DockerDiskUsage();
        foreach (var line in result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = line.Split('|');
            if (parts.Length < 4) continue;
            var item = new DockerDiskItem { Type = parts[0], Count = parts[1], Size = parts[2], Reclaimable = parts[3] };
            usage.Items.Add(item);
        }
        return usage;
    }

    public async Task<string> PruneSystemAsync(CancellationToken ct = default)
    {
        if (!await IsDockerInstalledAsync()) return "Docker no está instalado";
        var result = await ProcessRunner.RunAsync("docker", "system prune -af --volumes", 120000, ct);
        return result.Success ? result.Output : $"Error: {result.Error}";
    }

    public async Task<string> PruneImagesAsync(CancellationToken ct = default)
    {
        if (!await IsDockerInstalledAsync()) return "Docker no está instalado";
        var result = await ProcessRunner.RunAsync("docker", "image prune -af", 60000, ct);
        return result.Success ? result.Output : $"Error: {result.Error}";
    }

    public async Task<string> PruneBuildCacheAsync(CancellationToken ct = default)
    {
        if (!await IsDockerInstalledAsync()) return "Docker no está instalado";
        var result = await ProcessRunner.RunAsync("docker", "builder prune -af", 60000, ct);
        return result.Success ? result.Output : $"Error: {result.Error}";
    }
}

public class DockerDiskUsage { public List<DockerDiskItem> Items { get; set; } = new(); }
public class DockerDiskItem { public string Type { get; set; } = ""; public string Count { get; set; } = ""; public string Size { get; set; } = ""; public string Reclaimable { get; set; } = ""; }

