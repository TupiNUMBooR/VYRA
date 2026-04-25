using System.IO;
using System.Security.Cryptography;
using System.Text;
using VYRA.Core.History;

namespace VYRA.WPF.Services;

public sealed class OpenAiTokenStore
{
    private readonly string _path;

    public OpenAiTokenStore()
    {
        var paths = new HistoryPaths();
        Directory.CreateDirectory(paths.AppDataRootPath);
        _path = Path.Combine(paths.AppDataRootPath, "openai-token.bin");
    }

    public bool HasToken => !string.IsNullOrWhiteSpace(TryLoadToken());

    public string? TryLoadToken()
    {
        try
        {
            if (!File.Exists(_path))
                return null;

            var encrypted = File.ReadAllBytes(_path);
            var bytes = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            var token = Encoding.UTF8.GetString(bytes).Trim();

            return string.IsNullOrWhiteSpace(token) ? null : token;
        }
        catch (Exception ex)
        {
            ErrorHandler.Report(ex, "Load OpenAI token failed");
            return null;
        }
    }

    public void SaveToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token is empty.", nameof(token));

        var bytes = Encoding.UTF8.GetBytes(token.Trim());
        var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);

        File.WriteAllBytes(_path, encrypted);
    }

    public void ClearToken()
    {
        if (File.Exists(_path))
            File.Delete(_path);
    }
}
