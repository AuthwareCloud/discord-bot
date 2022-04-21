using System.Text.Json.Serialization;

namespace Authware.Bot.Common.Models;

public class AuthwareRole
{
    [JsonPropertyName("id")] public Guid Id { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; }
}