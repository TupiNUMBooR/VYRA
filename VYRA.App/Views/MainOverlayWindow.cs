using FormsTimer = System.Windows.Forms.Timer;
using VYRA.Services;

namespace VYRA.Views;

public sealed class MainOverlayWindow : Form
{
    private const int HotkeyToggleChat = 1;
    private const int HotkeyExit = 2;

    private FormsTimer? _fadeTimer;
    private double _targetOpacity;
    private Action? _afterFade;

    public event Action? ChatToggleRequested;
    public event Action? CloseRequested;
    public event Action? ExitRequested;

    public MainOverlayWindow()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.Black;
        Opacity = 0;
        TopMost = true;
        KeyPreview = true;
    }

    public void InitializeHotkeys()
    {
        _ = Handle;

        NativeMethods.RegisterHotKey(
            Handle,
            HotkeyToggleChat,
            NativeMethods.MOD_ALT | NativeMethods.MOD_NOREPEAT,
            (uint)Keys.T);

        NativeMethods.RegisterHotKey(
            Handle,
            HotkeyExit,
            NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT | NativeMethods.MOD_NOREPEAT,
            (uint)Keys.T);
    }

    public void PrepareForShow()
    {
        Bounds = SystemInformation.VirtualScreen;
        Opacity = 0;
        TopMost = true;
    }

    public void FadeTo(double targetOpacity, Action? afterFade = null)
    {
        _fadeTimer?.Stop();
        _fadeTimer?.Dispose();

        _targetOpacity = Math.Clamp(targetOpacity, 0, 1);
        _afterFade = afterFade;

        _fadeTimer = new FormsTimer { Interval = 15 };
        _fadeTimer.Tick += (_, _) =>
        {
            var diff = _targetOpacity - Opacity;

            if (Math.Abs(diff) < 0.035)
            {
                Opacity = _targetOpacity;
                _fadeTimer.Stop();
                _fadeTimer.Dispose();
                _fadeTimer = null;

                var callback = _afterFade;
                _afterFade = null;
                callback?.Invoke();
                return;
            }

            Opacity += diff * 0.28;
        };

        _fadeTimer.Start();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.Button == MouseButtons.Left)
            CloseRequested?.Invoke();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            CloseRequested?.Invoke();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY)
        {
            var id = m.WParam.ToInt32();

            if (id == HotkeyToggleChat)
            {
                ChatToggleRequested?.Invoke();
                return;
            }

            if (id == HotkeyExit)
            {
                ExitRequested?.Invoke();
                return;
            }
        }

        base.WndProc(ref m);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= NativeMethods.WS_EX_TOOLWINDOW;
            cp.ExStyle &= ~NativeMethods.WS_EX_APPWINDOW;
            return cp;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (IsHandleCreated)
        {
            NativeMethods.UnregisterHotKey(Handle, HotkeyToggleChat);
            NativeMethods.UnregisterHotKey(Handle, HotkeyExit);
        }

        if (disposing)
            _fadeTimer?.Dispose();

        base.Dispose(disposing);
    }
}
