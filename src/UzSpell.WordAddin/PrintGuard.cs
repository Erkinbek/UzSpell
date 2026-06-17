using System.Runtime.InteropServices;

namespace UzSpell.WordAddin;

// Chop etishda UzSpell belgilari (toʻlqinli chiziq) qogʻozga chiqib ketmasligi
// uchun Word'ning DocumentBeforePrint hodisasiga ulanamiz. Belgilar haqiqiy
// shrift formati boʻlgani uchun normal holda bosib chiqariladi; shuning uchun
// chop etishdan oldin ularni vaqtincha olib turamiz va chop etgach qaytaramiz.
//
// PIA talab qilinmasligi uchun OLE connection-point interfeyslari va Word
// ApplicationEvents4 dispinterfeysi qoʻlda eʼlon qilingan.

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("B196B284-BAB4-101A-B69C-00AA00341D07")]
internal interface IConnectionPointContainer
{
    void EnumConnectionPoints(out IntPtr ppEnum);
    void FindConnectionPoint(ref Guid riid, out IConnectionPoint ppCP);
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("B196B286-BAB4-101A-B69C-00AA00341D07")]
internal interface IConnectionPoint
{
    void GetConnectionInterface(out Guid pIID);
    void GetConnectionPointContainer(out IConnectionPointContainer ppCPC);
    void Advise([MarshalAs(UnmanagedType.IUnknown)] object pUnkSink, out int pdwCookie);
    void Unadvise(int dwCookie);
    void EnumConnections(out IntPtr ppEnum);
}

/// <summary>
/// Word Application hodisalari (dispinterface). Bizga faqat DocumentBeforePrint
/// (DISPID 7) kerak — qolgan hodisalar Invoke'da eʼtiborsiz qoldiriladi.
/// Interfeys GUID'i AYNAN Word ApplicationEvents4 IID boʻlishi shart, aks holda
/// IConnectionPoint.Advise ulanmaydi.
/// </summary>
[ComVisible(true)]
[Guid("00020A01-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
public interface IWordApplicationEvents4
{
    [DispId(7)]
    void DocumentBeforePrint(
        [MarshalAs(UnmanagedType.IDispatch)] object Doc,
        [In, Out] ref bool Cancel);
}

/// <summary>
/// DocumentBeforePrint hodisasini qabul qiluvchi. Word chop etishni boshlashidan
/// oldin: belgilarni olib turadi → hujjatni sinxron (Background:=False) bosib
/// chiqaradi → belgilarni qaytaradi → asl chop buyrugʻini bekor qiladi.
/// </summary>
[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
public sealed class PrintEventSink : IWordApplicationEvents4
{
    private readonly Addin _owner;
    private bool _busy;

    internal PrintEventSink(Addin owner) => _owner = owner;

    public void DocumentBeforePrint(object Doc, ref bool Cancel)
    {
        // Bizning PrintOut chaqirig'imiz hodisani qayta uyg'otadi — ichki
        // chaqiruvni shu yerda qoldiramiz (Cancel'ga tegmaymiz, chop etilsin).
        if (_busy)
            return;

        // Faqat belgilar mavjud boʻlsagina aralashamiz. Aks holda foydalanuvchi
        // tanlagan chop sozlamalarini (sahifa diapazoni va h.k.) buzmaymiz.
        if (!_owner.HasPrintMarks)
            return;

        bool removed = false;
        try
        {
            _busy = true;
            _owner.RemoveMarksForPrint();
            removed = true;

            dynamic doc = Doc;
            doc.PrintOut(false); // Background = false → sinxron, qaytguncha bloklaydi

            Cancel = true; // hujjatni oʻzimiz bosib chiqardik, asl buyruq bekor
        }
        catch (Exception ex)
        {
            AddinLog.Write("DocumentBeforePrint XATO: " + ex.Message);
            // Oʻzimizning chop muvaffaqiyatsiz boʻlsa, Word'ning normal
            // chop etishiga toʻsqinlik qilmaymiz.
            Cancel = false;
        }
        finally
        {
            if (removed)
            {
                try { _owner.RestoreMarksAfterPrint(); }
                catch (Exception ex) { AddinLog.Write("Belgilarni qaytarish XATO: " + ex.Message); }
            }
            _busy = false;
        }
    }
}
