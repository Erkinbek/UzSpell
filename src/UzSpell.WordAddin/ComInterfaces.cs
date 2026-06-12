using System.Runtime.InteropServices;

namespace UzSpell.WordAddin;

// Office COM add-in interfeyslari — PIA (Primary Interop Assembly) oʻrnatish
// talab qilinmasligi uchun qoʻlda eʼlon qilingan.
//
// MUHIM tafsilotlar:
//  - [ComImport] EMAS: bu interfeyslar managed sinfda AMALGA OSHIRILADI.
//  - InterfaceIsDual: Office OnConnection va boshqalarni ham vtable, ham
//    IDispatch orqali chaqirishi mumkin — Dual ikkalasini ham qoplaydi.
//  - [DispId(...)]: Office aynan shu raqamlar bilan chaqiradi (OnConnection=1,
//    ...). Raqamsiz CLR oʻzicha raqam berib, Word ulana olmay yiqilardi.
//  - Metod tartibi asl interfeys vtable tartibiga aniq mos kelishi shart.

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

/// <summary>Office add-in hayot sikli. IID — rasmiy IDTExtensibility2 qiymati.</summary>
[ComVisible(true)]
[Guid("B65AD801-ABAF-11D0-BB8B-00A0C90F2744")]
[InterfaceType(ComInterfaceType.InterfaceIsDual)]
public interface IDTExtensibility2
{
    [DispId(1)]
    void OnConnection(
        [MarshalAs(UnmanagedType.IDispatch)] object Application,
        ext_ConnectMode ConnectMode,
        [MarshalAs(UnmanagedType.IDispatch)] object AddInInst,
        ref Array custom);

    [DispId(2)]
    void OnDisconnection(ext_DisconnectMode RemoveMode, ref Array custom);

    [DispId(3)]
    void OnAddInsUpdate(ref Array custom);

    [DispId(4)]
    void OnStartupComplete(ref Array custom);

    [DispId(5)]
    void OnBeginShutdown(ref Array custom);
}

/// <summary>Ribbon (lenta) sozlash interfeysi. IID — rasmiy IRibbonExtensibility.</summary>
[ComVisible(true)]
[Guid("000C0396-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsDual)]
public interface IRibbonExtensibility
{
    [DispId(1)]
    string GetCustomUI(string RibbonID);
}
