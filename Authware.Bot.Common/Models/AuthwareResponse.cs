using System.Text.Json.Serialization;

namespace Authware.Bot.Common.Models;

public class AuthwareResponse
{
    [JsonPropertyName("code")] public int Code { get; set; }
}