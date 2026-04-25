using System.Text;

namespace VYRA.Core.History;

public static class HistoryFileName
{
    private const int MaxSlugLength = 48;

    public static string CreateImageFileName(DateTime timestamp, string? windowTitle)
    {
        var slug = Slugify(windowTitle);
        return string.IsNullOrWhiteSpace(slug)
            ? $"{timestamp:dd-HHmmss}.jpg"
            : $"{timestamp:dd-HHmmss}-{slug}.jpg";
    }

    public static string Slugify(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var builder = new StringBuilder(text.Length);
        var previousWasDash = false;

        foreach (var raw in text.Trim().ToLowerInvariant())
        {
            if (IsAsciiLetterOrDigit(raw))
            {
                builder.Append(raw);
                previousWasDash = false;
                continue;
            }

            if (char.IsWhiteSpace(raw) || raw == '-' || raw == '_' || raw == '.')
            {
                if (builder.Length == 0 || previousWasDash)
                    continue;

                builder.Append('-');
                previousWasDash = true;
            }
        }

        var slug = builder.ToString().Trim('-');
        if (slug.Length <= MaxSlugLength)
            return slug;

        return slug[..MaxSlugLength].Trim('-');
    }

    private static bool IsAsciiLetterOrDigit(char value) =>
        value is >= 'a' and <= 'z' or >= '0' and <= '9';
}
