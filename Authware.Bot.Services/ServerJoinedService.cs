using Authware.Bot.Common.Utils;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace Authware.Bot.Services;

public class ServerJoinedService
{
    #region Services

    private readonly ILogger<ServerJoinedService> _logger;

    #endregion

    public ServerJoinedService(DiscordSocketClient client, ILogger<ServerJoinedService> logger)
    {
        _logger = logger;

        // Subscribe to the server joined event
        client.JoinedGuild += HandleGuildJoinedAsync;
    }

    private async Task HandleGuildJoinedAsync(SocketGuild arg)
    {
        _logger.LogInformation("Bot has joined a new server!");

        var embed = GetEmbed();
        
        try
        {
            await arg.DefaultChannel.SendMessageAsync(embed: embed);
        }
        catch (Exception)
        {
            _logger.LogInformation("Failed to send message in default channel, trying direct messages");

            await arg.Owner.SendMessageAsync(embed: embed);
        }
    }

    private static Embed GetEmbed()
    {
        return new AuthwareEmbedBuilder()
            .WithTitle("Thanks for adding me!")
            .WithDescription(
                "Now you can access some of the amazing Authware Discord integration features plus some extra handy features for your community, such as:\n\n> Music in channels via YouTube, SoundCloud, Vimeo and many more!\n> Generating license keys on the fly\n> More fun commands like PP size\n\nYou can view the **source code** to our bot [over here](https://github.com/AuthwareCloud/discord-bot) and even host it yourself!")
            .Build();
    }
}