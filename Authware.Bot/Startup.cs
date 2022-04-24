using System.Net.Http.Headers;
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
            GatewayIntents = GatewayIntents.GuildVoiceStates | GatewayIntents.GuildMembers | GatewayIntents.Guilds,
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

        var closureReason = arg.Data.Components.FirstOrDefault(x => x.CustomId == "ticket-close-text")?.Value;
        var feedback = arg.Data.Components.FirstOrDefault(x => x.CustomId == "ticket-feedback-text")?.Value;

        // Check if the actual text input was valid
        _logger.Information("Closing ticket as it was confirmed");

        var userId = arg.Data.CustomId.Split('-').LastOrDefault();
        if (!ulong.TryParse(userId, out var validUserId)) return;
        var ticketUser = await _client.GetUserAsync(validUserId);

        // Delete the channel the interaction was fired in
        if (arg.Channel is not SocketTextChannel ticketChannel) return;

        await ticketChannel.DeleteAsync();

        // Notify the user it was closed
        var closureEmbed = new AuthwareEmbedBuilder()
            .WithTitle("Ticket has been closed")
            .WithDescription(
                "Hi there, the ticket you opened has been closed by either you or a staff member.")
            .AddField("> Reason", closureReason ?? "N/A")
            .Build();

        if (feedback is not null)
        {
            var logChannel =
                ticketChannel.Guild.TextChannels.FirstOrDefault(x =>
                    x.Name.Equals(Configuration.GetValue<string>("LogChannel"), StringComparison.OrdinalIgnoreCase));

            if (logChannel is not null)
            {
                var feedbackEmbed = new AuthwareEmbedBuilder()
                    .WithTitle("Ticket has been closed")
                    .WithDescription($"The ticket {arg.Channel.Name} has been closed by the end-user")
                    .AddField("> Reason", closureReason ?? "N/A")
                    .AddField("> Feedback", feedback)
                    .Build();

                await logChannel.SendMessageAsync(embed: feedbackEmbed);
            }
        }

        await ticketUser.SendMessageAsync(embed: closureEmbed);
    }

    private async Task HandleButtonExecuted(SocketMessageComponent arg)
    {
        switch (arg.Data.Type)
        {
            case ComponentType.Button:
            {
                if (!arg.Data.CustomId.StartsWith("close-ticket-")) return;

                _logger.Information("Confirming ticket closure");

                var userId = arg.Data.CustomId.Split('-').LastOrDefault();
                if (!ulong.TryParse(userId, out var validUserId)) return;

                Modal modal;

                if (arg.User.Id != validUserId)
                    // Do a confirm modal
                    modal = new ModalBuilder()
                        .WithTitle("Confirm closing ticket")
                        .WithCustomId("confirm-" + arg.Data.CustomId)
                        .AddTextInput("Why are you closing the ticket?", "ticket-close-text", TextInputStyle.Short,
                            "The issue was...", 0, 100, true)
                        .Build();
                else
                    modal = new ModalBuilder()
                        .WithTitle("Confirm closing ticket")
                        .WithCustomId("confirm-" + arg.Data.CustomId)
                        .AddTextInput("Why are you closing the ticket?", "ticket-close-text", TextInputStyle.Short,
                            "The issue was...", 0, 100, true)
                        .AddTextInput("Any feedback for how this ticket was handled?", "ticket-feedback-text",
                            TextInputStyle.Paragraph, "I really enjoyed...", 0, 1000, false)
                        .Build();

                await arg.RespondWithModalAsync(modal);
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
        var context = new SocketInteractionContext(_client, arg);
        var result = await _interaction.ExecuteCommandAsync(context, _provider);

        if (!result.IsSuccess && arg.Type == InteractionType.ApplicationCommand)
        {
            var errorEmbed = new AuthwareEmbedBuilder()
                .WithDescription($"**Error: **{result.ErrorReason}")
                .Build();

            await context.Interaction.RespondAsync(embed: errorEmbed, ephemeral: true);
        }
    }

    private async Task HandleOnReady()
    {
#if DEBUG
        _logger.Information("Registering slash commands to guild...");
        var registeredCommands =
            await _interaction.RegisterCommandsToGuildAsync(Configuration.GetValue<ulong>("TestingGuildId"));
        foreach (var command in registeredCommands) _logger.Information("Registered {Name}", command.Name);
#else
        _logger.Information("Registering slash commands globally...");
        await _interaction.RegisterCommandsGloballyAsync();
#endif

#if RELEASE
        _logger.Information("Initializing Lavalink...");
        await _provider.GetRequiredService<IAudioService>().InitializeAsync();

        _logger.Information("Initializing inactivity tracking...");
        _provider.GetRequiredService<InactivityTrackingService>().BeginTracking();
#endif

        await _client.SetGameAsync("the #1 cloud authentication solution");
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