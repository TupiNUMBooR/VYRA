using System.Globalization;
using System.Text;

namespace VYRA.Core.History;

public static class HistoryFileName
{
    private const int MaxSourceSlugLength = 80;

    public static string CreateImageFileName(DateTime timestamp, string? sourceName)
    {
        var slug = CreateSlug(sourceName, MaxSourceSlugLength);

        return string.IsNullOrWhiteSpace(slug)
            ? $"{timestamp:dd-HHmmss}.jpg"
            : $"{timestamp:dd-HHmmss}-{slug}.jpg";
    }

    public static string CreateSourceName(string? processName, string? windowTitle)
    {
        var processSlug = CreateSlug(processName, 32);
        var titleSlug = CreateSlug(windowTitle, MaxSourceSlugLength);

        if (string.IsNullOrWhiteSpace(processSlug))
            return titleSlug;

        if (string.IsNullOrWhiteSpace(titleSlug))
            return processSlug;

        return titleSlug.StartsWith(processSlug + "-", StringComparison.OrdinalIgnoreCase)
            ? titleSlug
            : $"{processSlug}-{titleSlug}";
    }

    private static string CreateSlug(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        var previousWasDash = false;

        foreach (var symbol in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(symbol);
            if (category == UnicodeCategory.NonSpacingMark)
                continue;

            var lower = char.ToLowerInvariant(symbol);

            if (IsAllowed(lower))
            {
                builder.Append(lower);
                previousWasDash = false;
                continue;
            }

            if (builder.Length == 0 || previousWasDash)
                continue;

            builder.Append('-');
            previousWasDash = true;
        }

        var result = builder.ToString().Trim('-');

        if (result.Length > maxLength)
            result = result[..maxLength].Trim('-');

        return result;
    }

    private static bool IsAllowed(char symbol)
    {
        return symbol is >= 'a' and <= 'z'
            || symbol is >= '0' and <= '9'
            || symbol == '-';
    }
}
