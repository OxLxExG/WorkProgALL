using Karambolo.Extensions.Logging.File;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileLogging
{
    [ProviderAlias("TraceFile")] // use this alias in appsettings.json to configure this provider
    public class TraceFileLoggerProvider : FileLoggerProvider
    {
        public TraceFileLoggerProvider(FileLoggerContext context, IOptionsMonitor<FileLoggerOptions> options, string optionsName) : base(context, options, optionsName) { }

        protected override FileLogger CreateLoggerCore(string categoryName)
        {
            // we instantiate our derived file logger which is modified to log only messages with log level information or below
            return new TraceFileLogger(categoryName, Processor, Settings, GetScopeProvider(), Context.GetTimestamp);
        }
    }

    class TraceFileLogger : FileLogger
    {
        public TraceFileLogger(string categoryName, IFileLoggerProcessor processor, IFileLoggerSettings settings, IExternalScopeProvider? scopeProvider = null, Func<DateTimeOffset>? timestampGetter = null)
            : base(categoryName, processor, settings, scopeProvider, timestampGetter) { }

        public override bool IsEnabled(LogLevel logLevel)
        {
            return
                logLevel <= LogLevel.Debug &&  // don't allow messages more severe than information to pass through
                base.IsEnabled(logLevel);
        }
    }
    [ProviderAlias("InfoFile")] // use this alias in appsettings.json to configure this provider
    public class InfoFileLoggerProvider : FileLoggerProvider
    {
        public InfoFileLoggerProvider(FileLoggerContext context, IOptionsMonitor<FileLoggerOptions> options, string optionsName) : base(context, options, optionsName) { }

        protected override FileLogger CreateLoggerCore(string categoryName)
        {
            // we instantiate our derived file logger which is modified to log only messages with log level information or below
            return new InfoFileLogger(categoryName, Processor, Settings, GetScopeProvider(), Context.GetTimestamp);
        }
    }

    public class InfoFileLogger : FileLogger
    {
        public InfoFileLogger(string categoryName, IFileLoggerProcessor processor, IFileLoggerSettings settings, IExternalScopeProvider? scopeProvider = null, Func<DateTimeOffset>? timestampGetter = null)
            : base(categoryName, processor, settings, scopeProvider, timestampGetter) { }

        public override bool IsEnabled(LogLevel logLevel)
        {
            return
                logLevel == LogLevel.Information &&  // don't allow messages more severe than information to pass through
                base.IsEnabled(logLevel);
        }
    }

}
