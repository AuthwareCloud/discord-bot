namespace Authware.Bot.Webhook.Responses;

public class DefaultResponse : ResponseBase
{
    [JsonConstructor]
    public DefaultResponse(int code, string message, string[]? errors = null, string? trace = null) : base(code)
    {
        Message = message;
        Errors = errors;
        Trace = trace;
    }

    [JsonPropertyName("message")] public string Message { get; }
    [JsonPropertyName("errors")] public string[]? Errors { get; }
    [JsonPropertyName("trace")] public string? Trace { get; }
}