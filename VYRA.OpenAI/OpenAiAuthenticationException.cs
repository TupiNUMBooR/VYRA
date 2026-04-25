namespace VYRA.OpenAI;

public sealed class OpenAiAuthenticationException : OpenAiException
{
    public OpenAiAuthenticationException(string message) : base(message)
    {
    }
}
