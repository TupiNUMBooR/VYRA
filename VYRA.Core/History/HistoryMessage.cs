namespace VYRA.Core.History;

public sealed class HistoryMessage
{
    public HistoryMessage(DateTime timestamp, string role, string? text, string? imageFileName)
    {
        Timestamp = timestamp;
        Role = string.IsNullOrWhiteSpace(role) ? "UNKNOWN" : role.Trim().ToUpperInvariant();
        Text = text;
        ImageFileName = imageFileName;
    }

    public DateTime Timestamp { get; }
    public string Role { get; }
    public string? Text { get; }
    public string? ImageFileName { get; }
}
