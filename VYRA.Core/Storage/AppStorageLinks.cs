using System.Diagnostics;
using VYRA.Core.History;

namespace VYRA.Core.Storage;

public static class AppStorageLinks
{
    public static IReadOnlyList<Exception> EnsureConvenienceLinks()
    {
        var errors = new List<Exception>();

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

            TryCreateJunction(appDataLinkPath, appDataRoot, errors);
            TryCreateJunction(exeLinkPath, exeDirectory, errors);
        }
        catch (Exception ex)
        {
            errors.Add(ex);
        }

        return errors;
    }

    private static void TryCreateJunction(string linkPath, string targetPath, List<Exception> errors)
    {
        try
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

            if (process == null)
            {
                errors.Add(new InvalidOperationException($"Failed to start mklink for '{linkPath}'."));
                return;
            }

            if (!process.WaitForExit(3000))
            {
                errors.Add(new TimeoutException($"Timed out creating junction '{linkPath}'."));
                return;
            }

            if (process.ExitCode != 0)
            {
                var stderr = process.StandardError.ReadToEnd().Trim();
                var stdout = process.StandardOutput.ReadToEnd().Trim();
                var details = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;

                errors.Add(new InvalidOperationException(
                    $"Failed to create junction '{linkPath}' -> '{targetPath}'. {details}"));
            }
        }
        catch (Exception ex)
        {
            errors.Add(ex);
        }
    }
}
