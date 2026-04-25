using System.Windows;
using System.Windows.Media.Imaging;
using VYRA.Core.History;
using VYRA.Core.Storage;
using VYRA.OpenAI;
using VYRA.WPF.Views;

namespace VYRA.WPF.Services;

public sealed class AppController : IDisposable
{
    private const int MaxContextMessages = 20;

    private readonly MainOverlayWindow _overlay = new();
    private readonly ChatWindow _chat = new();
    private readonly TrayIconManager _tray = new();
    private readonly ScreenshotService _screenshots = new();
    private readonly HistoryWriterService _historyWriter = new();
    private readonly OpenAiTokenStore _tokenStore = new();
    private readonly OpenAiChatClient _openAi = new();
    private readonly List<OpenAiChatMessage> _textContext = new();

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

        _chat.SendRequested += async (_, e) =>
        {
            try
            {
                await SendChatAsync(e.Text, e.SendScreenshot).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                ErrorHandler.Report(ex, "Send chat failed");
            }
        };

        _chat.CloseRequested += HideChat;

        _tray.OpenChatRequested += ShowChat;
        _tray.ConfigureOpenAiRequested += ConfigureOpenAiToken;
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

    private async Task SendChatAsync(string text, bool sendScreenshot)
    {
        var image = sendScreenshot ? _currentScreenshot : null;
        var sourceName = HistoryFileName.CreateSourceName(_currentProcessName, _currentWindowTitle);

        if (string.IsNullOrWhiteSpace(text) && image == null)
            return;

        var token = _tokenStore.TryLoadToken();
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("OpenAI token is not configured. Use tray menu: OpenAI token...");

        _chat.SetBusy(true);
        _chat.AddComboMessage(image, text, true);
        _chat.ClearInput();

        _historyWriter.EnqueueUserMessage(text, image, sourceName);

        var screenshotJpg = image == null ? null : ScreenshotService.ToJpgBytes(image);
        var userMessage = new OpenAiChatMessage(
            "user",
            string.IsNullOrWhiteSpace(text) ? "Look at the current screenshot." : text.Trim());

        var requestMessages = BuildRequestMessages(userMessage);

        try
        {
            var reply = await _openAi.SendAsync(
                    new OpenAiOptions(token),
                    new OpenAiChatRequest(requestMessages, screenshotJpg))
                .ConfigureAwait(true);

            _chat.AddTextMessage(reply.Text, isUser: false);
            _historyWriter.EnqueueAssistantMessage(reply.Text);

            RememberTextMessage(userMessage);
            RememberTextMessage(new OpenAiChatMessage("assistant", reply.Text));

            _currentScreenshot = null;
            _currentWindowTitle = null;
            _currentProcessName = null;
            _chat.SetPreviewImage(null);
        }
        finally
        {
            _chat.SetBusy(false);
        }
    }

    private IReadOnlyList<OpenAiChatMessage> BuildRequestMessages(OpenAiChatMessage currentUserMessage)
    {
        var previousCount = Math.Max(0, MaxContextMessages - 1);
        var messages = _textContext
            .TakeLast(previousCount)
            .Append(currentUserMessage)
            .ToList();

        return messages;
    }

    private void RememberTextMessage(OpenAiChatMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.Text))
            return;

        _textContext.Add(message);

        if (_textContext.Count <= MaxContextMessages)
            return;

        _textContext.RemoveRange(0, _textContext.Count - MaxContextMessages);
    }

    private void ConfigureOpenAiToken()
    {
        try
        {
            var window = new OpenAiTokenWindow(_tokenStore.HasToken)
            {
                Owner = _chat.IsVisible ? _chat : _overlay
            };

            var result = window.ShowDialog();
            if (result != true)
                return;

            if (window.ClearRequested)
            {
                _tokenStore.ClearToken();
                return;
            }

            if (string.IsNullOrWhiteSpace(window.Token))
                return;

            _tokenStore.SaveToken(window.Token);
        }
        catch (Exception ex)
        {
            ErrorHandler.Report(ex, "Configure OpenAI token failed");
        }
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

        _openAi.Dispose();
        _historyWriter.Dispose();

        _tray.Dispose();
        _chat.ForceClose();
        _overlay.Close();
    }
}
