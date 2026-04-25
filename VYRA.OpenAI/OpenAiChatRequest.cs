namespace VYRA.OpenAI;

public sealed record OpenAiChatRequest(
    IReadOnlyList<OpenAiChatMessage> Messages,
    byte[]? CurrentScreenshotJpg);
