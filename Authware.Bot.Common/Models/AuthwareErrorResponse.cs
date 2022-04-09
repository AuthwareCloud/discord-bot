using System.Text.Json.Serialization;

namespace Authware.Bot.Common.Models;

public class AuthwareErrorResponse : AuthwareResponse
{
    [JsonPropertyName("message")] public string Message { get; set; }
    [JsonPropertyName("errors")] public string[] Errors { get; set; }
}