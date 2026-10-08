using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

internal static class NativeMethods
{
    public const int WM_HOTKEY = 0x0312;
    public const int WM_NCHITTEST = 0x0084;
    public const int HTCAPTION = 2;
    public const int WM_POWERBROADCAST = 0x0218;
    public const int SW_RESTORE = 9;
    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
    [DllImport("user32.dll")] public static extern bool LockWorkStation();
    [DllImport("powrprof.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] public static extern uint GetLastError();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    public static IntPtr FindWindowByCaption(string caption) { return FindWindow(null, caption); }

    public static bool IsDesktopForeground()
    {
        IntPtr h = GetForegroundWindow();
        if (h == IntPtr.Zero) return false;
        StringBuilder sb = new StringBuilder(256);
        if (GetClassName(h, sb, sb.Capacity) <= 0) return false;
        string cls = sb.ToString();
        return String.Equals(cls, "Progman", StringComparison.OrdinalIgnoreCase)
            || String.Equals(cls, "WorkerW", StringComparison.OrdinalIgnoreCase);
    }
}

internal static class HotkeyParser
{
    public static bool TryParse(string text, out uint modifiers, out uint key)
    {
        modifiers = 0; key = 0;
        if (String.IsNullOrWhiteSpace(text)) return false;
        string[] parts = text.Split(new char[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
        bool haveKey = false;
        foreach (string raw in parts)
        {
            string p = raw.Trim();
            if (p.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || p.Equals("Control", StringComparison.OrdinalIgnoreCase)) modifiers |= NativeMethods.MOD_CONTROL;
            else if (p.Equals("Alt", StringComparison.OrdinalIgnoreCase)) modifiers |= NativeMethods.MOD_ALT;
            else if (p.Equals("Shift", StringComparison.OrdinalIgnoreCase)) modifiers |= NativeMethods.MOD_SHIFT;
            else if (p.Equals("Win", StringComparison.OrdinalIgnoreCase) || p.Equals("Windows", StringComparison.OrdinalIgnoreCase)) modifiers |= NativeMethods.MOD_WIN;
            else
            {
                if (haveKey) return false;
                if (p.Length == 1)
                {
                    char ch = Char.ToUpperInvariant(p[0]);
                    if ((ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9')) { key = (uint)ch; haveKey = true; continue; }
                }
                if (p.StartsWith("F", StringComparison.OrdinalIgnoreCase))
                {
                    int fn;
                    if (Int32.TryParse(p.Substring(1), out fn) && fn >= 1 && fn <= 24)
                    { key = (uint)(0x70 + fn - 1); haveKey = true; continue; }
                }
                return false;
            }
        }
        return haveKey && modifiers != 0;
    }
}

internal static class PowerController
{
    private static readonly string ShutdownExe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shutdown.exe");

    public static bool Execute(PowerAction action, bool testMode, out bool simulated, out string error)
    {
        simulated = false;
        error = null;
        if (testMode)
        {
            simulated = true;
            return true;
        }

        try
        {
            switch (action)
            {
                case PowerAction.Shutdown: return RunShutdown("/s /t 0", out error);
                case PowerAction.Restart: return RunShutdown("/r /t 0", out error);
                case PowerAction.Lock:
                    if (!NativeMethods.LockWorkStation()) { error = new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message; return false; }
                    return true;
                case PowerAction.Sleep:
                    if (!NativeMethods.SetSuspendState(false, false, false)) { error = new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message; return false; }
                    return true;
                case PowerAction.Hibernate:
                    if (!NativeMethods.SetSuspendState(true, false, false)) { error = new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message; return false; }
                    return true;
            }
        }
        catch (Exception ex) { error = ex.Message; }
        return false;
    }

    public static bool Schedule(PowerAction action, int seconds, bool testMode, out bool simulated, out string error)
    {
        simulated = false;
        error = null;
        if (seconds < 1) { error = "Delay must be at least 1 second."; return false; }
        if (action != PowerAction.Shutdown && action != PowerAction.Restart)
        {
            error = "Windows provides native delayed scheduling for shutdown and restart only.";
            return false;
        }
        if (testMode)
        {
            simulated = true;
            return true;
        }

        try
        {
            if (action == PowerAction.Shutdown) return RunShutdown("/s /t " + seconds, out error);
            if (action == PowerAction.Restart) return RunShutdown("/r /t " + seconds, out error);
        }
        catch (Exception ex) { error = ex.Message; return false; }
        error = "Unsupported scheduled power action.";
        return false;
    }

    public static bool CancelScheduled(out string error)
    {
        return RunShutdown("/a", out error);
    }

    private static bool RunShutdown(string args, out string error)
    {
        error = null;
        Process p = new Process();
        p.StartInfo.FileName = ShutdownExe;
        p.StartInfo.Arguments = args;
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.CreateNoWindow = true;
        p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        try
        {
            if (!p.Start()) { error = "Windows did not start shutdown.exe."; return false; }
            p.WaitForExit(5000);
            if (p.HasExited && p.ExitCode != 0) { error = "shutdown.exe returned exit code " + p.ExitCode + "."; return false; }
            return true;
        }
        finally { p.Dispose(); }
    }
}

internal static class StartupManager
{
    private const string RunKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
    private const string ValueName = "ShutdownUtilityPro";

    public static void SetEnabled(bool enabled, string exePath)
    {
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, true))
        {
            if (key == null) throw new InvalidOperationException("Unable to access the current-user startup registry key.");
            if (enabled) key.SetValue(ValueName, "\"" + exePath + "\" /tray");
            else key.DeleteValue(ValueName, false);
        }
    }

    public static bool IsEnabled()
    {
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, false))
        {
            return key != null && key.GetValue(ValueName) != null;
        }
    }
}

internal static class AppResources
{
    public static string BaseDir { get { return AppContext.BaseDirectory; } }
    public static string UserDataDir
    {
        get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ShutdownUtilityPro"); }
    }
    public static string ConfigPath { get { return Path.Combine(UserDataDir, "Shutdown.config"); } }
    public static string WindowStatePath { get { return Path.Combine(UserDataDir, "WindowState.txt"); } }

    public static void InitializeUserData()
    {
        Directory.CreateDirectory(UserDataDir);
        MigrateLegacyFile("Shutdown.config", ConfigPath);
        MigrateLegacyFile("Shutdown.log", Logger.PathName);
        MigrateLegacyFile("WindowState.txt", WindowStatePath);
    }

    private static void MigrateLegacyFile(string fileName, string destination)
    {
        string legacyPath = Path.Combine(BaseDir, fileName);
        if (!File.Exists(destination) && File.Exists(legacyPath)) File.Copy(legacyPath, destination);
    }

    public static string ResolveSound(AppConfig config, PowerAction action)
    {
        string actionFile = action == PowerAction.Shutdown ? "Shutdown.wav" :
                            action == PowerAction.Restart ? "Restart.wav" :
                            action == PowerAction.Sleep ? "Sleep.wav" :
                            action == PowerAction.Hibernate ? "Hibernate.wav" : "Lock.wav";
        string actionPath = Path.Combine(BaseDir, actionFile);
        if (File.Exists(actionPath)) return actionPath;
        string configured = config.SoundFile;
        if (Path.IsPathRooted(configured)) return configured;
        return Path.Combine(BaseDir, configured);
    }

    public static Icon GetAppIcon()
    {
        try
        {
            string iconPath = Path.Combine(BaseDir, "Shutdown.ico");
            if (File.Exists(iconPath)) return new Icon(iconPath);
            Icon exeIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (exeIcon != null) return exeIcon;
        }
        catch { }
        return SystemIcons.Application;
    }
}
