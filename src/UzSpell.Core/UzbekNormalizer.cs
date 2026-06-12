namespace UzSpell.Core;

/// <summary>
/// Lotin yozuvidagi apostrof variantlarini lugʻatdagi kanonik belgilarga keltiradi.
/// Lugʻat oʻ/gʻ uchun U+02BB (ʻ), tutuq belgisi uchun U+02BC (ʼ) ishlatadi,
/// lekin foydalanuvchilar koʻpincha oddiy apostrof (') yoki tipografik (‘ ’) belgilarni teradi.
/// </summary>
public static class UzbekNormalizer
{
    /// <summary>U+02BB — oʻ va gʻ harflaridagi belgi (okina).</summary>
    public const char Okina = 'ʻ';

    /// <summary>U+02BC — tutuq belgisi (maʼno, sanʼat).</summary>
    public const char Tutuq = 'ʼ';

    private static readonly char[] ApostropheLikes =
    {
        '\'',      // U+0027 oddiy apostrof
        '`',       // U+0060 grave
        '‘',  // ‘ chap tipografik
        '’',  // ’ oʻng tipografik
        'ʹ',  // ʹ modifier prime
        '′',  // ′ prime
    };

    public static bool IsApostropheLike(char c) =>
        c == Okina || c == Tutuq || Array.IndexOf(ApostropheLikes, c) >= 0;

    /// <summary>
    /// Belgilar soni oʻzgarmaydigan 1:1 normalizatsiya:
    /// o/g dan keyingi apostrof → ʻ (okina), boshqa joyda → ʼ (tutuq).
    /// Allaqachon kanonik boʻlgan ʻ va ʼ tegilmaydi.
    /// </summary>
    public static string NormalizeToken(string token)
    {
        char[]? chars = null;
        for (int i = 0; i < token.Length; i++)
        {
            char c = token[i];
            if (c == Okina || c == Tutuq)
                continue;
            if (Array.IndexOf(ApostropheLikes, c) < 0)
                continue;

            chars ??= token.ToCharArray();
            char prev = i > 0 ? char.ToLowerInvariant(token[i - 1]) : '\0';
            chars[i] = prev is 'o' or 'g' ? Okina : Tutuq;
        }

        return chars is null ? token : new string(chars);
    }
}
