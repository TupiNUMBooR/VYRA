using System.Windows;
using System.Windows.Media.Imaging;
using VYRA.Core.History;
using VYRA.Core.Storage;
using VYRA.WPF.Views;

namespace VYRA.WPF.Services;

public sealed class AppController : IDisposable
{
    private readonly MainOverlayWindow _overlay = new();
    private readonly ChatWindow _chat = new();
    private readonly TrayIconManager _tray = new();
    private readonly ScreenshotService _screenshots = new();
    private readonly HistoryWriterService _historyWriter = new();

    private BitmapSource? _currentScreenshot;
    private string? _currentWindowTitle;
    private string? _currentProcessName;
    private bool _isChatOpen;
    private bool _isDisposed;

    public AppController()
    {
        _overlay.ChatToggleRequested += ToggleChat;
        _overlay.CloseRequested += HideChat;
        _overlay.ExitRequested += Exit;

        _chat.SendRequested += (_, e) =>
        {
            try
            {
                SendChat(e.Text, e.SendScreenshot);
            }
            catch (Exception ex)
            {
                ErrorHandler.Report(ex, "Send chat failed");
            }
        };

        _chat.CloseRequested += HideChat;

        _tray.OpenChatRequested += ShowChat;
        _tray.ExitRequested += Exit;
    }

    public void Start()
    {
        foreach (var error in AppStorageLinks.EnsureConvenienceLinks())
            ErrorHandler.Report(error, "Create convenience links failed");

        _overlay.Show();
        _overlay.Hide();
        _tray.Show();
    }

    private void ToggleChat()
    {
        if (_isChatOpen)
        {
            HideChat();
            return;
        }

        CaptureContext();
        ShowChat();
    }

    private void ShowChat()
    {
        _isChatOpen = true;

        _overlay.PrepareForShow();
        _overlay.Show();

        _chat.Owner = _overlay;
        _chat.ResetForOpen();
        _chat.CenterOnVirtualScreen();
        _chat.SetPreviewImage(_currentScreenshot);
        _chat.CloseOnDeactivate = true;

        if (!_chat.IsVisible)
            _chat.Show();

        _chat.Activate();
        _chat.FocusInput();
    }

    private void HideChat()
    {
        _isChatOpen = false;
        _chat.CloseOnDeactivate = false;
        _chat.Hide();
        _overlay.Hide();
    }

    private void CaptureContext()
    {
        try
        {
            _currentScreenshot = _screenshots.CaptureVirtualScreen();

            var activeWindow = NativeMethods.GetActiveWindowInfo();
            _currentWindowTitle = activeWindow.Title;
            _currentProcessName = activeWindow.ProcessName;

            _chat.SetWindowTitle(_currentWindowTitle);
        }
        catch (Exception ex)
        {
            ErrorHandler.Report(ex, "Capture context failed");
        }
    }

    private void SendChat(string text, bool sendScreenshot)
    {
        var image = sendScreenshot ? _currentScreenshot : null;
        var sourceName = HistoryFileName.CreateSourceName(_currentProcessName, _currentWindowTitle);

        if (string.IsNullOrWhiteSpace(text) && image == null)
            return;

        _chat.AddComboMessage(image, text, true);
        _chat.ClearInput();

        _historyWriter.EnqueueUserMessage(text, image, sourceName);

        _currentScreenshot = null;
        _currentWindowTitle = null;
        _currentProcessName = null;

        HideChat();
    }

    private void Exit()
    {
        Dispose();
        System.Windows.Application.Current.Shutdown();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _historyWriter.Dispose();

        _tray.Dispose();
        _chat.ForceClose();
        _overlay.Close();
    }
}
