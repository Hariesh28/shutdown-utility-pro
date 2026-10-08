using System;
using System.Globalization;

internal static class CommandLineParser
{
    public static bool IsInteractiveLaunch(string[] args)
    {
        if (args == null || args.Length == 0) return true;
        return args.Length == 1
            && (String.Equals(args[0], "/tray", StringComparison.OrdinalIgnoreCase)
                || String.Equals(args[0], "/panel", StringComparison.OrdinalIgnoreCase));
    }

    public static bool TryParseDelay(string value, int minimum, int maximum, out int seconds)
    {
        seconds = 0;
        if (minimum < 1 || maximum < minimum || String.IsNullOrEmpty(value)) return false;

        int parsed;
        if (!Int32.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out parsed)
            || parsed < minimum || parsed > maximum)
            return false;

        seconds = parsed;
        return true;
    }
}
