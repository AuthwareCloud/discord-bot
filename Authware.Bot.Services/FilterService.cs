using System.Text.RegularExpressions;
using Authware.Bot.Common.Models;
using Authware.Bot.Services.Interfaces;
using Discord;
using Discord.WebSocket;

namespace Authware.Bot.Services;

public class FilterService
{
    private readonly Regex _inviteRegex = new(@"(discord\.gg\/)[a-zA-Z0-9]{2,12}",
                                              RegexOptions.Compiled | RegexOptions.IgnoreCase |
                                              RegexOptions.CultureInvariant);

    private readonly IModerationService _moderation;
    private readonly IWritableConfigurationService<AuthwareConfiguration> _configuration;
    private readonly DiscordSocketClient _client;

    public FilterService(DiscordSocketClient client, IModerationService moderation,
                         IWritableConfigurationService<AuthwareConfiguration> configuration)
    {
        _client = client;
        _moderation = moderation;
        _configuration = configuration;
        client.MessageReceived += ClientOnMessageReceived;
        client.MessageUpdated += ClientOnMessageUpdated;
    }

    private bool IsFiltered(string content)
    {
        return content.Contains("discord.com/invite") || _inviteRegex.IsMatch(content);
    }

    private async Task<bool> CanBeModerated(SocketGuildUser? user)
    {
        // Moderator, Enhanced Permissions, Administrator, Filter Bypass
        var bypassRole = _configuration.Load().BypassRole;
        if (user != null &&
            user.Roles.Any(x => x.Id is 910893141085794304 or 912353594102132846 or 910893141085794304 ||
                               x.Id == bypassRole))
            return false;
        return user is {IsBot: false};
    }

    private async Task ClientOnMessageUpdated(Cacheable<IMessage, ulong> arg1, SocketMessage arg2, ISocketMessageChannel arg3)
    {
        if (_configuration.Load().InviteFilter && await CanBeModerated(arg2.Author as SocketGuildUser) && IsFiltered(arg2.Content))
        {
            await arg2.DeleteAsync();
            
            await _moderation.WarnUserAsync(arg2.Author as SocketGuildUser, await _client.GetUserAsync(_client.CurrentUser.Id),
                                            "Triggered message filter");
        }
    }

    private async Task ClientOnMessageReceived(SocketMessage arg)
    {
        if (_configuration.Load().InviteFilter && await CanBeModerated(arg.Author as SocketGuildUser) && IsFiltered(arg.Content))
        {
            await arg.DeleteAsync();

            await _moderation.WarnUserAsync(arg.Author as SocketGuildUser, await _client.GetUserAsync(_client.CurrentUser.Id),
                                            "Triggered message filter");
        }
    }
}