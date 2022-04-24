using System.Net.Sockets;
using Authware.Bot.Common;
using Authware.Bot.Common.Models;
using Authware.Bot.Common.Utils;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace Authware.Bot.Commands;

[Group("tickets", "Commands related to the tickets system")]
[RequireContext(ContextType.Guild)]
public class Tickets : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IConfiguration _configuration;

    public Tickets(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [SlashCommand("new", "Creates a new ticket for the specified reason")]
    public async Task NewTicketAsync([Summary("type", "The type of query or issue you're experiencing")] TicketType type)
    {
        if (!_configuration.IsHomeGuild(Context.Guild.Id)) return;
        
        await Context.Interaction.DeferAsync(true);

        if (Context.Guild.Channels.Any(x => x.Name == $"ticket-{Context.User.Id}"))
        {
            await Context.Interaction.ErrorAsync("Cannot create ticket",
                "You already have a ticket open, close the existing one before opening another!", true);
            return;
        }

        // Get the support role (the users authorized to see the ticket) or create it if non existent
        
        var authorizedSupportRole = Context.Guild.Roles.FirstOrDefault(x =>
                                        x.Name.Equals(_configuration.GetValue<string>("SupportRole"),
                                            StringComparison.OrdinalIgnoreCase)) as IRole ??
                                    await Context.Guild.CreateRoleAsync(_configuration.GetValue<string>("SupportRole"),
                                        GuildPermissions.None, Color.Default,
                                        false, true);

        var ticketCategory = Context.Guild.CategoryChannels.FirstOrDefault(x =>
            x.Name.Equals("tickets", StringComparison.OrdinalIgnoreCase)) ?? (ICategoryChannel?) await Context.Guild.CreateCategoryChannelAsync("Tickets", properties =>
        {
            properties.Position = Context.Guild.Channels.MaxBy(x => x.Position).Position + 1;
        });

        // Create the actual ticket channel
        var ticketChannel = await Context.Guild.CreateTextChannelAsync($"ticket-{Context.User.Id}", properties =>
        {
            properties.Topic =
                $"Support ticket for {Context.User.Username}#{Context.User.Discriminator} regarding an issue relating to {type.ToString().ToLower()}";
            if (ticketCategory is not null) properties.CategoryId = ticketCategory.Id;
        });

        await ticketChannel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole,
            OverwritePermissions.DenyAll(ticketChannel));
        await ticketChannel.AddPermissionOverwriteAsync(authorizedSupportRole,
            OverwritePermissions.AllowAll(ticketChannel));
        await ticketChannel.AddPermissionOverwriteAsync(Context.User, OverwritePermissions.AllowAll(ticketChannel));

        var ticketEmbed = new AuthwareEmbedBuilder()
            .WithTitle($"Welcome, {Context.User.Username}!")
            .WithDescription("A support representative should be with you shortly, please be patient!")
            .AddField("> Topic", type)
            .Build();

        var ticketComponent = new ComponentBuilder()
            .WithButton("Close", $"close-ticket-{Context.User.Id}", ButtonStyle.Danger)
            .Build();

        var message = await ticketChannel.SendMessageAsync($"{Context.User.Mention} {authorizedSupportRole.Mention}",
            embed: ticketEmbed, components: ticketComponent);

        await message.PinAsync();

        await Context.Interaction.SuccessAsync("Ticket created",
            $"Your ticket has been created, check out {ticketChannel.Mention}!", true);
    }
}