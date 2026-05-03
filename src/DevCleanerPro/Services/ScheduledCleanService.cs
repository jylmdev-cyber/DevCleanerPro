using System.Text.Json;
using Microsoft.Win32.TaskScheduler;

namespace DevCleanerPro.Services;

/// <summary>
/// Módulo 18: Tareas programadas de limpieza con Windows Task Scheduler.
/// </summary>
public class ScheduledCleanService
{
    private const string TaskFolder = "DevCleanerPro";

    public record ScheduledProfile(string Name, string Description, string Schedule, string[] Modules);

    public static readonly ScheduledProfile[] Presets =
    {
        new("Limpieza Diaria Ligera", "Temp + DNS Flush", "Diario 03:00", new[] { "TempCleaner", "DNSFlush" }),
        new("Limpieza Semanal Profunda", "Todos los módulos de limpieza", "Semanal Domingo 02:00",
            new[] { "TempCleaner", "MemoryOptimizer", "DevPackageCleaner", "DNSFlush", "EventLogCleaner" }),
    };

    public bool CreateTask(string taskName, string description, DaysOfTheWeek days, short hour, short minute)
    {
        try
        {
            using var ts = new TaskService();
            var td = ts.NewTask();
            td.RegistrationInfo.Description = description;
            td.Triggers.Add(new WeeklyTrigger(days) { StartBoundary = DateTime.Today.AddHours(hour).AddMinutes(minute) });
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "DevCleanerPro.exe";
            td.Actions.Add(new ExecAction(exePath, "--quickclean --silent"));
            td.Principal.RunLevel = TaskRunLevel.Highest;
            td.Settings.StartWhenAvailable = true;

            var folder = ts.RootFolder;
            try { folder = ts.RootFolder.SubFolders[TaskFolder]; }
            catch { folder = ts.RootFolder.CreateFolder(TaskFolder); }
            folder.RegisterTaskDefinition(taskName, td);
            return true;
        }
        catch { return false; }
    }

    public List<(string Name, string Description, DateTime? NextRun, bool Enabled)> GetTasks()
    {
        var tasks = new List<(string, string, DateTime?, bool)>();
        try
        {
            using var ts = new TaskService();
            TaskFolder folder;
            try { folder = ts.RootFolder.SubFolders[TaskFolder]; } catch { return tasks; }
            foreach (var t in folder.GetTasks())
                tasks.Add((t.Name, t.Definition.RegistrationInfo.Description ?? "", t.NextRunTime, t.Enabled));
        }
        catch { }
        return tasks;
    }

    public bool DeleteTask(string taskName)
    {
        try
        {
            using var ts = new TaskService();
            ts.RootFolder.SubFolders[TaskFolder].DeleteTask(taskName);
            return true;
        }
        catch { return false; }
    }

    public void ExportConfig(string outputPath)
    {
        var tasks = GetTasks();
        File.WriteAllText(outputPath, JsonSerializer.Serialize(tasks, new JsonSerializerOptions { WriteIndented = true }));
    }
}
