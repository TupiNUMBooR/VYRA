using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using WpfInput = System.Windows.Input;
using System.Windows.Media.Imaging;
using VYRA.Core;
using VYRA.WPF.ViewModels;

namespace VYRA.WPF.Views;

public partial class ChatWindow : Window
{
    private readonly ObservableCollection<ChatMessageViewModel> _messages = new();
    private bool _isClosingForReal;

    public event EventHandler<ChatSendRequestedEventArgs>? SendRequested;
    public event Action? CloseRequested;

    public bool CloseOnDeactivate { get; set; }

    public ChatWindow()
    {
        InitializeComponent();
        HistoryList.ItemsSource = _messages;
        Loaded += (_, _) => FocusInput();
        UpdatePreviewLayout();
    }

    public void ResetForOpen()
    {
        SendScreenshotCheckBox.IsChecked = true;
        UpdatePreviewLayout();
        FocusInput();
    }

    public void FocusInput()
    {
        if (!IsLoaded) return;

        InputBox.Focus();
        InputBox.CaretIndex = InputBox.Text.Length;
    }

    public void ClearInput() => InputBox.Clear();

    public void SetBusy(bool isBusy)
    {
        InputBox.IsEnabled = !isBusy;
        SendScreenshotCheckBox.IsEnabled = !isBusy;
        Cursor = isBusy ? WpfInput.Cursors.Wait : null;
    }

    public void CenterOnVirtualScreen()
    {
        ApplyScreenRelativeSize();

        Left = SystemParameters.WorkArea.Left + (SystemParameters.WorkArea.Width - Width) / 2;
        Top = SystemParameters.WorkArea.Top + (SystemParameters.WorkArea.Height - Height) / 2;
    }

    public void SetPreviewImage(BitmapSource? image)
    {
        ScreenshotPreview.Source = image;
        UpdatePreviewLayout();
    }

    public void AddTextMessage(string text, bool isUser)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        _messages.Add(ChatMessageViewModel.TextMessage(text, isUser));
        ScrollHistoryToBottom();
    }

    public void AddImageMessage(BitmapSource image, bool isUser)
    {
        _messages.Add(ChatMessageViewModel.ImageMessage(image, isUser));
        ScrollHistoryToBottom();
    }

    public void AddComboMessage(BitmapSource? image, string? text, bool isUser)
    {
        if (image == null && string.IsNullOrWhiteSpace(text)) return;

        _messages.Add(ChatMessageViewModel.ComboMessage(image, text, isUser));
        ScrollHistoryToBottom();
    }

    public void SetWindowTitle(string? title)
    {
        Title = string.IsNullOrWhiteSpace(title)
            ? "VYRA"
            : $"VYRA [{title}]";
    }

    public void ForceClose()
    {
        _isClosingForReal = true;
        Close();
    }

    private void ApplyScreenRelativeSize()
    {
        Width = Math.Clamp(SystemParameters.WorkArea.Width * 0.68, MinWidth, 1280);
        Height = Math.Clamp(SystemParameters.WorkArea.Height * 0.8, MinHeight, 1000);
    }

    private void Send()
    {
        SendRequested?.Invoke(
            this,
            new ChatSendRequestedEventArgs(InputBox.Text, SendScreenshotCheckBox.IsChecked == true));
    }

    private void ToggleScreenshot()
    {
        SendScreenshotCheckBox.IsChecked = SendScreenshotCheckBox.IsChecked != true;
        UpdatePreviewLayout();
        FocusInput();
    }

    private void UpdatePreviewLayout()
    {
        var visible = ScreenshotPreview.Source != null && SendScreenshotCheckBox.IsChecked == true;

        ScreenshotPreviewHost.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        ScreenshotColumn.Width = visible ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

        Grid.SetColumnSpan(InputBox, visible ? 1 : 2);
        InputBox.Margin = visible ? new Thickness(0, 0, 10, 8) : new Thickness(0, 0, 0, 8);
    }

    private void ShowImagePreview(BitmapSource source)
    {
        var oldCloseOnDeactivate = CloseOnDeactivate;
        CloseOnDeactivate = false;

        var preview = new ImagePreviewWindow(source)
        {
            Owner = this
        };

        preview.Closed += (_, _) =>
        {
            CloseOnDeactivate = oldCloseOnDeactivate;
            Activate();
            FocusInput();
        };

        preview.Show();
    }

    private void MessageImage_MouseLeftButtonDown(object sender, WpfInput.MouseButtonEventArgs e)
    {
        if (sender is not Image image || image.Source is not BitmapSource source)
            return;

        ShowImagePreview(source);
        e.Handled = true;
    }

    private void ScreenshotPreview_MouseLeftButtonDown(object sender, WpfInput.MouseButtonEventArgs e)
    {
        if (ScreenshotPreview.Source is not BitmapSource source)
            return;

        ShowImagePreview(source);
        e.Handled = true;
    }

    private void ScrollHistoryToBottom()
    {
        Dispatcher.BeginInvoke(() => HistoryScrollViewer.ScrollToEnd());
    }

    private void Window_PreviewKeyDown(object sender, WpfInput.KeyEventArgs e)
    {
        if (e.Key == WpfInput.Key.Escape)
        {
            CloseRequested?.Invoke();
            e.Handled = true;
            return;
        }

        if (e.SystemKey == WpfInput.Key.S)
        {
            ToggleScreenshot();
            e.Handled = true;
        }
    }

    private void InputBox_PreviewKeyDown(object sender, WpfInput.KeyEventArgs e)
    {
        if (e.Key != WpfInput.Key.Enter) return;

        if (WpfInput.Keyboard.Modifiers.HasFlag(WpfInput.ModifierKeys.Shift))
        {
            var caret = InputBox.CaretIndex;
            InputBox.Text = InputBox.Text.Insert(caret, Environment.NewLine);
            InputBox.CaretIndex = caret + Environment.NewLine.Length;
            e.Handled = true;
            return;
        }

        Send();
        e.Handled = true;
    }

    private void SendScreenshotCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdatePreviewLayout();
        FocusInput();
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        if (CloseOnDeactivate && IsVisible)
            CloseRequested?.Invoke();
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_isClosingForReal) return;

        e.Cancel = true;
        CloseRequested?.Invoke();
    }
}
