namespace VYRA.Core.Models;

public sealed class ChatMessage
{
    public ChatMessage(ChatMessageKind kind, string? text, byte[]? imagePng, bool isUser)
    {
        Kind = kind;
        Text = text;
        ImagePng = imagePng;
        IsUser = isUser;
    }

    public ChatMessageKind Kind { get; }
    public string? Text { get; }
    public byte[]? ImagePng { get; }
    public bool IsUser { get; }
}
