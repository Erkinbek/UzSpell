using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace UzSpell.WordAddin;

/// <summary>
/// Word lentasida «UzSpell» boʻlimini qoʻshadigan COM add-in.
/// Roʻyxatga olish: scripts\install-word-addin.ps1 (administrator shart emas).
/// </summary>
[ComVisible(true)]
[Guid(ClsidString)]
[ProgId(ProgIdValue)]
// AutoDispatch (AutoDual EMAS): faqat IDispatch sinf interfeysi yaratiladi.
// AutoDual dual vtable yaratadi va Word jarayonida CCW yaratishda yiqilardi
// (ExecutionEngineException). Ribbon callback'lari nom boʻyicha (IDispatch)
// chaqirilgani uchun AutoDispatch yetarli va barqaror.
[ClassInterface(ClassInterfaceType.AutoDispatch)]
public class Addin : IDTExtensibility2, IRibbonExtensibility
{
    public const string ClsidString = "A1B2C3D4-E5F6-47A8-9B0C-1D2E3F4A5B6C";
    public const string ProgIdValue = "UzSpell.WordAddin";

    /// <summary>
    /// MUHIM: COM orqali yuklanganda host jarayonning (Word) papkasi AppBase boʻladi,
    /// shuning uchun add-in yonidagi bogʻliq DLL'lar (UzSpell.Core, WeCantSpell.Hunspell,
    /// System.* facade'lar) avtomatik topilmaydi. Shu resolver ularni add-in
    /// papkasidan yuklaydi — busiz Word add-in'ni yiqitib, «ishonchsiz»ga qoʻyadi.
    /// </summary>
    static Addin()
    {
        AddinLog.Write("=== static Addin() boshlandi ===");
        AppDomain.CurrentDomain.AssemblyResolve += ResolveFromAddinFolder;
        AddinLog.Write($"AssemblyResolve obuna boʻldi; AddinDir={AddinDir}");
    }

    private static readonly string AddinDir =
        Path.GetDirectoryName(typeof(Addin).Assembly.Location) ?? AppContext.BaseDirectory;

    private static Assembly? ResolveFromAddinFolder(object? sender, ResolveEventArgs args)
    {
        try
        {
            string name = new AssemblyName(args.Name).Name + ".dll";
            string path = Path.Combine(AddinDir, name);
            bool exists = File.Exists(path);
            AddinLog.Write($"Resolve so'raldi: {args.Name} -> {(exists ? path : "TOPILMADI")}");
            return exists ? Assembly.LoadFrom(path) : null;
        }
        catch (Exception ex)
        {
            AddinLog.Write("Resolve XATO: " + ex.Message);
            return null;
        }
    }

    private dynamic? _app;
    private ErrorsForm? _form;

    /// <summary>
    /// Add-in to'g'ri yuklanganini tashqaridan (masalan PowerShell COM orqali)
    /// tekshirish uchun: lugʻat va grammatikani yuklab, namunani tekshiradi.
    /// </summary>
    public string SelfTest()
    {
        try
        {
            var host = CheckerHost.Instance;
            var errors = host.CheckDocument(
                "Men maktabga boradi. Kitobb xato.", out int words);
            return $"OK: {words} soʻz, {errors.Count} xato topildi";
        }
        catch (Exception ex)
        {
            return "XATO: " + ex.GetType().Name + ": " + ex.Message;
        }
    }

    // ----- IDTExtensibility2 -----

    public void OnConnection(object Application, ext_ConnectMode ConnectMode, object AddInInst, ref Array custom)
    {
        AddinLog.Write($"OnConnection chaqirildi (mode={ConnectMode})");
        // Ulanishda hech qanday istisno Word'ni yiqitmasligi kerak
        try
        {
            _app = Application;
            AddinLog.Write("OnConnection: _app saqlandi, OK");
        }
        catch (Exception ex)
        {
            AddinLog.Write("OnConnection XATO: " + ex.Message);
        }
    }

    public void OnDisconnection(ext_DisconnectMode RemoveMode, ref Array custom)
    {
        try
        {
            _form?.Close();
        }
        catch
        {
            // yopilish vaqtida xatoga eʼtibor bermaymiz
        }
        _form = null;
        _app = null;
    }

    public void OnAddInsUpdate(ref Array custom) { }
    public void OnStartupComplete(ref Array custom) => AddinLog.Write("OnStartupComplete chaqirildi");
    public void OnBeginShutdown(ref Array custom) { }

    // ----- IRibbonExtensibility -----

    public string GetCustomUI(string RibbonID)
    {
        AddinLog.Write($"GetCustomUI chaqirildi (RibbonID={RibbonID})");
        return RibbonXml;
    }

    private const string RibbonXml = """
        <customUI xmlns="http://schemas.microsoft.com/office/2009/07/customui">
          <ribbon>
            <tabs>
              <tab id="uzspellTab" label="UzSpell">
                <group id="uzspellGroup" label="Oʻzbek imlo va grammatika">
                  <button id="uzspellCheck" label="Tekshirish" size="large"
                          imageMso="SpellingAndGrammar" onAction="OnCheck"
                          screentip="Hujjatni oʻzbek tilida tekshirish"
                          supertip="Imlo xatolari qizil, grammatika xatolari koʻk toʻlqinli chiziq bilan belgilanadi va takliflar oynasi ochiladi."/>
                  <button id="uzspellPanel" label="Xatolar roʻyxati" size="large"
                          imageMso="ReviewingPaneVertical" onAction="OnShowPanel"
                          screentip="Oxirgi tekshiruv natijalarini koʻrsatish"/>
                  <button id="uzspellClear" label="Belgilarni tozalash"
                          imageMso="ClearFormatting" onAction="OnClearMarks"
                          screentip="Hujjatdagi toʻlqinli belgilashlarni olib tashlash"/>
                  <button id="uzspellAbout" label="Haqida"
                          imageMso="Info" onAction="OnAbout"/>
                </group>
                <group id="uzspellTranslit" label="Transliteratsiya">
                  <button id="uzspellToCyr" label="Lot→Kir" size="large"
                          imageMso="TextDirectionContext" onAction="OnToCyrillic"
                          screentip="Belgilangan matnni lotindan kirillga oʻgirish"
                          supertip="Avval matnni belgilang, soʻng tugmani bosing. Hech narsa belgilanmagan boʻlsa butun hujjat oʻgiriladi."/>
                  <button id="uzspellToLat" label="Kir→Lot" size="large"
                          imageMso="TextDirectionLeftToRight" onAction="OnToLatin"
                          screentip="Belgilangan matnni kirilldan lotinga oʻgirish"
                          supertip="Avval matnni belgilang, soʻng tugmani bosing. Hech narsa belgilanmagan boʻlsa butun hujjat oʻgiriladi."/>
                </group>
              </tab>
            </tabs>
          </ribbon>
        </customUI>
        """;

    // ----- Ribbon tugmalari (Office IDispatch orqali nomi boʻyicha chaqiradi) -----

    public void OnCheck(object control)
    {
        try
        {
            RunCheck();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Tekshirishda xatolik: " + ex.Message, "UzSpell",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void OnShowPanel(object control)
    {
        try
        {
            if (_form is { IsDisposed: false })
            {
                _form.Show();
                _form.Activate();
            }
            else
            {
                RunCheck();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Xatolik: " + ex.Message, "UzSpell",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void OnClearMarks(object control)
    {
        try
        {
            if (_app is null || !WordMarker.HasActiveDocument(_app))
                return;
            Cursor.Current = Cursors.WaitCursor;
            WordMarker.ClearAllMarks(_app);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Tozalashda xatolik: " + ex.Message, "UzSpell",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor.Current = Cursors.Default;
        }
    }

    public void OnToCyrillic(object control)
    {
        TransliterateSelection(toCyrillic: true);
    }

    public void OnToLatin(object control)
    {
        TransliterateSelection(toCyrillic: false);
    }

    private void TransliterateSelection(bool toCyrillic)
    {
        try
        {
            if (_app is null || !WordMarker.HasActiveDocument(_app))
            {
                MessageBox.Show("Ochiq hujjat topilmadi.", "UzSpell",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Func<string, string> convert = toCyrillic
                ? UzSpell.Core.UzbekTransliterator.ToCyrillic
                : UzSpell.Core.UzbekTransliterator.ToLatin;

            dynamic sel = _app.Selection;
            string text = (string)sel.Text;

            if (!string.IsNullOrEmpty(text) && text.Length > 1)
            {
                sel.Text = convert(text);
                return;
            }

            // Hech narsa belgilanmagan — butun hujjatni soʻrab oʻgiramiz
            var answer = MessageBox.Show(
                "Hech narsa belgilanmagan. Butun hujjat " +
                (toCyrillic ? "kirillga" : "lotinga") + " oʻgirilsinmi?\n\n" +
                "Eslatma: abzats ichidagi qisman formatlash (qalin soʻzlar va h.k.) yoʻqolishi mumkin.",
                "UzSpell — transliteratsiya",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes)
                return;

            Cursor.Current = Cursors.WaitCursor;
            try
            {
                dynamic paragraphs = _app.ActiveDocument.Paragraphs;
                int count = (int)paragraphs.Count;
                for (int i = 1; i <= count; i++)
                {
                    dynamic range = paragraphs.Item(i).Range;
                    string original = (string)range.Text;
                    string converted = convert(original);
                    if (!string.Equals(original, converted, StringComparison.Ordinal))
                        range.Text = converted;
                }
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Oʻgirishda xatolik: " + ex.Message, "UzSpell",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void OnAbout(object control)
    {
        MessageBox.Show(
            "UzSpell — oʻzbek tili uchun imlo, grammatika va transliteratsiya.\n\n" +
            "• 100% oflayn — internet talab qilinmaydi\n" +
            "• Lotin va kirill yozuvlari\n" +
            "• uz-hunspell lugʻatlari (95 000+ soʻz) asosida\n\n" +
            "© 2026 Erkin Pardayev\n" +
            "Lugʻatlar: uz-hunspell (Alisher 'U2B3K' Jalolov,\n" +
            "Bilolbek Normoʻminov) — GPL litsenziyasi.\n" +
            "Imlo dvigateli: WeCantSpell.Hunspell.\n\n" +
            "github.com/Erkinbek/UzSpell",
            "UzSpell haqida", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // ----- Asosiy ish -----

    private void RunCheck()
    {
        if (_app is null)
            return;
        if (!WordMarker.HasActiveDocument(_app))
        {
            MessageBox.Show("Ochiq hujjat topilmadi.", "UzSpell",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Cursor.Current = Cursors.WaitCursor;
        List<ErrorEntry> entries;
        int totalWords;
        try
        {
            // Birinchi chaqiruvda lugʻatlar yuklanadi (bir necha soniya)
            var host = CheckerHost.Instance;
            string text = WordMarker.GetDocumentText(_app);
            entries = host.CheckDocument(text, out totalWords);

            foreach (var entry in entries)
            {
                try
                {
                    WordMarker.Mark(_app, entry);
                }
                catch
                {
                    // bitta belgilash muvaffaqiyatsiz boʻlsa davom etamiz
                }
            }
        }
        finally
        {
            Cursor.Current = Cursors.Default;
        }

        if (entries.Count == 0)
        {
            MessageBox.Show($"Xato topilmadi — {totalWords} ta soʻz tekshirildi. 🎉", "UzSpell",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            _form?.Close();
            return;
        }

        if (_form is { IsDisposed: false })
            _form.Close();
        _form = new ErrorsForm(_app, entries, totalWords);
        _form.Show();
    }
}
