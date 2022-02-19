using Lavalink4NET.Logging;
using ILogger = Serilog.ILogger;

namespace Authware.Bot.Services;

public class LavalinkLogger : Lavalink4NET.Logging.ILogger
{
    private readonly ILogger _logger;

    public LavalinkLogger(ILogger logger)
    {
        _logger = logger;
    }

    public void Log(object source, string message, LogLevel level = LogLevel.Information, Exception? exception = null) 
    { 
        if (exception is not null)
            _logger.Fatal(exception, "Exception thrown");
        else
            switch (level)
            {
                case LogLevel.Debug:
                    _logger.Debug(message);
                    break;
                case LogLevel.Error:
                    _logger.Error(message);
                    break;
                case LogLevel.Information:
                    _logger.Information(message);
                    break;
                case LogLevel.Trace:
                    _logger.Verbose(message);
                    break;
                case LogLevel.Warning:
                    _logger.Warning(message);
                    break;
            }
    }
}