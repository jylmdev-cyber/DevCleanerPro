using System.Diagnostics;
using System.Runtime.InteropServices;
using DevCleanerPro.Models;

namespace DevCleanerPro.Services;

/// <summary>
/// Módulo 2: Optimizador de memoria con EmptyWorkingSet y limpieza de Standby List.
/// </summary>
public class MemoryOptimizerService : ICleanerModule
{
    public string Name => "Optimizador de Memoria";
    public string Description => "Libera RAM de procesos y limpia Standby List";
    public string IconGlyph => "\uE950";

    [DllImport("psapi.dll")]
    private static extern bool EmptyWorkingSet(IntPtr hProcess);

    [DllImport("ntdll.dll")]
    private static extern uint NtSetSystemInformation(int infoClass, IntPtr info, int length);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr hProcess, uint access, out IntPtr hToken);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool LookupPrivilegeValue(string? host, string name, out long luid);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool AdjustTokenPrivileges(IntPtr hToken, bool disableAll, ref TOKEN_PRIVILEGES newState, int bufLen, IntPtr prev, IntPtr retLen);

    [StructLayout(LayoutKind.Sequential)]
    private struct TOKEN_PRIVILEGES { public int Count; public long Luid; public int Attributes; }

    private const int SE_PRIVILEGE_ENABLED = 2;
    private const int SystemMemoryListInformation = 0x0050;
    private const int MemoryPurgeStandbyList = 4;

    public (long freeBefore, long freeAfter) GetMemoryInfo()
    {
        var gc = GC.GetGCMemoryInfo();
        return (gc.TotalAvailableMemoryBytes, gc.TotalAvailableMemoryBytes);
    }

    public Task<long> AnalyzeAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var info = GC.GetGCMemoryInfo();
        var avail = info.TotalAvailableMemoryBytes;
        progress?.Report($"RAM disponible: {avail / 1024 / 1024} MB");
        return Task.FromResult(0L);
    }

    public async Task<CleaningResult> CleanAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var result = new CleaningResult { CategoryName = Name };
        var sw = Stopwatch.StartNew();
        var gcBefore = GC.GetGCMemoryInfo();
        long ramBefore = gcBefore.TotalAvailableMemoryBytes;

        // 1. EmptyWorkingSet para todos los procesos
        await Task.Run(() =>
        {
            int count = 0;
            foreach (var proc in Process.GetProcesses())
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    EmptyWorkingSet(proc.Handle);
                    count++;
                }
                catch { }
            }
            progress?.Report($"EmptyWorkingSet ejecutado en {count} procesos");
            result.ItemsCleaned = count;
        }, ct);

        // 2. Purge Standby List
        await Task.Run(() =>
        {
            try
            {
                EnablePrivilege("SeProfileSingleProcessPrivilege");
                int command = MemoryPurgeStandbyList;
                int size = Marshal.SizeOf(command);
                IntPtr ptr = Marshal.AllocHGlobal(size);
                Marshal.WriteInt32(ptr, command);
                uint status = NtSetSystemInformation(SystemMemoryListInformation, ptr, size);
                Marshal.FreeHGlobal(ptr);
                progress?.Report(status == 0 ? "Standby List purgada correctamente" : $"NtSetSystemInformation retornó: 0x{status:X}");
            }
            catch (Exception ex)
            {
                progress?.Report($"Error al purgar Standby List: {ex.Message}");
                result.Errors++;
            }
        }, ct);

        await Task.Delay(500, ct); // Esperar estabilización
        var gcAfter = GC.GetGCMemoryInfo();
        long ramAfter = gcAfter.TotalAvailableMemoryBytes;
        long freed = ramAfter - ramBefore;
        result.FreedBytes = freed > 0 ? freed : 0;
        sw.Stop();
        result.Duration = sw.Elapsed;
        progress?.Report($"RAM antes: {ramBefore / 1024 / 1024} MB → después: {ramAfter / 1024 / 1024} MB (liberados: {freed / 1024 / 1024} MB)");
        return result;
    }

    private static void EnablePrivilege(string privilege)
    {
        OpenProcessToken(Process.GetCurrentProcess().Handle, 0x0020 | 0x0008, out IntPtr token);
        LookupPrivilegeValue(null, privilege, out long luid);
        var tp = new TOKEN_PRIVILEGES { Count = 1, Luid = luid, Attributes = SE_PRIVILEGE_ENABLED };
        AdjustTokenPrivileges(token, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
    }
}
