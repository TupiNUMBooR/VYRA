using System.Runtime.InteropServices;
using System.Text;

namespace VYRA.Services;

internal static class NativeMethods
{
    public const int WM_HOTKEY = 0x0312;
    public const int WM_SYSCOMMAND = 0x0112;
    public const int WM_NCLBUTTONDOWN = 0x00A1;

    public const int SC_MOVE = 0xF010;
    public const int HTCAPTION = 0x0002;

    public const int WS_EX_TOOLWINDOW = 0x00000080;
    public const int WS_EX_APPWINDOW = 0x00040000;

    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_NOREPEAT = 0x4000;

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    public static string GetActiveWindowTitle()
    {
        var handle = GetForegroundWindow();
        var sb = new StringBuilder(256);
        GetWindowText(handle, sb, sb.Capacity);
        return sb.ToString();
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RegisterHotKey(
        IntPtr hWnd,
        int id,
        uint fsModifiers,
        uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterHotKey(
        IntPtr hWnd,
        int id);
}
