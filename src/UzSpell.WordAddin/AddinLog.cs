using System.IO;

namespace UzSpell.WordAddin;

/// <summary>
/// Add-in yuklanish bosqichlarini faylga yozadigan oddiy jurnal.
/// Har bir yozuvdan keyin fayl yopiladi — jarayon yiqilsa ham saqlanadi.
/// Fayl: %LOCALAPPDATA%\UzSpell\addin-debug.log
/// </summary>
internal static class AddinLog
{
    private static readonly string LogPath = BuildPath();

    private static string BuildPath()
    {
        try
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "UzSpell");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "addin-debug.log");
        }
        catch
        {
            return Path.Combine(Path.GetTempPath(), "uzspell-addin-debug.log");
        }
    }

    public static void Write(string message)
    {
        try
        {
            string line = $"{DateTime.Now:HH:mm:ss.fff}  {message}{Environment.NewLine}";
            File.AppendAllText(LogPath, line);
        }
        catch
        {
            // jurnal yoza olmasa ham add-in ishlashda davom etadi
        }
    }
}
