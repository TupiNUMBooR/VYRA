using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using Wpf = System.Windows;
using Media = System.Windows.Media;

namespace VYRA.WPF.ViewModels;

public sealed class ChatMessageViewModel : INotifyPropertyChanged
{
    private ChatMessageStatus _status;

    private ChatMessageViewModel(
        string? text,
        BitmapSource? image,
        bool isUser,
        ChatMessageStatus status,
        DateTime timestamp,
        bool isDateSeparator,
        string? dateText)
    {
        Text = text;
        Image = image;
        IsUser = isUser;
        _status = status;
        Timestamp = timestamp;
        IsDateSeparator = isDateSeparator;
        DateText = dateText;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string? Text { get; }
    public BitmapSource? Image { get; }
    public bool IsUser { get; }
    public DateTime Timestamp { get; }
    public bool IsDateSeparator { get; }
    public bool IsMessage => !IsDateSeparator;
    public string? DateText { get; }
    public string TimeText => Timestamp.ToString("HH:mm");
    public bool HasText => !string.IsNullOrWhiteSpace(Text);
    public bool HasImage => Image != null;
    public bool HasStatus => IsUser && Status != ChatMessageStatus.None;
    public bool HasMeta => IsMessage;

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

    public static ChatMessageViewModel DateSeparator(DateTime date) => new(
        text: null,
        image: null,
        isUser: false,
        status: ChatMessageStatus.None,
        timestamp: date.Date,
        isDateSeparator: true,
        dateText: $"──── {date:yyyy.MM.dd} ────");

    public static ChatMessageViewModel TextMessage(string text, bool isUser, DateTime? timestamp = null) => new(
        text,
        null,
        isUser,
        ChatMessageStatus.None,
        timestamp ?? DateTime.Now,
        isDateSeparator: false,
        dateText: null);

    public static ChatMessageViewModel ImageMessage(BitmapSource image, bool isUser, DateTime? timestamp = null) => new(
        null,
        image,
        isUser,
        ChatMessageStatus.None,
        timestamp ?? DateTime.Now,
        isDateSeparator: false,
        dateText: null);

    public static ChatMessageViewModel ComboMessage(BitmapSource? image, string? text, bool isUser, DateTime? timestamp = null) => new(
        text,
        image,
        isUser,
        ChatMessageStatus.None,
        timestamp ?? DateTime.Now,
        isDateSeparator: false,
        dateText: null);

    public static ChatMessageViewModel PendingUserMessage(BitmapSource? image, string? text, DateTime? timestamp = null) => new(
        text,
        image,
        true,
        ChatMessageStatus.Sending,
        timestamp ?? DateTime.Now,
        isDateSeparator: false,
        dateText: null);

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
