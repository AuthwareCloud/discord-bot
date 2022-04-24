namespace Authware.Bot.Webhook.Forms;

public class WebhookUserForm
{
    [JsonPropertyName("user_id")] public ulong UserId { get; set; }

    [JsonPropertyName("added_roles")] public List<string>? AddedRoles { get; set; } = new();
    [JsonPropertyName("removed_roles")] public List<string>? RemovedRoles { get; set; } = new();

    [JsonPropertyName("intent")] public WebhookUpdateIntent Intent { get; set; } = WebhookUpdateIntent.USER_UPDATED;
}