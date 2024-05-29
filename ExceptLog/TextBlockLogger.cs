using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Diagnostics;
using System.Threading;
using System.Windows.Documents;
using System.Xml.Linq;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Automation.Peers;
//using Core;

namespace TextBlockLogging
{
    //public enum TextBlockConsoleCategoty
    //{
    //    None = 0,
    //    Exception = 1,
    //    Info = 2,
    //    Trace = 3,
    //}
    public class TextBlockOptions
    {
        public string ConsoleCategoty { get; set; } = "";

        private ConsoleLoggerQueueFullMode _queueFullMode = ConsoleLoggerQueueFullMode.DropWrite;
        /// <summary>
        /// Gets or sets the desired console logger behavior when the queue becomes full. Defaults to <c>Wait</c>.
        /// </summary>
        public ConsoleLoggerQueueFullMode QueueFullMode
        {
            get => _queueFullMode;
            set
            {
                if (value != ConsoleLoggerQueueFullMode.Wait && value != ConsoleLoggerQueueFullMode.DropWrite)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _queueFullMode = value;
            }
        }

        internal const int DefaultMaxQueueLengthValue = 30;
        internal const int DefaultMaxTextBlockLength = 30;
        private int _maxQueuedMessages = DefaultMaxQueueLengthValue;
        private int _maxTextBlockLength = DefaultMaxTextBlockLength;

        /// <summary>
        /// Gets or sets the maximum number of enqueued messages. Defaults to 2500.
        /// </summary>
        public int MaxQueueLength
        {
            get => _maxQueuedMessages;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _maxQueuedMessages = value;
            }
        }
        public int MaxTextBlockLength
        {
            get => _maxTextBlockLength;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _maxTextBlockLength = value;
            }
        }
    }

    public struct InlineStr
    {
        public readonly bool _bold;
        public readonly string? _message;
        public Brush? Fore;
        public Brush? Back;
        public InlineStr(string? message = null, bool bold = false)
        {
            _message = message;
            _bold = bold;
        }
        public static explicit operator Inline(InlineStr ist) 
        {
            if (ist._message != null)
            {
                var r = new Run(ist._message);
                if (ist.Fore != null) r.Foreground = ist.Fore;
                if (ist.Back != null) r.Background = ist.Back;
                if (ist._bold) return new Bold(r);
                else return r;
            }
            else return new LineBreak();
        }
    }
    
    public readonly struct LogMessageEntry
    {
        public readonly IEnumerable<InlineStr> _message;
        public LogMessageEntry(IEnumerable<InlineStr> message)
        {
            _message = message;
        }
        public readonly IEnumerable<Inline> Message
        {
            get => _message.Select((ils)=>(Inline)ils);            
        }
    }

    public class TextBlockProcessor
    {
        private readonly Queue<LogMessageEntry> _messageQueue;
        private volatile int _messagesDropped;
        private bool _isAddingCompleted;
        private int _maxQueuedMessages = TextBlockOptions.DefaultMaxQueueLengthValue;
        private int _maxTextBlockLength = TextBlockOptions.DefaultMaxTextBlockLength;
        public int MaxTextBlockLength
        {
            get => _maxTextBlockLength;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                lock (_messageQueue)
                {
                    _maxTextBlockLength = value;
                    Monitor.PulseAll(_messageQueue);
                }
            }
        }
        public int MaxQueueLength
        {
            get => _maxQueuedMessages;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                lock (_messageQueue)
                {
                    _maxQueuedMessages = value;
                    Monitor.PulseAll(_messageQueue);
                }
            }
        }
        private ConsoleLoggerQueueFullMode _fullMode = ConsoleLoggerQueueFullMode.Wait;
        public ConsoleLoggerQueueFullMode FullMode
        {
            get => _fullMode;
            set
            {
                if (value != ConsoleLoggerQueueFullMode.Wait && value != ConsoleLoggerQueueFullMode.DropWrite)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                lock (_messageQueue)
                {
                    // _fullMode is used inside the lock and is safer to guard setter with lock as well
                    // this set is not expected to happen frequently
                    _fullMode = value;
                    Monitor.PulseAll(_messageQueue);
                }
            }
        }
        private string _consoleCategory = "None";
        public string ConsoleCategory
        {
            get => _consoleCategory;
            set
            {
                lock (_messageQueue)
                {
                    Console = null;
                    _consoleCategory = value;
                    Monitor.PulseAll(_messageQueue);
                }
            }

        }
        private readonly Thread _outputThread;
        private readonly ILogTextBlockService _consoleService;
        private TextBlock? Console { get; set; }

        public TextBlockProcessor(string ConsoleCategory, ILogTextBlockService consoleService, 
                                  ConsoleLoggerQueueFullMode fullMode, int maxQueueLength, int maxTextBlockLength)
        {
            _messageQueue = new Queue<LogMessageEntry>();
            FullMode = fullMode;
            MaxQueueLength = maxQueueLength;
            MaxTextBlockLength = maxTextBlockLength;
            _consoleService = consoleService;
            _consoleCategory = ConsoleCategory;
            // Start Console message queue processor
            _outputThread = new Thread(ProcessLogQueue)
            {
                IsBackground = true,
                Name = "TextBlock logger queue processing thread " + ConsoleCategory,
            };
            _outputThread.Start();
        }

        public virtual void EnqueueMessage(LogMessageEntry message)
        {
            // cannot enqueue when adding is completed
            if (!Enqueue(message))
            {
                WriteMessage(message);
            }
        }

        // internal for testing
        internal void WriteMessage(LogMessageEntry entry)
        {
            try
            {
                Console ??= _consoleService.GetLogTextBlock(_consoleCategory);
                if (Console == null) return;
                Console.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => {
                    if (Console is IFreeze f && f.Freeze) return;
                    while (Console.Inlines.Count > MaxTextBlockLength)
                    {
                        do
                        {
                            Console.Inlines.Remove(Console.Inlines.LastInline);
                        }
                        while (Console.Inlines.LastInline is not LineBreak);
                    }

                    if (Console.Inlines.Count == 0) 
                         Console.Inlines.AddRange(entry.Message);
                    else Console.Inlines.InsertRange(entry.Message);
                }));

            }
            catch
            {
                CompleteAdding();
            }
        }

        private void ProcessLogQueue()
        {
            while (TryDequeue(out LogMessageEntry message))
            {
                WriteMessage(message);
            }
        }

        public bool Enqueue(LogMessageEntry item)
        {
            lock (_messageQueue)
            {
                while (_messageQueue.Count >= MaxQueueLength && !_isAddingCompleted)
                {
                    if (FullMode == ConsoleLoggerQueueFullMode.DropWrite)
                    {
                        _messagesDropped++;
                        return true;
                    }

                    Debug.Assert(FullMode == ConsoleLoggerQueueFullMode.Wait);
                    Monitor.Wait(_messageQueue);
                }

                if (!_isAddingCompleted)
                {
                    Debug.Assert(_messageQueue.Count < MaxQueueLength);
                    bool startedEmpty = _messageQueue.Count == 0;
                    if (_messagesDropped > 0)
                    {
                        _messageQueue.Enqueue(new LogMessageEntry(new InlineStr[]
                             { new InlineStr($"Messages Dropped [{_messagesDropped}]"){Fore = Brushes.Red}, new InlineStr()}));

                        _messagesDropped = 0;
                    }
                    // if we just logged the dropped message warning this could push the queue size to
                    // MaxLength + 1, that shouldn't be a problem. It won't grow any further until it is less than
                    // MaxLength once again.
                    _messageQueue.Enqueue(item);

                    // if the queue started empty it could be at 1 or 2 now
                    if (startedEmpty)
                    {
                        // pulse for wait in Dequeue
                        Monitor.PulseAll(_messageQueue);
                    }

                    return true;
                }
            }

            return false;
        }

        public bool TryDequeue(out LogMessageEntry item)
        {
            lock (_messageQueue)
            {
                while (_messageQueue.Count == 0 && !_isAddingCompleted)
                {
                    Monitor.Wait(_messageQueue);
                }

                if (_messageQueue.Count > 0 && !_isAddingCompleted)
                {
                    item = _messageQueue.Dequeue();
                    if (_messageQueue.Count == MaxQueueLength - 1)
                    {
                        // pulse for wait in Enqueue
                        Monitor.PulseAll(_messageQueue);
                    }

                    return true;
                }

                item = default;
                return false;
            }
        }

        public void Dispose()
        {
            CompleteAdding();

            try
            {
                _outputThread.Join(1500); // with timeout in-case Console is locked by user input
            }
            catch (ThreadStateException) { }
        }

        private void CompleteAdding()
        {
            lock (_messageQueue)
            {
                _isAddingCompleted = true;
                Monitor.PulseAll(_messageQueue);
            }
        }

    }

    public class TextBlockLogger : ILogger
    {
        private readonly string _name;
        private readonly TextBlockProcessor _queueProcessor;

        internal TextBlockLogger(
            string name,
            TextBlockProcessor loggerProcessor,
            TextBlockLoggerFormatter formatter,
            IExternalScopeProvider? scopeProvider,
            TextBlockOptions options)
        {
            if (name is null) throw new ArgumentNullException(nameof(name));
            _name = name;
            _queueProcessor = loggerProcessor;
            Formatter = formatter;
            ScopeProvider = scopeProvider;
            Options = options;
        }

        internal TextBlockLoggerFormatter Formatter { get; set; }
        internal IExternalScopeProvider? ScopeProvider { get; set; }
        internal TextBlockOptions Options { get; set; }

        [ThreadStatic]
        private static List<InlineStr>? writer;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }
            if (formatter is null) throw new ArgumentNullException(nameof(formatter));

            writer ??= new();
            LogEntry<TState> logEntry = new LogEntry<TState>(logLevel, _name, eventId, state, exception, formatter);
            Formatter.Write(in logEntry, ScopeProvider, writer);

            if (writer.Count == 0)
            {
                return;
            }
            var sb = writer.ToArray();// .GetStringBuilder(); //string computedAnsiString = sb.ToString();

            writer.Clear();

            if (writer.Capacity > 1024)
            {
                writer.Capacity = 1024;
            }
            _queueProcessor.EnqueueMessage(new LogMessageEntry(sb));//, logAsError: logLevel >= Options.LogToStandardErrorThreshold));
        }

        public virtual bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => ScopeProvider?.Push(state) ?? NullScope.Instance;
    }
    
    public class TraceTextBlockOptions : TextBlockOptions
    {
        public TraceTextBlockOptions() { ConsoleCategoty = "Trace"; }
    }
    internal class TraceTextBlockLogger: TextBlockLogger
    {
        internal TraceTextBlockLogger(
            string name,
            TextBlockProcessor loggerProcessor,
            TextBlockLoggerFormatter formatter,
            IExternalScopeProvider? scopeProvider,
            TextBlockOptions options) : base(name,loggerProcessor,formatter,scopeProvider,options) { }

        public override bool IsEnabled(LogLevel logLevel)
        {
            return
                logLevel <= LogLevel.Debug &&  // don't allow messages more severe than information to pass through
                base.IsEnabled(logLevel);
        }
    }

    public class InfoTextBlockOptions : TextBlockOptions
    {
        public InfoTextBlockOptions() { ConsoleCategoty = "Info"; }
    }
    public class ExceptionTextBlockOptions : TextBlockOptions
    {
        public ExceptionTextBlockOptions() { ConsoleCategoty = "Exception"; }
    }

    internal class InfoTextBlockLogger : TextBlockLogger
    {
        internal InfoTextBlockLogger(
            string name,
            TextBlockProcessor loggerProcessor,
            TextBlockLoggerFormatter formatter,
            IExternalScopeProvider? scopeProvider,
            TextBlockOptions options) : base(name, loggerProcessor, formatter, scopeProvider, options) { }

        public override bool IsEnabled(LogLevel logLevel)
        {
            return
                logLevel == LogLevel.Information &&  // don't allow messages more severe than information to pass through
                base.IsEnabled(logLevel);
        }
    }

    internal sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new NullScope();

        private NullScope()
        {
        }
        /// <inheritdoc />
        public void Dispose()
        {
        }
    }

    public interface IFreeze
    {
        bool Freeze { get; set; }
    }
    public class LogTextBlock : TextBlock, IFreeze
    {
        public static DependencyProperty FreezeProperty = DependencyProperty.Register("Freeze", typeof(bool), typeof(LogTextBlock));
            //, new FrameworkPropertyMetadata(
            //                    false,
            //                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,// | FrameworkPropertyMetadataOptions.Journal,
            //                    new PropertyChangedCallback(OnIsCheckedChanged)));
        public bool Freeze
        {
            get { return (bool)GetValue(FreezeProperty); }
            set { SetValue(FreezeProperty, value); }
        }
        //private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        //{
        //    LogTextBlock textBlock = (LogTextBlock)d;
        //    bool o = (bool)e.OldValue;
        //    bool n = (bool)e.NewValue;
        //    var l = VMBase.ServiceProvider.GetRequiredService<ILogger<LogTextBlock>>();
        //    var s = VMBase.ServiceProvider.GetRequiredService<ILogTextBlockService>().FindCategoty(textBlock);

        //    l.LogTrace("CNG_freeze {} hach {} old:{} new:{} real:{}", s, textBlock.GetHashCode(), o, n, textBlock.Freeze);
        //}
    }
    public interface ILogTextBlockService
    {
        void SetLogTextBlock(string Categoty, LogTextBlock textBlock);
        string? FindCategoty(LogTextBlock textBlock);

        LogTextBlock? GetLogTextBlock(string Categoty);
    }
    public class LogTextBlockService : ILogTextBlockService
    {
        private static readonly ConcurrentDictionary<string, LogTextBlock> _dic = new();
        public void SetLogTextBlock(string Categoty, LogTextBlock textBlock) => _dic.AddOrUpdate(Categoty, textBlock, (c, v) => v   );
        public LogTextBlock? GetLogTextBlock(string Categoty) => _dic.GetValueOrDefault(Categoty);

        public string? FindCategoty(LogTextBlock textBlock)
        {
           return _dic.FirstOrDefault(x=> x.Value == textBlock).Key;
        }
    }
}

