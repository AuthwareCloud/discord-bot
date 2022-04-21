using System.Text.Json.Serialization;

namespace Authware.Bot.Common.Models;

public class AuthwareApplication
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("version")] public string Version { get; set; }
    [JsonPropertyName("date_created")] public DateTime DateCreated { get; set; }
    [JsonPropertyName("is_disabled")] public bool IsDisabled { get; set; }

    [JsonPropertyName("is_hwid_checking_enabled")]
    public bool IsHwidCheckingEnabled { get; set; }

    [JsonPropertyName("is_version_checking_enabled")]
    public bool IsVersionCheckingEnabled { get; set; }

    [JsonPropertyName("updater_uri")] public string UpdaterUri { get; set; }

    [JsonPropertyName("is_discord_audit_logs_enabled")]
    public bool IsDiscordAuditLogsEnabled { get; set; }

    [JsonPropertyName("is_audit_logs_enabled")]
    public bool IsAuditLogsEnabled { get; set; }

    [JsonPropertyName("can_users_add_variables")]
    public bool CanUsersAddVariables { get; set; }

    [JsonPropertyName("discord_webhook_uri")]
    public string DiscordWebhookUri { get; set; }

    [JsonPropertyName("user_count")] public int UserCount { get; set; }
    [JsonPropertyName("request_count")] public int RequestCount { get; set; }
    [JsonPropertyName("token_count")] public int TokenCount { get; set; }
    [JsonPropertyName("seller_api_key")] public string SellerApiKey { get; set; }
    [JsonPropertyName("roles")] public List<AuthwareRole> Roles { get; set; }
}