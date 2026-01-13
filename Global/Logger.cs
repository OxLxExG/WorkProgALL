using Serilog;
using Serilog.Core;

namespace Global

{
    public static class Logger
    {
        public const Serilog.Events.LogEventLevel LevelStop = Serilog.Events.LogEventLevel.Fatal;
        public const Serilog.Events.LogEventLevel LevelDefaultTrace = Serilog.Events.LogEventLevel.Verbose;
        public const Serilog.Events.LogEventLevel LevelDefaultInfo = Serilog.Events.LogEventLevel.Information;
        public const Serilog.Events.LogEventLevel LevelDefaultError = Serilog.Events.LogEventLevel.Warning;

        public const Serilog.Events.LogEventLevel LevelMonitor = Serilog.Events.LogEventLevel.Debug;
        public const Serilog.Events.LogEventLevel LevelMonitorTx = Serilog.Events.LogEventLevel.Information;
        public static LoggingLevelSwitch TraceLevel { get; } = new(LevelDefaultTrace);
        static public LoggingLevelSwitch InfoLevel { get; }  = new(LevelDefaultInfo);
        static public LoggingLevelSwitch ErrorLevel { get; } = new(LevelDefaultError);
        static public ILogger? Trace { get; set; }
        static public ILogger? Info { get; set; }
    }
}
