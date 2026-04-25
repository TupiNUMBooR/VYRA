namespace VYRA.OpenAI;

public sealed class OpenAiConnectionException : OpenAiException
{
    public OpenAiConnectionException(string message) : base(message)
    {
    }

    public OpenAiConnectionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
