namespace VYRA.Core.History;

public sealed class HistoryMessage
{
    public HistoryMessage(
        DateTime timestamp,
        string role,
        string? text,
        string? imageFileName,
        string? monthDirectory = null)
    {
        Timestamp = timestamp;
        Role = string.IsNullOrWhiteSpace(role) ? "UNKNOWN" : role.Trim().ToUpperInvariant();
        Text = string.IsNullOrWhiteSpace(text) ? null : text;
        ImageFileName = string.IsNullOrWhiteSpace(imageFileName) ? null : imageFileName.Trim();
        MonthDirectory = monthDirectory;
    }

    public DateTime Timestamp { get; }
    public string Role { get; }
    public string? Text { get; }
    public string? ImageFileName { get; }
    public string? MonthDirectory { get; }

    public bool IsUser => string.Equals(Role, "USER", StringComparison.OrdinalIgnoreCase);
    public bool IsAssistant => string.Equals(Role, "ASSISTANT", StringComparison.OrdinalIgnoreCase);
    public bool HasText => !string.IsNullOrWhiteSpace(Text);
}
