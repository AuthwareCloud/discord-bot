﻿using Authware.Bot.Common;
using Authware.Bot.Common.Models;
using Authware.Bot.Commands.Utils;
using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.Player;

namespace Authware.Bot.Commands;

public class Music : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IAudioService _audioService;

    public Music(IAudioService audioService)
    {
        _audioService = audioService;
    }

    [SlashCommand("leave", "Leaves the voice channel, if connected")]
    public async Task LeaveAsync()
    {
        await Context.Interaction.DeferAsync();

        if (!await Context.IsUserInVoiceChannelAsync()) return;

        var player = _audioService.GetPlayer<QueuedLavalinkPlayer>(Context.Guild.Id);
        switch (player)
        {
            case null:
                return;
            case {State: PlayerState.Playing}:
                await player.StopAsync();
                break;
        }

        await player.DestroyAsync();
        await player.DisconnectAsync();

        await Context.Interaction.SuccessAsync("Bye!", "Left the voice channel!", false);
    }

    [SlashCommand("pause", "Pauses the current song")]
    public async Task PauseAsync()
    {
        await Context.Interaction.DeferAsync();

        if (!await Context.IsUserInVoiceChannelAsync()) return;

        var player = _audioService.GetPlayer<QueuedLavalinkPlayer>(Context.Guild.Id);

        if (player is not {State: PlayerState.Playing})
        {
            await Context.Interaction.ErrorAsync("Cannot pause",
                "There is currently no song to pause in the first place!", false);
            return;
        }
    }

    [SlashCommand("loop", "Toggles looping the current song")]
    public async Task LoopAsync(
        [Summary("state", "The new looping state to set to, by default it will toggle from off and on")]
        bool? state = null)
    {
        await Context.Interaction.DeferAsync();

        if (!await Context.IsUserInVoiceChannelAsync()) return;

        var player = _audioService.GetPlayer<QueuedLavalinkPlayer>(Context.Guild.Id);

        if (player is not {State: PlayerState.Playing})
        {
            await Context.Interaction.ErrorAsync("Cannot loop",
                "There is currently no song to loop in the first place!", false);
            return;
        }

        if (state is null) player.IsLooping = !player.IsLooping;
        else player.IsLooping = state.Value;

        if (player.IsLooping)
            await Context.Interaction.SuccessAsync("Looping!", "The current song is now playing on loop", false);
        else
            await Context.Interaction.SuccessAsync("Stopped looping", "The current song is no longer playing on loop",
                false);
    }

    [SlashCommand("replay", "Replays the current song")]
    public async Task ReplayAsync()
    {
        await Context.Interaction.DeferAsync();

        if (!await Context.IsUserInVoiceChannelAsync()) return;

        var player = _audioService.GetPlayer<QueuedLavalinkPlayer>(Context.Guild.Id);

        if (player is not {State: PlayerState.Playing})
        {
            await Context.Interaction.ErrorAsync("Cannot replay", "There are currently no songs queued!", false);
            return;
        }

        await player.ReplayAsync();

        await Context.Interaction.SuccessAsync("Replayed", "Replayed the current song!", false);
    }

    [SlashCommand("skip", "Skips to the next song in the queue")]
    public async Task SkipAsync([Summary("amount", "The amount of songs to skip, defaults at 1")] ushort amount = 1)
    {
        await Context.Interaction.DeferAsync();

        if (!await Context.IsUserInVoiceChannelAsync()) return;

        var player = _audioService.GetPlayer<QueuedLavalinkPlayer>(Context.Guild.Id);

        if (player is not {State: PlayerState.Playing} || player.Queue.IsEmpty)
        {
            await Context.Interaction.ErrorAsync("Cannot skip", "There are currently no songs queued!", false);
            return;
        }

        if (amount < 1)
        {
            await Context.Interaction.ErrorAsync("Cannot skip",
                "You must skip an amount of songs that is greater or equal to one!", false);
            return;
        }

        await player.SkipAsync(amount);

        await Context.Interaction.SuccessAsync("Skipped", $"Skipped *{amount}* song(s) ahead!", false);
    }

    [SlashCommand("queue", "Gets all the songs in the queue")]
    public async Task QueueAsync()
    {
        await Context.Interaction.DeferAsync();

        var player = _audioService.GetPlayer<QueuedLavalinkPlayer>(Context.Guild.Id);
        if (player is not {State: PlayerState.Playing})
        {
            await Context.Interaction.ErrorAsync("Cannot show queue", "There are currently no songs queued!", false);
            return;
        }

        var embed = new AuthwareEmbedBuilder()
            .WithTitle($"Queue - {player.Queue.Count} song(s)");

        // This adds the currently playing track to the list of fields for the song queue
        if (player.CurrentTrack?.Context is not TrackContext currentTrackContext) return;
        var currentTrackRequester = await Context.Client.GetUserAsync(currentTrackContext.RequesterId);

        embed.AddField($"> **Currently playing: {player.CurrentTrack?.Author} - {player.CurrentTrack?.Title}**",
            $"**Added by: **{currentTrackRequester.Mention}\n**Duration: **{player.CurrentTrack?.Duration.ToHms()}\n**Position: **{player.Position.Position.ToHms()}");

        // Loops through each song in the queue and adds it to the field list
        foreach (var track in player.Queue)
        {
            if (track.Context is not TrackContext trackContext) return;
            var user = await Context.Client.GetUserAsync(trackContext.RequesterId);

            embed.AddField($"> {track.Author} - {track.Title}",
                $"**Added by: **{user.Mention}\n**Duration: **{track.Duration.ToHms()}");
        }

        await Context.Interaction.FollowupAsync(embed: embed.Build());
    }

    [SlashCommand("stop", "Stops the currently playing song")]
    public async Task StopAsync()
    {
        await Context.Interaction.DeferAsync();

        if (!await Context.IsUserInVoiceChannelAsync()) return;

        var player = _audioService.GetPlayer<QueuedLavalinkPlayer>(Context.Guild.Id);

        if (player is not {State: PlayerState.Playing})
        {
            await Context.Interaction.ErrorAsync("Cannot stop playing", "There is currently no song playing!", false);
            return;
        }

        await player.StopAsync();

        await Context.Interaction.SuccessAsync("Stopped", "Stopped the currently playing song!", false);
    }

    [SlashCommand("play", "Plays the specific song")]
    public async Task PlayAsync([Summary("song", "The name of the song to queue")] string song)
    {
        await Context.Interaction.DeferAsync();

        var guildUser = Context.User as SocketGuildUser;
        if (!await Context.IsUserInVoiceChannelAsync()) return;

        var player = _audioService.GetPlayer<QueuedLavalinkPlayer>(Context.Guild.Id)
                     ?? await _audioService.JoinAsync<QueuedLavalinkPlayer>(Context.Guild.Id,
                         guildUser!.VoiceChannel.Id);

        var track = await _audioService.GetTrackAsync(song);

        if (track is null)
        {
            await Context.Interaction.ErrorAsync("Cannot play song", "Couldn't find anything from that song query!",
                false);
            return;
        }

        track.Context = new TrackContext
        {
            OriginalQuery = song,
            RequesterId = Context.User.Id
        };

        await player.PlayAsync(track);

        var embed = new AuthwareEmbedBuilder()
            .WithTitle($"{track.Author} - {track.Title}")
            .AddField("> Duration", track.Duration.ToHms())
            .AddField("> Provider", track.Provider)
            .WithUrl(track.Source)
            .Build();

        await Context.Interaction.FollowupAsync(embed: embed);
    }
}