namespace UzSpell.Core;

/// <summary>
/// uz_UZ.dic faylidan soʻz turkumlari haqida yengil morfologik maʼlumot oladi.
/// Lugʻatdagi flaglar: X — feʼl qoʻshimchalari, V — kelishik, S — koʻplik,
/// B/C/D/E/F — egalik, A — sifat yasovchi.
/// </summary>
public sealed class UzbekMorphology
{
    private readonly HashSet<string> _verbStems;
    private readonly HashSet<string> _nominalStems;

    private UzbekMorphology(HashSet<string> verbs, HashSet<string> nominals)
    {
        _verbStems = verbs;
        _nominalStems = nominals;
    }

    public static UzbekMorphology LoadFromDic(string dicPath)
    {
        var verbs = new HashSet<string>(StringComparer.Ordinal);
        var nominals = new HashSet<string>(StringComparer.Ordinal);

        foreach (var line in File.ReadLines(dicPath))
        {
            if (line.Length == 0 || char.IsDigit(line[0]))
                continue;

            int slash = line.IndexOf('/');
            string word, flags;
            if (slash < 0)
            {
                word = line.Trim();
                flags = "";
            }
            else
            {
                word = line.Substring(0, slash).Trim();
                flags = line.Substring(slash + 1).Trim();
            }

            if (word.Length == 0)
                continue;

            string lower = word.ToLowerInvariant();
            if (flags.IndexOf('X') >= 0)
                verbs.Add(lower);
            if (flags.IndexOf('V') >= 0 || flags.IndexOf('S') >= 0)
                nominals.Add(lower);
        }

        return new UzbekMorphology(verbs, nominals);
    }

    /// <summary>Soʻz aynan ot/sifat oʻzagi shaklida (qoʻshimchasiz) berilganmi.</summary>
    public bool IsNominalStem(string lowerWord) => _nominalStems.Contains(lowerWord);

    /// <summary>Soʻz feʼl oʻzagimi (lugʻatdagi koʻrinishida).</summary>
    public bool IsVerbStem(string lowerWord) => _verbStems.Contains(lowerWord);

    /// <summary>
    /// Soʻz ichidagi -lar qoʻshimchasini olib tashlashga urinadi:
    /// "kitoblar" → "kitob", "kitoblarni" → "kitobni".
    /// Oʻzak lugʻatda topilmasa null qaytaradi; larIndex — qoʻshimchaning oʻrni.
    /// </summary>
    public string? TryRemovePlural(string lowerWord, out int larIndex)
    {
        int idx = lowerWord.IndexOf("lar", StringComparison.Ordinal);
        while (idx > 0)
        {
            string stem = lowerWord.Substring(0, idx);
            if (_nominalStems.Contains(stem))
            {
                larIndex = idx;
                return stem + lowerWord.Substring(idx + 3);
            }
            idx = lowerWord.IndexOf("lar", idx + 1, StringComparison.Ordinal);
        }
        larIndex = -1;
        return null;
    }
}
