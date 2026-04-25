using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using Wpf = System.Windows;
using Media = System.Windows.Media;

namespace VYRA.WPF.ViewModels;

public sealed class ChatMessageViewModel : INotifyPropertyChanged
{
    private ChatMessageStatus _status;

    private ChatMessageViewModel(string? text, BitmapSource? image, bool isUser, ChatMessageStatus status)
    {
        Text = text;
        Image = image;
        IsUser = isUser;
        _status = status;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string? Text { get; }
    public BitmapSource? Image { get; }
    public bool IsUser { get; }
    public bool HasText => !string.IsNullOrWhiteSpace(Text);
    public bool HasImage => Image != null;
    public bool HasStatus => IsUser && Status != ChatMessageStatus.None;

    public ChatMessageStatus Status
    {
        get => _status;
        private set
        {
            if (_status == value)
                return;

            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(HasStatus));
        }
    }

    public string StatusText => Status switch
    {
        ChatMessageStatus.Sending => "…",
        ChatMessageStatus.Delivered => "✓",
        ChatMessageStatus.Failed => "!",
        _ => string.Empty
    };

    public Wpf.HorizontalAlignment BubbleAlignment => IsUser
        ? Wpf.HorizontalAlignment.Right
        : Wpf.HorizontalAlignment.Left;

    public Media.Brush BubbleBrush => IsUser
        ? new Media.SolidColorBrush(Media.Color.FromRgb(30, 60, 30))
        : new Media.SolidColorBrush(Media.Color.FromRgb(20, 30, 20));

    public void MarkDelivered() => Status = ChatMessageStatus.Delivered;

    public void MarkFailed() => Status = ChatMessageStatus.Failed;

    public static ChatMessageViewModel TextMessage(string text, bool isUser) => new(text, null, isUser, ChatMessageStatus.None);

    public static ChatMessageViewModel ImageMessage(BitmapSource image, bool isUser) => new(null, image, isUser, ChatMessageStatus.None);

    public static ChatMessageViewModel ComboMessage(BitmapSource? image, string? text, bool isUser) => new(text, image, isUser, ChatMessageStatus.None);

    public static ChatMessageViewModel PendingUserMessage(BitmapSource? image, string? text) => new(text, image, true, ChatMessageStatus.Sending);

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
