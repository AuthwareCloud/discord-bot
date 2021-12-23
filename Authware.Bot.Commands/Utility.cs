using Authware.Bot.Commands.Utils;
using Authware.Bot.Common;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Authware.Bot.Commands;

public class Utility : InteractionModuleBase<SocketInteractionContext>
{
    private readonly Random _rng = new();
    
    [RequireContext(ContextType.Guild)]
    [SlashCommand("ping", "Returns the ping between the bot and the Discord gateway")]
    public async Task PingAsync()
    {
        var embed = new AuthwareEmbedBuilder()
            .WithDescription($"The ping is {Context.Client.Latency}ms")
            .Build();

        await Context.Interaction.FollowupAsync(embed: embed);
    }

    [RequireContext(ContextType.Guild)]
    [UserCommand("Get avatar")]
    public async Task AvatarAsync(IUser user)
    {
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
        var ppSize = _rng.Next(0, 20);
        var ppBars = string.Empty;
        for (var i = 0; i < ppSize; i++)
        {
            ppBars += "=";
        }

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

        var embed = new AuthwareEmbedBuilder()
            .WithDescription($"**{safeMessages.Count()}** response(s) have been cleaned.")
            .Build();

        await Context.Interaction.FollowupAsync(embed: embed, ephemeral: true);
    }

    [RequireContext(ContextType.Guild)]
    [RequireBotPermission(ChannelPermission.ManageRoles)]
    [SlashCommand("dev",
        "Gives you the 'Developer' role which allows access to the channels specifically for developers")]
    public async Task AddDevAsync()
    {
        var guildUser = Context.User as SocketGuildUser;
        if (guildUser.Roles.Any(x => x.Id == 910893494128746518))
        {
            var errorEmbed = new AuthwareEmbedBuilder()
                .WithDescription("**Error: **You're already a developer.")
                .Build();

            await Context.Interaction.FollowupAsync(embed: errorEmbed, ephemeral: true);
            return;
        }
        
        await guildUser.AddRoleAsync(910893494128746518);

        var embed = new AuthwareEmbedBuilder()
            .WithDescription("You've been given the 'Developer' role.")
            .Build();

        await Context.Interaction.FollowupAsync(embed: embed, ephemeral: true);
    }

    [RequireContext(ContextType.Guild)]
    [RequireBotPermission(GuildPermission.ManageMessages)]
    [RequireUserPermission(GuildPermission.ManageMessages)]
    [SlashCommand("clear", "Clears a set amount of messages from a channel")]
    public async Task ClearAsync([Summary("amount", "The amount of messages to clear from the channel")] uint amount)
    {
        var messages = await Context.Channel.GetMessagesAsync().FlattenAsync();
        var safeMessages = messages.Where(x => x.CreatedAt - DateTimeOffset.Now < TimeSpan.FromDays(14))
            .TruncateList(amount);
        
        if (!safeMessages.Any())
        {
            var errorEmbed = new AuthwareEmbedBuilder()
                .WithDescription("**Error: **There is nothing for me to delete.")
                .WithFooter("Remember that I cannot delete messages older than 14 days")
                .Build();

            await Context.Interaction.FollowupAsync(embed: errorEmbed, ephemeral: true);
            return;
        }

        await ((SocketTextChannel) Context.Channel).DeleteMessagesAsync(safeMessages);

        var embed = new AuthwareEmbedBuilder()
            .WithDescription($"**{safeMessages.Count()}** message(s) have been deleted.")
            .Build();

        await Context.Interaction.FollowupAsync(embed: embed);
    }
}