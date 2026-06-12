using System.Text;
using UzSpell.Core;

Console.OutputEncoding = Encoding.UTF8;

string? filePath = null;
UzbekScript? forcedScript = null;
bool noSuggestions = false;
bool checkAllCaps = false;

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

string dictDir = Path.Combine(AppContext.BaseDirectory, "dictionaries");
var checker = new UzbekSpellChecker(dictDir)
{
    ForcedScript = forcedScript,
    SkipAllCaps = !checkAllCaps,
};

var result = checker.CheckText(text);

// Qator/ustunni bitta oʻtishda hisoblaymiz (xatolar tartiblangan)
int line = 1, col = 1, pos = 0;
foreach (var error in result.Errors)
{
    while (pos < error.Start)
    {
        if (text[pos] == '\n') { line++; col = 1; }
        else if (text[pos] != '\r') col++;
        pos++;
    }

    if (noSuggestions)
    {
        Console.WriteLine($"{line}:{col}\t{error.Word}");
    }
    else
    {
        var suggestions = checker.Suggest(error.Normalized, error.Script);
        string sugg = suggestions.Count > 0 ? string.Join(", ", suggestions) : "(taklif yoʻq)";
        Console.WriteLine($"{line}:{col}\t{error.Word}\t→ {sugg}");
    }
}

Console.WriteLine();
Console.WriteLine($"Jami {result.TotalWords} ta soʻz tekshirildi, {result.Errors.Count} ta xato topildi.");
return result.Errors.Count > 0 ? 1 : 0;

static void PrintHelp()
{
    Console.WriteLine("""
        uzspell — oʻzbek tili uchun oflayn imlo tekshiruvchi

        Foydalanish:
          uzspell <fayl.txt> [parametrlar]
          type matn.txt | uzspell

        Parametrlar:
          --lotin       Faqat lotin lugʻatidan foydalanish
          --kirill      Faqat kirill lugʻatidan foydalanish
          --taklifsiz   Takliflarsiz, tezroq ishlaydi
          --allcaps     BOSH HARFLI qisqartmalarni ham tekshirish
          --yordam      Shu maʼlumotni koʻrsatish

        Chiqish kodi: 0 — xato yoʻq, 1 — xato topildi, 2 — notoʻgʻri chaqiruv.
        """);
}
