using System.IO;
using System.Text.Json;
using UzSpell.Core;

namespace UzSpell.App;

/// <summary>Foydalanuvchi sozlamalari — %APPDATA%\UzSpell\settings.json ga saqlanadi.</summary>
public sealed class AppSettings
{
    /// <summary>null — avto-aniqlash, aks holda majburiy yozuv.</summary>
    public UzbekScript? Script { get; set; }

    /// <summary>Grammatika qoidalarini tekshirish.</summary>
    public bool Grammar { get; set; } = true;

    /// <summary>BOSH HARFLI qisqartmalarni (AQSH, BMT) ham tekshirish.</summary>
    public bool CheckAllCaps { get; set; }

    /// <summary>Har bir xato uchun koʻrsatiladigan takliflar soni.</summary>
    public int MaxSuggestions { get; set; } = 6;

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "UzSpell", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var s = JsonSerializer.Deserialize<AppSettings>(json);
                if (s is not null)
                {
                    s.MaxSuggestions = Math.Clamp(s.MaxSuggestions, 1, 15);
                    return s;
                }
            }
        }
        catch
        {
            // sozlamalarni oʻqib boʻlmasa standart qiymatlar
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
        catch
        {
            // saqlab boʻlmasa ham dastur ishlayveradi
        }
    }
}
