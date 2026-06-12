using System.Text.RegularExpressions;

namespace UzSpell.Core;

/// <summary>Topilgan grammatik yoki punktuatsion xato.</summary>
public sealed class GrammarIssue
{
    public required string RuleId { get; init; }
    public required string Message { get; init; }
    public int Start { get; init; }
    public int Length { get; init; }
    public IReadOnlyList<string> Suggestions { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Gap qurilishi darajasidagi qoidalar mexanizmi (lotin yozuvi uchun):
///  - koʻmakchilar bilan kelishik moslashuvi (darsdan keyin, uyga qadar)
///  - ega va kesimning shaxs-son moslashuvi (men keldim / men keldi)
///  - sondan keyin otning birlikda kelishi (beshta kitob / beshta kitoblar)
///  - «-mi» yuklamasining qoʻshib yozilishi
///  - takror soʻzlar
///  - punktuatsiya (boʻshliqlar, gap boshidagi bosh harf)
/// Qoidalar yuqori aniqlikka moʻljallangan: shubhali holatlarda jim qoladi.
/// </summary>
public sealed class GrammarChecker
{
    private readonly UzbekMorphology _morph;
    private readonly Func<string, bool> _isCorrect;

    public GrammarChecker(UzbekMorphology morphology, Func<string, bool>? isCorrectWord = null)
    {
        _morph = morphology;
        _isCorrect = isCorrectWord ?? (_ => false);
    }

    /// <summary>Lugʻat papkasi va imlo tekshiruvchidan grammatika tekshiruvchi yasaydi.</summary>
    public static GrammarChecker CreateFromDictionary(string dictionariesDir, UzbekSpellChecker spellChecker)
    {
        var morph = UzbekMorphology.LoadFromDic(Path.Combine(dictionariesDir, "uz_UZ.dic"));
        return new GrammarChecker(morph,
            word => spellChecker.IsCorrect(word, UzbekScript.Latin, out _));
    }

    // ----- Lugʻaviy konstantalar (kanonik apostroflar bilan) -----

    /// <summary>Chiqish kelishigini (-dan) talab qiladigan koʻmakchilar.</summary>
    private static readonly HashSet<string> RequireDan = new(StringComparer.Ordinal)
    {
        "keyin", "soʻng", "buyon", "beri", "tashqari",
    };

    /// <summary>Joʻnalish kelishigini (-ga) talab qiladigan koʻmakchilar.</summary>
    private static readonly HashSet<string> RequireGa = new(StringComparer.Ordinal)
    {
        "qadar", "koʻra", "binoan", "muvofiq", "qaramay", "qaramasdan",
    };

    /// <summary>Ega-kesim qoidasini oʻchirib qoʻyadigan soʻzlar (murakkab gap belgilari).</summary>
    private static readonly HashSet<string> ComplexSentenceMarkers = new(StringComparer.Ordinal)
    {
        "va", "hamda", "yoki", "lekin", "ammo", "biroq", "bilan", "ki",
        "chunki", "agar", "deb", "degan", "esa",
    };

    private static readonly HashSet<string> RepeatExclusions = new(StringComparer.Ordinal)
    {
        "u", "ha",
    };

    // Shaxs indekslari: 0=men 1=sen 2=biz 3=siz 4=u 5=ular
    private static readonly Dictionary<string, int> SubjectPronouns = new(StringComparer.Ordinal)
    {
        ["men"] = 0, ["sen"] = 1, ["biz"] = 2, ["siz"] = 3, ["u"] = 4, ["ular"] = 5,
    };

    /// <summary>Tuslovchi qoʻshimcha oilalari: [men, sen, biz, siz, u, ular].</summary>
    private static readonly string?[][] EndingFamilies =
    {
        new string?[] { "dim", "ding", "dik", "dingiz", "di", "dilar" },          // oddiy oʻtgan zamon
        new string?[] { "aman", "asan", "amiz", "asiz", "adi", "adilar" },        // hozirgi-kelasi (-a)
        new string?[] { "yman", "ysan", "ymiz", "ysiz", "ydi", "ydilar" },        // hozirgi-kelasi (-y)
        new string?[] { "yapman", "yapsan", "yapmiz", "yapsiz", "yapti", "yaptilar" }, // hozirgi davom
        new string?[] { "moqdaman", "moqdasan", "moqdamiz", "moqdasiz", "moqda", "moqdalar" },
        new string?[] { "ganman", "gansan", "ganmiz", "gansiz", "gan", "ganlar" },     // oʻtgan zamon (-gan)
        new string?[] { "sam", "sang", "sak", "sangiz", "sa", "salar" },          // shart mayli
        // kesimlik: -siz va -dir ataylab yoʻq (yasovchi -siz bilan adashmaslik uchun)
        new string?[] { "man", "san", "miz", null, null, null },
    };

    /// <summary>(suffiks, oila, shaxs) — uzunligi boʻyicha kamayish tartibida.</summary>
    private static readonly List<(string Suffix, int Family, int Person)> SortedEndings = BuildSortedEndings();

    private static List<(string, int, int)> BuildSortedEndings()
    {
        var list = new List<(string, int, int)>();
        for (int f = 0; f < EndingFamilies.Length; f++)
            for (int p = 0; p < EndingFamilies[f].Length; p++)
                if (EndingFamilies[f][p] is { } s)
                    list.Add((s, f, p));
        list.Sort((a, b) => b.Item1.Length.CompareTo(a.Item1.Length));
        return list;
    }

    // ----- Asosiy kirish nuqtasi -----

    public List<GrammarIssue> Check(string text)
    {
        var issues = new List<GrammarIssue>();

        var tokens = new List<Token>(Tokenizer.Tokenize(text));
        var norms = new List<string>(tokens.Count);
        foreach (var t in tokens)
            norms.Add(UzbekNormalizer.NormalizeToken(t.Text).ToLowerInvariant());

        bool isFirstSentence = true;
        foreach (var (from, to) in SplitSentences(text, tokens))
        {
            CheckSentence(text, tokens, norms, from, to, isFirstSentence, issues);
            isFirstSentence = false;
        }

        CheckPunctuation(text, issues);

        issues.Sort((a, b) => a.Start.CompareTo(b.Start));
        return issues;
    }

    /// <summary>Token indekslari boʻyicha gap chegaralari [from, to).</summary>
    private static IEnumerable<(int From, int To)> SplitSentences(string text, List<Token> tokens)
    {
        int from = 0;
        for (int i = 0; i < tokens.Count; i++)
        {
            bool boundary = i == tokens.Count - 1;
            if (!boundary)
            {
                int gapStart = tokens[i].Start + tokens[i].Length;
                int gapEnd = tokens[i + 1].Start;
                for (int g = gapStart; g < gapEnd; g++)
                {
                    char c = text[g];
                    if (c is '.' or '!' or '?' or '…' or ';' or '\n')
                    {
                        boundary = true;
                        break;
                    }
                }
            }

            if (boundary)
            {
                yield return (from, i + 1);
                from = i + 1;
            }
        }
    }

    private void CheckSentence(
        string text, List<Token> tokens, List<string> norms,
        int from, int to, bool isFirstSentence, List<GrammarIssue> issues)
    {
        int count = to - from;
        if (count == 0)
            return;

        CheckSentenceCapitalization(text, tokens, from, isFirstSentence, issues);

        for (int i = from; i < to; i++)
        {
            if (i > from)
            {
                CheckRepeatedWord(tokens, norms, i, issues);
                CheckPostpositionCase(tokens, norms, from, i, issues);
                CheckStandaloneMi(text, tokens, norms, i, issues);
            }
            if (i + 1 < to)
                CheckNumeralPlural(tokens, norms, i, to, issues);
        }

        CheckPersonAgreement(tokens, norms, from, to, issues);
    }

    // ----- 1-qoida: takror soʻz -----

    private static void CheckRepeatedWord(List<Token> tokens, List<string> norms, int i, List<GrammarIssue> issues)
    {
        string cur = norms[i];
        if (cur != norms[i - 1] || cur.Length < 2)
            return;
        if (RepeatExclusions.Contains(cur) || UzbekNumerals.IsNumeral(cur))
            return;

        var prev = tokens[i - 1];
        var tok = tokens[i];
        issues.Add(new GrammarIssue
        {
            RuleId = "TAKROR",
            Message = $"«{tok.Text}» soʻzi ketma-ket takrorlangan",
            Start = prev.Start,
            Length = tok.Start + tok.Length - prev.Start,
            Suggestions = new[] { prev.Text },
        });
    }

    // ----- 2-qoida: koʻmakchilar bilan kelishik moslashuvi -----

    private void CheckPostpositionCase(List<Token> tokens, List<string> norms, int from, int i, List<GrammarIssue> issues)
    {
        string word = norms[i];
        bool needDan = RequireDan.Contains(word);
        bool needGa = !needDan && RequireGa.Contains(word);
        if (!needDan && !needGa)
            return;

        string prev = norms[i - 1];
        var prevTok = tokens[i - 1];

        if (needDan)
        {
            if (EndsWith(prev, "dan"))
                return;
            // Faqat aniq ot oʻzagi (qoʻshimchasiz) boʻlsa xato deymiz
            if (!_morph.IsNominalStem(prev) || SubjectPronouns.ContainsKey(prev))
                return;

            string suggestion = prevTok.Text + "dan";
            issues.Add(new GrammarIssue
            {
                RuleId = "KELISHIK-DAN",
                Message = $"«{word}» koʻmakchisi chiqish kelishigini talab qiladi: «{prevTok.Text}dan {tokens[i].Text}»",
                Start = prevTok.Start,
                Length = prevTok.Length,
                Suggestions = _isCorrect(suggestion) ? new[] { suggestion } : Array.Empty<string>(),
            });
        }
        else
        {
            if (EndsWith(prev, "ga") || EndsWith(prev, "ka") || EndsWith(prev, "qa"))
                return;
            if (!_morph.IsNominalStem(prev) || SubjectPronouns.ContainsKey(prev))
                return;

            string suggestion = BuildDative(prevTok.Text);
            issues.Add(new GrammarIssue
            {
                RuleId = "KELISHIK-GA",
                Message = $"«{word}» koʻmakchisi joʻnalish kelishigini talab qiladi: «{BuildDative(prevTok.Text)} {tokens[i].Text}»",
                Start = prevTok.Start,
                Length = prevTok.Length,
                Suggestions = _isCorrect(suggestion) ? new[] { suggestion } : Array.Empty<string>(),
            });
        }
    }

    /// <summary>Otga joʻnalish kelishigi qoʻshimchasini toʻgʻri shaklda qoʻshadi.</summary>
    private static string BuildDative(string stem)
    {
        string lower = stem.ToLowerInvariant();
        if (EndsWith(lower, "gʻ"))
            return stem.Substring(0, stem.Length - 2) + "qqa"; // bogʻ → boqqa
        if (EndsWith(lower, "k"))
            return stem + "ka"; // koʻylak → koʻylakka
        if (EndsWith(lower, "q"))
            return stem + "qa"; // qishloq → qishloqqa
        return stem + "ga";
    }

    // ----- 3-qoida: son + koʻplik -----

    private void CheckNumeralPlural(List<Token> tokens, List<string> norms, int i, int to, List<GrammarIssue> issues)
    {
        if (!UzbekNumerals.IsNumeral(norms[i]))
            return;

        int j = i + 1;
        if (j >= to)
            return;

        // "5 ta kitoblar" — oradagi alohida "ta" ni ham qoplaymiz
        if (norms[j] == "ta")
        {
            j++;
            if (j >= to)
                return;
        }

        string noun = norms[j];
        if (noun.Length < 5)
            return;

        string? singular = _morph.TryRemovePlural(noun, out int larIndex);
        if (singular is null)
            return;

        // Izofa birikmalarini chetlab oʻtish: "beshta bolalar bogʻchasi"
        if (j + 1 < to)
        {
            string next = norms[j + 1];
            if (EndsWith(next, "i") && DetectPersonEnding(next) is null)
                return;
        }

        string raw = tokens[j].Text;
        string suggestion = raw.Remove(larIndex, 3);
        issues.Add(new GrammarIssue
        {
            RuleId = "SON-BIRLIK",
            Message = $"Sondan keyin ot birlikda qoʻllanadi: «{tokens[i].Text} {suggestion}»",
            Start = tokens[j].Start,
            Length = tokens[j].Length,
            Suggestions = new[] { suggestion },
        });
    }

    // ----- 4-qoida: «-mi» alohida yozilgan -----

    private void CheckStandaloneMi(string text, List<Token> tokens, List<string> norms, int i, List<GrammarIssue> issues)
    {
        if (norms[i] != "mi")
            return;

        var prev = tokens[i - 1];
        var cur = tokens[i];

        // Orada faqat boʻshliq boʻlishi kerak
        for (int g = prev.Start + prev.Length; g < cur.Start; g++)
            if (text[g] != ' ')
                return;

        string joined = prev.Text + "mi";
        string joinedNorm = norms[i - 1] + "mi";
        if (!_isCorrect(joinedNorm))
            return;

        issues.Add(new GrammarIssue
        {
            RuleId = "MI-QOSHIB",
            Message = "«-mi» yuklamasi soʻzga qoʻshib yoziladi",
            Start = prev.Start,
            Length = cur.Start + cur.Length - prev.Start,
            Suggestions = new[] { joined },
        });
    }

    // ----- 5-qoida: ega va kesimning shaxs-son moslashuvi -----

    private void CheckPersonAgreement(List<Token> tokens, List<string> norms, int from, int to, List<GrammarIssue> issues)
    {
        int count = to - from;
        if (count is < 2 or > 9)
            return;

        // Ega — gapning birinchi yoki ikkinchi soʻzi boʻlgan kishilik olmoshi
        int subjIdx = -1;
        if (SubjectPronouns.ContainsKey(norms[from]))
            subjIdx = from;
        else if (count > 2 && SubjectPronouns.ContainsKey(norms[from + 1]))
            subjIdx = from + 1;
        if (subjIdx < 0)
            return;

        int subjPerson = SubjectPronouns[norms[subjIdx]];
        int last = to - 1;
        if (last <= subjIdx)
            return;

        // Murakkab gap belgilarida jim qolamiz
        for (int i = subjIdx + 1; i < last; i++)
        {
            string w = norms[i];
            if (ComplexSentenceMarkers.Contains(w))
                return;
            if (EndsWith(w, "gan") || EndsWith(w, "kan") || EndsWith(w, "qan") || EndsWith(w, "digan"))
                return;
        }

        string final = norms[last];
        if (final.IndexOf('-') >= 0)
            return;

        var detected = DetectPersonEnding(final);
        if (detected is null)
            return;

        var (family, person, suffix) = detected.Value;

        // u/ular oʻzaro almashinishi mumkin (ular keldi — toʻgʻri)
        bool matches = subjPerson switch
        {
            4 or 5 => person is 4 or 5,
            _ => person == subjPerson,
        };
        if (matches)
            return;

        // Taklif: shu oilaning toʻgʻri shaxsdagi qoʻshimchasi
        var suggestions = new List<string>();
        string stem = final.Substring(0, final.Length - suffix.Length);
        int targetPerson = subjPerson == 5 ? 4 : subjPerson;
        if (EndingFamilies[family][targetPerson] is { } target)
        {
            string rawStem = tokens[last].Text.Substring(0, tokens[last].Text.Length - suffix.Length);
            string candidate = rawStem + target;
            if (_isCorrect(stem + target))
                suggestions.Add(candidate);
        }

        issues.Add(new GrammarIssue
        {
            RuleId = "SHAXS-SON",
            Message = $"Ega («{tokens[subjIdx].Text}») bilan kesim shaxs-sonda mos kelmayapti",
            Start = tokens[last].Start,
            Length = tokens[last].Length,
            Suggestions = suggestions,
        });
    }

    /// <summary>Soʻz oxiridagi tuslovchi qoʻshimchani aniqlaydi: (oila, shaxs, suffiks).</summary>
    private (int Family, int Person, string Suffix)? DetectPersonEnding(string word)
    {
        // Yaxlit oʻzak soʻzlarni chetlab oʻtamiz: dushman, organ, hindi…
        if (_morph.IsNominalStem(word) || _morph.IsVerbStem(word))
            return null;

        foreach (var (suffix, family, person) in SortedEndings)
        {
            if (word.Length >= suffix.Length + 2 && EndsWith(word, suffix))
                return (family, person, suffix);
        }
        return null;
    }

    // ----- 6-qoida: gap bosh harf bilan boshlanadi -----

    private static void CheckSentenceCapitalization(
        string text, List<Token> tokens, int from, bool isFirstSentence, List<GrammarIssue> issues)
    {
        if (isFirstSentence)
            return;

        var tok = tokens[from];
        if (tok.Length < 2)
            return;

        char first = tok.Text[0];
        if (!char.IsLower(first))
            return;

        // Oldingi belgi qisqartma nuqtasi boʻlmasin: "h.k. dan" emas, "… keldi. keyin"
        int p = tok.Start - 1;
        while (p >= 0 && char.IsWhiteSpace(text[p]))
            p--;
        if (p < 1 || text[p] is not ('.' or '!' or '?' or '…'))
            return;
        // nuqtadan oldin kamida 3 harfli soʻz boʻlsin (qisqartmalardan saqlanish);
        // apostrof belgilari ham soʻz tarkibida hisoblanadi (yoʻq, bogʻ)
        int q = p - 1;
        int letters = 0;
        while (q >= 0 && (char.IsLetter(text[q]) || UzbekNormalizer.IsApostropheLike(text[q])))
        {
            letters++;
            q--;
        }
        if (letters < 3)
            return;

        string suggestion = char.ToUpperInvariant(first) + tok.Text.Substring(1);
        issues.Add(new GrammarIssue
        {
            RuleId = "BOSH-HARF",
            Message = "Gap bosh harf bilan boshlanadi",
            Start = tok.Start,
            Length = tok.Length,
            Suggestions = new[] { suggestion },
        });
    }

    // ----- 7-qoida: punktuatsiya -----

    private static readonly Regex SpaceBeforePunct =
        new(@"[ \t]+(?=[,;:!?]|\.(?!\.))", RegexOptions.Compiled);

    private static readonly Regex NoSpaceAfterComma =
        new(@"[,;](?=[^\s\d])", RegexOptions.Compiled);

    private static readonly Regex DoubleSpace =
        new(@"(?<=\S)[ ]{2,}(?=\S)", RegexOptions.Compiled);

    /// <summary>Gap tugagach boʻshliqsiz bosh harf: «bor.U yerda» (bosh harfli soʻzdan
    /// oldin kamida 2 harf — A.Qodiriy kabi initsiallarni chetlab oʻtish uchun).</summary>
    private static readonly Regex NoSpaceAfterPeriod =
        new(@"(?<=\p{L}{2})[.!?](?=\p{Lu})", RegexOptions.Compiled);

    private static void CheckPunctuation(string text, List<GrammarIssue> issues)
    {
        foreach (Match m in SpaceBeforePunct.Matches(text))
        {
            issues.Add(new GrammarIssue
            {
                RuleId = "PUNKT-OLDIN",
                Message = "Tinish belgisidan oldin boʻshliq qoʻyilmaydi",
                Start = m.Index,
                Length = m.Length + 1, // boʻshliqlar + belgining oʻzi
                Suggestions = new[] { text[m.Index + m.Length].ToString() },
            });
        }

        foreach (Match m in NoSpaceAfterComma.Matches(text))
        {
            issues.Add(new GrammarIssue
            {
                RuleId = "PUNKT-KEYIN",
                Message = "Tinish belgisidan keyin boʻshliq qoʻyiladi",
                Start = m.Index,
                Length = 1,
                Suggestions = new[] { text[m.Index] + " " },
            });
        }

        foreach (Match m in DoubleSpace.Matches(text))
        {
            issues.Add(new GrammarIssue
            {
                RuleId = "PUNKT-BOSHLIQ",
                Message = "Ortiqcha boʻshliq",
                Start = m.Index,
                Length = m.Length,
                Suggestions = new[] { " " },
            });
        }

        foreach (Match m in NoSpaceAfterPeriod.Matches(text))
        {
            issues.Add(new GrammarIssue
            {
                RuleId = "PUNKT-NUQTA",
                Message = "Gap tugagach boʻshliq qoʻyiladi",
                Start = m.Index,
                Length = 1,
                Suggestions = new[] { text[m.Index] + " " },
            });
        }
    }

    private static bool EndsWith(string s, string suffix) =>
        s.EndsWith(suffix, StringComparison.Ordinal);
}
