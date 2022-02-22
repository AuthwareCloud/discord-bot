using System.Text;
using Authware.Bot.Common;
using Authware.Bot.Common.Utils;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace Authware.Bot.Commands;

public class Utility : InteractionModuleBase<SocketInteractionContext>
{
    private readonly Random _rng = new();
    private readonly IConfiguration _configuration;

    public Utility(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [RequireContext(ContextType.Guild)]
    [SlashCommand("ping", "Returns the ping between the bot and the Discord gateway")]
    public async Task PingAsync()
    {
        await Context.Interaction.DeferAsync();

        var embed = new AuthwareEmbedBuilder()
            .WithTitle("Ping")
            .WithDescription($"The ping is {Context.Client.Latency}ms")
            .Build();

        await Context.Interaction.FollowupAsync(embed: embed);
    }

    [RequireContext(ContextType.Guild)]
    [UserCommand("Get avatar")]
    public async Task AvatarAsync(IUser user)
    {
        await Context.Interaction.DeferAsync(true);

        var avatarUrl = user.GetAvatarUrl();

        var embed = new AuthwareEmbedBuilder()
            .WithImageUrl(avatarUrl)
            .Build();

        await Context.Interaction.FollowupAsync(embed: embed, ephemeral: true);
    }

    [RequireContext(ContextType.Guild)]
    [UserCommand("PP size")]
    public async Task GetPpSizeAsync(IUser user)
    {
        await Context.Interaction.DeferAsync();

        var ppSize = _rng.Next(0, 20);
        var ppBars = string.Empty;
        for (var i = 0; i < ppSize; i++) ppBars += "=";

        var embed = new AuthwareEmbedBuilder()
            .WithTitle($"{user.Username}'s PP")
            .WithDescription($"8{ppBars}D")
            .Build();

        await Context.Interaction.FollowupAsync(embed: embed);
    }

    [RequireContext(ContextType.Guild)]
    [UserCommand("Get user information")]
    public async Task GetUserInformationAsync(IUser user)
    {
        await Context.Interaction.DeferAsync(true);

        var guildUser = user as SocketGuildUser;

        var clients = user.ActiveClients?
            .Aggregate(string.Empty, (current, clientType) => current + $"{clientType}, ")
            .TrimEnd(' ').TrimEnd(',');

        var roles = guildUser?.Roles?.OrderByDescending(x => x.Position)
            .Aggregate(string.Empty, (current, role) => current + $"{role.Mention} ");

        var embed = new AuthwareEmbedBuilder()
            .WithThumbnailUrl(user.GetAvatarUrl())
            .AddField("Username", user.Username)
            .AddField("Nickname", guildUser?.Nickname ?? "None")
            .AddField("Discriminator", user.Discriminator)
            .AddField("Status", user.Status)
            .AddField("Created at", user.CreatedAt)
            .AddField("Roles", string.IsNullOrWhiteSpace(roles) ? "None" : roles)
            .AddField("Active clients", string.IsNullOrWhiteSpace(clients) ? "None" : clients)
            .AddField("Joined at", guildUser.JoinedAt)
            .Build();

        await Context.Interaction.FollowupAsync(embed: embed, ephemeral: true);
    }

    [RequireContext(ContextType.Guild)]
    [RequireBotPermission(GuildPermission.ManageMessages)]
    [RequireUserPermission(GuildPermission.ManageMessages)]
    [SlashCommand("clean", "Cleans up the bots responses")]
    public async Task CleanAsync()
    {
        await Context.Interaction.DeferAsync(true);

        var messages = await Context.Channel.GetMessagesAsync().FlattenAsync();
        var safeMessages = messages.Where(x => x.Author.Id == Context.Client.CurrentUser.Id);

        if (!safeMessages.Any())
        {
            var errorEmbed = new AuthwareEmbedBuilder()
                .WithDescription("**Error: **There is nothing for me to delete.")
                .Build();

            await Context.Interaction.FollowupAsync(embed: errorEmbed, ephemeral: true);
            return;
        }

        await ((SocketTextChannel) Context.Channel).DeleteMessagesAsync(safeMessages);

        await Context.Interaction.SuccessAsync("Responses cleaned", $"**{safeMessages.Count()}** response(s) have been deleted.", true);
    }

    [RequireContext(ContextType.Guild)]
    [RequireBotPermission(ChannelPermission.ManageRoles)]
    [SlashCommand("dev",
        "Gives you the 'Developer' role which allows access to the channels specifically for developers")]
    public async Task AddDevAsync()
    {
        await Context.Interaction.DeferAsync(true);
        
        // Get the support role (the users authorized to see the ticket) or create it if non existent
        var developerRole = Context.Guild.Roles.FirstOrDefault(x =>
                                        x.Name.Equals(_configuration.GetValue<string>("DeveloperRole"),
                                            StringComparison.OrdinalIgnoreCase)) as IRole ??
                                    await Context.Guild.CreateRoleAsync(_configuration.GetValue<string>("DeveloperRole"), GuildPermissions.None, Color.Default,
                                        false, true);
        
        var guildUser = Context.User as SocketGuildUser;
        if (guildUser.Roles.Any(x => x.Id == developerRole.Id))
        {
            await Context.Interaction.ErrorAsync("Cannot become a developer", "You're already a developer.", true);
            return;
        }

        await guildUser.AddRoleAsync(developerRole);

        await Context.Interaction.SuccessAsync("You've became a developer", "You've been given the **Developer** role.", true);
    }

    [RequireContext(ContextType.Guild)]
    [MessageCommand("Reverse message")]
    public async Task ReverseMessage(IMessage message)
    {
        await Context.Interaction.DeferAsync(true);

        var reversedMessageChars = message.Content.Reverse();
        var reversedMessage = reversedMessageChars.Aggregate(string.Empty, (current, item) => current + item);

        await Context.Interaction.FollowupAsync(reversedMessage, ephemeral: true);
    }

    [RequireContext(ContextType.Guild)]
    [MessageCommand("Base64 decode")]
    public async Task Base64Decode(IMessage message)
    {
        await Context.Interaction.DeferAsync(true);

        try
        {
            var content = Convert.FromBase64String(message.Content);

            await Context.Interaction.FollowupAsync(Encoding.UTF8.GetString(content), ephemeral: true);
        }
        catch (Exception)
        {
            await Context.Interaction.ErrorAsync("Cannot convert message", "The message is not a Base64 string.", true);
        }
    }

    [RequireContext(ContextType.Guild)]
    [MessageCommand("Base64 encode")]
    public async Task Base64Encode(IMessage message)
    {
        await Context.Interaction.DeferAsync(true);

        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(message.Content));

        await Context.Interaction.FollowupAsync(base64, ephemeral: true);
    }

    [RequireContext(ContextType.Guild)]
    [RequireBotPermission(GuildPermission.ManageMessages)]
    [RequireUserPermission(GuildPermission.ManageMessages)]
    [SlashCommand("clear", "Clears a set amount of messages from a channel")]
    public async Task ClearAsync([Summary("amount", "The amount of messages to clear from the channel")] uint amount)
    {
        await Context.Interaction.DeferAsync(true);

        var messages = await Context.Channel.GetMessagesAsync().FlattenAsync();
        var safeMessages = messages.Where(x => x.CreatedAt - DateTimeOffset.Now < TimeSpan.FromDays(14))
            .TruncateList(amount);

        if (!safeMessages.Any())
        {
            await Context.Interaction.ErrorAsync("Cannot clear messages", "There is nothing for me to delete.", true);
            return;
        }

        await ((SocketTextChannel) Context.Channel).DeleteMessagesAsync(safeMessages);

        await Context.Interaction.ErrorAsync("Cleared messages", $"**{safeMessages.Count()}** message(s) have been deleted.", true);
    }
}