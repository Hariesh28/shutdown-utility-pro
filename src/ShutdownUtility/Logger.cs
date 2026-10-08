using System;
using System.IO;
using System.Text;

internal static class Logger
{
    private static readonly object Sync = new object();
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ShutdownUtilityPro", "Shutdown.log");

    public static string PathName { get { return LogPath; } }

    public static void Write(string message)
    {
        try
        {
            lock (Sync)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
                if (File.Exists(LogPath))
                {
                    FileInfo info = new FileInfo(LogPath);
                    if (info.Length > 2 * 1024 * 1024)
                    {
                        string oldPath = LogPath + ".old";
                        try { if (File.Exists(oldPath)) File.Delete(oldPath); File.Move(LogPath, oldPath); } catch { }
                    }
                }
                File.AppendAllText(LogPath,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  " + message + Environment.NewLine,
                    Encoding.UTF8);
            }
        }
        catch { }
    }
}
