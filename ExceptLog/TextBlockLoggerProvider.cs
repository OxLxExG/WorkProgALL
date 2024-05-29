using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace TextBlockLogging
{
     /// <summary>
    /// A provider of <see cref="ConsoleLogger"/> instances.
    /// </summary>
   // [UnsupportedOSPlatform("browser")]
    //[ProviderAlias("TextBlock")]
    public class TextBlockLoggerProvider : ILoggerProvider, ISupportExternalScope
    {

        public static string Alias() => "TextBlock";
        private readonly IOptionsMonitor<TextBlockOptions> _options;
            private readonly ConcurrentDictionary<string, TextBlockLogger> _loggers;
            private TextBlockLoggerFormatter _formatter;
            //private readonly ConcurrentDictionary<string, TextBlockProcessor> _messageQueues;
            private readonly TextBlockProcessor _messageQueue;

            private IDisposable? _optionsReloadToken;
            private IExternalScopeProvider _scopeProvider = NullExternalScopeProvider.Instance;

            /// <summary>
            /// Creates an instance of <see cref="ConsoleLoggerProvider"/>.
            /// </summary>
            /// <param name="options">The options to create <see cref="ConsoleLogger"/> instances with.</param>
            public TextBlockLoggerProvider(IOptionsMonitor<TextBlockOptions> options, ILogTextBlockService consoleService)
                :this(options, consoleService, null!) { }

            /// <summary>
            /// Creates an instance of <see cref="ConsoleLoggerProvider"/>.
            /// </summary>
            /// <param name="options">The options to create <see cref="ConsoleLogger"/> instances with.</param>
            /// <param name="formatters">Log formatters added for <see cref="ConsoleLogger"/> insteaces.</param>
            public TextBlockLoggerProvider(IOptionsMonitor<TextBlockOptions> options,
                   ILogTextBlockService consoleService, TextBlockFormatter formatter)
            {
                _options = options;
                _loggers = new ConcurrentDictionary<string, TextBlockLogger>();
                SetFormatters((TextBlockLoggerFormatter)formatter);
            //IConsole? console;
            //IConsole? errorConsole;
            //if (DoesConsoleSupportAnsi())
            //{
            //    console = new AnsiLogConsole();
            //    errorConsole = new AnsiLogConsole(stdErr: true);
            //}
            //else
            //{
            //    console = new AnsiParsingLogConsole();
            //    errorConsole = new AnsiParsingLogConsole(stdErr: true);
            //}
            var s = GetType().ToString();
            var o = options.Get(s);
            _messageQueue = new TextBlockProcessor(
                o.ConsoleCategoty,
                consoleService,
                o.QueueFullMode,
                o.MaxQueueLength,
                o.MaxTextBlockLength); ;

                ReloadLoggerOptions(o, s);
                _optionsReloadToken = _options.OnChange(ReloadLoggerOptions);
            }

            //[UnsupportedOSPlatformGuard("windows")]
            //private static bool DoesConsoleSupportAnsi()
            //{
            //    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            //    {
            //        return true;
            //    }

            //    // for Windows, check the console mode
            //    var stdOutHandle = Interop.Kernel32.GetStdHandle(Interop.Kernel32.STD_OUTPUT_HANDLE);
            //    if (!Interop.Kernel32.GetConsoleMode(stdOutHandle, out int consoleMode))
            //    {
            //        return false;
            //    }

            //    return (consoleMode & Interop.Kernel32.ENABLE_VIRTUAL_TERMINAL_PROCESSING) == Interop.Kernel32.ENABLE_VIRTUAL_TERMINAL_PROCESSING;
            //}

            [MemberNotNull(nameof(_formatter))]
            private void SetFormatters(TextBlockLoggerFormatter? formatter = null)
            {
                if (formatter != null) _formatter = formatter;
                _formatter ??= new TextBlockLoggerFormatter(GetType().ToString(),
                    new FormatterOptionsMonitor<TextBlockLoggerFormatterOptions>(new TextBlockLoggerFormatterOptions())); 
            }

            // warning:  ReloadLoggerOptions can be called before the ctor completed,... before registering all of the state used in this method need to be initialized
            private void ReloadLoggerOptions(TextBlockOptions options, string? name)
            {
//                if (options.FormatterName == null || !_formatters.TryGetValue(options.FormatterName, out ConsoleFormatter? logFormatter))
//                {
//#pragma warning disable CS0618
//                    logFormatter = options.Format switch
//                    {
//                        ConsoleLoggerFormat.Systemd => _formatters[ConsoleFormatterNames.Systemd],
//                        _ => _formatters[ConsoleFormatterNames.Simple],
//                    };
//                    if (options.FormatterName == null)
//                    {
//                        UpdateFormatterOptions(logFormatter, options);
//                    }
//#pragma warning restore CS0618
//                }
            if (name == GetType().ToString())
            {
                _messageQueue.ConsoleCategory = options.ConsoleCategoty;
                _messageQueue.FullMode = options.QueueFullMode;
                _messageQueue.MaxQueueLength = options.MaxQueueLength;
                _messageQueue.MaxTextBlockLength = options.MaxTextBlockLength;
                foreach (KeyValuePair<string, TextBlockLogger> logger in _loggers)
                {
                    logger.Value.Options = options;
                    //logger.Value.Formatter = logFormatter;
                }
            }
        }



        protected virtual TextBlockLogger CreateLoggerCore(string name,TextBlockProcessor loggerProcessor,TextBlockLoggerFormatter formatter,
            IExternalScopeProvider? scopeProvider,
            TextBlockOptions options)
    {
            return new TextBlockLogger(name, loggerProcessor, formatter, scopeProvider, options);
    }
            /// <inheritdoc />
            public ILogger CreateLogger(string name)
            {
//                if (_options.CurrentValue.FormatterName == null || !_formatters.TryGetValue(_options.CurrentValue.FormatterName, out ConsoleFormatter? logFormatter))
//                {
//#pragma warning disable CS0618
//                    logFormatter = _options.CurrentValue.Format switch
//                    {
//                        ConsoleLoggerFormat.Systemd => _formatters[ConsoleFormatterNames.Systemd],
//                        _ => _formatters[ConsoleFormatterNames.Simple],
//                    };
//#pragma warning restore CS0618

//                    if (_options.CurrentValue.FormatterName == null)
//                    {
//                        UpdateFormatterOptions(logFormatter, _options.CurrentValue);
//                    }
//                }

                return _loggers.TryGetValue(name, out TextBlockLogger? logger) ?
                    logger :
                    _loggers.GetOrAdd(name, CreateLoggerCore(name, _messageQueue, _formatter, _scopeProvider, _options.CurrentValue));
            }

//#pragma warning disable CS0618
//            private static void UpdateFormatterOptions(ConsoleFormatter formatter, ConsoleLoggerOptions deprecatedFromOptions)
//            {
//                // kept for deprecated apis:
//                if (formatter is SimpleConsoleFormatter defaultFormatter)
//                {
//                    defaultFormatter.FormatterOptions = new SimpleConsoleFormatterOptions()
//                    {
//                        ColorBehavior = deprecatedFromOptions.DisableColors ? LoggerColorBehavior.Disabled : LoggerColorBehavior.Default,
//                        IncludeScopes = deprecatedFromOptions.IncludeScopes,
//                        TimestampFormat = deprecatedFromOptions.TimestampFormat,
//                        UseUtcTimestamp = deprecatedFromOptions.UseUtcTimestamp,
//                    };
//                }
//                else
//                if (formatter is SystemdConsoleFormatter systemdFormatter)
//                {
//                    systemdFormatter.FormatterOptions = new ConsoleFormatterOptions()
//                    {
//                        IncludeScopes = deprecatedFromOptions.IncludeScopes,
//                        TimestampFormat = deprecatedFromOptions.TimestampFormat,
//                        UseUtcTimestamp = deprecatedFromOptions.UseUtcTimestamp,
//                    };
//                }
//            }
//#pragma warning restore CS0618

            /// <inheritdoc />
            public void Dispose()
            {
                _optionsReloadToken?.Dispose();
                _messageQueue.Dispose();
            }

            /// <inheritdoc />
            public void SetScopeProvider(IExternalScopeProvider scopeProvider)
            {
                _scopeProvider = scopeProvider;

                foreach (KeyValuePair<string, TextBlockLogger> logger in _loggers)
                {
                    logger.Value.ScopeProvider = _scopeProvider;
                }
            }
        }

    [ProviderAlias("InfoTextBlock")]
    public class InfoTextBlockLoggerProvider : TextBlockLoggerProvider
    {
        public new const string Alias = "InfoTextBlock";
        public InfoTextBlockLoggerProvider(IOptionsMonitor<TextBlockOptions> options,
                   ILogTextBlockService consoleService, InfoTextBlockLoggerFormatter formatter) : base(options, consoleService, formatter) { }

        protected override TextBlockLogger CreateLoggerCore(string name, TextBlockProcessor loggerProcessor, TextBlockLoggerFormatter formatter, IExternalScopeProvider? scopeProvider, TextBlockOptions options)
        {
            return new InfoTextBlockLogger(name, loggerProcessor, formatter, scopeProvider, options);
        }
    }
    [ProviderAlias("TraceTextBlock")]
    public class TraceTextBlockLoggerProvider : TextBlockLoggerProvider
    {
        public new const string Alias = "TraceTextBlock";
        public TraceTextBlockLoggerProvider(IOptionsMonitor<TextBlockOptions> options,
                   ILogTextBlockService consoleService, TraceTextBlockLoggerFormatter formatter) : base(options, consoleService, formatter) { }

        protected override TextBlockLogger CreateLoggerCore(string name, TextBlockProcessor loggerProcessor, TextBlockLoggerFormatter formatter, IExternalScopeProvider? scopeProvider, TextBlockOptions options)
        {
            return new TraceTextBlockLogger(name, loggerProcessor, formatter, scopeProvider, options);
        }
    }
    [ProviderAlias("ExceptionTextBlock")]
    public class ExcTextBlockLoggerProvider : TextBlockLoggerProvider
    {
        public new const string Alias = "EcxeptionTextBlock";
        public ExcTextBlockLoggerProvider(IOptionsMonitor<TextBlockOptions> options,
                   ILogTextBlockService consoleService, ExcTextBlockLoggerFormatter formatter) : base(options, consoleService, formatter) { }

        protected override TextBlockLogger CreateLoggerCore(string name, TextBlockProcessor loggerProcessor, TextBlockLoggerFormatter formatter, IExternalScopeProvider? scopeProvider, TextBlockOptions options)
        {
            return new TextBlockLogger(name, loggerProcessor, formatter, scopeProvider, options);
        }
    }

    internal sealed class FormatterOptionsMonitor
        <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TOptions> :
        IOptionsMonitor<TOptions>
        where TOptions : ConsoleFormatterOptions
    {
        private TOptions _options;
        public FormatterOptionsMonitor(TOptions options)
        {
            _options = options;
        }

        public TOptions Get(string? name) => _options;

        public IDisposable? OnChange(Action<TOptions, string> listener)
        {
            return null;
        }

        public TOptions CurrentValue => _options;
    }

    /// <summary>
    /// Scope provider that does nothing.
    /// </summary>
    internal sealed class NullExternalScopeProvider : IExternalScopeProvider
    {
        private NullExternalScopeProvider()
        {
        }

        /// <summary>
        /// Returns a cached instance of <see cref="NullExternalScopeProvider"/>.
        /// </summary>
        public static IExternalScopeProvider Instance { get; } = new NullExternalScopeProvider();

        /// <inheritdoc />
        void IExternalScopeProvider.ForEachScope<TState>(Action<object?, TState> callback, TState state)
        {
        }

        /// <inheritdoc />
        IDisposable IExternalScopeProvider.Push(object? state)
        {
            return NullScope.Instance;
        }
    }

}
