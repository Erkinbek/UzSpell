using System.Text;

namespace UzSpell.Core;

/// <summary>
/// Oʻzbek lotin ⇄ kirill transliteratsiyasi. Kontekstli qoidalar:
///  - е/э (soʻz boshida va unlidan keyin э, aks holda е)
///  - ц → ts (unlidan keyin) / s (aks holda); -tsiya/-tsion → -ция/-цион
///  - ъ: е/ё/ю/я dan oldin ajratuvchi (obyekt), aks holda tutuq (maʼno)
///  - yoʻl → йўл (yo+ʻ), dunyo → дунё (yo)
///  - soʻz boshidagi ye → е (yer → ер)
/// </summary>
public static class UzbekTransliterator
{
    private static bool IsApos(char c) =>
        c is '\'' or '`' or 'ʻ' or 'ʼ' or '‘' or '’' or 'ʹ' or '′';

    private static bool IsLatinVowel(char c) =>
        char.ToLowerInvariant(c) is 'a' or 'e' or 'i' or 'o' or 'u';

    private static bool IsCyrVowel(char c) =>
        "аеёиоуўэюяы".IndexOf(char.ToLowerInvariant(c)) >= 0;

    // ---------------- Lotin -> Kirill ----------------

    public static string ToCyrillic(string text)
    {
        var sb = new StringBuilder(text.Length);
        int n = text.Length;
        char lastSrcLetter = '\0'; // oxirgi lotin harfi (soʻz boshi/unli konteksti uchun)

        for (int i = 0; i < n;)
        {
            char c = text[i];

            if (IsApos(c))
            {
                // Harflar orasidagi apostrof — tutuq belgisi (ъ)
                bool between = lastSrcLetter != '\0' && i + 1 < n && char.IsLetter(text[i + 1]);
                sb.Append(between ? 'ъ' : c);
                i++;
                continue;
            }

            if (!char.IsLetter(c))
            {
                sb.Append(c);
                lastSrcLetter = '\0';
                i++;
                continue;
            }

            char lo = char.ToLowerInvariant(c);
            char next = i + 1 < n ? text[i + 1] : '\0';
            char lon = next == '\0' ? '\0' : char.ToLowerInvariant(next);
            bool wordStart = lastSrcLetter == '\0';

            string? mapped = null;
            int consumed = 1;

            // Uch belgili: yoʻ -> йў (yoʻl → йўл)
            if (lo == 'y' && lon == 'o' && i + 2 < n && IsApos(text[i + 2]))
            {
                mapped = "йў";
                consumed = 3;
            }
            else if (lo == 'o' && next != '\0' && IsApos(next)) { mapped = "ў"; consumed = 2; }
            else if (lo == 'g' && next != '\0' && IsApos(next)) { mapped = "ғ"; consumed = 2; }
            else if (lo == 's' && lon == 'h') { mapped = "ш"; consumed = 2; }
            else if (lo == 'c' && lon == 'h') { mapped = "ч"; consumed = 2; }
            else if (lo == 'y' && lon == 'o') { mapped = "ё"; consumed = 2; }
            else if (lo == 'y' && lon == 'u') { mapped = "ю"; consumed = 2; }
            else if (lo == 'y' && lon == 'a') { mapped = "я"; consumed = 2; }
            else if (lo == 'y' && lon == 'e' && wordStart) { mapped = "е"; consumed = 2; }
            else if (lo == 't' && lon == 's' && IsTsLoanSuffix(text, i + 2))
            {
                // federatsiya → федерация, funktsional → функционал
                mapped = "ц";
                consumed = 2;
            }
            else
            {
                mapped = lo switch
                {
                    'a' => "а",
                    'b' => "б",
                    'c' => "ц",
                    'd' => "д",
                    'e' => wordStart || IsLatinVowel(lastSrcLetter) ? "э" : "е",
                    'f' => "ф",
                    'g' => "г",
                    'h' => "ҳ",
                    'i' => "и",
                    'j' => "ж",
                    'k' => "к",
                    'l' => "л",
                    'm' => "м",
                    'n' => "н",
                    'o' => "о",
                    'p' => "п",
                    'q' => "қ",
                    'r' => "р",
                    's' => "с",
                    't' => "т",
                    'u' => "у",
                    'v' => "в",
                    'w' => "в",
                    'x' => "х",
                    'y' => "й",
                    'z' => "з",
                    _ => c.ToString(),
                };
            }

            AppendWithCase(sb, mapped, text, i, consumed);
            lastSrcLetter = consumed >= 2 ? text[i + consumed - 1] : c;
            if (IsApos(lastSrcLetter))
                lastSrcLetter = text[i]; // oʻ/gʻ/yoʻ — kontekst sifatida bosh harf
            i += consumed;
        }

        return sb.ToString();
    }

    /// <summary>"ts" dan keyin -iya/-ion kelsa (federatsiya, funktsional) — ц.</summary>
    private static bool IsTsLoanSuffix(string text, int idx)
    {
        return Matches(text, idx, "iya") || Matches(text, idx, "ion");

        static bool Matches(string t, int start, string suffix)
        {
            if (start + suffix.Length > t.Length)
                return false;
            for (int k = 0; k < suffix.Length; k++)
                if (char.ToLowerInvariant(t[start + k]) != suffix[k])
                    return false;
            return true;
        }
    }

    // ---------------- Kirill -> Lotin ----------------

    public static string ToLatin(string text)
    {
        var sb = new StringBuilder(text.Length + text.Length / 4);
        int n = text.Length;
        char prevSrc = '\0'; // oxirgi kirill harfi (kontekst uchun, ь/ъ ham kiradi)

        for (int i = 0; i < n; i++)
        {
            char c = text[i];

            if (!char.IsLetter(c))
            {
                sb.Append(c);
                prevSrc = '\0';
                continue;
            }

            char lo = char.ToLowerInvariant(c);
            bool wordStart = prevSrc == '\0';
            char nextLetter = i + 1 < n ? text[i + 1] : '\0';

            string? mapped;
            switch (lo)
            {
                case 'а': mapped = "a"; break;
                case 'б': mapped = "b"; break;
                case 'в': mapped = "v"; break;
                case 'г': mapped = "g"; break;
                case 'д': mapped = "d"; break;
                case 'е':
                    mapped = wordStart || IsCyrVowel(prevSrc) || prevSrc is 'ь' or 'ъ'
                        ? "ye" : "e";
                    break;
                case 'ё': mapped = "yo"; break;
                case 'ж': mapped = "j"; break;
                case 'з': mapped = "z"; break;
                case 'и': mapped = "i"; break;
                case 'й': mapped = "y"; break;
                case 'к': mapped = "k"; break;
                case 'л': mapped = "l"; break;
                case 'м': mapped = "m"; break;
                case 'н': mapped = "n"; break;
                case 'о': mapped = "o"; break;
                case 'п': mapped = "p"; break;
                case 'р': mapped = "r"; break;
                case 'с': mapped = "s"; break;
                case 'т': mapped = "t"; break;
                case 'у': mapped = "u"; break;
                case 'ф': mapped = "f"; break;
                case 'х': mapped = "x"; break;
                case 'ц': mapped = IsCyrVowel(prevSrc) ? "ts" : "s"; break;
                case 'ч': mapped = "ch"; break;
                case 'ш': mapped = "sh"; break;
                case 'щ': mapped = "sh"; break;
                case 'ъ':
                    // е/ё/ю/я dan oldin ajratuvchi (obyekt) — tashlanadi;
                    // aks holda tutuq belgisi (maʼno)
                    mapped = char.ToLowerInvariant(nextLetter) is 'е' or 'ё' or 'ю' or 'я'
                        ? null : "ʼ";
                    break;
                case 'ь': mapped = null; break; // альбом → albom
                case 'ы': mapped = "i"; break;
                case 'э': mapped = "e"; break;
                case 'ю': mapped = "yu"; break;
                case 'я': mapped = "ya"; break;
                case 'ў': mapped = "oʻ"; break;
                case 'қ': mapped = "q"; break;
                case 'ғ': mapped = "gʻ"; break;
                case 'ҳ': mapped = "h"; break;
                default: mapped = c.ToString(); break;
            }

            if (mapped is not null)
                AppendWithCase(sb, mapped, text, i, 1);
            prevSrc = c;
        }

        return sb.ToString();
    }

    // ---------------- Katta-kichik harf ----------------

    /// <summary>
    /// Natijaga manba belgilarining katta-kichikligini koʻchiradi.
    /// Bir belgidan koʻp belgili natija (ч→ch) uchun qoʻshni harflarga qarab
    /// BUTUN SOʻZ bosh harfda yozilganmi-yoʻqmi aniqlanadi.
    /// </summary>
    private static void AppendWithCase(StringBuilder sb, string mapped, string src, int srcIndex, int consumed)
    {
        char first = src[srcIndex];
        if (!char.IsUpper(first))
        {
            sb.Append(mapped);
            return;
        }

        bool restUpper;
        if (consumed >= 2 && char.IsLetter(src[srcIndex + consumed - 1]))
        {
            restUpper = char.IsUpper(src[srcIndex + consumed - 1]);
        }
        else
        {
            // Qoʻshni harflarga qaraymiz: SHAHAR → CH/SH katta, Shahar → kichik
            char neighbor = '\0';
            for (int k = srcIndex + consumed; k < src.Length; k++)
            {
                if (char.IsLetter(src[k])) { neighbor = src[k]; break; }
                if (!IsApos(src[k])) break;
            }
            if (neighbor == '\0')
            {
                for (int k = srcIndex - 1; k >= 0; k--)
                {
                    if (char.IsLetter(src[k])) { neighbor = src[k]; break; }
                    if (!IsApos(src[k])) break;
                }
            }
            restUpper = neighbor != '\0' && char.IsUpper(neighbor);
        }

        sb.Append(char.ToUpperInvariant(mapped[0]));
        for (int k = 1; k < mapped.Length; k++)
            sb.Append(restUpper ? char.ToUpperInvariant(mapped[k]) : mapped[k]);
    }
}
