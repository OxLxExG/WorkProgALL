using Microsoft.Extensions.Logging.Console;
using System.Windows.Media;

namespace TextBlockLogging
{
  
    public class TextBlockMonitor
    {
       // private AbstractConnection? connection;
        private readonly string _name;
        private readonly TextBlockProcessor _queueProcessor;
        private  int _maxDataLen;
        private DateTime lastTime;
        [ThreadStatic]
        private static List<InlineStr>? writer;

        public TextBlockMonitor(string name, ILogTextBlockService consoleService, 
                                  ConsoleLoggerQueueFullMode fullMode, int maxQueueLength, int maxTextBlockLength, int MaxDataLen) 
        {
            _name = name;
            _maxDataLen = MaxDataLen;
            _queueProcessor = new TextBlockProcessor(name, consoleService, fullMode, maxQueueLength, maxTextBlockLength);
           //if (Conn != null) Connect(Conn);
        }
        public void onTxData(byte[] buf, int oldof, int of)
        {
            writer ??= new();
            lastTime = DateTime.Now;
            writer.Add(new InlineStr("W " + lastTime.ToString("ss.fff"), true) { Fore = Brushes.CadetBlue });
            int count = of - oldof;
            int cnt = Math.Min(count, _maxDataLen);
            writer.Add(new InlineStr(count.ToString().PadLeft(6), true) { Fore = Brushes.Green });
            writer.Add(new InlineStr("   " + BitConverter.ToString(buf, oldof, cnt)) { Fore = Brushes.Magenta });
            if (count > _maxDataLen) writer.Add(new InlineStr("...") { Fore = Brushes.Magenta });
            writer.Add(new InlineStr());
            Write();
        }

        public void onRxData(byte[] buf, int oldof, int of)
        {
            writer ??= new();
            int m = (int)(DateTime.Now - lastTime).TotalMilliseconds;
            writer.Add(new InlineStr("R " + m.ToString().PadLeft(6)));
            int count = of - oldof;
            int cnt = Math.Min(count, _maxDataLen);
            if (cnt > 0)
            {
                writer.Add(new InlineStr(count.ToString().PadLeft(6), true) { Fore = Brushes.Green });
                writer.Add(new InlineStr("   " + BitConverter.ToString(buf, oldof, cnt)) {  });
                if (count > _maxDataLen) writer.Add(new InlineStr("...") { });
            }
            else if (cnt == 0) writer.Add(new InlineStr("  --nodata") { Fore = Brushes.Red });
            else if (cnt == -1) writer.Add(new InlineStr("  --timout") { Fore = Brushes.Red });
            else if (cnt == -2) writer.Add(new InlineStr("  --abort") { Fore = Brushes.Red });
            writer.Add(new InlineStr());
            Write();
        }
        private void Write()
        {
            var sb = writer!.ToArray();// .GetStringBuilder(); //string computedAnsiString = sb.ToString();

            writer.Clear();

            if (writer.Capacity > 1024)
            {
                writer.Capacity = 1024;
            }
            _queueProcessor.EnqueueMessage(new LogMessageEntry(sb));//, logAsError: logLevel >= Options.LogToStandardErrorThreshold));
        }

       // public bool Freeze { get; set; } = false;

        //public void DisConnect()
        //{
        //    if (connection == null) { return; }
        //    connection.OnRowDataHandler -= onRxData;
        //    connection.OnRowSendHandler -= onTxData;
        //    connection = null;
        //}

        //public void Connect(AbstractConnection connection)
        //{
        //    if (this.connection != null) { DisConnect(); }
        //    this.connection = connection;
        //    connection.OnRowDataHandler += onRxData;
        //    connection.OnRowSendHandler += onTxData;
        //}
    }
}
