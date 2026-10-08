using System;
using System.IO;

internal static class RegressionTests
{
    private static int Assertions;

    private static int Main()
    {
        string testDirectory = Path.Combine(Path.GetTempPath(), "ShutdownUtilityPro.Tests." + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDirectory);

        try
        {
            TestSafeDefaults(testDirectory);
            TestConfigurationParsing(testDirectory);
            TestConfigurationPersistence(testDirectory);
            TestPowerActionsAreSimulated();
            TestScheduleValidation();
            Console.WriteLine("PASS: " + Assertions + " assertions.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL after " + Assertions + " assertions: " + ex);
            return 1;
        }
        finally
        {
            if (Directory.Exists(testDirectory)) Directory.Delete(testDirectory, true);
        }
    }

    private static void TestSafeDefaults(string directory)
    {
        AppConfig defaults = AppConfig.Load(Path.Combine(directory, "missing.config"));
        Assert(defaults.TestMode, "Test mode must default to enabled.");
        Assert(defaults.CountdownSeconds == 3, "Default countdown must be three seconds.");
        Assert(defaults.ScheduledDefaultMinutes == 10, "Default schedule must be ten minutes.");
    }

    private static void TestConfigurationParsing(string directory)
    {
        string path = Path.Combine(directory, "parsed.config");
        File.WriteAllText(path,
            "# case-insensitive parsing\r\n" +
            "countdownseconds=0\r\n" +
            "SCHEDULEDDEFAULTMINUTES=99999\r\n" +
            "testmode=false\r\n" +
            "PlaySound=false\r\n" +
            "SoundFile=C:\\sounds\\custom.wav\r\n" +
            "unknown-option=ignored\r\n");

        AppConfig config = AppConfig.Load(path);
        Assert(config.CountdownSeconds == 1, "Countdown must clamp to one second.");
        Assert(config.ScheduledDefaultMinutes == 10080, "Schedule delay must clamp to seven days.");
        Assert(!config.TestMode, "Explicit test-mode configuration must be respected.");
        Assert(!config.PlaySound, "Boolean configuration must parse.");
        Assert(config.SoundFile == "C:\\sounds\\custom.wav", "Paths containing separators must parse.");

        string invalidPath = Path.Combine(directory, "invalid.config");
        File.WriteAllText(invalidPath, "CountdownSeconds=not-a-number\r\nTestMode=maybe\r\n");
        AppConfig invalid = AppConfig.Load(invalidPath);
        Assert(invalid.CountdownSeconds == 3, "Invalid integer values must retain defaults.");
        Assert(invalid.TestMode, "Invalid booleans must retain safe defaults.");
    }

    private static void TestConfigurationPersistence(string directory)
    {
        string path = Path.Combine(directory, "nested", "settings.config");
        AppConfig original = new AppConfig();
        original.CountdownSeconds = 42;
        original.SoundFile = "sounds\\quiet.wav";
        original.PlaySound = false;
        original.WaitForSound = false;
        original.ShowNotification = false;
        original.EnableDesktopOnlyHotkey = true;
        original.Hotkey = "Ctrl+Alt+Shift+Q";
        original.StartInTray = true;
        original.MinimizeToTray = false;
        original.CloseToTray = false;
        original.StartWithWindows = true;
        original.ConfirmScheduledActions = false;
        original.RememberWindowPosition = false;
        original.DarkTheme = false;
        original.TestMode = true;
        original.ScheduledDefaultMinutes = 120;
        original.Save(path);
        original.CountdownSeconds = 43;
        original.Save(path);

        AppConfig loaded = AppConfig.Load(path);
        Assert(loaded.CountdownSeconds == original.CountdownSeconds, "Atomic replacement must persist the latest countdown value.");
        Assert(loaded.SoundFile == original.SoundFile, "Sound path must round-trip.");
        Assert(loaded.PlaySound == original.PlaySound, "PlaySound must round-trip.");
        Assert(loaded.WaitForSound == original.WaitForSound, "WaitForSound must round-trip.");
        Assert(loaded.ShowNotification == original.ShowNotification, "Notification setting must round-trip.");
        Assert(loaded.EnableDesktopOnlyHotkey == original.EnableDesktopOnlyHotkey, "Hotkey setting must round-trip.");
        Assert(loaded.Hotkey == original.Hotkey, "Hotkey value must round-trip.");
        Assert(loaded.StartInTray == original.StartInTray, "StartInTray must round-trip.");
        Assert(loaded.MinimizeToTray == original.MinimizeToTray, "MinimizeToTray must round-trip.");
        Assert(loaded.CloseToTray == original.CloseToTray, "CloseToTray must round-trip.");
        Assert(loaded.StartWithWindows == original.StartWithWindows, "StartWithWindows must round-trip.");
        Assert(loaded.ConfirmScheduledActions == original.ConfirmScheduledActions, "Schedule confirmation must round-trip.");
        Assert(loaded.RememberWindowPosition == original.RememberWindowPosition, "Window-state setting must round-trip.");
        Assert(loaded.DarkTheme == original.DarkTheme, "Theme setting must round-trip.");
        Assert(loaded.TestMode == original.TestMode, "Test mode must round-trip.");
        Assert(loaded.ScheduledDefaultMinutes == original.ScheduledDefaultMinutes, "Schedule delay must round-trip.");
        Assert(Directory.GetFiles(directory, "*.tmp", SearchOption.AllDirectories).Length == 0,
            "Saving must not leave temporary files behind.");
    }

    private static void TestPowerActionsAreSimulated()
    {
        foreach (PowerAction action in Enum.GetValues(typeof(PowerAction)))
        {
            bool simulated;
            string error;
            bool succeeded = PowerController.Execute(action, true, out simulated, out error);
            Assert(succeeded, action + " should succeed in test mode.");
            Assert(simulated, action + " must be reported as simulated.");
            Assert(error == null, action + " simulation must not produce a Windows action error.");
        }
    }

    private static void TestScheduleValidation()
    {
        PowerAction[] schedulableActions = { PowerAction.Shutdown, PowerAction.Restart };
        foreach (PowerAction action in schedulableActions)
        {
            bool simulated;
            string error;
            bool succeeded = PowerController.Schedule(action, 60, true, out simulated, out error);
            Assert(succeeded, action + " schedule should succeed in test mode.");
            Assert(simulated, action + " schedule must be reported as simulated.");
            Assert(error == null, action + " schedule simulation must not produce an error.");
        }

        bool invalidSimulated;
        string invalidError;
        Assert(!PowerController.Schedule(PowerAction.Shutdown, 0, true, out invalidSimulated, out invalidError),
            "A zero-second schedule must be rejected even in test mode.");
        Assert(!invalidSimulated, "Rejected schedules must not be reported as simulated success.");
        Assert(!String.IsNullOrEmpty(invalidError), "Rejected schedules must explain the validation error.");

        bool unsupportedSimulated;
        string unsupportedError;
        Assert(!PowerController.Schedule(PowerAction.Sleep, 60, true, out unsupportedSimulated, out unsupportedError),
            "Unsupported native schedule actions must be rejected in test mode.");
        Assert(!unsupportedSimulated, "Unsupported actions must not be reported as simulated success.");
        Assert(!String.IsNullOrEmpty(unsupportedError), "Unsupported schedules must explain the validation error.");
    }

    private static void Assert(bool condition, string message)
    {
        Assertions++;
        if (!condition) throw new InvalidOperationException(message);
    }
}
