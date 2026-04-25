using System.Text;

namespace VYRA.Core.History;

public sealed class HistoryService
{
    private static readonly SemaphoreSlim WriteLock = new(1, 1);
    private readonly HistoryPaths _paths;

    public HistoryService(HistoryPaths? paths = null)
    {
        _paths = paths ?? new HistoryPaths();
    }

    public string RootPath => _paths.RootPath;

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

            var message = new HistoryMessage(now, role, text, imageFileName);
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
