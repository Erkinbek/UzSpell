using System.Runtime.InteropServices;

namespace UzSpell.WordAddin;

// Office COM add-in interfeyslari — PIA (Primary Interop Assembly) oʻrnatish
// talab qilinmasligi uchun qoʻlda eʼlon qilingan. Imzolar rasmiy
// Extensibility/Office PIA'lariga AYNAN mos.
//
// MUHIM tafsilotlar (har biri yiqilishlar evaziga aniqlangan):
//  - [ComImport] EMAS: interfeyslar managed sinfda amalga oshiriladi.
//  - InterfaceIsDual + [DispId]: Office ham vtable, ham IDispatch orqali
//    chaqiradi; DISPID raqamlari rasmiy qiymatlar bilan bir xil boʻlishi shart.
//  - ref Array parametrlarida [MarshalAs(SafeArray, VT_VARIANT)] MAJBURIY:
//    Word u yerga native SAFEARRAY(VARIANT)* uzatadi. Atributsiz CLR uni
//    boshqacha talqin qilib, OnConnection chaqiruvida access violation bilan
//    butun Word jarayonini yiqitadi (ExecutionEngineException 0x80131506).

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
        [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] ref Array custom);

    [DispId(2)]
    void OnDisconnection(
        ext_DisconnectMode RemoveMode,
        [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] ref Array custom);

    [DispId(3)]
    void OnAddInsUpdate(
        [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] ref Array custom);

    [DispId(4)]
    void OnStartupComplete(
        [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] ref Array custom);

    [DispId(5)]
    void OnBeginShutdown(
        [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] ref Array custom);
}

/// <summary>Ribbon (lenta) sozlash interfeysi. IID — rasmiy IRibbonExtensibility.</summary>
[ComVisible(true)]
[Guid("000C0396-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsDual)]
public interface IRibbonExtensibility
{
    [DispId(1)]
    string GetCustomUI([MarshalAs(UnmanagedType.BStr)] string RibbonID);
}
