using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using VYRA.Core.History;
using VYRA.Core.Storage;
using VYRA.OpenAI;
using VYRA.WPF.ViewModels;
using VYRA.WPF.Views;

namespace VYRA.WPF.Services;

public sealed class AppController : IDisposable
{
    private const int MaxContextMessages = 20;
    private const int MaxHistoryLookbackDays = 90;
    private const int HistoryPreviewWidth = 320;

    private readonly MainOverlayWindow _overlay = new();
    private readonly ChatWindow _chat = new();
    private readonly TrayIconManager _tray = new();
    private readonly ScreenshotService _screenshots = new();
    private readonly HistoryWriterService _historyWriter;
    private readonly OpenAiTokenStore _tokenStore = new();
    private readonly OpenAiChatClient _openAi = new();
    private readonly HistoryPaths _historyPaths = new();
    private readonly HistoryService _history;
    private readonly List<OpenAiChatMessage> _textContext = new();

    private Task<HistoryLoadResult>? _startupHistoryTask;
    private HistoryLoadResult? _startupHistory;
    private DateTime _oldestLoadedUiDate = DateTime.Today;
    private DateTime? _lastUiMessageDate;
    private BitmapSource? _currentScreenshot;
    private string? _currentWindowTitle;
    private string? _currentProcessName;
    private bool _isChatOpen;
    private bool _isDisposed;
    private bool _isInitialHistoryShown;
    private bool _isLoadingPreviousHistory;

    public AppController()
    {
        _history = new HistoryService(_historyPaths);
        _historyWriter = new HistoryWriterService(_history);

        _overlay.ChatToggleRequested += ToggleChat;
        _overlay.CloseRequested += HideChat;
        _overlay.ExitRequested += Exit;

        _chat.SendRequested += async (_, e) =>
        {
            await SendChatAsync(e.Text, e.SendScreenshot).ConfigureAwait(true);
        };

        _chat.CloseRequested += HideChat;
        _chat.PreviousHistoryRequested += () => _ = LoadPreviousHistoryDayAsync();

        _tray.OpenChatRequested += ShowChat;
        _tray.ConfigureOpenAiRequested += ConfigureOpenAiToken;
        _tray.OpenHistoryRequested += OpenHistoryFolder;
        _tray.ExitRequested += Exit;
    }

    public void Start()
    {
        foreach (var error in AppStorageLinks.EnsureConvenienceLinks())
            ErrorHandler.Report(error, "Create convenience links failed");

        _startupHistoryTask = LoadStartupHistoryAsync();

        _overlay.Show();
        _overlay.Hide();
        _tray.Show();

        _ = CheckOpenAiConnectionAndNotifyAsync("OpenAI connection OK.");
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
        _ = ShowChatAsync();
    }

    private async Task ShowChatAsync()
    {
        _isChatOpen = true;

        await EnsureStartupHistoryLoadedAsync().ConfigureAwait(true);
        ShowInitialHistoryIfNeeded();

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
        if (_chat.IsSending)
            return;

        await EnsureStartupHistoryLoadedAsync().ConfigureAwait(true);

        var image = sendScreenshot ? _currentScreenshot : null;
        var trimmedText = text.Trim();

        if (string.IsNullOrWhiteSpace(trimmedText) && image == null)
            return;

        var timestamp = DateTime.Now;
        var sourceName = HistoryFileName.CreateSourceName(_currentProcessName, _currentWindowTitle);
        EnsureDateSeparatorForLiveMessage(timestamp);
        var userBubble = _chat.AddPendingUserMessage(image, trimmedText);

        _historyWriter.EnqueueUserMessage(trimmedText, image, sourceName);
        _chat.ClearInput();

        if (image != null)
        {
            _currentScreenshot = null;
            _currentWindowTitle = null;
            _currentProcessName = null;
            _chat.ClearScreenshotInput();
        }

        _chat.SetBusy(true);

        try
        {
            var token = LoadTokenOrThrow();
            var screenshotJpg = image == null ? null : ScreenshotService.ToJpgBytes(image);
            var userMessage = new OpenAiChatMessage(
                "user",
                string.IsNullOrWhiteSpace(trimmedText) ? "Look at the current screenshot." : trimmedText);

            var requestMessages = BuildRequestMessages(userMessage);
            var reply = await _openAi.SendAsync(
                    new OpenAiOptions(token),
                    new OpenAiChatRequest(requestMessages, screenshotJpg))
                .ConfigureAwait(true);

            userBubble.MarkDelivered();
            EnsureDateSeparatorForLiveMessage(DateTime.Now);
            _chat.AddTextMessage(reply.Text, isUser: false);
            _historyWriter.EnqueueAssistantMessage(reply.Text);

            RememberTextMessage(userMessage);
            RememberTextMessage(new OpenAiChatMessage("assistant", reply.Text));

            if (!_isChatOpen)
                NotificationService.ShowInfo("VYRA answered", reply.Text);
        }
        catch (Exception ex)
        {
            userBubble.MarkFailed();
            ErrorHandler.Report(ex, "Send chat failed");
        }
        finally
        {
            _chat.SetBusy(false);
            _chat.FocusInput();
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

    private async Task<HistoryLoadResult> LoadStartupHistoryAsync()
    {
        try
        {
            var result = await _history.LoadStartupHistoryAsync(
                    MaxContextMessages,
                    MaxHistoryLookbackDays)
                .ConfigureAwait(false);

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _startupHistory = result;
                _oldestLoadedUiDate = result.Today.Date;
                RebuildTextContext(result.ContextMessages);
            });

            return result;
        }
        catch (Exception ex)
        {
            ErrorHandler.Report(ex, "Startup history load failed");
            var emptyToday = new HistoryDay(DateTime.Today, Array.Empty<HistoryMessage>());
            var empty = new HistoryLoadResult(
                emptyToday,
                new[] { emptyToday },
                Array.Empty<HistoryMessage>());

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _startupHistory = empty;
                _oldestLoadedUiDate = empty.Today.Date;
                _textContext.Clear();
            });

            return empty;
        }
    }

    private async Task EnsureStartupHistoryLoadedAsync()
    {
        _startupHistoryTask ??= LoadStartupHistoryAsync();
        _startupHistory ??= await _startupHistoryTask.ConfigureAwait(true);
    }

    private void ShowInitialHistoryIfNeeded()
    {
        if (_isInitialHistoryShown)
            return;

        var history = _startupHistory;
        if (history == null)
            return;

        IReadOnlyList<HistoryDay> loadedDays = history.LoadedDaysForUi.Count > 0
            ? history.LoadedDaysForUi
            : new[] { history.Today };

        var viewModels = loadedDays
            .SelectMany(CreateDayViewModels)
            .ToList();

        _chat.LoadHistoryDay(viewModels);
        _oldestLoadedUiDate = loadedDays[0].Date;
        _lastUiMessageDate = loadedDays[^1].Date;
        _isInitialHistoryShown = true;
    }

    private async Task LoadPreviousHistoryDayAsync()
    {
        if (_isLoadingPreviousHistory)
            return;

        _isLoadingPreviousHistory = true;
        try
        {
            await EnsureStartupHistoryLoadedAsync().ConfigureAwait(true);

            var day = await _history.ReadPreviousExistingDayAsync(
                    _oldestLoadedUiDate,
                    MaxHistoryLookbackDays)
                .ConfigureAwait(true);

            if (day == null)
                return;

            _oldestLoadedUiDate = day.Date;
            _chat.PrependHistoryDay(CreateDayViewModels(day));
        }
        catch (Exception ex)
        {
            ErrorHandler.Report(ex, "Load previous history day failed");
        }
        finally
        {
            _isLoadingPreviousHistory = false;
        }
    }

    private void RebuildTextContext(IEnumerable<HistoryMessage> messages)
    {
        _textContext.Clear();

        foreach (var message in messages)
        {
            if (!message.HasText)
                continue;

            var role = message.IsUser ? "user" : message.IsAssistant ? "assistant" : null;
            if (role == null)
                continue;

            RememberTextMessage(new OpenAiChatMessage(role, message.Text!));
        }
    }

    private IEnumerable<ChatMessageViewModel> CreateDayViewModels(HistoryDay day)
    {
        yield return ChatMessageViewModel.DateSeparator(day.Date);

        foreach (var message in day.Messages)
        {
            var image = TryLoadHistoryImage(message);
            var isUser = message.IsUser;

            if (image == null && string.IsNullOrWhiteSpace(message.Text))
                continue;

            yield return ChatMessageViewModel.ComboMessage(
                image,
                message.Text,
                isUser,
                message.Timestamp);
        }
    }

    private BitmapSource? TryLoadHistoryImage(HistoryMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.ImageFileName) || string.IsNullOrWhiteSpace(message.MonthDirectory))
            return null;

        var path = Path.Combine(message.MonthDirectory, message.ImageFileName);
        if (!File.Exists(path))
            return null;

        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = HistoryPreviewWidth;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception ex)
        {
            ErrorHandler.Report(ex, "Load history image failed");
            return null;
        }
    }

    private void EnsureDateSeparatorForLiveMessage(DateTime timestamp)
    {
        if (_lastUiMessageDate == timestamp.Date)
            return;

        _chat.AppendMessage(ChatMessageViewModel.DateSeparator(timestamp.Date));
        _lastUiMessageDate = timestamp.Date;
    }

    private async void ConfigureOpenAiToken()
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
                NotificationService.ShowWarning("OpenAI token", "OpenAI token cleared.");
                return;
            }

            if (string.IsNullOrWhiteSpace(window.Token))
            {
                NotificationService.ShowWarning("OpenAI token", "OpenAI token is empty.");
                return;
            }

            _tokenStore.SaveToken(window.Token);
            await CheckOpenAiConnectionAndNotifyAsync("OpenAI token saved and verified.").ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            ErrorHandler.Report(ex, "Configure OpenAI token failed");
        }
    }

    private async Task CheckOpenAiConnectionAndNotifyAsync(string successMessage)
    {
        try
        {
            var token = LoadTokenOrThrow();
            await _openAi.CheckConnectionAsync(new OpenAiOptions(token)).ConfigureAwait(true);
            NotificationService.ShowInfo("VYRA", successMessage);
        }
        catch (Exception ex)
        {
            ErrorHandler.Report(ex, "OpenAI connection check failed");
        }
    }

    private string LoadTokenOrThrow()
    {
        var token = _tokenStore.TryLoadToken();

        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("OpenAI token is not configured. Use tray menu: OpenAI token...");

        return token;
    }

    private void OpenHistoryFolder()
    {
        try
        {
            Directory.CreateDirectory(_historyPaths.RootPath);

            Process.Start(new ProcessStartInfo
            {
                FileName = _historyPaths.RootPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ErrorHandler.Report(ex, "Open history folder failed");
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
