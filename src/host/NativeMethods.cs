using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OpenCodeDesktopWidget;

internal static class NativeMethods
{
    public static readonly int ShowMessage = RegisterWindowMessage("OpenCodeDesktopWidget.WebView2.Show");
    private const int HwndBroadcast = 0xffff;
    public const int WmNclButtonDown = 0x00A1;
    public const int HtCaption = 0x0002;
    private const int GwlExStyle = -20;
    private const int WsExTransparent = 0x00000020;
    private const int FlashwAll = 3;
    private const int FlashwTimerNoFg = 12;

    public static void BroadcastShowMessage() => PostMessage((IntPtr)HwndBroadcast, ShowMessage, IntPtr.Zero, IntPtr.Zero);

    public static void BeginWindowDrag(IntPtr handle)
    {
        ReleaseCapture();
        SendMessage(handle, WmNclButtonDown, (IntPtr)HtCaption, IntPtr.Zero);
    }

    public static void SetClickThrough(IntPtr handle, bool enabled)
    {
        var style = GetWindowLongPtr(handle, GwlExStyle).ToInt64();
        style = enabled ? style | WsExTransparent : style & ~WsExTransparent;
        SetWindowLongPtr(handle, GwlExStyle, (IntPtr)style);
    }

    public static void FlashWindow(IntPtr handle, bool start)
    {
        var info = new FlashWInfo
        {
            Size = (uint)Marshal.SizeOf<FlashWInfo>(),
            Window = handle,
            Flags = start ? FlashwAll | FlashwTimerNoFg : 0,
            Count = start ? uint.MaxValue : 0,
            Timeout = 0
        };
        FlashWindowEx(ref info);
    }

    public static void OpenExternal(string target)
    {
        Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int RegisterWindowMessage(string message);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int index);

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int index);

    private static IntPtr GetWindowLongPtr(IntPtr hWnd, int index) => IntPtr.Size == 8
        ? GetWindowLongPtr64(hWnd, index)
        : GetWindowLongPtr32(hWnd, index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int index, IntPtr value);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern IntPtr SetWindowLongPtr32(IntPtr hWnd, int index, IntPtr value);

    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int index, IntPtr value) => IntPtr.Size == 8
        ? SetWindowLongPtr64(hWnd, index, value)
        : SetWindowLongPtr32(hWnd, index, value);

    [DllImport("user32.dll")]
    private static extern bool FlashWindowEx(ref FlashWInfo info);

    [StructLayout(LayoutKind.Sequential)]
    private struct FlashWInfo
    {
        public uint Size;
        public IntPtr Window;
        public int Flags;
        public uint Count;
        public uint Timeout;
    }
}
