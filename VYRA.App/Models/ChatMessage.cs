namespace VYRA.Models;

public sealed class ChatMessage : IDisposable
{
    public ChatMessage(string text, Image? image)
    {
        Text = text;
        Image = image;
    }

    public string Text { get; }
    public Image? Image { get; }

    public void Dispose()
    {
        Image?.Dispose();
    }
}
