using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Serilog;

namespace Authware.Bot.Services;

public class LoggingService
{
    private readonly ILogger _logger;

    public LoggingService(DiscordSocketClient client, InteractionService commands, ILogger logger)
    {
        _logger = logger;
        client.Log += LogAsync;
        commands.Log += LogAsync;
    }

    private Task LogAsync(LogMessage message)
    {
        if (message.Exception is CommandException cmdException)
            _logger.Fatal(cmdException, "Failed to execute command");
        else if (message.Exception is not null)
            _logger.Fatal(message.Exception, "Exception thrown");
        else
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                    _logger.Fatal(message.Message);
                    break;
                case LogSeverity.Debug:
                    _logger.Debug(message.Message);
                    break;
                case LogSeverity.Error:
                    _logger.Error(message.Message);
                    break;
                case LogSeverity.Info:
                    _logger.Information(message.Message);
                    break;
                case LogSeverity.Verbose:
                    _logger.Verbose(message.Message);
                    break;
                case LogSeverity.Warning:
                    _logger.Warning(message.Message);
                    break;
            }

        return Task.CompletedTask;
    }
}