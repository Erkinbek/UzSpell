using System.IO;
using System.Reflection;
using UzSpell.Core;

namespace UzSpell.WordAddin;

/// <summary>Panel va belgilash uchun bitta xato yozuvi.</summary>
internal sealed class ErrorEntry
{
    public required string Text { get; init; }
    public required string Normalized { get; init; }
    public UzbekScript Script { get; init; }
    public bool IsGrammar { get; init; }
    public string? Message { get; init; }
    public int Occurrences { get; set; }
    public List<string>? Suggestions { get; set; }
}

/// <summary>
/// Imlo va grammatika tekshiruvchilarni bir marta yuklab, butun add-in
/// hayoti davomida qayta ishlatadi (lugʻat yuklash bir necha soniya oladi).
/// </summary>
internal sealed class CheckerHost
{
    private static CheckerHost? _instance;
    public static CheckerHost Instance => _instance ??= Load();

    public UzbekSpellChecker Spell { get; }
    private readonly GrammarChecker _grammarLatin;
    private readonly GrammarChecker? _grammarCyrillic;

    private CheckerHost(UzbekSpellChecker spell, GrammarChecker latin, GrammarChecker? cyrillic)
    {
        Spell = spell;
        _grammarLatin = latin;
        _grammarCyrillic = cyrillic;
    }

    private static CheckerHost Load()
    {
        string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        string dictDir = Path.Combine(baseDir, "dictionaries");

        var spell = new UzbekSpellChecker(dictDir);
        spell.WarmUp();
        var latin = GrammarChecker.CreateLatin(dictDir, spell);
        var cyrillic = GrammarChecker.CreateCyrillic(dictDir, spell);
        return new CheckerHost(spell, latin, cyrillic);
    }

    /// <summary>Hujjat yozuviga mos grammatika tekshiruvchisi.</summary>
    private GrammarChecker GrammarFor(string text)
    {
        var script = Spell.ForcedScript ?? ScriptDetector.DetectDominant(text);
        return script == UzbekScript.Cyrillic && _grammarCyrillic is not null
            ? _grammarCyrillic
            : _grammarLatin;
    }

    /// <summary>Hujjat matnini tekshirib, birlashtirilgan xatolar roʻyxatini qaytaradi.</summary>
    public List<ErrorEntry> CheckDocument(string text, out int totalWords)
    {
        var entries = new List<ErrorEntry>();

        var spelling = Spell.CheckText(text);
        totalWords = spelling.TotalWords;

        foreach (var group in spelling.Errors.GroupBy(e => e.Word, StringComparer.Ordinal))
        {
            var first = group.First();
            entries.Add(new ErrorEntry
            {
                Text = group.Key,
                Normalized = first.Normalized,
                Script = first.Script,
                IsGrammar = false,
                Occurrences = group.Count(),
            });
        }

        var grammarScript = Spell.ForcedScript ?? ScriptDetector.DetectDominant(text);
        foreach (var group in GrammarFor(text).Check(text)
                     .GroupBy(i => (i.RuleId, Span: SafeSubstring(text, i.Start, i.Length))))
        {
            string span = group.Key.Span;
            if (span.Trim().Length == 0)
                continue;

            var first = group.First();
            entries.Add(new ErrorEntry
            {
                Text = span,
                Normalized = span,
                Script = grammarScript,
                IsGrammar = true,
                Message = first.Message,
                Occurrences = group.Count(),
                Suggestions = new List<string>(first.Suggestions),
            });
        }

        return entries;
    }

    /// <summary>Imlo xatosi uchun takliflar (kerak boʻlganda hisoblanadi).</summary>
    public List<string> SuggestionsFor(ErrorEntry entry)
    {
        if (entry.Suggestions is not null)
            return entry.Suggestions;
        entry.Suggestions = Spell.Suggest(entry.Normalized, entry.Script).ToList();
        return entry.Suggestions;
    }

    private static string SafeSubstring(string text, int start, int length)
    {
        if (start < 0 || start >= text.Length)
            return "";
        return text.Substring(start, Math.Min(length, text.Length - start));
    }
}
