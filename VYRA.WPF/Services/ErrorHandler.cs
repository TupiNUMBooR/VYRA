using System.IO;
using System.Text;

namespace VYRA.WPF.Services;

public static class ErrorHandler
{
    private static readonly object Lock = new();

    public static void Install()
    {
        System.Windows.Application.Current.DispatcherUnhandledException += (_, e) =>
        {
            Report(e.Exception, "UI thread exception");
            e.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                Report(ex, "Unhandled exception");
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Report(e.Exception, "Unobserved task exception");
            e.SetObserved();
        };
    }

    public static void Report(Exception ex, string context)
    {
        try
        {
            var text = BuildText(ex, context);

            Console.Error.WriteLine(text);
            WriteLog(text);
            NotificationService.ShowError("VYRA error", $"{context}\n{ex.Message}");
        }
        catch
        {
            // Error reporting must never crash the app.
        }
    }

    private static string BuildText(Exception ex, string context)
    {
        var builder = new StringBuilder();

        builder.Append('[')
            .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
            .AppendLine("]");

        builder.AppendLine(context);
        builder.AppendLine();
        builder.AppendLine(ex.ToString());
        builder.AppendLine("---");
        builder.AppendLine();

        return builder.ToString();
    }

    private static void WriteLog(string text)
    {
        var appDataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VYRA");

        Directory.CreateDirectory(appDataRoot);

        var path = Path.Combine(appDataRoot, "errors.log");

        lock (Lock)
            File.AppendAllText(path, text, Encoding.UTF8);
    }
}
