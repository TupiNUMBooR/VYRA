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

            TryCreateOrUpdateJunction(appDataLinkPath, appDataRoot, errors);
            TryCreateOrUpdateJunction(exeLinkPath, exeDirectory, errors);
        }
        catch (Exception ex)
        {
            errors.Add(ex);
        }

        return errors;
    }

    private static void TryCreateOrUpdateJunction(string linkPath, string targetPath, List<Exception> errors)
    {
        try
        {
            targetPath = NormalizePath(targetPath);

            if (Directory.Exists(linkPath) || File.Exists(linkPath))
            {
                if (IsCorrectLink(linkPath, targetPath))
                    return;

                if (!TryDeleteExistingLink(linkPath, errors))
                    return;
            }

            CreateJunction(linkPath, targetPath, errors);
        }
        catch (Exception ex)
        {
            errors.Add(ex);
        }
    }

    private static bool IsCorrectLink(string linkPath, string expectedTargetPath)
    {
        try
        {
            var directoryInfo = new DirectoryInfo(linkPath);

            if (!directoryInfo.Exists)
                return false;

            if ((directoryInfo.Attributes & FileAttributes.ReparsePoint) == 0)
                return false;

            var targetInfo = directoryInfo.ResolveLinkTarget(true);

            if (targetInfo == null)
                return false;

            return PathsEqual(targetInfo.FullName, expectedTargetPath);
        }
        catch
        {
            return false;
        }
    }

    private static bool TryDeleteExistingLink(string linkPath, List<Exception> errors)
    {
        try
        {
            if (Directory.Exists(linkPath))
            {
                var directoryInfo = new DirectoryInfo(linkPath);

                if ((directoryInfo.Attributes & FileAttributes.ReparsePoint) == 0)
                {
                    errors.Add(new IOException(
                        $"Path exists and is not a junction/link, refusing to delete: '{linkPath}'."));

                    return false;
                }

                Directory.Delete(linkPath);
                return true;
            }

            if (File.Exists(linkPath))
            {
                var fileInfo = new FileInfo(linkPath);

                if ((fileInfo.Attributes & FileAttributes.ReparsePoint) == 0)
                {
                    errors.Add(new IOException(
                        $"Path exists and is not a link, refusing to delete: '{linkPath}'."));

                    return false;
                }

                File.Delete(linkPath);
                return true;
            }

            return true;
        }
        catch (Exception ex)
        {
            errors.Add(ex);
            return false;
        }
    }

    private static void CreateJunction(string linkPath, string targetPath, List<Exception> errors)
    {
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
            try
            {
                process.Kill(true);
            }
            catch
            {
            }

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

    private static bool PathsEqual(string left, string right)
    {
        return string.Equals(
            NormalizePath(left),
            NormalizePath(right),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string path)
    {
        return Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
