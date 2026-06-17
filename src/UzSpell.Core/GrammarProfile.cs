namespace UzSpell.Core;

/// <summary>
/// Grammatika qoidalari uchun yozuvga (lotin/kirill) bogʻliq lugʻaviy maʼlumotlar.
/// <see cref="GrammarChecker"/> bir xil mantiqni ikkala yozuvda ham ishlatishi uchun
/// barcha soʻz roʻyxatlari, qoʻshimchalar va yordamchilar shu profilga toʻplangan.
/// </summary>
public sealed class GrammarProfile
{
    public UzbekScript Script { get; private init; }

    /// <summary>Chiqish kelishigini (-dan) talab qiladigan koʻmakchilar.</summary>
    public HashSet<string> RequireDan { get; private init; } = new(StringComparer.Ordinal);

    /// <summary>Joʻnalish kelishigini (-ga) talab qiladigan koʻmakchilar.</summary>
    public HashSet<string> RequireGa { get; private init; } = new(StringComparer.Ordinal);

    /// <summary>Ega-kesim qoidasini oʻchiradigan soʻzlar (murakkab gap belgilari).</summary>
    public HashSet<string> ComplexSentenceMarkers { get; private init; } = new(StringComparer.Ordinal);

    /// <summary>Takror soʻz qoidasidan istisno (u, ha).</summary>
    public HashSet<string> RepeatExclusions { get; private init; } = new(StringComparer.Ordinal);

    /// <summary>Kishilik olmoshlari → shaxs indeksi (0=men … 5=ular).</summary>
    public Dictionary<string, int> SubjectPronouns { get; private init; } = new(StringComparer.Ordinal);

    /// <summary>Tuslovchi qoʻshimcha oilalari: [men, sen, biz, siz, u, ular].</summary>
    public string?[][] EndingFamilies { get; private init; } = Array.Empty<string?[]>();

    /// <summary>Sifatdosh qoʻshimchalari (gan/kan/qan/digan) — ega-kesim qoidasini jim qoldiradi.</summary>
    public string[] ParticipleSuffixes { get; private init; } = Array.Empty<string>();

    public string PluralSuffix { get; private init; } = "lar";
    public string DanSuffix { get; private init; } = "dan";
    public string MiParticle { get; private init; } = "mi";
    public string TaWord { get; private init; } = "ta";

    /// <summary>Joʻnalish kelishigi qoʻshimchasi allaqachon bor-yoʻqligini aniqlaydi.</summary>
    public Func<string, bool> HasDativeEnding { get; private init; } = _ => false;

    /// <summary>Otga joʻnalish kelishigi qoʻshimchasini toʻgʻri shaklda qoʻshadi.</summary>
    public Func<string, string> BuildDative { get; private init; } = s => s;

    /// <summary>Soʻzni qoʻshimcha aniqlash uchun normallashtiradi.</summary>
    public Func<string, string> Normalize { get; private init; } = s => s;

    /// <summary>(suffiks, oila, shaxs) — uzunligi boʻyicha kamayish tartibida.</summary>
    public IReadOnlyList<(string Suffix, int Family, int Person)> SortedEndings { get; private init; }
        = Array.Empty<(string, int, int)>();

    private GrammarProfile() { }

    private static bool EndsWith(string s, string suffix) =>
        s.EndsWith(suffix, StringComparison.Ordinal);

    // ---------------- Lotin profili ----------------

    public static GrammarProfile Latin()
    {
        var families = new string?[][]
        {
            new string?[] { "dim", "ding", "dik", "dingiz", "di", "dilar" },          // oddiy oʻtgan zamon
            new string?[] { "aman", "asan", "amiz", "asiz", "adi", "adilar" },        // hozirgi-kelasi (-a)
            new string?[] { "yman", "ysan", "ymiz", "ysiz", "ydi", "ydilar" },        // hozirgi-kelasi (-y)
            new string?[] { "yapman", "yapsan", "yapmiz", "yapsiz", "yapti", "yaptilar" }, // hozirgi davom
            new string?[] { "moqdaman", "moqdasan", "moqdamiz", "moqdasiz", "moqda", "moqdalar" },
            new string?[] { "ganman", "gansan", "ganmiz", "gansiz", "gan", "ganlar" },     // oʻtgan zamon (-gan)
            new string?[] { "sam", "sang", "sak", "sangiz", "sa", "salar" },          // shart mayli
            new string?[] { "man", "san", "miz", null, null, null },                  // kesimlik (-siz/-dir ataylab yoʻq)
        };

        return new GrammarProfile
        {
            Script = UzbekScript.Latin,
            RequireDan = new(StringComparer.Ordinal) { "keyin", "soʻng", "buyon", "beri", "tashqari" },
            RequireGa = new(StringComparer.Ordinal) { "qadar", "koʻra", "binoan", "muvofiq", "qaramay", "qaramasdan" },
            ComplexSentenceMarkers = new(StringComparer.Ordinal)
            {
                "va", "hamda", "yoki", "lekin", "ammo", "biroq", "bilan", "ki",
                "chunki", "agar", "deb", "degan", "esa",
            },
            RepeatExclusions = new(StringComparer.Ordinal) { "u", "ha" },
            SubjectPronouns = new(StringComparer.Ordinal)
            {
                ["men"] = 0, ["sen"] = 1, ["biz"] = 2, ["siz"] = 3, ["u"] = 4, ["ular"] = 5,
            },
            EndingFamilies = families,
            ParticipleSuffixes = new[] { "gan", "kan", "qan", "digan" },
            PluralSuffix = "lar",
            DanSuffix = "dan",
            MiParticle = "mi",
            TaWord = "ta",
            HasDativeEnding = w => EndsWith(w, "ga") || EndsWith(w, "ka") || EndsWith(w, "qa"),
            BuildDative = stem =>
            {
                string lower = stem.ToLowerInvariant();
                if (EndsWith(lower, "gʻ")) return stem.Substring(0, stem.Length - 2) + "qqa"; // bogʻ → boqqa
                if (EndsWith(lower, "k")) return stem + "ka";   // koʻylak → koʻylakka
                if (EndsWith(lower, "q")) return stem + "qa";   // qishloq → qishloqqa
                return stem + "ga";
            },
            Normalize = UzbekNormalizer.NormalizeToken,
            SortedEndings = BuildSortedEndings(families),
        };
    }

    // ---------------- Kirill profili ----------------

    public static GrammarProfile Cyrillic()
    {
        var families = new string?[][]
        {
            new string?[] { "дим", "динг", "дик", "дингиз", "ди", "дилар" },
            new string?[] { "аман", "асан", "амиз", "асиз", "ади", "адилар" },
            new string?[] { "йман", "йсан", "ймиз", "йсиз", "йди", "йдилар" },
            new string?[] { "япман", "япсан", "япмиз", "япсиз", "япти", "яптилар" },
            new string?[] { "моқдаман", "моқдасан", "моқдамиз", "моқдасиз", "моқда", "моқдалар" },
            new string?[] { "ганман", "гансан", "ганмиз", "гансиз", "ган", "ганлар" },
            new string?[] { "сам", "санг", "сак", "сангиз", "са", "салар" },
            new string?[] { "ман", "сан", "миз", null, null, null },
        };

        return new GrammarProfile
        {
            Script = UzbekScript.Cyrillic,
            RequireDan = new(StringComparer.Ordinal) { "кейин", "сўнг", "буён", "бери", "ташқари" },
            RequireGa = new(StringComparer.Ordinal) { "қадар", "кўра", "биноан", "мувофиқ", "қарамай", "қарамасдан" },
            ComplexSentenceMarkers = new(StringComparer.Ordinal)
            {
                "ва", "ҳамда", "ёки", "лекин", "аммо", "бироқ", "билан", "ки",
                "чунки", "агар", "деб", "деган", "эса",
            },
            RepeatExclusions = new(StringComparer.Ordinal) { "у", "ҳа" },
            SubjectPronouns = new(StringComparer.Ordinal)
            {
                ["мен"] = 0, ["сен"] = 1, ["биз"] = 2, ["сиз"] = 3, ["у"] = 4, ["улар"] = 5,
            },
            EndingFamilies = families,
            ParticipleSuffixes = new[] { "ган", "кан", "қан", "диган" },
            PluralSuffix = "лар",
            DanSuffix = "дан",
            MiParticle = "ми",
            TaWord = "та",
            HasDativeEnding = w => EndsWith(w, "га") || EndsWith(w, "ка") || EndsWith(w, "қа"),
            BuildDative = stem =>
            {
                string lower = stem.ToLowerInvariant();
                if (EndsWith(lower, "ғ")) return stem.Substring(0, stem.Length - 1) + "ққа"; // боғ → боққа
                if (EndsWith(lower, "к")) return stem + "ка";
                if (EndsWith(lower, "қ")) return stem + "қа";
                return stem + "га";
            },
            Normalize = s => s, // kirillda apostrof normalizatsiyasi shart emas
            SortedEndings = BuildSortedEndings(families),
        };
    }

    private static List<(string, int, int)> BuildSortedEndings(string?[][] families)
    {
        var list = new List<(string, int, int)>();
        for (int f = 0; f < families.Length; f++)
            for (int p = 0; p < families[f].Length; p++)
                if (families[f][p] is { } s)
                    list.Add((s, f, p));
        list.Sort((a, b) => b.Item1.Length.CompareTo(a.Item1.Length));
        return list;
    }
}
