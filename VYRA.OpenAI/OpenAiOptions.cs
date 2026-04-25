namespace VYRA.OpenAI;

public sealed class OpenAiOptions
{
    public const string DefaultModel = "gpt-4.1-mini";

    public OpenAiOptions(string apiKey, string model = DefaultModel)
    {
        ApiKey = string.IsNullOrWhiteSpace(apiKey)
            ? throw new ArgumentException("OpenAI API token is empty.", nameof(apiKey))
            : apiKey.Trim();

        Model = string.IsNullOrWhiteSpace(model) ? DefaultModel : model.Trim();
    }

    public string ApiKey { get; }
    public string Model { get; }
}
