using System.Text;
using System.Text.RegularExpressions;

namespace VYRA.Core.History;

public sealed class HistoryService
{
    private const int DefaultLookbackDays = 90;
    private static readonly SemaphoreSlim WriteLock = new(1, 1);
    private static readonly Regex HeaderRegex = new(
        @"^###\s+(?<time>\d{1,2}:\d{2}:\d{2})\s+(?<role>\S+)\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ImageRegex = new(
        @"^!\[\]\((?<file>[^)]+)\)\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly HistoryPaths _paths;

    public HistoryService(HistoryPaths? paths = null)
    {
        _paths = paths ?? new HistoryPaths();
    }

    public string RootPath => _paths.RootPath;

    public async Task<HistoryLoadResult> LoadStartupHistoryAsync(
        int contextMessageLimit,
        int maxLookbackDays = DefaultLookbackDays,
        DateTime? today = null,
        CancellationToken cancellationToken = default)
    {
        var todayDate = (today ?? DateTime.Now).Date;
        var todayHistory = await ReadDayAsync(todayDate, cancellationToken).ConfigureAwait(false)
            ?? new HistoryDay(todayDate, Array.Empty<HistoryMessage>());

        var loadedDays = new List<HistoryDay> { todayHistory };
        var context = new List<HistoryMessage>();
        AddContextMessages(context, todayHistory.Messages, contextMessageLimit);

        var cursor = todayDate;
        while (context.Count < contextMessageLimit)
        {
            var previous = await ReadPreviousExistingDayAsync(cursor, maxLookbackDays, cancellationToken)
                .ConfigureAwait(false);

            if (previous == null)
                break;

            loadedDays.Add(previous);
            AddContextMessages(context, previous.Messages, contextMessageLimit);
            cursor = previous.Date;
        }

        context.Reverse();
        loadedDays.Reverse();

        return new HistoryLoadResult(todayHistory, loadedDays, context);
    }

    public async Task<HistoryDay?> ReadDayAsync(
        DateTime day,
        CancellationToken cancellationToken = default)
    {
        var dayDate = day.Date;
        var path = _paths.GetDayFilePath(dayDate);

        if (!File.Exists(path))
            return null;

        string markdown;
        try
        {
            markdown = await File.ReadAllTextAsync(path, Encoding.UTF8, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }

        var monthDirectory = _paths.GetMonthDirectory(dayDate);
        var messages = ParseDayMarkdown(dayDate, monthDirectory, markdown);

        return new HistoryDay(dayDate, messages);
    }

    public async Task<HistoryDay?> ReadPreviousExistingDayAsync(
        DateTime beforeDate,
        int maxLookbackDays = DefaultLookbackDays,
        CancellationToken cancellationToken = default)
    {
        var cursor = beforeDate.Date.AddDays(-1);
        var limit = Math.Max(1, maxLookbackDays);

        for (var offset = 0; offset < limit; offset++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var day = cursor.AddDays(-offset);
            var result = await ReadDayAsync(day, cancellationToken).ConfigureAwait(false);

            if (result != null)
                return result;
        }

        return null;
    }

    public async Task<HistoryMessage> SaveMessageAsync(
        string role,
        string? text,
        byte[]? imageJpg,
        string? windowTitle,
        DateTime? timestamp = null,
        CancellationToken cancellationToken = default)
    {
        var now = timestamp ?? DateTime.Now;
        var monthDirectory = _paths.GetMonthDirectory(now);
        var dayFilePath = _paths.GetDayFilePath(now);
        string? imageFileName = null;

        Directory.CreateDirectory(monthDirectory);

        await WriteLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (imageJpg is { Length: > 0 })
            {
                imageFileName = HistoryFileName.CreateImageFileName(now, windowTitle);
                imageFileName = GetUniqueFileName(monthDirectory, imageFileName);

                var imagePath = Path.Combine(monthDirectory, imageFileName);

                await File.WriteAllBytesAsync(imagePath, imageJpg, cancellationToken)
                    .ConfigureAwait(false);
            }

            var message = new HistoryMessage(now, role, text, imageFileName, monthDirectory);
            var markdown = RenderMarkdown(message);

            await File.AppendAllTextAsync(dayFilePath, markdown, Encoding.UTF8, cancellationToken)
                .ConfigureAwait(false);

            return message;
        }
        finally
        {
            WriteLock.Release();
        }
    }

    private static void AddContextMessages(
        List<HistoryMessage> context,
        IReadOnlyList<HistoryMessage> messages,
        int contextMessageLimit)
    {
        foreach (var message in messages.Reverse())
        {
            if (context.Count >= contextMessageLimit)
                return;

            if (!message.HasText || (!message.IsUser && !message.IsAssistant))
                continue;

            context.Add(message);
        }
    }

    private static IReadOnlyList<HistoryMessage> ParseDayMarkdown(
        DateTime day,
        string monthDirectory,
        string markdown)
    {
        var messages = new List<HistoryMessage>();
        var blocks = Regex.Split(markdown, @"(?m)^---\s*$");

        foreach (var block in blocks)
        {
            var message = TryParseBlock(day, monthDirectory, block);
            if (message != null)
                messages.Add(message);
        }

        return messages;
    }

    private static HistoryMessage? TryParseBlock(DateTime day, string monthDirectory, string block)
    {
        if (string.IsNullOrWhiteSpace(block))
            return null;

        var normalized = block.Replace("\r\n", "\n").Replace('\r', '\n');
        var lines = normalized.Split('\n');

        var headerIndex = Array.FindIndex(lines, line => HeaderRegex.IsMatch(line));
        if (headerIndex < 0)
            return null;

        var headerMatch = HeaderRegex.Match(lines[headerIndex]);
        if (!TimeSpan.TryParse(headerMatch.Groups["time"].Value, out var time))
            return null;

        var role = headerMatch.Groups["role"].Value;
        var timestamp = day.Date.Add(time);
        string? imageFileName = null;
        var textLines = new List<string>();

        for (var index = headerIndex + 1; index < lines.Length; index++)
        {
            var line = lines[index];

            if (string.IsNullOrWhiteSpace(line) && textLines.Count == 0)
                continue;

            if (imageFileName == null)
            {
                var imageMatch = ImageRegex.Match(line.Trim());
                if (imageMatch.Success)
                {
                    imageFileName = imageMatch.Groups["file"].Value.Trim();
                    continue;
                }
            }

            textLines.Add(line);
        }

        var text = string.Join(Environment.NewLine, textLines).Trim();

        return new HistoryMessage(
            timestamp,
            role,
            string.IsNullOrWhiteSpace(text) ? null : text,
            imageFileName,
            monthDirectory);
    }

    private static string GetUniqueFileName(string directory, string fileName)
    {
        if (!File.Exists(Path.Combine(directory, fileName)))
            return fileName;

        var name = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);

        for (var index = 2; index < 1000; index++)
        {
            var candidate = $"{name}-{index}{extension}";
            if (!File.Exists(Path.Combine(directory, candidate)))
                return candidate;
        }

        return $"{name}-{Guid.NewGuid():N}{extension}";
    }

    private static string RenderMarkdown(HistoryMessage message)
    {
        var builder = new StringBuilder();

        builder.Append("### ")
            .Append(message.Timestamp.ToString("HH:mm:ss"))
            .Append(' ')
            .AppendLine(message.Role);

        builder.AppendLine();

        if (!string.IsNullOrWhiteSpace(message.ImageFileName))
        {
            builder.Append("![](")
                .Append(message.ImageFileName)
                .AppendLine(")");

            builder.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(message.Text))
        {
            builder.AppendLine(message.Text.TrimEnd());
            builder.AppendLine();
        }

        builder.AppendLine("---");
        builder.AppendLine();

        return builder.ToString();
    }
}
