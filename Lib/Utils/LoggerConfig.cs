using NLog;
using NLog.Config;
using NLog.Targets;

namespace LiveChatLib2.Utils;

internal static class LoggerConfig
{
    public static void InitLogger()
    {
        var config = new LoggingConfiguration();
        var layout = @"${longdate} | ${pad:padding=5:fixedlength=true:inner=${level:uppercase=true}} | [${logger:shortName=true}] ${message} ${onexception:${newline}${IndentException}} ${newline}";

        // Add file target
        var fileTarget = new FileTarget();
        config.AddTarget("file", fileTarget);
        fileTarget.FileName = "${basedir}/logs/${shortdate}.log";
        fileTarget.Layout = layout;

        // Add Console Target
        var consoleTarget = new ColoredConsoleTarget();
        config.AddTarget("console", consoleTarget);
        consoleTarget.Layout = layout;

        // Add rules
        config.LoggingRules.Add(new LoggingRule("*", NLog.LogLevel.Trace, consoleTarget));
        config.LoggingRules.Add(new LoggingRule("*", NLog.LogLevel.Info, fileTarget));

        LogManager.Configuration = config;
    }
}
