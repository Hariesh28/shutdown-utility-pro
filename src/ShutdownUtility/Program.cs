using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Threading;
using System.Windows.Forms;

internal static class SingleInstance
{
    private static Mutex Mutex;
    public static bool Acquire()
    {
        bool created;
        string name = "Local\\ShutdownUtilityPro-" + Environment.UserName;
        Mutex = new Mutex(true, name, out created);
        return created;
    }
}

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (!SingleInstance.Acquire())
        {
            IntPtr existing = NativeMethods.FindWindowByCaption("Shutdown Utility Pro");
            if (existing != IntPtr.Zero)
            {
                NativeMethods.ShowWindow(existing, NativeMethods.SW_RESTORE);
                NativeMethods.SetForegroundWindow(existing);
            }
            else
            {
                MessageBox.Show("Shutdown Utility Pro is already running. Check the system tray.", "Shutdown Utility Pro", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e) { Logger.Write("UI exception: " + e.Exception); MessageBox.Show("An unexpected error occurred.\r\n\r\n" + e.Exception.Message, "Shutdown Utility Pro", MessageBoxButtons.OK, MessageBoxIcon.Error); };
        AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e) { Logger.Write("Unhandled exception: " + e.ExceptionObject); };

        try { AppResources.InitializeUserData(); }
        catch (Exception ex)
        {
            MessageBox.Show("Could not initialize the per-user data folder.\r\n\r\n" + ex.Message,
                "Shutdown Utility Pro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        AppConfig config = AppConfig.Load(AppResources.ConfigPath);
        bool forceTestMode = false;
        List<string> commandArgs = new List<string>();
        if (args != null)
        {
            foreach (string arg in args)
            {
                if (String.Equals(arg, "/test-mode", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(arg, "--test-mode", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(arg, "/test", StringComparison.OrdinalIgnoreCase))
                    forceTestMode = true;
                else commandArgs.Add(arg);
            }
        }
        if (forceTestMode)
        {
            config.TestMode = true;
            config.ForceTestMode = true;
            Logger.Write("Test mode forced for this run by command line.");
        }
        HandleCommandLine(commandArgs.ToArray(), config);
    }

    private static void HandleCommandLine(string[] args, AppConfig config)
    {
        string first = args != null && args.Length > 0 ? args[0].ToLowerInvariant() : "";
        int seconds;
        if (first == "/help" || first == "-help" || first == "--help")
        {
            MessageBox.Show(GetHelpText(), "Shutdown Utility Pro", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (first == "/version" || first == "-version")
        {
            MessageBox.Show("Shutdown Utility Pro 3.0.0", "Shutdown Utility Pro", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (first == "/cancel" || first == "-cancel")
        {
            string error; bool ok = PowerController.CancelScheduled(out error);
            MessageBox.Show(ok ? "Any pending Windows shutdown/restart timer was cancelled." : "Windows did not cancel a pending timer.\r\n\r\n" + error, "Shutdown Utility Pro", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            Logger.Write("Command-line cancel requested: success=" + ok);
            return;
        }
        if (first == "/test-sound")
        {
            TestConfiguredSound(config); return;
        }
        if (args == null || args.Length == 0) { Application.Run(new MainForm(config)); return; }
        if (first == "/tray" || first == "/panel") { Application.Run(new MainForm(config)); return; }

        if (first == "/schedule-shutdown" || first == "/schedule-restart")
        {
            seconds = config.ScheduledDefaultMinutes * 60;
            if (args.Length > 1 && !Int32.TryParse(args[1], out seconds))
            {
                MessageBox.Show("Schedule delay must be a whole number of seconds.", "Invalid command", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (args.Length > 1) seconds = Math.Max(1, Math.Min(604800, seconds));
            bool simulated;
            string error;
            PowerAction action = first == "/schedule-restart" ? PowerAction.Restart : PowerAction.Shutdown;
            if (!PowerController.Schedule(action, seconds, config.TestMode, out simulated, out error))
                MessageBox.Show(error, "Shutdown Utility Pro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else if (simulated)
                MessageBox.Show("Test mode is enabled. No native schedule was created.", "Shutdown Utility Pro", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show(String.Format("Scheduled {0} in {1} seconds.", action == PowerAction.Shutdown ? "shutdown" : "restart", seconds), "Shutdown Utility Pro", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        PowerAction action2;
        if (!TryParseAction(first, out action2)) { Application.Run(new MainForm(config)); return; }
        seconds = config.CountdownSeconds;
        if (args.Length > 1 && !Int32.TryParse(args[1], out seconds))
        {
            MessageBox.Show("Countdown must be a whole number of seconds.", "Invalid command", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        seconds = Math.Max(1, Math.Min(3600, seconds));
        using (CountdownForm f = new CountdownForm(config, action2, seconds)) f.ShowDialog();
    }

    private static bool TryParseAction(string s, out PowerAction action)
    {
        action = PowerAction.Shutdown;
        if (s == "/shutdown" || s == "shutdown") { action = PowerAction.Shutdown; return true; }
        if (s == "/restart" || s == "restart") { action = PowerAction.Restart; return true; }
        if (s == "/sleep" || s == "sleep") { action = PowerAction.Sleep; return true; }
        if (s == "/hibernate" || s == "hibernate") { action = PowerAction.Hibernate; return true; }
        if (s == "/lock" || s == "lock") { action = PowerAction.Lock; return true; }
        return false;
    }

    private static void TestConfiguredSound(AppConfig config)
    {
        string path = AppResources.ResolveSound(config, PowerAction.Shutdown);
        if (!File.Exists(path)) { MessageBox.Show("Sound not found:\r\n" + path, "Shutdown Utility Pro", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        try { using (SoundPlayer p = new SoundPlayer(path)) { p.Load(); p.PlaySync(); } }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Shutdown Utility Pro", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private static string GetHelpText()
    {
        return "Shutdown Utility Pro command line\r\n\r\n" +
               "Shutdown.exe                 Open dashboard / interactive shutdown\r\n" +
               "Shutdown.exe /shutdown 3      Shutdown after a 3-second countdown\r\n" +
               "Shutdown.exe /tray           Start in system tray\r\n" +
               "Shutdown.exe /restart 5      Restart after 5-second countdown\r\n" +
               "Shutdown.exe /sleep 5        Sleep after 5-second countdown\r\n" +
               "Shutdown.exe /hibernate 5    Hibernate after 5-second countdown\r\n" +
               "Shutdown.exe /lock 3         Lock after a 3-second countdown\r\n" +
               "Shutdown.exe /schedule-shutdown 300  Schedule shutdown in 300 seconds\r\n" +
               "Shutdown.exe /schedule-restart 600   Schedule restart in 600 seconds\r\n" +
               "Shutdown.exe /cancel             Cancel Windows scheduled shutdown/restart\r\n" +
               "Shutdown.exe --test-mode /shutdown 3  Run a simulated 3-second shutdown\r\n" +
               "Shutdown.exe /test-sound         Play configured shutdown sound\r\n" +
               "Shutdown.exe /help           Show this help";
    }
}
