using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace VYRA.WPF.Services;

internal static class NativeMethods
{
    public const int WM_HOTKEY = 0x0312;
    public const int GWL_EXSTYLE = -20;
    public const int WS_EX_TOOLWINDOW = 0x00000080;
    public const int WS_EX_APPWINDOW = 0x00040000;

    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_NOREPEAT = 0x4000;

    public const uint VK_T = 0x54;

    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    public static Rectangle GetVirtualScreenBounds()
    {
        return new Rectangle(
            GetSystemMetrics(SM_XVIRTUALSCREEN),
            GetSystemMetrics(SM_YVIRTUALSCREEN),
            GetSystemMetrics(SM_CXVIRTUALSCREEN),
            GetSystemMetrics(SM_CYVIRTUALSCREEN));
    }

    public static ActiveWindowInfo GetActiveWindowInfo()
    {
        var handle = GetForegroundWindow();

        return new ActiveWindowInfo(
            Title: GetWindowTitle(handle),
            ProcessName: GetProcessName(handle));
    }

    public static string GetActiveWindowTitle()
    {
        return GetActiveWindowInfo().Title ?? string.Empty;
    }

    public static void HideFromAltTab(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero) return;

        var exStyle = GetWindowLong(handle, GWL_EXSTYLE);
        exStyle |= WS_EX_TOOLWINDOW;
        exStyle &= ~WS_EX_APPWINDOW;
        SetWindowLong(handle, GWL_EXSTYLE, exStyle);
    }

    private static string? GetWindowTitle(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
            return null;

        var length = GetWindowTextLength(handle);
        if (length <= 0)
            return null;

        var builder = new StringBuilder(length + 1);
        GetWindowText(handle, builder, builder.Capacity);

        var title = builder.ToString();
        return string.IsNullOrWhiteSpace(title) ? null : title;
    }

    private static string? GetProcessName(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
            return null;

        GetWindowThreadProcessId(handle, out var processId);

        if (processId == 0)
            return null;

        try
        {
            using var process = Process.GetProcessById((int)processId);
            return string.IsNullOrWhiteSpace(process.ProcessName)
                ? null
                : process.ProcessName;
        }
        catch
        {
            return null;
        }
    }
}

internal sealed record ActiveWindowInfo(string? Title, string? ProcessName);
