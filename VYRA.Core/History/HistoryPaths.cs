using System.Globalization;

namespace VYRA.Core.History;

public sealed class HistoryPaths
{
    public HistoryPaths(string? rootPath = null)
    {
        RootPath = string.IsNullOrWhiteSpace(rootPath)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VYRA", "history")
            : rootPath;
    }

    public string RootPath { get; }

    public string GetMonthDirectory(DateTime timestamp) =>
        Path.Combine(RootPath, timestamp.ToString("yyyy-MM", CultureInfo.InvariantCulture));

    public string GetDayFilePath(DateTime timestamp)
    {
        var dayName = timestamp.ToString("dddd", CultureInfo.InvariantCulture).ToLowerInvariant();
        return Path.Combine(GetMonthDirectory(timestamp), $"{timestamp:dd}-{dayName}.md");
    }
}
