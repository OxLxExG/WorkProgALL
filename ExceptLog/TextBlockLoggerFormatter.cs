using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media;
using System.Diagnostics;
using ExceptionExtensions;

namespace TextBlockLogging
{
    public class TextBlockLoggerFormatterOptions: SimpleConsoleFormatterOptions
    {
        public TextBlockLoggerFormatterOptions() { }

        public int MaxTextBlockLength { get; set; } = 30;
    }

    public class TextBlockFormatter
    {
        public TextBlockFormatter(string optionName, IOptionsMonitor<TextBlockLoggerFormatterOptions> options) { }
    }

    public class TextBlockLoggerFormatter : TextBlockFormatter
    {
        private const string LoglevelPadding = ": ";
        private static readonly string _messagePadding = new string(' ', GetLogLevelString(LogLevel.Information).Length + LoglevelPadding.Length);
        private static readonly string _newLineWithMessagePadding = Environment.NewLine + _messagePadding;
        private IDisposable? _optionsReloadToken;
        private string opt; 

        public TextBlockLoggerFormatter(string optionName, IOptionsMonitor<TextBlockLoggerFormatterOptions> options)
            : base(optionName, options) 
        {
            opt = optionName;
            FormatterOptions = options.Get(optionName);
            //ReloadLoggerOptions(options.Get(optionName), opt);
            _optionsReloadToken = options.OnChange(ReloadLoggerOptions);
        }

        //[MemberNotNull(nameof(FormatterOptions))]
        private void ReloadLoggerOptions(TextBlockLoggerFormatterOptions options, string? name)
        {
            if (opt == name)
            {
                FormatterOptions = options;
            }
        }

        public void Dispose()
        {
            _optionsReloadToken?.Dispose();
        }

        internal TextBlockLoggerFormatterOptions FormatterOptions { get; set; }

        public void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, List<InlineStr> writer)
        {
            string message = logEntry.Formatter(logEntry.State, logEntry.Exception);

            var msk = Convert.ToByte(logEntry.EventId.Id) & 0b0100;

            if (logEntry.EventId != 0 && msk == 0)
            {
                return;
            }
            if (logEntry.Exception == null && message == null)
            {
                return;
            }
            LogLevel logLevel = logEntry.LogLevel;
            ConsoleColors logLevelColors = GetLogLevelConsoleColors(logLevel);
            string logLevelString = GetLogLevelString(logLevel);

            string? timestamp = null;
            string? timestampFormat = FormatterOptions.TimestampFormat;
            if (timestampFormat != null)
            {
                DateTimeOffset dateTimeOffset = GetCurrentDateTime();
                timestamp = dateTimeOffset.ToString(timestampFormat);
            }
            if (timestamp != null)
            {
                writer.Add(new InlineStr(timestamp) { Fore = Brushes.Blue });
            }
            if (logLevelString != null)
            {
                writer.Add(new InlineStr(logLevelString) { Back = logLevelColors.Background, Fore = logLevelColors.Foreground });
            }
            CreateDefaultLogMessage(writer, logEntry, message, scopeProvider);
        }

        private void CreateDefaultLogMessage<TState>(List<InlineStr> writer, in LogEntry<TState> logEntry, string message, IExternalScopeProvider? scopeProvider)
        {
            bool singleLine = FormatterOptions.SingleLine;
            int eventId = logEntry.EventId.Id;
            Exception? exception = logEntry.Exception;

            // Example:
            // info: ConsoleApp.Program[10]
            //       Request received

            // category and event id
            writer.Add(new InlineStr(LoglevelPadding + logEntry.Category + "[") { Fore = Brushes.Gray });
            writer.Add(new InlineStr(eventId.ToString(), true));
            writer.Add(new InlineStr("] ") { Fore = Brushes.Gray });
            // scope information
            WriteScopeInformation(writer, scopeProvider, true);// singleLine);
            if (!singleLine)
            {
                writer.Add(new InlineStr());
            }

            // Example:
            // System.InvalidOperationException
            //    at Namespace.Class.Function() in File:line X
            if (exception != null)
            {
                // exception message
                WriteExceptionMessage(writer, exception, eventId);
            }
            else
            {
                WriteMessage(writer, message, singleLine);
            }
            if (singleLine)
            {
                writer.Add(new InlineStr());
            }
        }

        private void WriteExceptionMessage(List<InlineStr> writer, Exception exception, int eventId)
        {

            var flags = (ExceptionLogFlags)eventId;
            string InnerExceptionPrefix = " ---> ";
            string className = exception.GetType().ToString();
            string? message = exception.Message;
            string innerExceptionString = exception.InnerException?.ToString() ?? "";
            //string endOfInnerExceptionResource = SR.Exception_EndOfInnerExceptionStack;
            string? stackTrace = exception.StackTrace;

            // Calculate result string length
            int length = className.Length;
            //checked
            //{
            //    if (!string.IsNullOrEmpty(message))
            //    {
            //        length += 2 + message.Length;
            //    }
            //    if (exception.InnerException != null)
            //    {
            //        length += Environment.NewLineConst.Length + InnerExceptionPrefix.Length + innerExceptionString.Length + Environment.NewLineConst.Length + 3 + endOfInnerExceptionResource.Length;
            //    }
            //    if (stackTrace != null)
            //    {
            //        length += Environment.NewLineConst.Length + stackTrace.Length;
            //    }
            //}

            // Create the string
            //string result = string.FastAllocateString(length);
            //Span<char> resultSpan = new Span<char>(ref result.GetRawStringData(), result.Length);

            // Fill it in
            //            Write(className, ref resultSpan);
            writer.Add(new InlineStr(className));
            //writer.Add(new Run(className));
            if (!string.IsNullOrEmpty(message))
            {
                writer.Add(new InlineStr(": ") { Fore = Brushes.Gray });
                //writer.Add(new Run(": ") { Foreground = Brushes.Gray});
                writer.Add(new InlineStr(message, true) { Fore = Brushes.BlueViolet });
                //writer.Add(new Bold(new Run(message) { Foreground =Brushes.BlueViolet}));
                //Write(": ", ref resultSpan);
                //Write(message, ref resultSpan);
            }
            if (exception.InnerException != null)
            {
                writer.Add(new InlineStr());
                //Write(Environment.NewLineConst, ref resultSpan);
                writer.Add(new InlineStr(InnerExceptionPrefix));
                writer.Add(new InlineStr(innerExceptionString));
                //  Write(InnerExceptionPrefix, ref resultSpan);
                //  Write(innerExceptionString, ref resultSpan);
                //writer.Add(new LineBreak());
                // Write(Environment.NewLineConst, ref resultSpan);
                //Write("   ", ref resultSpan);
                //Write(endOfInnerExceptionResource, ref resultSpan);
            }
            if ((stackTrace != null) && flags.LogStack)
            {
                writer.Add(new InlineStr());
                writer.Add(new InlineStr(stackTrace) { Fore = Brushes.CadetBlue });
                //Write(Environment.NewLineConst, ref resultSpan);
                //Write(stackTrace, ref resultSpan);
            }
            writer.Add(new InlineStr());
            //Debug.Assert(resultSpan.Length == 0);

            // Return it
            // return result;

            //static void Write(string source, ref Span<char> dest)
            //{
            //    source.CopyTo(dest);
            //    dest = dest.Slice(source.Length);
            //}

        }

        private static void WriteMessage(List<InlineStr> writer, string message, bool singleLine)
        {
            if (!string.IsNullOrEmpty(message))
            {
                if (singleLine)
                {
                    writer.Add(new InlineStr(" "));
                    WriteReplacing(writer, Environment.NewLine, " ", message);
                }
                else
                {
                    writer.Add(new InlineStr(_messagePadding));
                    WriteReplacing(writer, Environment.NewLine, _newLineWithMessagePadding, message);
                    writer.Add(new InlineStr());
                }
            }

            static void WriteReplacing(List<InlineStr> writer, string oldValue, string newValue, string message)
            {
                string newMessage = message.Replace(oldValue, newValue);
                writer.Add(new InlineStr(newMessage, true) { Fore = Brushes.DarkSlateBlue });
            }
        }
    
       

        private DateTimeOffset GetCurrentDateTime()
        {
            return FormatterOptions.UseUtcTimestamp ? DateTimeOffset.UtcNow : DateTimeOffset.Now;
        }

        private static string GetLogLevelString(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => "trce",
                LogLevel.Debug => "dbug",
                LogLevel.Information => "info",
                LogLevel.Warning => "warn",
                LogLevel.Error => "fail",
                LogLevel.Critical => "crit",
                _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
            };
        }

        private ConsoleColors GetLogLevelConsoleColors(LogLevel logLevel)
        {
            bool disableColors = (FormatterOptions.ColorBehavior == LoggerColorBehavior.Disabled);
                //||(FormatterOptions.ColorBehavior == LoggerColorBehavior.Default && !ConsoleUtils.EmitAnsiColorCodes);
            if (disableColors)
            {
                return new ConsoleColors(null, null);
            }
            // We must explicitly set the background color if we are setting the foreground color,
            // since just setting one can look bad on the users console.
            return logLevel switch
            {
                LogLevel.Trace => new ConsoleColors(Brushes.Gray, Brushes.White),
                LogLevel.Debug => new ConsoleColors(Brushes.AliceBlue, Brushes.Black),
                LogLevel.Information => new ConsoleColors(Brushes.LightGreen, Brushes.Black),
                LogLevel.Warning => new ConsoleColors(Brushes.Yellow, Brushes.Black),
                LogLevel.Error => new ConsoleColors(Brushes.Red, Brushes.Black),
                LogLevel.Critical => new ConsoleColors(Brushes.White, Brushes.DarkRed),
                _ => new ConsoleColors(null, null)
            };
        }

        private void WriteScopeInformation(List<InlineStr> writer, IExternalScopeProvider? scopeProvider, bool singleLine)
        {
            if (FormatterOptions.IncludeScopes && scopeProvider != null)
            {
                bool paddingNeeded = !singleLine;
                scopeProvider.ForEachScope((scope, state) =>
                {
                    if (paddingNeeded)
                    {
                        paddingNeeded = false;
//                        state.Write(_messagePadding);
                        state.Add(new InlineStr(_messagePadding) { Fore = Brushes.Brown });
  //                      state.Write("=> ");
                        state.Add(new InlineStr(" => ") { Fore = Brushes.Aquamarine,Back=Brushes.Gray });
                    }
                    else
                    {
                        state.Add(new InlineStr(" => ") { Fore= Brushes.Aquamarine, Back = Brushes.Gray });
                    }
                    if (scope is not null) state.Add( new InlineStr(scope.ToString()) { Fore = Brushes.DarkGreen});
                }, writer);

                if (!paddingNeeded && !singleLine)
                {
                    writer.Add(new InlineStr());
                }
            }
        }

        private readonly struct ConsoleColors
        {
            public ConsoleColors(Brush? background, Brush? foreground)
            {
                Foreground = foreground;
                Background = background;
            }

            public Brush? Foreground { get; }

            public Brush? Background { get; }
        }
    }

    public class ExcTextBlockLoggerFormatter : TextBlockLoggerFormatter
    {
        public ExcTextBlockLoggerFormatter(string optionName, IOptionsMonitor<TextBlockLoggerFormatterOptions> options)
            : base(optionName, options) { }
    }
    public class InfoTextBlockLoggerFormatter : TextBlockLoggerFormatter
    {
        public InfoTextBlockLoggerFormatter(string optionName, IOptionsMonitor<TextBlockLoggerFormatterOptions> options)
            : base(optionName, options) { }
    }
    public class TraceTextBlockLoggerFormatter : TextBlockLoggerFormatter
    {
        public TraceTextBlockLoggerFormatter(string optionName, IOptionsMonitor<TextBlockLoggerFormatterOptions> options)
            : base(optionName, options) { }
    }

}

