using System.Diagnostics;

namespace DevCleanerPro.Services;

/// <summary>
/// Módulo 16: Monitor de procesos en tiempo real.
/// </summary>
public class ProcessMonitorService
{
    public record ProcessItem(int Pid, string Name, double CpuPercent, long WorkingSetMb, string UserName, string? FilePath);

    public List<ProcessItem> GetProcesses()
    {
        var items = new List<ProcessItem>();
        foreach (var p in Process.GetProcesses())
        {
            try
            {
                items.Add(new ProcessItem(p.Id, p.ProcessName, 0,
                    p.WorkingSet64 / 1024 / 1024, "", TryGetPath(p)));
            }
            catch { }
            finally { try { p.Dispose(); } catch { } }
        }
        return items.OrderByDescending(p => p.WorkingSetMb).ToList();
    }

    public async Task<List<ProcessItem>> GetProcessesAsync()
    {
        return await Task.Run(GetProcesses);
    }

    public bool KillProcess(int pid)
    {
        try { Process.GetProcessById(pid).Kill(); return true; } catch { return false; }
    }

    public bool KillProcessTree(int pid)
    {
        try { Process.GetProcessById(pid).Kill(entireProcessTree: true); return true; } catch { return false; }
    }

    public bool SetPriority(int pid, ProcessPriorityClass priority)
    {
        try { Process.GetProcessById(pid).PriorityClass = priority; return true; } catch { return false; }
    }

    private static string? TryGetPath(Process p) { try { return p.MainModule?.FileName; } catch { return null; } }
}
