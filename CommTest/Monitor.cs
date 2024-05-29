//#define SIMPLE_STR

using Connections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using Commands;
using Connection.Interface;
using Microsoft.Extensions.Logging;
using System.Windows.Documents;
using System.Windows.Media;

namespace SerialPortTest
{
    internal class qeData
    {
        public int isTx;
        public DateTime time;
        public int count;
        public byte[]? data;
        public qeData(int drops)
        {
            isTx = 2;
            count = drops;
        }
        public qeData(bool rw, byte[] data, int oldof, int of, int MaxData=16) 
        {
            isTx = rw? 1: 0;
            time = DateTime.Now;
            count = of-oldof;
            int cnt = Math.Min(count, MaxData);
            if (cnt > 0) 
            {
                this.data = new byte[cnt]; 
                Buffer.BlockCopy(data, oldof, this.data, 0, cnt); 
            }
            else { this.data = null; }
        }
    }
    internal class ConnectionMonitor
    {
        private TextBlock textBlock;
        private AbstractConnection? connection;
        private bool _disposed = false;
        private Thread readthread;
        private DateTime lastTime;

        private Queue<qeData> queue = new Queue<qeData>();
#if SIMPLE_STR
        private Queue<string> lines = new Queue<string>();
#else
       // private Queue<List<Inline>> lines = new Queue<List<Inline>>();
#endif
      //  private AutoResetEvent inData = new AutoResetEvent(false);  // объект-событие


        public ConnectionMonitor(TextBlock textBlock)
        {
            this.textBlock = textBlock;
            readthread = new Thread(ReadThread);
            readthread.IsBackground= true;
            readthread.Start();
        }
        public int MaxData { get; set; } = 16;
        public int MaxLines { get; set; } = 300;
        public bool Freeze { get; set; } = false;
        private int drops = 0;

        public void DisConnect()
        {
            if (connection == null) { return; }
            connection.OnRowDataHandler -= onRxData;
            connection.OnRowSendHandler -= onTxData;
            connection = null;
        }
        public void Connect(AbstractConnection connection)
        {
            if (this.connection != null) { DisConnect();}
            this.connection = connection;
            connection.OnRowDataHandler += onRxData;
            connection.OnRowSendHandler += onTxData;
        }
        public void Clear()
        {
            textBlock.Inlines.Clear();
        }
        private void onTxData(byte[] buf, int oldof, int of)
        {
            if (!Freeze && connection != null)
            {
                lock(queue)
                {
                    if (queue.Count > MaxLines) drops++;
                    else
                    {
                        if (drops > 0)
                        {
                            queue.Enqueue(new qeData(drops));
                            drops = 0;
                        }
                        queue.Enqueue(new qeData(true, buf, oldof, of, MaxData));
                    }
                }
              //  inData.Set();
            }
        }

        private void onRxData(byte[] buf, int oldof, int of)
        {
            if (!Freeze && connection != null) 
            { 
                lock(queue) 
                {
                    if (queue.Count > MaxLines)
                    {
                        drops++;
                    }
                    else
                    {
                        if (drops > 0)
                        {
                            queue.Enqueue(new qeData(drops));
                            drops = 0;
                        }
                        queue.Enqueue(new qeData(false, buf, oldof, of, MaxData));
                    }                    }
              //  inData.Set();
            }
        }

        private void RemoveLines()
        {

#if SIMPLE_STR
            lock(lines) while (lines.Count > MaxLines) lines.Dequeue(); 

            StringBuilder sb2 = new StringBuilder();

            for (int i = lines.Count - 1; i >= 0; i--) { sb2.Append(lines.ElementAt(i)); }

            textBlock.Text = sb2.ToString();
#else
                //if (lines.Count==0) textBlock.Inlines.Clear();
                while (textBlock.Inlines.Count > MaxLines)
                {
                   // lines.Dequeue();
                    do
                    {
                        textBlock.Inlines.Remove(textBlock.Inlines.LastInline);
                    }
                    while (textBlock.Inlines.LastInline is not LineBreak);
                }
#endif
        }

        private void LineAdd(qeData qe)
        {
#if SIMPLE_STR
            StringBuilder sb = new StringBuilder();

            if ( qe.isTx) 
            {
                lastTime = qe.time;
                sb.Append("W " + qe.time.ToString("ss.fff"));
            }
            else 
            {
                int m = (int)(qe.time - lastTime).TotalMilliseconds;
                sb.Append("R " + m.ToString().PadLeft(6));  
                lastTime = qe.time;
            }

            if (qe.data != null)
            {
                sb.Append(qe.count.ToString().PadLeft(6) +"   "+BitConverter.ToString(qe.data));
                if (qe.count > MaxData) sb.Append("...");
            }
            else if (qe.count == -1) sb.Append("  --timout");
            else if (qe.count == -2) sb.Append("  --abort");

            sb.Append(Environment.NewLine);
            lock(lines) lines.Enqueue(sb.ToString());
#else
            List<Inline> ic = new();
            if(qe.isTx==2)
            {
                ic.Add(new Run($"drops [{qe.count}] ") { Foreground = Brushes.Red });
            }
            else if (qe.isTx == 1)
            {
                lastTime = qe.time;
                ic.Add(new Bold(new Run("W " + qe.time.ToString("ss.fff")) { Foreground = Brushes.Blue } ));
            }
            else
            {
                int m = (int)(qe.time - lastTime).TotalMilliseconds;
                ic.Add(new Run("R " + m.ToString().PadLeft(6)));
                lastTime = qe.time;
            }
            if (qe.isTx != 2)
            {
                if (qe.data != null)
                {
                    ic.Add(new Bold(new Run(qe.count.ToString().PadLeft(6)) { Foreground = qe.isTx == 1 ? Brushes.Green : Brushes.Green }));
                    var fg = qe.isTx == 1 ? Brushes.DarkMagenta : Brushes.Black;
                    ic.Add(new Run("   " + BitConverter.ToString(qe.data)) { Foreground = fg });
                    if (qe.count > MaxData) ic.Add(new Run("...") { Foreground = fg });
                }
                else if (qe.count == -1) ic.Add(new Run("  --timout") { Foreground = Brushes.Red });
                else if (qe.count == -2) ic.Add(new Run("  --abort") { Foreground = Brushes.Red });
            }

            ic.Add(new LineBreak());

            if (textBlock.Inlines.Count == 0) textBlock.Inlines.AddRange(ic);
            else textBlock.Inlines.InsertRange(ic);
#endif
        }
        private void AddRrop(int drops)
        {
            List<Inline> ic = new();
            ic.Add(new Run($"drops [{drops}] ") { Foreground= Brushes.Red });
            ic.Add((new LineBreak()));
            if (textBlock.Inlines.Count == 0) textBlock.Inlines.AddRange(ic);
            else textBlock.Inlines.InsertRange(ic);
        }

        private void ReadThread()
        {
            while (true) 
            {
              //  inData.WaitOne();

                if (_disposed) { return; }

                if (queue.Count >0)
                textBlock.Dispatcher.Invoke(DispatcherPriority.Background, () => 
                {
                    RemoveLines();
                    //while (queue.Count > 0)
                    {
                        qeData line;
                        lock (queue)
                        {
                            line = queue.Dequeue();
                        }
                        LineAdd(line);
                        if (drops > 0) AddRrop(drops);
                        drops = 0;
                    }
                });
            }
        }
    }
}
