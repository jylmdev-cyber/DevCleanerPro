using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DevCleanerPro.Services;
using Hardcodet.Wpf.TaskbarNotification;
using Wpf.Ui.Controls;

namespace DevCleanerPro;

public partial class MainWindow : FluentWindow
{
    private readonly DispatcherTimer _metricsTimer = new();
    private readonly ObservableCollection<OutputLine> _outputLines = new();
    private readonly SettingsService _settingsService = new();
    private PerformanceCounter? _cpuCounter;
    private TaskbarIcon? _trayIcon;

    public MainWindow()
    {
        InitializeComponent();
        OutputList.ItemsSource = _outputLines;

        try { InitAdminBadge(); } catch { }
        try { InitKeyboardShortcuts(); } catch { }

        // Defer heavy init to after window is shown
        Loaded += (_, _) =>
        {
            try { InitMetrics(); } catch { }
            try { InitSystemTray(); } catch { }
            AddOutput("INFO", "DevCleaner Pro v2.0 iniciado — 21 módulos disponibles");
        };
    }

    // ═══ Admin Detection ═══
    private void InitAdminBadge()
    {
        var isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        if (!isAdmin)
        {
            TxtAdmin.Text = "NO ADMIN";
            TxtAdmin.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xf8, 0x51, 0x49));
            AdminBadge.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x15, 0xf8, 0x51, 0x49));
        }
    }

    // ═══ Real-time Metrics ═══
    private void InitMetrics()
    {
        try { _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"); _cpuCounter.NextValue(); } catch { }
        _metricsTimer.Interval = TimeSpan.FromSeconds(2);
        _metricsTimer.Tick += (_, _) => { try { UpdateMetrics(); } catch { } };
        _metricsTimer.Start();
        try { UpdateMetrics(); } catch { }
    }

    private void UpdateMetrics()
    {
        if (_cpuCounter != null)
        {
            var cpu = (int)_cpuCounter.NextValue();
            TxtCpu.Text = $"{cpu}%";
            TxtCpu.Foreground = new SolidColorBrush(cpu > 80
                ? System.Windows.Media.Color.FromRgb(0xf8, 0x51, 0x49)
                : cpu > 50 ? System.Windows.Media.Color.FromRgb(0xd2, 0x99, 0x22)
                : System.Windows.Media.Color.FromRgb(0xe6, 0xed, 0xf3));
        }
        try
        {
            using var ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            var availMb = ramCounter.NextValue();
            var gcInfo = GC.GetGCMemoryInfo();
            var totalGb = gcInfo.TotalAvailableMemoryBytes / 1073741824.0;
            var usedGb = totalGb - (availMb / 1024.0);
            TxtRam.Text = $"{usedGb:F1}/{totalGb:F0} GB";
        }
        catch { }
        try
        {
            var drive = new DriveInfo("C");
            var diskPct = (int)((double)(drive.TotalSize - drive.AvailableFreeSpace) / drive.TotalSize * 100);
            TxtDisk.Text = $"{diskPct}%";
            TxtDisk.Foreground = new SolidColorBrush(diskPct > 90
                ? System.Windows.Media.Color.FromRgb(0xf8, 0x51, 0x49)
                : diskPct > 75 ? System.Windows.Media.Color.FromRgb(0xd2, 0x99, 0x22)
                : System.Windows.Media.Color.FromRgb(0xe6, 0xed, 0xf3));
        }
        catch { }
        try
        {
            var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
            TxtUptime.Text = $"↑ {(int)uptime.TotalHours}h {uptime.Minutes}m";
        }
        catch { }
    }

    // ═══ System Tray ═══
    private void InitSystemTray()
    {
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "DevCleaner Pro v2.0",
            Visibility = Visibility.Collapsed
        };
        try
        {
            var exePath = Environment.ProcessPath;
            if (exePath != null)
                _trayIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
        }
        catch { }

        var menu = new System.Windows.Controls.ContextMenu();
        var miQuick = new System.Windows.Controls.MenuItem { Header = "⚡ Quick Clean" };
        miQuick.Click += (_, _) => { ShowFromTray(); BtnQuickClean_Click(this, new RoutedEventArgs()); };
        var miOpen = new System.Windows.Controls.MenuItem { Header = "Abrir DevCleaner Pro" };
        miOpen.Click += (_, _) => ShowFromTray();
        menu.Items.Add(miQuick);
        menu.Items.Add(miOpen);
        menu.Items.Add(new System.Windows.Controls.Separator());
        var miExit = new System.Windows.Controls.MenuItem { Header = "Salir" };
        miExit.Click += (_, _) => { _trayIcon?.Dispose(); Application.Current.Shutdown(); };
        menu.Items.Add(miExit);
        _trayIcon.ContextMenu = menu;
        _trayIcon.TrayMouseDoubleClick += (_, _) => ShowFromTray();
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        if (WindowState == WindowState.Minimized && _settingsService.Load().MinimizeToTray)
        {
            Hide();
            if (_trayIcon != null) _trayIcon.Visibility = Visibility.Visible;
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (_settingsService.Load().MinimizeToTray)
        {
            e.Cancel = true;
            WindowState = WindowState.Minimized;
            Hide();
            if (_trayIcon != null)
            {
                _trayIcon.Visibility = Visibility.Visible;
                _trayIcon.ShowBalloonTip("DevCleaner Pro", "Minimizado a la bandeja", BalloonIcon.Info);
            }
            return;
        }
        _trayIcon?.Dispose();
        base.OnClosing(e);
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        if (_trayIcon != null) _trayIcon.Visibility = Visibility.Collapsed;
    }

    // ═══ Keyboard Shortcuts ═══
    private void InitKeyboardShortcuts()
    {
        InputBindings.Add(new KeyBinding(new RelayCmd(() =>
            AddOutput("INFO", "Refrescando módulo actual (F5)")), Key.F5, ModifierKeys.None));

        InputBindings.Add(new KeyBinding(new RelayCmd(() =>
        {
            NavigationView.Navigate(typeof(Views.Pages.QuickCleanPage));
            AddOutput("INFO", "Quick Clean iniciado (Ctrl+Shift+C)");
        }), Key.C, ModifierKeys.Control | ModifierKeys.Shift));

        InputBindings.Add(new KeyBinding(new RelayCmd(() =>
        {
            _outputLines.Clear();
            TxtOutputCount.Text = "";
        }), Key.L, ModifierKeys.Control));

        InputBindings.Add(new KeyBinding(new RelayCmd(() =>
            NavigationView.Navigate(typeof(Views.Pages.SettingsPage))), Key.OemComma, ModifierKeys.Control));
    }

    // ═══ Navigation ═══
    private void NavigationView_Loaded(object sender, RoutedEventArgs e)
    {
        if (NavigationView.MenuItems.Count > 0)
            NavigationView.Navigate(typeof(Views.Pages.DashboardPage));
    }

    private void BtnQuickClean_Click(object sender, RoutedEventArgs e) =>
        NavigationView.Navigate(typeof(Views.Pages.QuickCleanPage));

    // ═══ Output Panel ═══
    public void AddOutput(string level, string message)
    {
        _outputLines.Add(new OutputLine(level, message));
        TxtOutputCount.Text = $"{_outputLines.Count} líneas";
        if (_outputLines.Count > 500) _outputLines.RemoveAt(0);
        if (OutputList.Items.Count > 0) OutputList.ScrollIntoView(OutputList.Items[^1]);
    }

    private void BtnToggleOutput_Click(object sender, RoutedEventArgs e) =>
        OutputPanel.Visibility = OutputPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

    private void BtnCopyOutput_Click(object sender, RoutedEventArgs e)
    {
        if (_outputLines.Count > 0)
        {
            var text = string.Join(Environment.NewLine, _outputLines.Select(l => $"{l.Timestamp} {l.LevelTag} {l.Message}"));
            Clipboard.SetText(text);
            StatusText.Text = "Output copiado";
        }
    }

    private void BtnClearOutput_Click(object sender, RoutedEventArgs e)
    {
        _outputLines.Clear();
        TxtOutputCount.Text = "";
    }

    public void SetProgress(int value)
    {
        GlobalProgress.Visibility = value > 0 && value < 100 ? Visibility.Visible : Visibility.Collapsed;
        GlobalProgress.Value = value;
    }

    private class RelayCmd(Action execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute();
    }

    /// <summary>Structured output line with syntax highlighting support.</summary>
    public class OutputLine
    {
        private static readonly SolidColorBrush InfoBrush = new(System.Windows.Media.Color.FromRgb(0x8b, 0x94, 0x9e));  // DevTextMuted
        private static readonly SolidColorBrush WarnBrush = new(System.Windows.Media.Color.FromRgb(0xd2, 0x99, 0x22));  // DevAccentYellow
        private static readonly SolidColorBrush ErrorBrush = new(System.Windows.Media.Color.FromRgb(0xf8, 0x51, 0x49)); // DevAccentRed
        private static readonly SolidColorBrush OkBrush = new(System.Windows.Media.Color.FromRgb(0x3f, 0xb9, 0x50));    // DevAccentGreen
        private static readonly SolidColorBrush DebugBrush = new(System.Windows.Media.Color.FromRgb(0xbc, 0x8c, 0xff)); // DevAccentPurple

        public string Timestamp { get; }
        public string LevelTag { get; }
        public string Message { get; }
        public SolidColorBrush LevelBrush { get; }

        public OutputLine(string level, string message)
        {
            Timestamp = $"[{DateTime.Now:HH:mm:ss}]";
            LevelTag = $"[{level}]";
            Message = message;
            LevelBrush = level.ToUpperInvariant() switch
            {
                "WARN" or "WARNING" => WarnBrush,
                "ERROR" or "FAIL" => ErrorBrush,
                "OK" or "SUCCESS" or "DONE" => OkBrush,
                "DEBUG" => DebugBrush,
                _ => InfoBrush
            };
        }
    }
}