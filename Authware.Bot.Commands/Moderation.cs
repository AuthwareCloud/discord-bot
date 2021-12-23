using Authware.Bot.Commands.Utils;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Authware.Bot.Commands;

public class Moderation : InteractionModuleBase<SocketInteractionContext>
{
    private async Task<bool> CanBeModerated(SocketGuildUser? user)
    {
        // Moderator, Enhanced Permissions, Administrator
        if (user != null &&
            user.Roles.Any(x => x.Id is 910893141085794304 or 912353594102132846 or 910893141085794304)) return false;
        return user != null && user.Id != Context.Client.CurrentUser.Id && !user.IsBot;
    }

    [RequireUserPermission(GuildPermission.KickMembers)]
    [RequireBotPermission(GuildPermission.KickMembers)]
    [RequireContext(ContextType.Guild)]
    [SlashCommand("kick", "Kicks the specified user from the server for a reason")]
    public async Task KickUserAsync([Summary("user", "The user to kick")] IUser user,
        [Summary("reason", "The reason they were kicked")]
        string reason)
    {
        if (!await CanBeModerated(user as SocketGuildUser))
        {
            var errorEmbed = new AuthwareEmbedBuilder()
                .WithDescription("**Error: **That user cannot be moderated due to permissions or other conditions.")
                .Build();
            
            await Context.Interaction.FollowupAsync(embed: errorEmbed);
            return;
        }

        var embed = new AuthwareEmbedBuilder()
            .WithDescription($"**{user.Username}#{user.Discriminator}** has been kicked.")
            .Build();

        await Context.Interaction.FollowupAsync(embed: embed);
    }
}