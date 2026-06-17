namespace UzSpell.Core;

/// <summary>
/// Son soʻzlari va ularning «-ta» dona koʻrsatkichli shakllari.
/// Lugʻatda «beshta», «ikkita» kabi shakllar yoʻq, lekin ular imloviy toʻgʻri.
/// </summary>
public static class UzbekNumerals
{
    private static readonly HashSet<string> Words = new(StringComparer.Ordinal)
    {
        // Lotin
        "bir", "ikki", "uch", "toʻrt", "besh", "olti", "yetti", "sakkiz",
        "toʻqqiz", "oʻn", "yigirma", "oʻttiz", "qirq", "ellik", "oltmish",
        "yetmish", "sakson", "toʻqson", "yuz", "ming", "million", "milliard",
        "necha", "yarim",
        // Kirill
        "бир", "икки", "уч", "тўрт", "беш", "олти", "етти", "саккиз",
        "тўққиз", "ўн", "йигирма", "ўттиз", "қирқ", "эллик", "олтмиш",
        "етмиш", "саксон", "тўқсон", "юз", "минг", "миллион", "миллиард",
        "неча", "ярим",
    };

    private static readonly HashSet<string> SpecialTaForms = new(StringComparer.Ordinal)
    {
        "bitta",   // bir → bitta
        "nechta",  // necha → nechta
        "битта",
        "нечта",
    };

    private static readonly string[] TaSuffixes = { "ta", "та" };

    /// <summary>Sof son soʻzi yoki raqam (besh, oʻn, 25).</summary>
    public static bool IsNumberWord(string norm) =>
        AllDigits(norm) || Words.Contains(norm);

    /// <summary>Son, raqam yoki «-ta» shakli (besh, beshta, 5ta, bitta).</summary>
    public static bool IsNumeral(string norm)
    {
        if (norm.Length == 0)
            return false;
        if (IsNumberWord(norm) || SpecialTaForms.Contains(norm))
            return true;
        foreach (var ta in TaSuffixes)
        {
            if (norm.Length > ta.Length && norm.EndsWith(ta, StringComparison.Ordinal))
            {
                string head = norm.Substring(0, norm.Length - ta.Length);
                if (AllDigits(head) || Words.Contains(head))
                    return true;
            }
        }
        return false;
    }

    private static bool AllDigits(string s)
    {
        if (s.Length == 0)
            return false;
        foreach (char c in s)
            if (!char.IsDigit(c))
                return false;
        return true;
    }
}
