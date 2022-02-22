using Discord;

namespace Authware.Bot.Commands.Utils;

public class AuthwareEmbedBuilder : EmbedBuilder
{
    public AuthwareEmbedBuilder()
    {
        WithFooter("https://authware.org");
        WithCurrentTimestamp();
        WithColor(new Color(68, 180, 112));
    }
}