using System.Text;
using UzSpell.Core;

Console.OutputEncoding = Encoding.UTF8;

string? filePath = null;
UzbekScript? forcedScript = null;
bool noSuggestions = false;
bool checkAllCaps = false;
bool noGrammar = false;
string? translit = null; // "kirillga" yoki "lotinga"

foreach (string arg in args)
{
    switch (arg)
    {
        case "--lotin" or "--latin":
            forcedScript = UzbekScript.Latin;
            break;
        case "--kirill" or "--cyrillic":
            forcedScript = UzbekScript.Cyrillic;
            break;
        case "--taklifsiz" or "--no-suggest":
            noSuggestions = true;
            break;
        case "--allcaps":
            checkAllCaps = true;
            break;
        case "--grammatikasiz" or "--no-grammar":
            noGrammar = true;
            break;
        case "--kirillga" or "--to-cyrillic":
            translit = "kirillga";
            break;
        case "--lotinga" or "--to-latin":
            translit = "lotinga";
            break;
        case "--yordam" or "--help" or "-h":
            PrintHelp();
            return 0;
        default:
            if (arg.StartsWith('-'))
            {
                Console.Error.WriteLine($"Nomaʼlum parametr: {arg}");
                PrintHelp();
                return 2;
            }
            filePath = arg;
            break;
    }
}

string text;
if (filePath is not null)
{
    if (!File.Exists(filePath))
    {
        Console.Error.WriteLine($"Fayl topilmadi: {filePath}");
        return 2;
    }
    text = File.ReadAllText(filePath, Encoding.UTF8);
}
else if (Console.IsInputRedirected)
{
    text = Console.In.ReadToEnd();
}
else
{
    PrintHelp();
    return 2;
}

// Transliteratsiya rejimi: tekshirmasdan oʻgirib chiqaradi
if (translit is not null)
{
    Console.Write(translit == "kirillga"
        ? UzbekTransliterator.ToCyrillic(text)
        : UzbekTransliterator.ToLatin(text));
    return 0;
}

string dictDir = Path.Combine(AppContext.BaseDirectory, "dictionaries");
var checker = new UzbekSpellChecker(dictDir)
{
    ForcedScript = forcedScript,
    SkipAllCaps = !checkAllCaps,
};

var result = checker.CheckText(text);

// Imlo va grammatika xatolarini bitta tartiblangan roʻyxatga yigʻamiz
var report = new List<(int Start, string Kind, string Shown, string Detail)>();

foreach (var error in result.Errors)
{
    string detail;
    if (noSuggestions)
    {
        detail = "";
    }
    else
    {
        var suggestions = checker.Suggest(error.Normalized, error.Script);
        detail = suggestions.Count > 0 ? "→ " + string.Join(", ", suggestions) : "(taklif yoʻq)";
    }
    report.Add((error.Start, "imlo", error.Word, detail));
}

int grammarCount = 0;
if (!noGrammar)
{
    var grammar = GrammarChecker.CreateFromDictionary(dictDir, checker);
    var issues = grammar.Check(text);
    grammarCount = issues.Count;
    foreach (var issue in issues)
    {
        string shown = text.Substring(issue.Start, Math.Min(issue.Length, 40)).Replace("\r", "").Replace("\n", " ");
        string detail = issue.Message;
        if (issue.Suggestions.Count > 0)
            detail += " → " + string.Join(", ", issue.Suggestions);
        report.Add((issue.Start, "gram", shown, detail));
    }
}

report.Sort((a, b) => a.Start.CompareTo(b.Start));

// Qator/ustunni bitta oʻtishda hisoblaymiz
int line = 1, col = 1, pos = 0;
foreach (var (start, kind, shown, detail) in report)
{
    while (pos < start)
    {
        if (text[pos] == '\n') { line++; col = 1; }
        else if (text[pos] != '\r') col++;
        pos++;
    }
    Console.WriteLine($"{line}:{col}\t[{kind}]\t{shown}\t{detail}");
}

Console.WriteLine();
Console.WriteLine($"Jami {result.TotalWords} ta soʻz tekshirildi: {result.Errors.Count} ta imlo, {grammarCount} ta grammatika/punktuatsiya xatosi.");
return report.Count > 0 ? 1 : 0;

static void PrintHelp()
{
    Console.WriteLine("""
        uzspell — oʻzbek tili uchun oflayn imlo tekshiruvchi

        Foydalanish:
          uzspell <fayl.txt> [parametrlar]
          type matn.txt | uzspell

        Parametrlar:
          --lotin          Faqat lotin lugʻatidan foydalanish
          --kirill         Faqat kirill lugʻatidan foydalanish
          --taklifsiz      Takliflarsiz, tezroq ishlaydi
          --allcaps        BOSH HARFLI qisqartmalarni ham tekshirish
          --grammatikasiz  Faqat imlo (grammatika qoidalarisiz)
          --kirillga       Matnni lotindan kirillga oʻgirish (tekshirmasdan)
          --lotinga        Matnni kirilldan lotinga oʻgirish (tekshirmasdan)
          --yordam         Shu maʼlumotni koʻrsatish

        Chiqish kodi: 0 — xato yoʻq, 1 — xato topildi, 2 — notoʻgʻri chaqiruv.
        """);
}
