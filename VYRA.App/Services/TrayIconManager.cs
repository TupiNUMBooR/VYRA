namespace VYRA.Services;

public sealed class TrayIconManager : IDisposable
{
    private readonly NotifyIcon _notifyIcon;

    public event Action? OpenChatRequested;
    public event Action? ExitRequested;

    public TrayIconManager()
    {
        var menu = new ContextMenuStrip();

        var openChat = new ToolStripMenuItem("Open chat");
        openChat.Click += (_, _) => OpenChatRequested?.Invoke();

        var exit = new ToolStripMenuItem("Exit");
        exit.Click += (_, _) => ExitRequested?.Invoke();

        menu.Items.Add(openChat);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exit);

        _notifyIcon = new NotifyIcon
        {
            Text = "VYRA",
            Icon = LoadIcon(),
            ContextMenuStrip = menu,
            Visible = false
        };

        _notifyIcon.DoubleClick += (_, _) => OpenChatRequested?.Invoke();
    }

    public void Show()
    {
        _notifyIcon.Visible = true;
    }

    private static Icon LoadIcon()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico");

        if (File.Exists(path))
            return new Icon(path);

        return SystemIcons.Application;
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
