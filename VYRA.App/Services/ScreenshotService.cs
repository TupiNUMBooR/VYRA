namespace VYRA.Services;

public sealed class ScreenshotService
{
    public Bitmap CaptureVirtualScreen()
    {
        var bounds = SystemInformation.VirtualScreen;
        var bitmap = new Bitmap(bounds.Width, bounds.Height);

        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);

        return bitmap;
    }
}
