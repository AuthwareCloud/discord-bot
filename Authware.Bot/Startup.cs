using System.Reflection;
using Authware.Bot.Commands;
using Authware.Bot.Common;
using Authware.Bot.Common.Models;
using Authware.Bot.Common.Utils;
using Authware.Bot.Services;
using Authware.Bot.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.MemoryCache;
using Lavalink4NET.Tracking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

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
        _client.ButtonExecuted += HandleButtonExecuted;
        _client.ModalSubmitted += HandleModalSubmitted;
    }

    private async Task HandleModalSubmitted(SocketModal arg)
    {
        if (!arg.Data.CustomId.StartsWith("confirm-close-ticket-")) return;

        await arg.DeferAsync(true);
        
        // Check if the actual text input was valid
        if (arg.Data.Components.FirstOrDefault(x => x.CustomId == "ticket-confirm-text")?.Value == "OK")
        {
            
            _logger.Information("Closing ticket as it was confirmed");

            var ticketDataArray = arg.Data.CustomId.Split('-');
            var userId = ulong.Parse(ticketDataArray.LastOrDefault());
            var ticketUser = await _client.GetUserAsync(userId);

            // Delete the channel the interaction was fired in
            var ticketChannel = arg.Channel as SocketTextChannel;
            await ticketChannel.DeleteAsync();

            // Notify the user it was closed
            var closureEmbed = new AuthwareEmbedBuilder()
                .WithTitle("Ticket has been closed")
                .WithDescription(
                    "Hi there, the ticket you opened has been closed by either you or a staff member.")
                .Build();

            await ticketUser.SendMessageAsync(embed: closureEmbed);
        }
        else
        {
            await arg.ErrorAsync("Cannot close ticket", "You need to type `OK` to confirm closure of this ticket!", true);
        }
    }

    private async Task HandleButtonExecuted(SocketMessageComponent arg)
    {
        switch (arg.Data.Type)
        {
            case ComponentType.Button:
            {
                if (!arg.Data.CustomId.StartsWith("close-ticket-")) return;

                _logger.Information("Confirming ticket closure");

                // Do a confirm modal
                var confirmDeleteModal = new ModalBuilder()
                    .WithTitle("Confirm closing ticket")
                    .WithCustomId("confirm-" + arg.Data.CustomId)
                    .AddTextInput("Type 'OK' to confirm and close the ticket", "ticket-confirm-text")
                    .Build();

                await arg.RespondWithModalAsync(confirmDeleteModal);
                break;
            }
            default:
            {
                // Unhandled interaction type
                _logger.Information("Unhandled interaction type: {Type}", arg.Type);
                break;
            }
        }
    }

    private async Task ClientOnInteractionCreated(SocketInteraction arg)
    {
        switch (arg.Type)
        {
            case InteractionType.ApplicationCommand:
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

                break;
            }
        }
    }

    private async Task HandleOnReady()
    {
#if DEBUG
        _logger.Information("Registering slash commands to guild...");
        await _interaction.RegisterCommandsToGuildAsync(Configuration.GetValue<ulong>("TestingGuildId"));
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