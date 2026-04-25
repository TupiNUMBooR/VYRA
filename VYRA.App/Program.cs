using VYRA.Services;

namespace VYRA;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        ErrorHandler.Install();

        using var controller = new AppController();
        controller.Start();

        Application.Run();
    }
}
