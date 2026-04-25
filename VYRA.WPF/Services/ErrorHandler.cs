using System.IO;
using System.Windows;

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
    }

    public static void Report(Exception ex, string context)
    {
        try
        {
            var text = $"""
            [{DateTime.Now:yyyy-MM-dd HH:mm:ss}]
            {context}

            {ex}

            """;

            Console.Error.WriteLine(text);

            var path = Path.Combine(AppContext.BaseDirectory, "vyra-errors.log");

            lock (Lock)
                File.AppendAllText(path, text);

            System.Windows.MessageBox.Show(text, "VYRA error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        catch
        {
        }
    }
}
