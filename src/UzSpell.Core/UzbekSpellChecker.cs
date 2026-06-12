using WeCantSpell.Hunspell;

namespace UzSpell.Core;

/// <summary>Topilgan imlo xatosi haqida maʼlumot.</summary>
public sealed class SpellError
{
    /// <summary>Matnda yozilgan asl koʻrinish.</summary>
    public required string Word { get; init; }

    /// <summary>Apostroflari kanonik holatga keltirilgan koʻrinish (takliflar uchun).</summary>
    public required string Normalized { get; init; }

    public int Start { get; init; }
    public int Length { get; init; }
    public UzbekScript Script { get; init; }
}

/// <summary>Matn tekshiruvi natijasi.</summary>
public sealed class CheckResult
{
    public required IReadOnlyList<SpellError> Errors { get; init; }
    public int TotalWords { get; init; }
}

/// <summary>
/// Oʻzbek tili imlo tekshiruvchisi. uz-hunspell lugʻatlari asosida
/// lotin va kirill yozuvlarini qoʻllab-quvvatlaydi, toʻliq oflayn ishlaydi.
/// </summary>
public sealed class UzbekSpellChecker
{
    private readonly Lazy<WordList> _latin;
    private readonly Lazy<WordList?> _cyrillic;

    /// <summary>null boʻlsa yozuv har bir soʻz uchun avtomatik aniqlanadi.</summary>
    public UzbekScript? ForcedScript { get; set; }

    /// <summary>BOSH HARFLI qisqartmalarni (AQSH, BMT) tekshirmaslik.</summary>
    public bool SkipAllCaps { get; set; } = true;

    /// <summary>Foydalanuvchi lugʻatiga qoʻshilgan soʻzlar (normalizatsiya qilingan).</summary>
    public HashSet<string> CustomWords { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Shu sessiyada eʼtiborsiz qoldirilgan soʻzlar.</summary>
    public HashSet<string> IgnoredWords { get; } = new(StringComparer.OrdinalIgnoreCase);

    public UzbekSpellChecker(string dictionariesDir)
    {
        string latinDic = Path.Combine(dictionariesDir, "uz_UZ.dic");
        string latinAff = Path.Combine(dictionariesDir, "uz_UZ.aff");
        if (!File.Exists(latinDic) || !File.Exists(latinAff))
            throw new FileNotFoundException(
                $"Lugʻat fayllari topilmadi: {latinDic}. \"dictionaries\" papkasida uz_UZ.dic va uz_UZ.aff boʻlishi kerak.");

        _latin = new Lazy<WordList>(
            () => WordList.CreateFromFiles(latinDic, latinAff),
            LazyThreadSafetyMode.ExecutionAndPublication);

        string cyrlDic = Path.Combine(dictionariesDir, "uz_UZ_Cyrl.dic");
        string cyrlAff = Path.Combine(dictionariesDir, "uz_UZ_Cyrl.aff");
        _cyrillic = new Lazy<WordList?>(
            () => File.Exists(cyrlDic) && File.Exists(cyrlAff)
                ? WordList.CreateFromFiles(cyrlDic, cyrlAff)
                : null,
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>Lugʻatlarni oldindan yuklab qoʻyish (dastur ochilishida fonda chaqirish uchun).</summary>
    public void WarmUp()
    {
        _ = _latin.Value;
        _ = _cyrillic.Value;
    }

    private WordList? GetWordList(UzbekScript script) =>
        script == UzbekScript.Latin ? _latin.Value : _cyrillic.Value;

    /// <summary>Butun matnni tekshiradi va xatolar roʻyxatini qaytaradi.</summary>
    public CheckResult CheckText(string text)
    {
        var errors = new List<SpellError>();
        int total = 0;

        foreach (var token in Tokenizer.Tokenize(text))
        {
            if (token.Length < 2)
                continue;

            total++;

            if (SkipAllCaps && IsAllUpper(token.Text))
                continue;

            var script = ForcedScript ?? ScriptDetector.DetectToken(token.Text);
            if (script is null)
                continue;

            if (!IsCorrect(token.Text, script.Value, out string normalized))
            {
                errors.Add(new SpellError
                {
                    Word = token.Text,
                    Normalized = normalized,
                    Start = token.Start,
                    Length = token.Length,
                    Script = script.Value,
                });
            }
        }

        return new CheckResult { Errors = errors, TotalWords = total };
    }

    /// <summary>Bitta soʻzni tekshiradi. normalized — kanonik apostrofli koʻrinish.</summary>
    public bool IsCorrect(string word, UzbekScript script, out string normalized)
    {
        normalized = script == UzbekScript.Latin
            ? UzbekNormalizer.NormalizeToken(word)
            : word;

        if (CustomWords.Contains(normalized) || IgnoredWords.Contains(normalized))
            return true;

        // Son + «-ta» shakllari (beshta, ikkita) lugʻatda yoʻq, lekin toʻgʻri
        if (UzbekNumerals.IsNumeral(normalized.ToLowerInvariant()))
            return true;

        var wordList = GetWordList(script);
        if (wordList is null)
            return true; // lugʻat yoʻq boʻlsa hukm chiqarmaymiz

        if (wordList.Check(normalized))
            return true;
        if (!ReferenceEquals(word, normalized) && wordList.Check(word))
            return true;

        // Juft soʻzlar (katta-katta, oz-moz): har bir qismi alohida toʻgʻri boʻlsa qabul qilamiz
        if (normalized.IndexOf('-') >= 0)
        {
            var parts = normalized.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1 && parts.All(p => p.Length > 1 && wordList.Check(p)))
                return true;
        }

        return false;
    }

    /// <summary>Notoʻgʻri soʻz uchun takliflar.</summary>
    public IReadOnlyList<string> Suggest(string normalizedWord, UzbekScript script, int max = 6)
    {
        var wordList = GetWordList(script);
        if (wordList is null)
            return Array.Empty<string>();
        return wordList.Suggest(normalizedWord).Take(max).ToList();
    }

    private static bool IsAllUpper(string word)
    {
        int upper = 0;
        foreach (char c in word)
        {
            if (char.IsLower(c))
                return false;
            if (char.IsUpper(c))
                upper++;
        }
        return upper >= 2;
    }
}
