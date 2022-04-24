using System.Net;
using System.Text;
using System.Text.Json;
using Authware.Bot.Common;
using Authware.Bot.Common.Models;
using Authware.Bot.Common.Utils;
using Authware.Bot.Services.AutocompleteHandlers;
using Discord.Interactions;

namespace Authware.Bot.Commands;

public class AuthwareUtilities : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IHttpClientFactory _factory;

    public AuthwareUtilities(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    [SlashCommand("generate-license-key",
        "Generates a license key for the selected application, granted that you've linked your Discord")]
    public async Task GenerateLicenseKeyAsync([Autocomplete(typeof(AppIdAutocomplete))] [Summary("app-id", "The ID of the app to target")] string appId, 
        [Autocomplete(typeof(RoleIdAutocomplete))] [Summary("role-id", "The ID of the role to create the license key with")] string? roleId = null, 
        [Summary("unit", "The time picker unit to set for the users expiration")] TimePickerUnit? unit = null,
        [Summary("time", "The amount of time for the picked unit, ensure that the previous unit is set or issues may arise")] uint? time = null)
    {
        await Context.Interaction.DeferAsync(true);

        var client = _factory.CreateClient("authware");
        var appResponse = await client.GetAsync($"/apps/by-did/{Context.User.Id}");
        if (appResponse.StatusCode != HttpStatusCode.OK)
        {
            // Error apps not able to be fetched
            await Context.Interaction.ErrorAsync("Couldn't get apps",
                "Make sure that your Discord account is linked to your Authware account", true);
            return;
        }
        
        var applications = JsonSerializer.Deserialize<List<AuthwareApplication>>(await appResponse.Content.ReadAsStringAsync());
        if (applications is null)
        {
            // Error apps not able to be fetched
            await Context.Interaction.ErrorAsync("Couldn't get apps",
                "Make sure that your Discord account is linked to your Authware account", true);
            return;
        }

        var targetApplication = applications.FirstOrDefault(x => x.Id.ToString() == appId);
        if (targetApplication is null)
        {
            // Error app not found
            await Context.Interaction.ErrorAsync("Couldn't find app",
                "Make sure that the app ID specified is valid", true);
            return;
        }

        if (targetApplication.Roles.All(x => x.Id.ToString() != roleId))
        {
            // Error role not found
            await Context.Interaction.ErrorAsync("Couldn't find role",
                "Make sure that the role ID specified is valid", true);
            return;
        }

        var urlBuilder = new StringBuilder();
        var appends = 0;
        if (!string.IsNullOrWhiteSpace(roleId))
        {
            urlBuilder.Append($"?roleId={roleId}");
            appends++;
        }
        
        if (unit is not null && time is not null)
        {
            urlBuilder.Append(appends > 0
                ? $"&expiryUnit={(int) unit}&expiryTime={time}"
                : $"?expiryUnit={(int) unit}&expiryTime={time}");
        }
        
        var response = await client.GetAsync(
            $"commerce/{targetApplication?.SellerApiKey}{urlBuilder}");
        if (response.StatusCode != HttpStatusCode.OK)
        {
            // Error seller key gen failed
            await Context.Interaction.ErrorAsync("Couldn't generate key",
                "Are you sure you have access to the Seller API (Basic plan or above)?", true);
            return;
        }

        var embed = new Utils.AuthwareEmbedBuilder()
            .WithTitle("Key generated")
            .WithDescription(await response.Content.ReadAsStringAsync());

        await Context.Interaction.FollowupAsync(embed: embed.Build());
    }

    [SlashCommand("profile", "Gets your user profile from Authware, granted that you've linked your Discord")]
    public async Task ProfileAsync()
    {
        await Context.Interaction.DeferAsync();

        var client = _factory.CreateClient("authware");

        var response = await client.GetAsync($"/user/by-did/{Context.User.Id}");
        var json = JsonSerializer.Deserialize<AuthwareProfile>(await response.Content.ReadAsStringAsync());

        if (json is null || json.Code != 0)
            await Context.Interaction.ErrorAsync("Account not linked",
                "It seems like your Discord account hasn't been linked to Authware. In-order to use these types of commands, you need to link your account so we know who you are",
                false);

        var embed = new AuthwareEmbedBuilder()
            .WithTitle($"{json?.UserName}")
            .WithThumbnailUrl(Context.User.GetAvatarUrl());
        
        if (json is not null)
        {
            embed.AddField("> ID", json.Id);
            embed.AddField("> Applications", json.AppCount);
            embed.AddField("> APIs", json.ApiCount);
            embed.AddField("> Users", json.UserCount);
            embed.AddField("> Roles",
                json.Roles.Aggregate(string.Empty, (current, s) => current + $"{s}, ").TrimEnd(' ').TrimEnd(','));
            embed.AddField("> Plan expiration", $"<t:{new DateTimeOffset(json.PlanExpire).ToUnixTimeSeconds()}:R>");
            embed.AddField("> Account created at", $"<t:{new DateTimeOffset(json.DateCreated).ToUnixTimeSeconds()}:R>");
        }

        await Context.Interaction.FollowupAsync(embed: embed.Build());
    }
}