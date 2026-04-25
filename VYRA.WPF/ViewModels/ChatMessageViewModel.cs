using System.Windows.Media.Imaging;
using Wpf = System.Windows;
using Media = System.Windows.Media;

namespace VYRA.WPF.ViewModels;

public sealed class ChatMessageViewModel
{
    private ChatMessageViewModel(string? text, BitmapSource? image, bool isUser)
    {
        Text = text;
        Image = image;
        IsUser = isUser;
    }

    public string? Text { get; }
    public BitmapSource? Image { get; }
    public bool IsUser { get; }
    public bool HasText => !string.IsNullOrWhiteSpace(Text);
    public bool HasImage => Image != null;

    public Wpf.HorizontalAlignment BubbleAlignment => IsUser
        ? Wpf.HorizontalAlignment.Right
        : Wpf.HorizontalAlignment.Left;

    public Media.Brush BubbleBrush => IsUser
        ? new Media.SolidColorBrush(Media.Color.FromRgb(30, 60, 30))
        : new Media.SolidColorBrush(Media.Color.FromRgb(20, 30, 20));

    public static ChatMessageViewModel TextMessage(string text, bool isUser) => new(text, null, isUser);

    public static ChatMessageViewModel ImageMessage(BitmapSource image, bool isUser) => new(null, image, isUser);

    public static ChatMessageViewModel ComboMessage(BitmapSource? image, string? text, bool isUser) => new(text, image, isUser);
}
