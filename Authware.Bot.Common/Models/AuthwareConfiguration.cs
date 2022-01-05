using System.Text.Json.Serialization;
using Authware.Bot.Common.Utils;

namespace Authware.Bot.Common.Models;

public class AuthwareConfiguration
{
    [JsonPropertyName("moderation_channel")]
    [FriendlyName("Moderation channel")]
    [SettingsType(SettingsDataType.TextChannel)]
    public ulong ModerationChannel { get; set; } = 910711106551545906;
    
    [JsonPropertyName("strike_threshold")]
    [FriendlyName("Strike threshold")]
    [SettingsType(SettingsDataType.Regular)]
    public ushort StrikeThreshold { get; set; } = 5;

    [JsonPropertyName("antispam_enabled")]
    [FriendlyName("Anti-spam")]
    [SettingsType(SettingsDataType.Boolean)]
    public bool AntispamEnabled { get; set; } = true;

    [JsonPropertyName("message_interval")]
    [FriendlyName("Anti-spam message interval")]
    [SettingsType(SettingsDataType.Regular)]
    public float MessageInterval { get; set; } = 0.75f;

    [JsonPropertyName("bypass_role")]
    [FriendlyName("Bypass role")]
    [SettingsType(SettingsDataType.Role)]
    public ulong BypassRole { get; set; } = 910893959742648321;

    [JsonPropertyName("invite_filter_enabled")]
    [FriendlyName("Invite filter")]
    [SettingsType(SettingsDataType.Boolean)]
    public bool InviteFilter { get; set; } = true;
}