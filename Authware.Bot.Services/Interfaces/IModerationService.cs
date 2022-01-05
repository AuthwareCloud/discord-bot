using Discord;
using Discord.WebSocket;

namespace Authware.Bot.Services.Interfaces;

public interface IModerationService
{
    Task<int> CreateCaseAsync(IUser user, IUser moderatingUser, string action, string reason);
    Task<int> WarnUserAsync(SocketGuildUser user, IUser moderatingUser, string reason);
}