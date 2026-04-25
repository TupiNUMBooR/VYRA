using VYRA.Models;
using VYRA.Views;

namespace VYRA.Services;

public sealed class AppController : IDisposable
{
    private readonly MainOverlayWindow _overlay = new();
    private readonly ChatWindow _chat = new();
    private readonly TrayIconManager _tray = new();
    private readonly ScreenshotService _screenshots = new();

    private Bitmap? CurrentScreenshot;
    private string? CurrentWindowTitle;

    private bool _isChatOpen;
    private bool _isDisposed;

    public AppController()
    {
        _overlay.ChatToggleRequested += ToggleChat;
        _overlay.CloseRequested += HideChat;
        _overlay.ExitRequested += Exit;

        _chat.SendRequested += (text, sendScreenshot) =>
        {
            try
            {
                SendChat(text, sendScreenshot);
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
        _overlay.InitializeHotkeys();
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

        _chat.ResetForOpen();
        _chat.CenterOnVirtualScreen();

        if (!_chat.Visible)
            _chat.Show(_overlay);

        _chat.SetPreviewImage(CurrentScreenshot);
        _chat.CloseOnDeactivate = true;

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
            CurrentScreenshot = _screenshots.CaptureVirtualScreen();
            CurrentWindowTitle = NativeMethods.GetActiveWindowTitle();

            _chat.SetWindowTitle(CurrentWindowTitle);
        }
        catch (Exception ex)
        {
            ErrorHandler.Report(ex, "Capture context failed");
        }
    }

    private void SendChat(string text, bool sendScreenshot)
    {
        if (string.IsNullOrWhiteSpace(text) && !sendScreenshot)
            return;

        if (!string.IsNullOrWhiteSpace(text))
            _chat.AddTextMessage(text, true);

        if (sendScreenshot && CurrentScreenshot != null)
            _chat.AddImageMessage(CurrentScreenshot, true);

        _chat.ClearInput();
        CurrentScreenshot = null;

        HideChat();
    }

    private void Exit()
    {
        Dispose();
        Application.Exit();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        CurrentScreenshot?.Dispose();

        _tray.Dispose();
        _chat.Dispose();
        _overlay.Dispose();
    }
}
