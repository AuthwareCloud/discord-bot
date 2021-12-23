using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;

namespace Authware.Bot.Services;

public class LoggingService
{
    public LoggingService(DiscordSocketClient client, InteractionService commands)
    {
        client.Log += LogAsync;
        commands.Log += LogAsync;
    }

    private static Task LogAsync(LogMessage message)
    {
        if (message.Exception is CommandException cmdException)
        {
            Console.WriteLine($"[{message.Source}/{message.Severity}] {cmdException.Command.Aliases[0]}"
                              + $" failed to execute in {cmdException.Context.Channel}.");
            Console.WriteLine(cmdException);
        }
        else if (message.Exception is not null)
        {
            Console.WriteLine($"[{message.Source}/{message.Severity}] {message.Exception}");
        }
        else
        {
            Console.WriteLine("[{0}/{1}] {2}", message.Source, message.Severity, message.Message);
        }

        return Task.CompletedTask;
    }
}