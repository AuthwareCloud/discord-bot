using System.Text.Json;
using Authware.Bot.Common;
using Authware.Bot.Common.Models;
using Authware.Bot.Common.Utils;
using Discord.Interactions;

namespace Authware.Bot.Commands;

public class AuthwareUtilities : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IHttpClientFactory _factory;

    public AuthwareUtilities(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    [SlashCommand("profile", "Gets your user profile from Authware, granted that you've linked your Discord")]
    public async Task ProfileAsync()
    {
        await Context.Interaction.DeferAsync();

        var client = _factory.CreateClient("authware");

        var response = await client.GetAsync($"/user/by-did/{Context.User.Id}");
        var json = JsonSerializer.Deserialize<AuthwareProfile>(await response.Content.ReadAsStringAsync());

        if (json.Code != 0)
            await Context.Interaction.ErrorAsync("Account not linked",
                "It seems like your Discord account hasn't been linked to Authware. In-order to use these types of commands, you need to link your account so we know who you are",
                false);

        var embed = new AuthwareEmbedBuilder()
            .WithTitle($"{json.UserName}")
            .WithThumbnailUrl(Context.User.GetAvatarUrl());

        embed.AddField("ID", json.Id);
        embed.AddField("Applications", json.AppCount);
        embed.AddField("APIs", json.ApiCount);
        embed.AddField("Users", json.UserCount);
        embed.AddField("Roles",
            json.Roles.Aggregate(string.Empty, (current, s) => current + $"{s}, ").TrimEnd(' ').TrimEnd(','));
        embed.AddField("Plan expiration", json.PlanExpire);
        embed.AddField("Account created at", json.DateCreated);

        await Context.Interaction.FollowupAsync(embed: embed.Build());
    }
}