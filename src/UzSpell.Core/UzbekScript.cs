namespace UzSpell.Core;

/// <summary>Oʻzbek tilida ishlatiladigan yozuv turlari.</summary>
public enum UzbekScript
{
    Latin,
    Cyrillic,
}

public static class ScriptDetector
{
    /// <summary>
    /// Soʻzdagi harflarga qarab yozuv turini aniqlaydi.
    /// Harf boʻlmasa null qaytaradi.
    /// </summary>
    public static UzbekScript? DetectToken(string token)
    {
        int latin = 0, cyrillic = 0;
        foreach (char c in token)
        {
            if (c is >= 'a' and <= 'z' or >= 'A' and <= 'Z')
                latin++;
            else if (c is >= 'Ѐ' and <= 'ӿ') // Kirill bloki (ў, қ, ғ, ҳ ham shu yerda)
                cyrillic++;
        }

        if (latin == 0 && cyrillic == 0)
            return null;
        return cyrillic > latin ? UzbekScript.Cyrillic : UzbekScript.Latin;
    }

    /// <summary>
    /// Butun matndagi ustun yozuvni aniqlaydi (grammatikani toʻgʻri yozuvda
    /// ishlatish uchun). Harf topilmasa Latin qaytaradi.
    /// </summary>
    public static UzbekScript DetectDominant(string text)
    {
        int latin = 0, cyrillic = 0;
        foreach (char c in text)
        {
            if (c is >= 'a' and <= 'z' or >= 'A' and <= 'Z')
                latin++;
            else if (c is >= 'Ѐ' and <= 'ӿ')
                cyrillic++;
        }
        return cyrillic > latin ? UzbekScript.Cyrillic : UzbekScript.Latin;
    }
}
