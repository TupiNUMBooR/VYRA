using System.Runtime.InteropServices;

namespace VYRA.App;

public sealed class OverlayForm : Form
{
    private const int HOTKEY_ID = 1;
    private const int WM_HOTKEY = 0x0312;

    private const uint MOD_SHIFT = 0x0004;
    private const uint VK_T = 0x54;

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    public OverlayForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;

        Bounds = SystemInformation.VirtualScreen;

        BackColor = Color.Lime;
        TransparencyKey = Color.Lime;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        var style = GetWindowLong(Handle, GWL_EXSTYLE);
        SetWindowLong(
            Handle,
            GWL_EXSTYLE,
            style | WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW
        );

        RegisterHotKey(Handle, HOTKEY_ID, MOD_SHIFT, VK_T);
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        UnregisterHotKey(Handle, HOTKEY_ID);
        base.OnHandleDestroyed(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
        {
            MessageBox.Show(
                "VYRA heard Shift+T.",
                "VYRA",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
            return;
        }

        base.WndProc(ref m);
    }
}
