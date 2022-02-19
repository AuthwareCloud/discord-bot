// using Authware.Bot.Common.Utils;
// using Authware.Bot.Common;
// using Authware.Bot.Services.Interfaces;
// using Discord;
// using Discord.Interactions;
// using Discord.WebSocket;
//
// namespace Authware.Bot.Commands;
//
// public class Moderation : InteractionModuleBase<SocketInteractionContext>
// {
//     private readonly IModerationService _moderationService;
//
//     public Moderation(IModerationService moderationService)
//     {
//         _moderationService = moderationService;
//     }
//
//     private async Task<bool> CanBeModerated(SocketGuildUser? user)
//     {
//         // Moderator, Enhanced Permissions, Administrator
//         if (user != null &&
//             user.Roles.Any(x => x.Id is 910893141085794304 or 912353594102132846 or 910893141085794304)) return false;
//         return user != null && user.Id != Context.Client.CurrentUser.Id && !user.IsBot;
//     }
//     
//     [RequireUserPermission(GuildPermission.MuteMembers)]
//     [RequireBotPermission(GuildPermission.Administrator)]
//     [RequireContext(ContextType.Guild)]
//     [SlashCommand("timeout", "Puts the specified user into timeout for a reason")]
//     public async Task TimeoutUserAsync([Summary("user", "The user to timeout")] IUser user,
//                                        [Summary("reason", "The reason they were timed out")]
//                                        string reason,
//                                        [Summary("minutes", "The amount of time in minutes they are timed out for")]
//                                        int time = 40032)
//     {
//         await Context.Interaction.DeferAsync();
//
//         var guildUser = user as SocketGuildUser;
//         if (!await CanBeModerated(guildUser) || guildUser.IsTimedOut())
//         {
//             await Context.Interaction.ErrorAsync("That user cannot be moderated due to permissions or other conditions.", false);
//             
//             return;
//         }
//
//         var caseNumber = await _moderationService.CreateCaseAsync(user, Context.Interaction.User, "Timeout", reason);
//
//         await guildUser?.SetTimeOutAsync(TimeSpan.FromMinutes(time));
//         
//         await Context.Interaction.SuccessAsync($"**{user.Username}#{user.Discriminator}** has been timed out (#{caseNumber}).",false);
//     }
//     
//     [RequireUserPermission(GuildPermission.MuteMembers)]
//     [RequireBotPermission(GuildPermission.Administrator)]
//     [RequireContext(ContextType.Guild)]
//     [SlashCommand("remove-timeout", "Takes the specified user out of timeout for a reason")]
//     public async Task RemoveTimeoutUserAsync([Summary("user", "The user to remove from timeout")] IUser user)
//     {
//         await Context.Interaction.DeferAsync();
//
//         var guildUser = user as SocketGuildUser;
//         if (!await CanBeModerated(guildUser) || !guildUser.IsTimedOut())
//         {
//             await Context.Interaction.ErrorAsync("That user cannot be moderated due to permissions or other conditions.", false);
//
//             return;
//         }
//
//         var caseNumber = await _moderationService.CreateCaseAsync(user, Context.Interaction.User, "Timeout removed", "No reason");
//
//         await guildUser.RemoveTimeOutAsync();
//
//         await Context.Interaction.SuccessAsync(
//             $"**{user.Username}#{user.Discriminator}** has been removed from timed out (#{caseNumber}).", false);
//     }
//
//     [RequireUserPermission(GuildPermission.KickMembers)]
//     [RequireBotPermission(GuildPermission.KickMembers)]
//     [RequireContext(ContextType.Guild)]
//     [SlashCommand("kick", "Kicks the specified user from the server for a reason")]
//     public async Task KickUserAsync([Summary("user", "The user to kick")] IUser user,
//                                     [Summary("reason", "The reason they were kicked")]
//                                     string reason)
//     {
//         await Context.Interaction.DeferAsync();
//
//         var guildUser = user as SocketGuildUser;
//
//         if (!await CanBeModerated(guildUser))
//         {
//             await Context.Interaction.ErrorAsync("That user cannot be moderated due to permissions or other conditions.", false);
//
//             return;
//         }
//
//         var caseNumber = await _moderationService.CreateCaseAsync(user, Context.Interaction.User, "Kick", reason);
//
//         await guildUser?.KickAsync(reason);
//
//         await Context.Interaction.SuccessAsync(
//             $"**{user.Username}#{user.Discriminator}** has been kicked (#{caseNumber}).", false);
//     }
//
//     [RequireUserPermission(GuildPermission.BanMembers)]
//     [RequireBotPermission(GuildPermission.BanMembers)]
//     [RequireContext(ContextType.Guild)]
//     [SlashCommand("unban", "Unbans the specified user from the server for a reason")]
//     public async Task UnbanAsync([Summary("username", "The username and discriminator of the user")] string username)
//     {
//         await Context.Interaction.DeferAsync();
//
//         var bans = await Context.Guild.GetBansAsync();
//         if (!bans.Any(x => x.User.Username.Contains(username, StringComparison.OrdinalIgnoreCase)))
//         {
//             await Context.Interaction.ErrorAsync("That user wasn't found in the server ban list.", false);
//             return;
//         }
//
//         var ban = bans.FirstOrDefault(x => x.User.Username.Contains(username, StringComparison.OrdinalIgnoreCase));
//         await Context.Guild.RemoveBanAsync(ban?.User);
//
//         await Context.Interaction.SuccessAsync($"**{ban?.User.Username}#{ban?.User.Discriminator}** has been unbanned.",
//                                                false);
//     }
//     
//     [RequireUserPermission(GuildPermission.BanMembers)]
//     [RequireBotPermission(GuildPermission.BanMembers)]
//     [RequireContext(ContextType.Guild)]
//     [SlashCommand("ban", "Bans the specified user from the server for a reason")]
//     public async Task BanUserAsync([Summary("user", "The user to ban")] IUser user,
//                                     [Summary("reason", "The reason they were ban")]
//                                     string reason)
//     {
//         await Context.Interaction.DeferAsync();
//
//         var guildUser = user as SocketGuildUser;
//
//         if (!await CanBeModerated(guildUser))
//         {
//             await Context.Interaction.ErrorAsync("That user cannot be moderated due to permissions or other conditions.", false);
//             return;
//         }
//
//         var caseNumber = await _moderationService.CreateCaseAsync(user, Context.Interaction.User, "Ban", reason);
//
//         await guildUser?.BanAsync(reason: reason);
//
//         await Context.Interaction.SuccessAsync(
//             $"**{user.Username}#{user.Discriminator}** has been banned (#{caseNumber}).", false);
//     }
// }