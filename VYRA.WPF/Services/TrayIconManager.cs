using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Hardcodet.Wpf.TaskbarNotification;

namespace VYRA.WPF.Services;

public sealed class TrayIconManager : IDisposable
{
    private readonly TaskbarIcon _taskbarIcon;

    public event Action? OpenChatRequested;
    public event Action? ConfigureOpenAiRequested;
    public event Action? OpenHistoryRequested;
    public event Action? ExitRequested;

    public TrayIconManager()
    {
        _taskbarIcon = new TaskbarIcon
        {
            ToolTipText = "VYRA",
            IconSource = LoadIconSource(),
            ContextMenu = CreateContextMenu(),
            Visibility = Visibility.Collapsed
        };

        _taskbarIcon.TrayMouseDoubleClick += (_, _) => OpenChatRequested?.Invoke();
    }

    public void Show() => _taskbarIcon.Visibility = Visibility.Visible;

    private ContextMenu CreateContextMenu()
    {
        var menu = new ContextMenu();

        var openChat = new MenuItem { Header = "Open chat" };
        openChat.Click += (_, _) => OpenChatRequested?.Invoke();

        var openAiToken = new MenuItem { Header = "OpenAI token..." };
        openAiToken.Click += (_, _) => ConfigureOpenAiRequested?.Invoke();

        var openHistory = new MenuItem { Header = "Open history folder" };
        openHistory.Click += (_, _) => OpenHistoryRequested?.Invoke();

        var exit = new MenuItem { Header = "Exit" };
        exit.Click += (_, _) => ExitRequested?.Invoke();

        menu.Items.Add(openChat);
        menu.Items.Add(openAiToken);
        menu.Items.Add(openHistory);
        menu.Items.Add(new Separator());
        menu.Items.Add(exit);

        return menu;
    }

    private static BitmapImage LoadIconSource()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico");
        if (!File.Exists(path))
            path = Path.Combine(AppContext.BaseDirectory, "Assets", "VYRA1.jpg");

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = File.Exists(path)
            ? new Uri(path, UriKind.Absolute)
            : new Uri("pack://application:,,,/VYRA.WPF;component/Assets/VYRA1.jpg", UriKind.Absolute);
        image.EndInit();
        image.Freeze();
        return image;
    }

    public void Dispose() => _taskbarIcon.Dispose();
}
