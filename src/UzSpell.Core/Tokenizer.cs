namespace UzSpell.Core;

/// <summary>Matndagi bitta soʻz: matni va joylashuvi.</summary>
public readonly record struct Token(string Text, int Start, int Length);

/// <summary>
/// Matnni soʻzlarga ajratadi. Soʻz tarkibiga harflar, soʻz ichidagi defis
/// (katta-katta) va apostrof belgilari (oʻ, gʻ, maʼno, bogʻ) kiradi.
/// Raqamli boʻlaklar va tinish belgilari soʻzga kirmaydi.
/// </summary>
public static class Tokenizer
{
    public static IEnumerable<Token> Tokenize(string text)
    {
        int i = 0, n = text.Length;
        while (i < n)
        {
            if (!IsCoreLetter(text[i]))
            {
                i++;
                continue;
            }

            int start = i;
            while (i < n && IsWordChar(text, i))
                i++;

            int end = i;

            // Soʻz oxiridagi ortiqcha belgilarni olib tashlash:
            // defis va apostrof faqat o/g dan keyin oxirida qolishi mumkin (bogʻ, togʻ).
            while (end > start)
            {
                char c = text[end - 1];
                if (IsCoreLetter(c))
                    break;
                if (UzbekNormalizer.IsApostropheLike(c) && end - 1 > start)
                {
                    char prev = char.ToLowerInvariant(text[end - 2]);
                    if (prev is 'o' or 'g')
                        break;
                }
                end--;
            }

            if (end > start)
                yield return new Token(text[start..end], start, end - start);
        }
    }

    /// <summary>Apostrofdan boshqa "haqiqiy" harf (lotin, kirill va h.k.).</summary>
    private static bool IsCoreLetter(char c) =>
        char.IsLetter(c) && !UzbekNormalizer.IsApostropheLike(c);

    private static bool IsWordChar(string s, int i)
    {
        char c = s[i];
        if (IsCoreLetter(c))
            return true;

        // Apostrof va defis faqat harfdan keyin kelsa soʻz tarkibida hisoblanadi
        if (UzbekNormalizer.IsApostropheLike(c) || c == '-')
            return i > 0 && IsCoreLetter(s[i - 1]);

        return false;
    }

    /// <summary>Indeks boʻyicha qator/ustun hisoblash (1 dan boshlanadi).</summary>
    public static (int Line, int Column) GetLineColumn(string text, int index)
    {
        int line = 1, col = 1;
        for (int i = 0; i < index && i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                line++;
                col = 1;
            }
            else if (text[i] != '\r')
            {
                col++;
            }
        }
        return (line, col);
    }
}
