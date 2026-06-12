using System.Runtime.InteropServices;

namespace UzSpell.App;

/// <summary>
/// Ochiq turgan Microsoft Word bilan COM orqali ishlash:
/// hujjat matnini olish, xato soʻzlarni qizil toʻlqinli chiziq bilan belgilash,
/// almashtirish va belgilarni tozalash. Internet talab qilinmaydi.
/// </summary>
internal static class WordInterop
{
    [DllImport("ole32.dll")]
    private static extern int CLSIDFromProgID(
        [MarshalAs(UnmanagedType.LPWStr)] string lpszProgID, out Guid pclsid);

    [DllImport("oleaut32.dll")]
    private static extern int GetActiveObject(
        ref Guid rclsid, IntPtr pvReserved, [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);

    // Word enum qiymatlari
    private const int WdUnderlineNone = 0;
    private const int WdUnderlineWavy = 11;
    private const int WdColorRed = 255;
    private const int WdColorAutomatic = -16777216;
    private const int WdFindStop = 0;
    private const int WdReplaceAll = 2;
    private const int WdCollapseEnd = 0;

    /// <summary>Ishlab turgan Word ilovasini topadi; topilmasa null.</summary>
    public static dynamic? GetRunningWordApp()
    {
        if (CLSIDFromProgID("Word.Application", out Guid clsid) != 0)
            return null;
        return GetActiveObject(ref clsid, IntPtr.Zero, out object obj) == 0 ? obj : null;
    }

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

    public static string GetActiveDocumentText(dynamic app) =>
        (string)app.ActiveDocument.Content.Text;

    /// <summary>
    /// Hujjatdagi berilgan soʻzning barcha uchrashlarini qizil toʻlqinli chiziq
    /// bilan belgilaydi. Nechta joy belgilangani qaytariladi.
    /// </summary>
    public static int MarkWord(dynamic app, string word) =>
        SetUnderlineForWord(app, word, WdUnderlineWavy, WdColorRed);

    /// <summary>Berilgan soʻzdagi belgilashni olib tashlaydi.</summary>
    public static int UnmarkWord(dynamic app, string word) =>
        SetUnderlineForWord(app, word, WdUnderlineNone, WdColorAutomatic);

    private static int SetUnderlineForWord(dynamic app, string word, int underline, int color)
    {
        dynamic range = app.ActiveDocument.Content;
        dynamic find = range.Find;
        find.ClearFormatting();
        find.Text = word;
        find.MatchWholeWord = true;
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

    /// <summary>
    /// Soʻzning barcha uchrashlarini yangi soʻzga almashtiradi
    /// (almashtirilgan matndan belgilash olib tashlanadi).
    /// </summary>
    public static void ReplaceAll(dynamic app, string from, string to)
    {
        dynamic find = app.ActiveDocument.Content.Find;
        find.ClearFormatting();
        find.Replacement.ClearFormatting();
        find.Replacement.Font.Underline = WdUnderlineNone;
        find.Replacement.Font.UnderlineColor = WdColorAutomatic;
        find.Text = from;
        find.Replacement.Text = to;
        find.MatchWholeWord = true;
        find.MatchCase = true;
        find.Forward = true;
        find.Wrap = WdFindStop;
        find.Format = false;
        find.Execute(Replace: WdReplaceAll, Format: true);
    }

    /// <summary>Hujjatdagi barcha qizil toʻlqinli belgilashlarni tozalaydi.</summary>
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
}
