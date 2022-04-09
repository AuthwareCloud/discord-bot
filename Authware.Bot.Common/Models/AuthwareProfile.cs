using System.Text.Json.Serialization;

namespace Authware.Bot.Common.Models;

public class AuthwareProfile : AuthwareResponse
{
    [JsonPropertyName("roles")] public List<string> Roles { get; set; }
    [JsonPropertyName("username")] public string UserName { get; set; }
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("email")] public string Email { get; set; }
    [JsonPropertyName("date_created")] public DateTime DateCreated { get; set; }
    [JsonPropertyName("plan_expire")] public DateTime PlanExpire { get; set; }
    [JsonPropertyName("user_count")] public int UserCount { get; set; }
    [JsonPropertyName("app_count")] public int AppCount { get; set; }
    [JsonPropertyName("api_count")] public int ApiCount { get; set; }
}