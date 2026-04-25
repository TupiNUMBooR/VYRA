using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace VYRA.WPF.Services;

public sealed class ScreenshotService
{
    public BitmapSource CaptureVirtualScreen()
    {
        var bounds = NativeMethods.GetVirtualScreenBounds();
        using var bitmap = new Bitmap(bounds.Width, bounds.Height);
        using var graphics = Graphics.FromImage(bitmap);

        graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);

        return ToBitmapSource(bitmap);
    }

    public static byte[] ToPngBytes(BitmapSource source)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    public static byte[] ToJpgBytes(BitmapSource source, int qualityLevel = 80)
    {
        var encoder = new JpegBitmapEncoder
        {
            QualityLevel = Math.Clamp(qualityLevel, 1, 100)
        };
        encoder.Frames.Add(BitmapFrame.Create(source));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private static BitmapSource ToBitmapSource(Bitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        stream.Position = 0;

        var source = new BitmapImage();
        source.BeginInit();
        source.CacheOption = BitmapCacheOption.OnLoad;
        source.StreamSource = stream;
        source.EndInit();
        source.Freeze();
        return source;
    }
}
