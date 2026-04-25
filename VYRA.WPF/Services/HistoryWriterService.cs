using System.Threading.Channels;
using System.Windows.Media.Imaging;
using VYRA.Core.History;

namespace VYRA.WPF.Services;

public sealed class HistoryWriterService : IDisposable, IAsyncDisposable
{
    private readonly HistoryService _history = new();
    private readonly Channel<PendingHistoryMessage> _queue;
    private readonly Task _worker;
    private bool _isDisposed;

    public HistoryWriterService()
    {
        _queue = Channel.CreateUnbounded<PendingHistoryMessage>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });

        _worker = Task.Run(ProcessQueueAsync);
    }

    public void EnqueueUserMessage(string? text, BitmapSource? image, string? sourceName)
    {
        if (_isDisposed)
            return;

        var safeImage = FreezeForBackgroundThread(image);

        var message = new PendingHistoryMessage(
            Role: "USER",
            Text: text,
            Image: safeImage,
            SourceName: sourceName,
            Timestamp: DateTime.Now);

        if (!_queue.Writer.TryWrite(message))
            ErrorHandler.Report(new InvalidOperationException("History queue is closed"), "History enqueue failed");
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _queue.Writer.TryComplete();

        try
        {
            await _worker.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ErrorHandler.Report(ex, "History writer shutdown failed");
        }
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private async Task ProcessQueueAsync()
    {
        await foreach (var message in _queue.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            try
            {
                var imageJpg = message.Image == null
                    ? null
                    : ScreenshotService.ToJpgBytes(message.Image);

                await _history.SaveMessageAsync(
                        role: message.Role,
                        text: message.Text,
                        imageJpg: imageJpg,
                        windowTitle: message.SourceName,
                        timestamp: message.Timestamp)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ErrorHandler.Report(ex, "History write failed");
            }
        }
    }

    private static BitmapSource? FreezeForBackgroundThread(BitmapSource? image)
    {
        if (image == null)
            return null;

        if (image.IsFrozen)
            return image;

        var clone = image.Clone();

        if (clone.CanFreeze)
            clone.Freeze();

        return clone;
    }

    private sealed record PendingHistoryMessage(
        string Role,
        string? Text,
        BitmapSource? Image,
        string? SourceName,
        DateTime Timestamp);
}
