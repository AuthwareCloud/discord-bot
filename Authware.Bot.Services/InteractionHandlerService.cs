using Authware.Bot.Common.Utils;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Authware.Bot.Services;

public class InteractionHandlerService
{
    private readonly DiscordSocketClient _client;
    private readonly IServiceProvider _provider;
    private readonly IConfiguration _configuration;
    private readonly InteractionService _interaction;
    private readonly ILogger<InteractionHandlerService> _logger;

    public InteractionHandlerService(DiscordSocketClient client, IServiceProvider provider,
        ILogger<InteractionHandlerService> logger, IConfiguration configuration, InteractionService interaction)
    {
        _client = client;
        _provider = provider;
        _logger = logger;
        _configuration = configuration;
        _interaction = interaction;
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
        _logger.LogInformation("Closing ticket as it was confirmed");

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
                    x.Name.Equals(_configuration.GetValue<string>("LogChannel"), StringComparison.OrdinalIgnoreCase));

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

                _logger.LogInformation("Confirming ticket closure");

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
                _logger.LogInformation("Unhandled interaction type: {Type}", arg.Type);
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
}