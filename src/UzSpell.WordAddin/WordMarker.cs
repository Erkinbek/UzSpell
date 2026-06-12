namespace UzSpell.WordAddin;

/// <summary>
/// Word hujjatida xatolarni toʻlqinli chiziq bilan belgilash va almashtirish.
/// Barcha chaqiruvlar kech bogʻlanish (dynamic) orqali — PIA talab qilinmaydi.
/// </summary>
internal static class WordMarker
{
    private const int WdUnderlineNone = 0;
    private const int WdUnderlineWavy = 11;
    private const int WdColorRed = 255;        // BGR: 0x0000FF
    private const int WdColorBlue = 16711680;  // BGR: 0xFF0000
    private const int WdColorAutomatic = -16777216;
    private const int WdFindStop = 0;
    private const int WdReplaceAll = 2;
    private const int WdCollapseEnd = 0;

    public static bool HasActiveDocument(dynamic app)
    {
        try
        {
            return (int)app.Documents.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    public static string GetDocumentText(dynamic app) =>
        (string)app.ActiveDocument.Content.Text;

    public static int Mark(dynamic app, ErrorEntry entry) =>
        SetUnderline(app, entry.Text, IsSingleWord(entry.Text), WdUnderlineWavy,
            entry.IsGrammar ? WdColorBlue : WdColorRed);

    public static int Unmark(dynamic app, ErrorEntry entry) =>
        SetUnderline(app, entry.Text, IsSingleWord(entry.Text), WdUnderlineNone, WdColorAutomatic);

    public static void ReplaceAll(dynamic app, string from, string to)
    {
        if (string.IsNullOrEmpty(from) || from.Length > 250)
            return;

        dynamic find = app.ActiveDocument.Content.Find;
        find.ClearFormatting();
        find.Replacement.ClearFormatting();
        find.Replacement.Font.Underline = WdUnderlineNone;
        find.Replacement.Font.UnderlineColor = WdColorAutomatic;
        find.Text = from;
        find.Replacement.Text = to;
        find.MatchWholeWord = IsSingleWord(from);
        find.MatchCase = true;
        find.Forward = true;
        find.Wrap = WdFindStop;
        find.Format = false;
        find.Execute(Replace: WdReplaceAll, Format: true);
    }

    /// <summary>Birinchi uchrashni hujjatda tanlab koʻrsatadi.</summary>
    public static void SelectFirst(dynamic app, string text)
    {
        if (string.IsNullOrEmpty(text) || text.Length > 250)
            return;

        dynamic sel = app.Selection;
        sel.HomeKey(6); // wdStory
        dynamic find = sel.Find;
        find.ClearFormatting();
        find.Text = text;
        find.MatchWholeWord = IsSingleWord(text);
        find.MatchCase = true;
        find.Forward = true;
        find.Wrap = WdFindStop;
        find.Execute();
    }

    /// <summary>Hujjatdagi barcha toʻlqinli belgilashlarni tozalaydi.</summary>
    public static void ClearAllMarks(dynamic app)
    {
        dynamic find = app.ActiveDocument.Content.Find;
        find.ClearFormatting();
        find.Font.Underline = WdUnderlineWavy;
        find.Replacement.ClearFormatting();
        find.Replacement.Font.Underline = WdUnderlineNone;
        find.Replacement.Font.UnderlineColor = WdColorAutomatic;
        find.Text = "";
        find.Replacement.Text = "";
        find.Format = true;
        find.Execute(Replace: WdReplaceAll, Format: true);
    }

    private static bool IsSingleWord(string text)
    {
        if (text.Length == 0)
            return false;
        foreach (char c in text)
            if (!char.IsLetter(c))
                return false;
        return true;
    }

    private static int SetUnderline(dynamic app, string text, bool wholeWord, int underline, int color)
    {
        if (string.IsNullOrEmpty(text) || text.Length > 250)
            return 0;

        dynamic range = app.ActiveDocument.Content;
        dynamic find = range.Find;
        find.ClearFormatting();
        find.Text = text;
        find.MatchWholeWord = wholeWord;
        find.MatchCase = true;
        find.Forward = true;
        find.Wrap = WdFindStop;
        find.Format = false;

        int hits = 0;
        while ((bool)find.Execute() && hits < 2000)
        {
            range.Font.Underline = underline;
            range.Font.UnderlineColor = color;
            range.Collapse(WdCollapseEnd);
            hits++;
        }
        return hits;
    }
}
