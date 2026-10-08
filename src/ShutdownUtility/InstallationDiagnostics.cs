using System;
using System.IO;

internal static class InstallationDiagnostics
{
    public static bool Run(string applicationDirectory, string userDataDirectory, out string error)
    {
        error = null;
        Logger.Write("Installation self-test started.");

        string executablePath = Path.Combine(applicationDirectory, "Shutdown.exe");
        string seedConfigPath = Path.Combine(applicationDirectory, "Shutdown.config");
        if (!File.Exists(executablePath))
            return Fail("Shutdown.exe is missing from the application folder.", out error);
        Logger.Write("Installation self-test passed: application executable is present.");

        if (!File.Exists(seedConfigPath))
            return Fail("Shutdown.config is missing from the application folder.", out error);
        AppConfig seedConfig = AppConfig.Load(seedConfigPath);
        if (!seedConfig.TestMode)
            return Fail("The packaged configuration does not enable Test mode by default.", out error);
        Logger.Write("Installation self-test passed: packaged configuration enables Test mode.");

        if (!CanWriteUserData(userDataDirectory, out error)) return false;

        foreach (PowerAction action in Enum.GetValues(typeof(PowerAction)))
        {
            bool simulated;
            string actionError;
            if (!PowerController.Execute(action, true, out simulated, out actionError) || !simulated || actionError != null)
                return Fail("Safe simulation failed for " + action + ".", out error);
            Logger.Write("Installation self-test passed: " + action + " simulation.");
        }

        PowerAction[] scheduledActions = { PowerAction.Shutdown, PowerAction.Restart };
        foreach (PowerAction action in scheduledActions)
        {
            bool simulated;
            string scheduleError;
            if (!PowerController.Schedule(action, 60, true, out simulated, out scheduleError) || !simulated || scheduleError != null)
                return Fail("Safe schedule simulation failed for " + action + ".", out error);
            Logger.Write("Installation self-test passed: " + action + " schedule simulation.");
        }

        Logger.Write("Installation self-test passed. No Windows power action or native schedule was requested.");
        return true;
    }

    private static bool CanWriteUserData(string userDataDirectory, out string error)
    {
        error = null;
        string probePath = Path.Combine(userDataDirectory, ".self-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(userDataDirectory);
            File.WriteAllText(probePath, "Shutdown Utility Pro");
            if (!File.Exists(probePath))
                return Fail("The per-user data folder did not retain a test file.", out error);
            Logger.Write("Installation self-test passed: per-user data folder is writable.");
            return true;
        }
        catch (IOException ex)
        {
            return Fail("The per-user data folder is not writable: " + ex.Message, out error);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Fail("The per-user data folder is not writable: " + ex.Message, out error);
        }
        finally
        {
            if (File.Exists(probePath)) File.Delete(probePath);
        }
    }

    private static bool Fail(string message, out string error)
    {
        error = message;
        Logger.Write("Installation self-test failed: " + message);
        return false;
    }
}
