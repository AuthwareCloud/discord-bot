using System.Net.Http.Headers;
using System.Reflection;
using Authware.Bot.Commands;
using Authware.Bot.Common.Models;
using Authware.Bot.Services;
using Authware.Bot.Services.Interfaces;
using Authware.Bot.Shared;
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
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Authware.Bot;

public class Startup
{
    private readonly InteractionService _interaction;
    private readonly DiscordSocketClient _client;
    private readonly IServiceProvider _provider;
    private readonly LavalinkNode _lavalinkNode;
    private readonly DiscordClientWrapper _clientWrapper;
    private readonly LavalinkCache _lavalinkCache;
    private readonly InactivityTrackingService _inactivityTracking;

    private IConfiguration Configuration { get; }

    private string[] Arguments { get; }

    public Startup(string[] args, InteractionService? interaction = null, DiscordSocketClient? client = null)
    {
        Arguments = args;

        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();

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
        }, _clientWrapper, new EventLogger(), _lavalinkCache);

        _interaction = interaction ?? new InteractionService(_client, new InteractionServiceConfig
        {
            UseCompiledLambda = true,
            EnableAutocompleteHandlers = true,
            ExitOnMissingModalField = true,
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
        Globals.ServiceProvider = _provider;

        _interaction.AddModulesAsync(typeof(CommandsEntryPoint).Assembly, _provider);

        _client.Ready += HandleOnReady;
    }

    private async Task HandleOnReady()
    {
#if DEBUG
        _provider.GetRequiredService<ILogger<Startup>>().LogInformation("Registering slash commands to guild...");
        var registeredCommands =
            await _interaction.RegisterCommandsToGuildAsync(Configuration.GetValue<ulong>("TestingGuildId"));
        foreach (var command in registeredCommands)
            _provider.GetRequiredService<ILogger<Startup>>().LogInformation("Registered {Name}", command.Name);
#else
        _logger.Information("Registering slash commands globally...");
        await _interaction.RegisterCommandsGloballyAsync();
#endif

#if RELEASE
        _provider.GetRequiredService<ILogger<Startup>>().Information("Initializing Lavalink...");
        await _provider.GetRequiredService<IAudioService>().InitializeAsync();

        _provider.GetRequiredService<ILogger<Startup>>().Information("Initializing inactivity tracking...");
        _provider.GetRequiredService<InactivityTrackingService>().BeginTracking();
#endif

        await _client.SetGameAsync("the #1 cloud authentication solution");
    }

    public async Task RunAsync()
    {
        _provider.GetRequiredService<LoggingService>();
        // _provider.GetRequiredService<FilterService>();
        await _provider.GetRequiredService<IStartupService>().StartAsync();

        Webhook.Startup.Run();

        await Task.Delay(-1);
    }

    private IServiceProvider BuildServiceProvider()
    {
        return new ServiceCollection()
            .AddLogging(options =>
            {
#if DEBUG
                options.SetMinimumLevel(LogLevel.Debug);
                options.AddConsole();
#else
                options.SetMinimumLevel(LogLevel.Information);
                options.AddSystemdConsole():
#endif
            })
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
            .AddHttpClient<HttpClient>("authware", options =>
            {
                options.BaseAddress = new Uri("https://api.authware.org/");
                options.DefaultRequestVersion = new Version("2.0.0.0");
                options.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
                options.DefaultRequestHeaders.TryAddWithoutValidation("Authorization",
                    Configuration.GetValue<string>("Token"));
                options.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Authware-DotNet", "1.0.0.0"));
            })
            .Services.BuildServiceProvider();
    }
}