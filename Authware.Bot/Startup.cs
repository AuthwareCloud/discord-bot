using System.Reflection;
using Authware.Bot.Commands;
using Authware.Bot.Common.Models;
using Authware.Bot.Common.Utils;
using Authware.Bot.Services;
using Authware.Bot.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Logging;
using Lavalink4NET.MemoryCache;
using Lavalink4NET.Tracking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Authware.Bot;

public class Startup
{
    private readonly InteractionService _interaction;
    private readonly DiscordSocketClient _client;
    private readonly IServiceProvider _provider;
    private readonly ILogger _logger;
    private readonly LavalinkNode _lavalinkNode;
    private readonly DiscordClientWrapper _clientWrapper;
    private readonly LavalinkCache _lavalinkCache;
    private readonly InactivityTrackingService _inactivityTracking;
    private readonly LavalinkLogger _lavalinkLogger;

    private IConfiguration Configuration { get; }

    private string[] Arguments { get; }

    public Startup(string[] args, InteractionService? interaction = null, DiscordSocketClient? client = null)
    {
        Arguments = args;

        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();

        _logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(Configuration["LogFileLocation"])
#if DEBUG
            .MinimumLevel.Debug()
#else
            .MinimumLevel.Information()
#endif
            .CreateLogger();

        _lavalinkLogger = new LavalinkLogger(_logger);
        
        var lavalinkSection = Configuration.GetSection("LavalinkOptions");

        _client = client ?? new DiscordSocketClient(new DiscordSocketConfig
        {
            DefaultRetryMode = RetryMode.RetryRatelimit,
            GatewayIntents = GatewayIntents.All,
            LogLevel = LogSeverity.Debug
        });

        _clientWrapper = new DiscordClientWrapper(_client);

        _lavalinkCache = new LavalinkCache();

        _lavalinkNode = new LavalinkNode(new LavalinkNodeOptions
        {
            Password = lavalinkSection["Password"],
            UserAgent = $"Authware-Bot/{Assembly.GetEntryAssembly()?.GetName().Version}",
            AllowResuming = true,
            Label = lavalinkSection["NodeId"],
            RestUri = lavalinkSection["RestEndpoint"],
            WebSocketUri = lavalinkSection["WebsocketEndpoint"]
        }, _clientWrapper, _lavalinkLogger, _lavalinkCache);

        _interaction = interaction ?? new InteractionService(_client, new InteractionServiceConfig
        {
            UseCompiledLambda = true,
            EnableAutocompleteHandlers = true,
            DefaultRunMode = RunMode.Async
        });

        _inactivityTracking = new InactivityTrackingService(_lavalinkNode, _clientWrapper, new InactivityTrackingOptions
        {
            DisconnectDelay = TimeSpan.FromSeconds(30),
            DelayFirstTrack = true,
            PollInterval = TimeSpan.FromSeconds(5),
            TrackInactivity = false
        });

        // This stops commands from not getting registered.
        _provider = BuildServiceProvider();
        
        _interaction.AddModulesAsync(typeof(CommandsEntryPoint).Assembly, _provider);

        _client.Ready += HandleOnReady;
        _client.InteractionCreated += ClientOnInteractionCreated;
    }

    private async Task ClientOnInteractionCreated(SocketInteraction arg)
    {
        var context = new SocketInteractionContext(_client, arg);
        var result = await _interaction.ExecuteCommandAsync(context, _provider);

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
#if DEBUG
        _logger.Information("Registering slash commands to guild...");
        await _interaction.RegisterCommandsToGuildAsync(Configuration.GetValue<ulong>("GuildId"));
#else
        _logger.Information("Registering slash commands globally...");
        await _interaction.RegisterCommandsGloballyAsync();
#endif
        
        _logger.Information("Initializing Lavalink...");
        await _provider.GetRequiredService<IAudioService>().InitializeAsync();
        
        _logger.Information("Initializing inactivity tracking...");
        _provider.GetRequiredService<InactivityTrackingService>().BeginTracking();
    }

    public async Task RunAsync()
    {
        _provider.GetRequiredService<LoggingService>();
        // _provider.GetRequiredService<FilterService>();
        await _provider.GetRequiredService<IStartupService>().StartAsync();

        await Task.Delay(-1);
    }

    private IServiceProvider BuildServiceProvider()
    {

        return new ServiceCollection()
            .AddSingleton(_logger)
            .AddSingleton(_client)
            .AddSingleton(_interaction)
            .AddSingleton(Configuration)
            .AddSingleton<ILavalinkCache>(_lavalinkCache)
            .AddSingleton<IAudioService>(_lavalinkNode)
            .AddSingleton<IDiscordClientWrapper>(_clientWrapper)
            .AddSingleton(_inactivityTracking)
            .AddSingleton<LoggingService>()
            .AddSingleton<IWritableConfigurationService<AuthwareConfiguration>,
                WritableConfigurationService<AuthwareConfiguration>>()
            .AddSingleton<FilterService>()
            .AddSingleton<IStartupService, StartupService>()
            .AddSingleton<IModerationService, ModerationService>()
            .BuildServiceProvider();
    }
}