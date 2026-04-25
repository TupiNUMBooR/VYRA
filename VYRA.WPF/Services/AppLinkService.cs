using System.Diagnostics;
using System.IO;
using VYRA.Core.History;

namespace VYRA.WPF.Services;

public static class AppLinkService
{
    public static void EnsureConvenienceLinks()
    {
        try
        {
            var appDataRoot = new HistoryPaths().AppDataRootPath;
            var exeDirectory = AppContext.BaseDirectory.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);

            Directory.CreateDirectory(appDataRoot);

            var username = Environment.UserName;
            var appDataLinkPath = Path.Combine(exeDirectory, $"VYRA-appdata-{username}");
            var exeLinkPath = Path.Combine(appDataRoot, "exe-dir");

            CreateJunctionIfMissing(appDataLinkPath, appDataRoot);
            CreateJunctionIfMissing(exeLinkPath, exeDirectory);
        }
        catch (Exception ex)
        {
            ErrorHandler.Report(ex, "Create convenience links failed");
        }
    }

    private static void CreateJunctionIfMissing(string linkPath, string targetPath)
    {
        if (Directory.Exists(linkPath) || File.Exists(linkPath))
            return;

        var command = $"/c mklink /J \"{linkPath}\" \"{targetPath}\"";

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = command,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });

        process?.WaitForExit(3000);
    }
}
