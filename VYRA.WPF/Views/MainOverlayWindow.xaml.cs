using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using VYRA.WPF.Services;

namespace VYRA.WPF.Views;

public partial class MainOverlayWindow : Window
{
    private const int HotkeyToggleChat = 1;
    private const int HotkeyExit = 2;

    private HwndSource? _source;

    public event Action? ChatToggleRequested;
    public event Action? CloseRequested;
    public event Action? ExitRequested;

    public MainOverlayWindow()
    {
        InitializeComponent();

        SourceInitialized += OnSourceInitialized;
        MouseDown += (_, e) =>
        {
            if (e.ChangedButton == MouseButton.Left)
                CloseRequested?.Invoke();
        };
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
                CloseRequested?.Invoke();
        };
    }

    public void PrepareForShow()
    {
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
        Topmost = true;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        NativeMethods.HideFromAltTab(this);

        var helper = new WindowInteropHelper(this);
        _source = HwndSource.FromHwnd(helper.Handle);
        _source?.AddHook(WndProc);

        NativeMethods.RegisterHotKey(
            helper.Handle,
            HotkeyToggleChat,
            NativeMethods.MOD_ALT | NativeMethods.MOD_NOREPEAT,
            NativeMethods.VK_T);

        NativeMethods.RegisterHotKey(
            helper.Handle,
            HotkeyExit,
            NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT | NativeMethods.MOD_NOREPEAT,
            NativeMethods.VK_T);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != NativeMethods.WM_HOTKEY)
            return IntPtr.Zero;

        var id = wParam.ToInt32();

        if (id == HotkeyToggleChat)
        {
            ChatToggleRequested?.Invoke();
            handled = true;
        }
        else if (id == HotkeyExit)
        {
            ExitRequested?.Invoke();
            handled = true;
        }

        return IntPtr.Zero;
    }

    protected override void OnClosed(EventArgs e)
    {
        var helper = new WindowInteropHelper(this);
        if (helper.Handle != IntPtr.Zero)
        {
            NativeMethods.UnregisterHotKey(helper.Handle, HotkeyToggleChat);
            NativeMethods.UnregisterHotKey(helper.Handle, HotkeyExit);
        }

        _source?.RemoveHook(WndProc);
        _source = null;

        base.OnClosed(e);
    }
}
