using Authware.Bot.Common.Utils;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace Authware.Bot.Common;

public static class ExtensionMethods
{
    public static IEnumerable<T> TruncateList<T>(this IEnumerable<T> list, uint amount)
    {
        var index = 0;
        var newList = new List<T>();
        foreach (var item in list.TakeWhile(item => index != amount))
        {
            newList.Add(item);
            index++;
        }

        return newList;
    }

    public static bool IsHomeGuild(this IConfiguration configuration, ulong guildId)
    {
#if DEBUG
        var homeGuildId = configuration.GetValue<ulong>("TestingGuildId");
#else
        var homeGuildId = configuration.GetValue<ulong>("GuildId");
#endif

        return homeGuildId == guildId;
    }

    public static string ToHms(this TimeSpan time)
    {
        var converted = string.Empty;
        converted += time.Days switch
        {
            0 => "",
            1 => $"{time.Days} day ",
            _ => $"{time.Days} days "
        };
        converted += time.Hours switch
        {
            0 => "",
            1 => $"{time.Hours} hour ",
            _ => $"{time.Hours} hours "
        };
        converted += time.Minutes == 0 ? "" :
            time.Seconds == 0 ? time.Minutes == 1 ? $"{time.Minutes} minute " : $"{time.Minutes} minutes " :
            time.Minutes == 1 ? $"{time.Minutes} minute and " : $"{time.Minutes} minutes and ";
        converted += time.Seconds switch
        {
            0 => "",
            1 => $"{time.Seconds} second",
            _ => $"{time.Seconds} seconds"
        };
        return converted;
    }

    public static bool IsTimedOut(this IGuildUser user)
    {
        return user.TimedOutUntil > DateTimeOffset.Now;
    }

    public static async Task SuccessAsync(this SocketInteraction context, string title, string message, bool ephemeral)
    {
        var embed = new AuthwareEmbedBuilder()
            .WithTitle(title)
            .WithDescription(message)
            .Build();

        await context.FollowupAsync(embed: embed, ephemeral: ephemeral);
    }

    public static async Task ErrorAsync(this SocketInteraction context, string title, string message, bool ephemeral)
    {
        var embed = new AuthwareEmbedBuilder()
            .WithTitle(title)
            .WithDescription(message)
            .Build();

        await context.FollowupAsync(embed: embed, ephemeral: ephemeral);
    }
}