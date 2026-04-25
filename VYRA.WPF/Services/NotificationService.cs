using Forms = System.Windows.Forms;

namespace VYRA.WPF.Services;

public static class NotificationService
{
    public static void ShowInfo(string title, string message) => Show(title, message, Forms.ToolTipIcon.Info);

    public static void ShowWarning(string title, string message) => Show(title, message, Forms.ToolTipIcon.Warning);

    public static void ShowError(string title, string message) => Show(title, message, Forms.ToolTipIcon.Error);

    private static void Show(string title, string message, Forms.ToolTipIcon iconType)
    {
        var app = System.Windows.Application.Current;

        if (app?.Dispatcher == null || app.Dispatcher.CheckAccess())
        {
            ShowCore(title, message, iconType);
            return;
        }

        app.Dispatcher.BeginInvoke(() => ShowCore(title, message, iconType));
    }

    private static void ShowCore(string title, string message, Forms.ToolTipIcon iconType)
    {
        using var icon = new Forms.NotifyIcon
        {
            Icon = iconType == Forms.ToolTipIcon.Error
                ? System.Drawing.SystemIcons.Error
                : System.Drawing.SystemIcons.Information,
            Visible = true,
            Text = "VYRA"
        };

        icon.ShowBalloonTip(5000, title, message, iconType);
    }
}
