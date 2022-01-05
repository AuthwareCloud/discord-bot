using System.Reflection;
using Authware.Bot.Common;
using Authware.Bot.Common.Models;
using Authware.Bot.Common.Utils;
using Authware.Bot.Services.Interfaces;
using Discord;
using Discord.Interactions;

namespace Authware.Bot.Commands;

[Group("settings", "Commands to change the bot configuration")]
public class Settings : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IWritableConfigurationService<AuthwareConfiguration> _configuration;

    public Settings(IWritableConfigurationService<AuthwareConfiguration> configuration)
    {
        _configuration = configuration;
    }

    [SlashCommand("show", "Shows the current bot configuration")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    [RequireContext(ContextType.Guild)]
    public async Task ShowConfigurationAsync()
    {
        await Context.Interaction.DeferAsync(true);

        var configuration = _configuration.Load();
        var configurationProperties = configuration.GetType().GetProperties();

        var embed = new AuthwareEmbedBuilder()
                   .WithTitle($"{Context.Client.CurrentUser.Username} settings")
                   .WithThumbnailUrl(Context.Guild.IconUrl ?? string.Empty);

        foreach (var propertyInfo in configurationProperties)
        {
            var attributes = propertyInfo.GetCustomAttributes();
            if (attributes.All(x => x is not FriendlyNameAttribute) ||
                attributes.All(x => x is not SettingsTypeAttribute)) continue;

            var nameAttribute = attributes.FirstOrDefault(x => x is FriendlyNameAttribute) as FriendlyNameAttribute;
            var typeAttribute = attributes.FirstOrDefault(x => x is SettingsTypeAttribute) as SettingsTypeAttribute;

            var propertyValue = propertyInfo.GetValue(configuration);
            ulong.TryParse(propertyValue?.ToString(), out var propertyValueId);
            
            propertyValue = typeAttribute.DateType switch
            {
                SettingsDataType.TextChannel => Context.Guild.GetTextChannel(propertyValueId).Mention,
                SettingsDataType.VoiceChannel => Context.Guild.GetVoiceChannel(propertyValueId).Mention,
                SettingsDataType.Role => Context.Guild.GetRole(propertyValueId).Mention,
                SettingsDataType.User => Context.Guild.GetUser(propertyValueId).Mention,
                SettingsDataType.Regular => propertyValue.ToString(),
                SettingsDataType.Boolean => bool.Parse(propertyValue.ToString() ?? "false") ? "<:on:925407585400676392>" : "<:off:925407585262252072>",
                _ => throw new ArgumentOutOfRangeException()
            };

            embed.AddField(nameAttribute?.Name, propertyValue, true);
        }

        await Context.Interaction.FollowupAsync(embed: embed.Build(), ephemeral: true);
    }

    [SlashCommand("invite-filter", "Change the invite filter status")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    [RequireContext(ContextType.Guild)]
    public async Task ChangeInviteFilterAsync([Summary("enabled", "The new state to set the filter to")] bool enabled)
    {
        await Context.Interaction.DeferAsync(true);

        var configuration = _configuration.Load();
        configuration.InviteFilter = enabled;
        
        _configuration.Save(configuration);
        
        await Context.Interaction.SuccessAsync("Settings have been updated", true);
    }
    
    [SlashCommand("anti-spam", "Change the anti-spam status")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    [RequireContext(ContextType.Guild)]
    public async Task ChangeAntiSpamAsync([Summary("enabled", "The new state to set the anti-spam to")] bool enabled)
    {
        await Context.Interaction.DeferAsync(true);

        var configuration = _configuration.Load();
        configuration.AntispamEnabled = enabled;
        
        _configuration.Save(configuration);
        
        await Context.Interaction.SuccessAsync("Settings have been updated", true);
    }

    [SlashCommand("moderation-channel", "Change the channel where moderation messages are sent")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    [RequireContext(ContextType.Guild)]
    public async Task ChangeModerationChannelAsync(
        [Summary("new-channel", "The new channel to set")]
        ITextChannel channel)
    {
        await Context.Interaction.DeferAsync(true);

        var configuration = _configuration.Load();
        configuration.ModerationChannel = channel.Id;

        _configuration.Save(configuration);

        await Context.Interaction.SuccessAsync("Settings have been updated", true);
    }
    
    [SlashCommand("bypass-role", "Change the filter and anti-spam bypass role")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    [RequireContext(ContextType.Guild)]
    public async Task ChangeModerationChannelAsync(
        [Summary("new-role", "The new role to set")]
        IRole role)
    {
        await Context.Interaction.DeferAsync(true);

        var configuration = _configuration.Load();
        configuration.BypassRole = role.Id;

        _configuration.Save(configuration);

        await Context.Interaction.SuccessAsync("Settings have been updated", true);
    }
}