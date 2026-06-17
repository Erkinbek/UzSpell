namespace UzSpell.Core;

/// <summary>
/// Hunspell takliflarini oʻzbek tiliga xos tipik xatolar boʻyicha qayta tartiblaydi
/// va toʻldiradi. Eng koʻp uchraydigan chalkashliklar:
///  - lotin: x↔h (xato/hato), oʻ↔o va gʻ↔g (apostrof tushib qolishi: togri→toʻgʻri),
///    tutuq/okina belgisining unutilishi
///  - kirill: х↔ҳ, қ↔к, ў↔у, ғ↔г
/// Shu chalkashliklardan kelib chiqqan, lugʻatda mavjud nomzodlar roʻyxat boshiga
/// chiqariladi; qolganlari oʻzbekcha "yumshoq" tahrir masofasi boʻyicha saralanadi.
/// </summary>
public static class UzbekSuggester
{
    // Bir belgidan iborat chalkashlik juftlari (almashtirish arzon — 0.4)
    private static readonly (char A, char B)[] LatinSwaps =
    {
        ('x', 'h'),
    };

    private static readonly (char A, char B)[] CyrillicSwaps =
    {
        ('х', 'ҳ'), ('қ', 'к'), ('ў', 'у'), ('ғ', 'г'), ('в', 'ф'),
    };

    /// <summary>
    /// Hunspell nomzodlarini oʻzbekcha xatolarni hisobga olib qayta saralaydi va
    /// yuqori ishonchli (bitta chalkashlik bilan toʻgʻrilanadigan) variantlarni qoʻshadi.
    /// </summary>
    public static List<string> Refine(
        string word,
        UzbekScript script,
        IReadOnlyList<string> hunspellSuggestions,
        Func<string, bool> isValid,
        int max)
    {
        bool upperFirst = word.Length > 0 && char.IsUpper(word[0]);
        string lower = word.ToLowerInvariant();

        // 1) Yuqori ishonchli nomzodlar: bitta oʻzbekcha chalkashlik → lugʻatdagi soʻz
        var highConfidence = new List<string>();
        foreach (var cand in GenerateConfusionVariants(lower, script))
            if (cand != lower && isValid(cand))
                highConfidence.Add(cand);

        // 2) Barcha nomzodlarni birlashtiramiz (yuqori ishonchli + Hunspell)
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pool = new List<string>();
        void Add(string s)
        {
            if (s.Length > 0 && seen.Add(s))
                pool.Add(s);
        }
        foreach (var s in highConfidence) Add(s);
        foreach (var s in hunspellSuggestions) Add(s);

        // 3) Oʻzbekcha yumshoq masofa boʻyicha saralash (chalkashlik va apostrof arzon)
        var scored = pool
            .Select(s => (Word: s, Cost: WeightedDistance(lower, s.ToLowerInvariant(), script)))
            .OrderBy(t => t.Cost)
            .ThenBy(t => t.Word.Length)
            .Take(max)
            .Select(t => t.Word)
            .ToList();

        return upperFirst ? scored.Select(Capitalize).ToList() : scored;
    }

    /// <summary>Bitta chalkashlik almashtirish/apostrof qoʻshish bilan hosil boʻladigan variantlar.</summary>
    private static IEnumerable<string> GenerateConfusionVariants(string lower, UzbekScript script)
    {
        var swaps = script == UzbekScript.Cyrillic ? CyrillicSwaps : LatinSwaps;

        // Belgi almashtirishlari (har bir uchrashda alohida)
        for (int i = 0; i < lower.Length; i++)
        {
            foreach (var (a, b) in swaps)
            {
                if (lower[i] == a)
                    yield return lower.Substring(0, i) + b + lower.Substring(i + 1);
                else if (lower[i] == b)
                    yield return lower.Substring(0, i) + a + lower.Substring(i + 1);
            }
        }

        if (script == UzbekScript.Latin)
        {
            // Okina tushib qolgan: o→oʻ, g→gʻ (togri→toʻgʻri, ozbek→oʻzbek)
            for (int i = 0; i < lower.Length; i++)
            {
                if (lower[i] is 'o' or 'g')
                {
                    bool already = i + 1 < lower.Length && lower[i + 1] == UzbekNormalizer.Okina;
                    if (!already)
                        yield return lower.Substring(0, i + 1) + UzbekNormalizer.Okina + lower.Substring(i + 1);
                }
                // Ortiqcha okina: oʻ→o, gʻ→g
                if (lower[i] == UzbekNormalizer.Okina)
                    yield return lower.Substring(0, i) + lower.Substring(i + 1);
            }
        }
        else
        {
            // Kirillda ў/ғ ni у/г bilan chalkashtirish allaqachon swaps'da qoplangan
        }
    }

    /// <summary>
    /// Oʻzbekcha "yumshoq" tahrir masofasi: chalkashlik juftlari arzon (0.4),
    /// okina/tutuq qoʻshish/oʻchirish juda arzon (0.3), qolgani standart (1).
    /// </summary>
    private static double WeightedDistance(string a, string b, UzbekScript script)
    {
        int n = a.Length, m = b.Length;
        var d = new double[n + 1, m + 1];
        for (int i = 0; i <= n; i++) d[i, 0] = i == 0 ? 0 : d[i - 1, 0] + IndelCost(a[i - 1]);
        for (int j = 0; j <= m; j++) d[0, j] = j == 0 ? 0 : d[0, j - 1] + IndelCost(b[j - 1]);

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                double sub = d[i - 1, j - 1] + SubCost(a[i - 1], b[j - 1], script);
                double del = d[i - 1, j] + IndelCost(a[i - 1]);
                double ins = d[i, j - 1] + IndelCost(b[j - 1]);
                double best = Math.Min(sub, Math.Min(del, ins));

                // Oʻrin almashish (transpozitsiya): "kitbo"→"kitob"
                if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                    best = Math.Min(best, d[i - 2, j - 2] + 0.7);

                d[i, j] = best;
            }
        }
        return d[n, m];
    }

    private static double IndelCost(char c) =>
        c == UzbekNormalizer.Okina || c == UzbekNormalizer.Tutuq ? 0.3 : 1.0;

    private static double SubCost(char a, char b, UzbekScript script)
    {
        if (a == b) return 0;
        var swaps = script == UzbekScript.Cyrillic ? CyrillicSwaps : LatinSwaps;
        foreach (var (x, y) in swaps)
            if ((a == x && b == y) || (a == y && b == x))
                return 0.4;
        return 1.0;
    }

    private static string Capitalize(string s) =>
        s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
}
