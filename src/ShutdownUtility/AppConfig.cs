using System;
using System.IO;
using System.Text;

internal enum PowerAction
{
    Shutdown,
    Restart,
    Sleep,
    Hibernate,
    Lock
}
internal sealed class AppConfig
{
    public int CountdownSeconds = 3;
    public string SoundFile = "Shutdown.wav";
    public bool PlaySound = true;
    public bool WaitForSound = true;
    public bool ShowNotification = true;
    public bool EnableDesktopOnlyHotkey = false;
    public string Hotkey = "Ctrl+Alt+Shift+S";
    public bool StartInTray = false;
    public bool MinimizeToTray = true;
    public bool CloseToTray = true;
    public bool StartWithWindows = false;
    public bool ConfirmScheduledActions = true;
    public bool RememberWindowPosition = true;
    public bool DarkTheme = true;
    public bool TestMode = true;
    public bool ForceTestMode = false;
    public int ScheduledDefaultMinutes = 10;

    public static AppConfig Load(string path)
    {
        AppConfig c = new AppConfig();
        if (!File.Exists(path)) return c;
        try
        {
            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#") || !line.Contains("=")) continue;
                int eq = line.IndexOf('=');
                string key = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim();
                ParseInt(key, value, "CountdownSeconds", ref c.CountdownSeconds, 1, 3600);
                ParseInt(key, value, "ScheduledDefaultMinutes", ref c.ScheduledDefaultMinutes, 1, 10080);
                ParseString(key, value, "SoundFile", ref c.SoundFile);
                ParseString(key, value, "Hotkey", ref c.Hotkey);
                ParseBool(key, value, "PlaySound", ref c.PlaySound);
                ParseBool(key, value, "WaitForSound", ref c.WaitForSound);
                ParseBool(key, value, "ShowNotification", ref c.ShowNotification);
                ParseBool(key, value, "EnableDesktopOnlyHotkey", ref c.EnableDesktopOnlyHotkey);
                ParseBool(key, value, "StartInTray", ref c.StartInTray);
                ParseBool(key, value, "MinimizeToTray", ref c.MinimizeToTray);
                ParseBool(key, value, "CloseToTray", ref c.CloseToTray);
                ParseBool(key, value, "StartWithWindows", ref c.StartWithWindows);
                ParseBool(key, value, "ConfirmScheduledActions", ref c.ConfirmScheduledActions);
                ParseBool(key, value, "RememberWindowPosition", ref c.RememberWindowPosition);
                ParseBool(key, value, "DarkTheme", ref c.DarkTheme);
                ParseBool(key, value, "TestMode", ref c.TestMode);
            }
        }
        catch (Exception ex)
        {
            Logger.Write("Config load failed: " + ex);
        }
        c.SoundFile = String.IsNullOrWhiteSpace(c.SoundFile) ? "Shutdown.wav" : c.SoundFile;
        c.Hotkey = String.IsNullOrWhiteSpace(c.Hotkey) ? "Ctrl+Alt+Shift+S" : c.Hotkey;
        return c;
    }

    public void Save(string path)
    {
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Shutdown Utility Pro configuration");
            sb.AppendLine("CountdownSeconds=" + CountdownSeconds);
            sb.AppendLine("SoundFile=" + SoundFile);
            sb.AppendLine("PlaySound=" + PlaySound);
            sb.AppendLine("WaitForSound=" + WaitForSound);
            sb.AppendLine("ShowNotification=" + ShowNotification);
            sb.AppendLine("EnableDesktopOnlyHotkey=" + EnableDesktopOnlyHotkey);
            sb.AppendLine("Hotkey=" + Hotkey);
            sb.AppendLine("StartInTray=" + StartInTray);
            sb.AppendLine("MinimizeToTray=" + MinimizeToTray);
            sb.AppendLine("CloseToTray=" + CloseToTray);
            sb.AppendLine("StartWithWindows=" + StartWithWindows);
            sb.AppendLine("ConfirmScheduledActions=" + ConfirmScheduledActions);
            sb.AppendLine("RememberWindowPosition=" + RememberWindowPosition);
            sb.AppendLine("DarkTheme=" + DarkTheme);
            sb.AppendLine("TestMode=" + TestMode);
            sb.AppendLine("ScheduledDefaultMinutes=" + ScheduledDefaultMinutes);
            File.WriteAllText(temp, sb.ToString(), Encoding.UTF8);
            if (File.Exists(path)) File.Replace(temp, path, null);
            else File.Move(temp, path);
        }
        catch (Exception ex)
        {
            Logger.Write("Config save failed: " + ex);
            try { if (File.Exists(temp)) File.Delete(temp); } catch { }
            throw;
        }
    }

    private static void ParseInt(string key, string value, string target, ref int dest, int min, int max)
    {
        if (!String.Equals(key, target, StringComparison.OrdinalIgnoreCase)) return;
        int n;
        if (Int32.TryParse(value, out n)) dest = Math.Max(min, Math.Min(max, n));
    }
    private static void ParseString(string key, string value, string target, ref string dest)
    {
        if (String.Equals(key, target, StringComparison.OrdinalIgnoreCase)) dest = value;
    }
    private static void ParseBool(string key, string value, string target, ref bool dest)
    {
        if (!String.Equals(key, target, StringComparison.OrdinalIgnoreCase)) return;
        bool b;
        if (Boolean.TryParse(value, out b)) dest = b;
    }
}
