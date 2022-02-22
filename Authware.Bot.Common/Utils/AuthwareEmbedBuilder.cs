using Discord;

namespace Authware.Bot.Common.Utils;

public class AuthwareEmbedBuilder : EmbedBuilder
{
    public AuthwareEmbedBuilder()
    {
        WithFooter("https://authware.org");
        WithCurrentTimestamp();
        WithColor(new Color(68, 180, 112));
    }
}