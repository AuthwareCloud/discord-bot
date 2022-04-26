using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace Authware.Bot.Services;

public class LoggingService
{
    private readonly ILogger<LoggingService> _logger;

    public LoggingService(DiscordSocketClient client, InteractionService commands, ILogger<LoggingService> logger)
    {
        _logger = logger;
        client.Log += LogAsync;
        commands.Log += LogAsync;
    }

    private Task LogAsync(LogMessage message)
    {
        if (message.Exception is CommandException cmdException)
            _logger.LogCritical(cmdException, "Failed to execute command");
        else if (message.Exception is not null)
            _logger.LogCritical(message.Exception, "Exception thrown");
        else
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                    _logger.LogCritical(message.Message);
                    break;
                case LogSeverity.Debug:
                    _logger.LogDebug(message.Message);
                    break;
                case LogSeverity.Error:
                    _logger.LogError(message.Message);
                    break;
                case LogSeverity.Info:
                    _logger.LogInformation(message.Message);
                    break;
                case LogSeverity.Verbose:
                    _logger.LogTrace(message.Message);
                    break;
                case LogSeverity.Warning:
                    _logger.LogWarning(message.Message);
                    break;
            }

        return Task.CompletedTask;
    }
}