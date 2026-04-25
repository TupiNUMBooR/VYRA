using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace VYRA.OpenAI;

public sealed class OpenAiChatClient : IDisposable
{
    private const string ResponsesEndpoint = "https://api.openai.com/v1/responses";
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    public OpenAiChatClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _ownsHttpClient = httpClient == null;
    }

    public async Task<OpenAiChatResponse> SendAsync(
        OpenAiOptions options,
        OpenAiChatRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Messages.Count == 0 && request.CurrentScreenshotJpg is not { Length: > 0 })
            throw new OpenAiException("OpenAI request is empty.");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ResponsesEndpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        httpRequest.Content = new StringContent(
            BuildRequestJson(options, request),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            throw new OpenAiException($"OpenAI request failed: {(int)response.StatusCode} {response.ReasonPhrase}\n{body}");

        var text = ExtractOutputText(body);
        if (string.IsNullOrWhiteSpace(text))
            throw new OpenAiException($"OpenAI response did not contain text.\n{body}");

        return new OpenAiChatResponse(text.Trim());
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
            _httpClient.Dispose();
    }

    private static string BuildRequestJson(OpenAiOptions options, OpenAiChatRequest request)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            writer.WriteString("model", options.Model);
            writer.WriteString("instructions", VyraSystemPrompt.Text);

            writer.WritePropertyName("input");
            writer.WriteStartArray();

            for (var index = 0; index < request.Messages.Count; index++)
            {
                var message = request.Messages[index];
                var isLastUserMessage = index == request.Messages.Count - 1
                    && string.Equals(message.Role, "user", StringComparison.OrdinalIgnoreCase);

                WriteMessage(writer, message, isLastUserMessage ? request.CurrentScreenshotJpg : null);
            }

            if (request.Messages.Count == 0 && request.CurrentScreenshotJpg is { Length: > 0 })
                WriteMessage(writer, new OpenAiChatMessage("user", "Look at the current screenshot."), request.CurrentScreenshotJpg);

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteMessage(Utf8JsonWriter writer, OpenAiChatMessage message, byte[]? screenshotJpg)
    {
        var role = string.Equals(message.Role, "assistant", StringComparison.OrdinalIgnoreCase)
            ? "assistant"
            : "user";

        writer.WriteStartObject();
        writer.WriteString("role", role);
        writer.WritePropertyName("content");
        writer.WriteStartArray();

        if (!string.IsNullOrWhiteSpace(message.Text))
        {
            writer.WriteStartObject();
            writer.WriteString("type", role == "assistant" ? "output_text" : "input_text");
            writer.WriteString("text", message.Text.Trim());
            writer.WriteEndObject();
        }

        if (screenshotJpg is { Length: > 0 })
        {
            writer.WriteStartObject();
            writer.WriteString("type", "input_image");
            writer.WriteString("image_url", $"data:image/jpeg;base64,{Convert.ToBase64String(screenshotJpg)}");
            writer.WriteString("detail", "low");
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static string ExtractOutputText(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.TryGetProperty("output_text", out var outputText)
                && outputText.ValueKind == JsonValueKind.String)
                return outputText.GetString() ?? string.Empty;

            if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
                return string.Empty;

            var builder = new StringBuilder();

            foreach (var item in output.EnumerateArray())
            {
                if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var part in content.EnumerateArray())
                {
                    if (!part.TryGetProperty("type", out var type)
                        || !string.Equals(type.GetString(), "output_text", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (part.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                        builder.AppendLine(text.GetString());
                }
            }

            return builder.ToString();
        }
        catch (JsonException ex)
        {
            throw new OpenAiException("OpenAI response JSON is invalid.", ex);
        }
    }
}
