using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace Authware.Bot.Services;

public class BoostService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _factory;

    public BoostService(DiscordSocketClient client, IConfiguration configuration, IHttpClientFactory factory)
    {
        _configuration = configuration;
        _factory = factory;
        // client.MessageReceived += MessageReceived;
    }

    private async Task MessageReceived(SocketMessage arg)
    {
        if (arg.Channel is not SocketGuildChannel channel) return;
        var guildId = _configuration.GetValue<ulong>("GuildId");

        if (arg.Type == MessageType.UserPremiumGuildSubscription && channel.Guild.Id == guildId)
        {
            using var client = _factory.CreateClient("authware");
            
            // var response = await client.GetAsync()
        }
    }
}