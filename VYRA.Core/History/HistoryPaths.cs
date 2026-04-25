namespace VYRA.Core.History;

public sealed class HistoryPaths
{
    public HistoryPaths(string? appDataRootPath = null)
    {
        AppDataRootPath = appDataRootPath
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "VYRA");

        RootPath = Path.Combine(AppDataRootPath, "history");
    }

    public string AppDataRootPath { get; }

    public string RootPath { get; }

    public string GetMonthDirectory(DateTime timestamp)
    {
        return Path.Combine(RootPath, timestamp.ToString("yyyy-MM"));
    }

    public string GetDayFilePath(DateTime timestamp)
    {
        var fileName = $"{timestamp:dd}-{timestamp.DayOfWeek.ToString().ToLowerInvariant()}.md";
        return Path.Combine(GetMonthDirectory(timestamp), fileName);
    }
}
