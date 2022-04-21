using System.Net;
using System.Text.Json;
using Authware.Bot.Common.Models;
using Discord;
using Discord.Interactions;
using FuzzySharp;

namespace Authware.Bot.Services.AutocompleteHandlers;

public class AppIdAutocomplete : AutocompleteHandler
{
    private readonly IHttpClientFactory _factory;

    public AppIdAutocomplete(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter, IServiceProvider services)
    {
        using var client = _factory.CreateClient("authware");

        var response = await client.GetAsync($"/apps/by-did/{context.User.Id}");
        if (response.StatusCode != HttpStatusCode.OK)
            return AutocompletionResult.FromError(InteractionCommandError.UnmetPrecondition, "You need to link your Discord");

        var json = JsonSerializer.Deserialize<List<AuthwareApplication>>(await response.Content.ReadAsStringAsync());
        if (json is null) return AutocompletionResult.FromError(InteractionCommandError.UnmetPrecondition, "You need to link your Discord");

        var results = new List<AutocompleteResult>();
        var allResults = json.Select(x => new AutocompleteResult(x.Name, x.Id.ToString()));

        foreach (var result in allResults)
        {
            if (!string.IsNullOrWhiteSpace(autocompleteInteraction.Data.Current.Value.ToString()))
            {
                if (Fuzz.PartialRatio(result.Name.ToLower(),
                        autocompleteInteraction.Data.Current.Value.ToString()?.ToLower()) <= 55 && !result.Value
                        .ToString().ToLower()
                        .StartsWith(autocompleteInteraction.Data.Current.Value.ToString()?.ToLower()))
                    continue;
                results.Add(result);
            }
            else
            {
                results.Add(result);
            }
        }
        
        return AutocompletionResult.FromSuccess(results);
    }
}