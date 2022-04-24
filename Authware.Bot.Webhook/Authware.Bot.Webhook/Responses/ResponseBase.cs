namespace Authware.Bot.Webhook.Responses;

public abstract class ResponseBase
{
    [JsonConstructor]
    protected ResponseBase(int code)
    {
        Code = code;
    }

    [JsonPropertyName("code")] public int Code { get; }
}