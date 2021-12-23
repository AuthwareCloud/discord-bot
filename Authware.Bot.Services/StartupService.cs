using Authware.Bot.Services.Interfaces;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace Authware.Bot.Services;

public class StartupService : IStartupService
{
    private readonly IConfiguration _configuration;
    private readonly DiscordSocketClient _client;

    public StartupService(DiscordSocketClient client,
        IConfiguration configuration)
    {
        _client = client;
        _configuration = configuration;
    }

    public async Task StartAsync()
    {
        if (string.IsNullOrWhiteSpace(_configuration["Token"]))
            throw new ArgumentNullException(nameof(_configuration),
                "Bot token is null");

        await _client.LoginAsync(TokenType.Bot, _configuration["Token"]);
        await _client.StartAsync();
    }
}