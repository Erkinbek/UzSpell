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
    public GrammarChecker Grammar { get; }

    private CheckerHost(UzbekSpellChecker spell, GrammarChecker grammar)
    {
        Spell = spell;
        Grammar = grammar;
    }

    private static CheckerHost Load()
    {
        string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        string dictDir = Path.Combine(baseDir, "dictionaries");

        var spell = new UzbekSpellChecker(dictDir);
        spell.WarmUp();
        var grammar = GrammarChecker.CreateFromDictionary(dictDir, spell);
        return new CheckerHost(spell, grammar);
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

        foreach (var group in Grammar.Check(text)
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
                Script = UzbekScript.Latin,
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
