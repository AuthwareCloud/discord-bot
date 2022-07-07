using Authware.Bot.Common;
using Discord.Interactions;
using Discord.WebSocket;

namespace Authware.Bot.Commands.Utils;

public static class ExtensionMethods
{
    public static async Task<bool> IsUserInVoiceChannelAsync(this SocketInteractionContext context)
    {
        if (context.User is not SocketGuildUser guildUser) return false;
        if (guildUser.VoiceChannel is not null) return true;
        
        await context.NoMusicPermissionsAsync();
        return false;
    }

    public static async Task NoMusicPermissionsAsync(this SocketInteractionContext context)
    {
        await context.Interaction.ErrorAsync("Cannot perform that action!", "You must be in a voice channel to control the music",
            false);
    }
}