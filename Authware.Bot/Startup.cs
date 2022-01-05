using System.Reflection;
using Authware.Bot.Commands;
using Authware.Bot.Common.Models;
using Authware.Bot.Common.Utils;
using Authware.Bot.Services;
using Authware.Bot.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Authware.Bot;

public class Startup
{
    private readonly InteractionService _commands;
    private readonly DiscordSocketClient _client;
    private readonly IServiceProvider _provider;

    private IConfiguration Configuration { get; }

    private string[] Arguments { get; }

    public Startup(string[] args, InteractionService? commands = null, DiscordSocketClient? client = null)
    {
        Arguments = args;

        Configuration = new ConfigurationBuilder()
                       .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                       .AddJsonFile("appsettings.json")
                       .Build();

        _client = client ?? new DiscordSocketClient(new DiscordSocketConfig
        {
            DefaultRetryMode = RetryMode.RetryRatelimit,
            MessageCacheSize = int.Parse(Configuration.GetSection("SocketConfig")["MessageCacheSize"]),
            UseSystemClock = bool.Parse(Configuration.GetSection("SocketConfig")["UseSystemClock"]),
            AlwaysDownloadUsers = bool.Parse(Configuration.GetSection("SocketConfig")["AlwaysDownloadUsers"]),
            GatewayIntents = GatewayIntents.All,
            LogLevel = LogSeverity.Debug
        });

        _commands = commands ?? new InteractionService(_client, new InteractionServiceConfig
        {
            UseCompiledLambda = true,
            EnableAutocompleteHandlers = true
        });

        // This stops commands from not getting registered.
        _provider = BuildServiceProvider();

        _commands.AddModulesAsync(Assembly.GetAssembly(typeof(EntryPoint)), _provider);

        _client.Ready += HandleOnReady;
        _client.InteractionCreated += ClientOnInteractionCreated;
    }

    private async Task ClientOnInteractionCreated(SocketInteraction arg)
    {
        var context = new SocketInteractionContext(_client, arg);
        var result = await _commands.ExecuteCommandAsync(context, _provider);

        if (!result.IsSuccess)
        {
            var errorEmbed = new AuthwareEmbedBuilder()
                            .WithDescription($"**Error: **{result.ErrorReason}")
                            .Build();

            await context.Interaction.FollowupAsync(embed: errorEmbed, ephemeral: true);
        }
    }

    private async Task HandleOnReady()
    {
        if (Arguments.FirstOrDefault() == "reg")
        {
            Console.WriteLine("Registering slash commands...");
            await _commands.RegisterCommandsToGuildAsync(ulong.Parse(Configuration["GuildId"]));
        }

        Console.WriteLine("Ready!");
    }

    public async Task RunAsync()
    {
        _provider.GetRequiredService<LoggingService>();
        _provider.GetRequiredService<FilterService>();
        await _provider.GetRequiredService<IStartupService>().StartAsync();

        await Task.Delay(-1);
    }

    private IServiceProvider BuildServiceProvider()
    {
        return new ServiceCollection()
              .AddSingleton(_client)
              .AddSingleton(_commands)
              .AddSingleton(Configuration)
              .AddSingleton<LoggingService>()
              .AddSingleton<IWritableConfigurationService<AuthwareConfiguration>,
                   WritableConfigurationService<AuthwareConfiguration>>()
              .AddSingleton<FilterService>()
              .AddSingleton<IStartupService, StartupService>()
              .AddSingleton<IModerationService, ModerationService>()
              .BuildServiceProvider();
    }
}