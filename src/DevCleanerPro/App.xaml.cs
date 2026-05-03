using System.IO;
using System.Windows;
using Wpf.Ui.Appearance;

namespace DevCleanerPro;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Global exception handlers
        DispatcherUnhandledException += (_, args) =>
        {
            LogCrash("Dispatcher", args.Exception);
            MessageBox.Show($"Error no controlado:\n{args.Exception.Message}\n\n{args.Exception.StackTrace}",
                "DevCleaner Pro — Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                LogCrash("AppDomain", ex);
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            LogCrash("Task", args.Exception);
            args.SetObserved();
        };

        base.OnStartup(e);

        try
        {
            var settingsSvc = new DevCleanerPro.Services.SettingsService();
            var settings = settingsSvc.Load();
            DevCleanerPro.Helpers.ThemeManagerHelper.ApplyTheme(settings.Theme);
            DevCleanerPro.Helpers.LanguageManager.ChangeLanguage(settings.Language);
        }
        catch (Exception ex)
        {
            LogCrash("StartupConfig", ex);
        }
    }

    private static void LogCrash(string source, Exception ex)
    {
        try
        {
            var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash-logs");
            Directory.CreateDirectory(logDir);
            var logFile = Path.Combine(logDir, $"crash-{DateTime.Now:yyyyMMdd-HHmmss}.log");
            File.WriteAllText(logFile, $"[{source}] {DateTime.Now:O}\n{ex}\n");
        }
        catch { }
    }
}
