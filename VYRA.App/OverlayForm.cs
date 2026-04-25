namespace VYRA.App;

public sealed class OverlayForm : Form
{
    private const int ShowStubHotkeyId = 1;
    private const int ExitHotkeyId = 2;

    private readonly NotifyIcon trayIcon;

    public OverlayForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;

        Bounds = SystemInformation.VirtualScreen;

        BackColor = Color.Lime;
        TransparencyKey = Color.Lime;

        trayIcon = new NotifyIcon
        {
            Icon = LoadTrayIcon(),
            Text = "VYRA",
            Visible = true,
            ContextMenuStrip = BuildTrayMenu()
        };
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        ApplyOverlayWindowStyle();

        NativeMethods.RegisterHotKey(
            Handle,
            ShowStubHotkeyId,
            NativeMethods.MOD_SHIFT,
            NativeMethods.VK_T
        );

        NativeMethods.RegisterHotKey(
            Handle,
            ExitHotkeyId,
            NativeMethods.MOD_SHIFT | NativeMethods.MOD_ALT,
            NativeMethods.VK_T
        );
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        ApplyOverlayWindowStyle();
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        NativeMethods.UnregisterHotKey(Handle, ShowStubHotkeyId);
        NativeMethods.UnregisterHotKey(Handle, ExitHotkeyId);

        trayIcon.Visible = false;
        trayIcon.Dispose();

        base.OnHandleDestroyed(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY)
        {
            HandleHotkey(m.WParam.ToInt32());
            return;
        }

        base.WndProc(ref m);
    }

    private void HandleHotkey(int hotkeyId)
    {
        switch (hotkeyId)
        {
            case ShowStubHotkeyId:
                ShowStub();
                break;

            case ExitHotkeyId:
                Application.Exit();
                break;
        }
    }

    private void ShowStub()
    {
        NativeMethods.SetForegroundWindow(Handle);

        MessageBox.Show(
            this,
            "VYRA heard Shift+T.",
            "VYRA",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );
    }

    private void ApplyOverlayWindowStyle()
    {
        var style = NativeMethods.GetWindowLong(
            Handle,
            NativeMethods.GWL_EXSTYLE
        );

        style |= NativeMethods.WS_EX_LAYERED;
        style |= NativeMethods.WS_EX_TRANSPARENT;
        style |= NativeMethods.WS_EX_TOOLWINDOW;
        style &= ~NativeMethods.WS_EX_APPWINDOW;

        NativeMethods.SetWindowLong(
            Handle,
            NativeMethods.GWL_EXSTYLE,
            style
        );
    }

    private static ContextMenuStrip BuildTrayMenu()
    {
        var menu = new ContextMenuStrip();

        menu.Items.Add("Show stub", null, (_, _) =>
        {
            MessageBox.Show(
                "VYRA is still here.",
                "VYRA",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        });

        menu.Items.Add("Exit", null, (_, _) => Application.Exit());

        return menu;
    }

    private static Icon LoadTrayIcon()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "icon.ico"
        );

        if (File.Exists(path))
        {
            return new Icon(path);
        }

        return SystemIcons.Application;
    }
}
