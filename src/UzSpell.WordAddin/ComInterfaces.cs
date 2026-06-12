using System.Runtime.InteropServices;

namespace UzSpell.WordAddin;

// Office COM add-in interfeyslari — PIA (Primary Interop Assembly) oʻrnatish
// talab qilinmasligi uchun qoʻlda eʼlon qilingan.
//
// MUHIM: bu interfeyslar managed sinfda AMALGA OSHIRILADI, shuning uchun
// ular [ComImport] EMAS (ComImport faqat COM'dan import qilish uchun).
// Ular oddiy managed interfeys boʻlib, [Guid] orqali kerakli IID'ni,
// [InterfaceType] orqali vtable tartibini beradi — Word shu IID'larni
// QueryInterface qiladi, CCW esa ularni toʻgʻri vtable bilan taqdim etadi.

public enum ext_ConnectMode
{
    ext_cm_AfterStartup = 0,
    ext_cm_Startup = 1,
    ext_cm_External = 2,
    ext_cm_CommandLine = 3,
}

public enum ext_DisconnectMode
{
    ext_dm_HostShutdown = 0,
    ext_dm_UserClosed = 1,
}

/// <summary>Office add-in hayot sikli. IID — rasmiy IDTExtensibility2 qiymati.
/// Metod tartibi asl vtable tartibiga aniq mos kelishi shart.</summary>
[ComVisible(true)]
[Guid("B65AD801-ABAF-11D0-BB8B-00A0C90F2744")]
[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
public interface IDTExtensibility2
{
    void OnConnection(
        [MarshalAs(UnmanagedType.IDispatch)] object Application,
        ext_ConnectMode ConnectMode,
        [MarshalAs(UnmanagedType.IDispatch)] object AddInInst,
        ref Array custom);

    void OnDisconnection(ext_DisconnectMode RemoveMode, ref Array custom);
    void OnAddInsUpdate(ref Array custom);
    void OnStartupComplete(ref Array custom);
    void OnBeginShutdown(ref Array custom);
}

/// <summary>Ribbon (lenta) sozlash interfeysi. IID — rasmiy IRibbonExtensibility.</summary>
[ComVisible(true)]
[Guid("000C0396-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
public interface IRibbonExtensibility
{
    string GetCustomUI(string RibbonID);
}
