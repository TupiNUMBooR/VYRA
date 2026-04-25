namespace VYRA.Services;

public static class ErrorHandler
{
    private static readonly object Lock = new();

    public static void Install()
    {
        Application.ThreadException += (_, e) =>
            Report(e.Exception, "UI thread exception");

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
            {
                File.AppendAllText(path, text);
            }

            MessageBox.Show(
                text,
                "VYRA error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch
        {
            // если даже логгер сломался — молчим, не убиваем приложение
        }
    }
}
