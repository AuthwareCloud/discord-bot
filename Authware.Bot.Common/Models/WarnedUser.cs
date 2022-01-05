using Discord.WebSocket;

namespace Authware.Bot.Common.Models;

public class WarnedUser
{
    public int WarnCount { get; set; }
    public bool IsBeingPunished { get; set; }
    public SocketGuildUser User { get; set; }
}