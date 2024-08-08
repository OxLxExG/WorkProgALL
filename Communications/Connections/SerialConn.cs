using Connections.Interface;
using Microsoft.Extensions.Logging;
using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace Connections
{
    public class SerialConn : AbstractConnection, ISerialConnection,IDisposable
    {
        private readonly SerialPort port = new SerialPort();
        private Thread? netThread;


        static SerialConn()
        {
            _ConnectionTypes.Add(typeof(SerialConn));
        }
        public SerialConn() : base()
        {
            port.BaudRate = 125000;
            //port.ReadTimeout = -1;
            //port.ReadBufferSize = 0x8000;
            //port.DataReceived += DataReceivedHandler;

            /// TODO: create netThread then port is open
            /// Destroy netThreadthen port is close
            /// or in netThread open and close port
            /// 
            //var netThread = new Thread(NetReadThread);
            //netThread.Name = "====netThread====";
            //netThread.IsBackground = true;
            //netThread.Start();
        }
        public override string? ToString()
        {
            return PortName;
        }
        protected override void Dispose(bool disposing)
        {
            port.Dispose();

            rxEndEvent.Dispose();
            rxDisposeEvent.Dispose();

            base.Dispose(disposing);
        }
        public int BaudRate
        {
            get { return port.BaudRate; }
            set { port.BaudRate = value; }
        }
        public string PortName// { get; set; }
        {
            get { return port.PortName; }
            set { port.PortName = value; }
        }

        public override bool IsOpen => port.IsOpen;

        public override Task Close(int timout = 2000)
        {
            if (netThread != null && Ctsreadthread != null)
            {
                logger?.LogInformation($"{Thread.CurrentThread.ManagedThreadId} {dbg} CLOSE {port.PortName}   {port.BaudRate}");
                port.Close();
                Ctsreadthread?.Cancel();
                while (netThread.IsAlive && --timout > 0) Thread.Sleep(1);
                netThread = null;
            }            
            return base.Close(timout);
        }

        public override Task Open(int timout = 500)
        {
            logger?.LogInformation($"{Thread.CurrentThread.ManagedThreadId} {dbg} OPEN {port.PortName}   {port.BaudRate}");
            port.Open();

            base.Open(timout);

            if (netThread == null)
            {
                netThread = new Thread(() => NetReadThread(Ctsreadthread!.Token));
                netThread.Name = "====netThread====";
                netThread.IsBackground = true;
                netThread.Start();
            }
            return Task.CompletedTask;
        }
        protected readonly AutoResetEvent rxEndEvent =  new AutoResetEvent(false);
        protected readonly AutoResetEvent rxDisposeEvent = new AutoResetEvent(false);
        // private bool _portDisposed = false;
        //protected override void RecreateCts()
        //{
        //    lock (_lock)
        //    {
        //        base.RecreateCts();
        //    }
        //    Cts.Token.Register(() =>
        //    {
        //        lock (_lock)
        //        {
        //            App.logger?.LogInformation($"{Thread.CurrentThread.ManagedThreadId} {dbg} Cancel Event port.Dispose() {port.PortName}   {port.BaudRate}");
        //            port.Dispose();// Close();
        //            Open();
        //            _disposed = true;
        //        }
        //    });
        //}
        protected override void BeginRead()
        {
            base.BeginRead();
            if (Cts != null)
            {
                rxDisposeEvent.Reset();
                Cts.Token.Register(() =>
                {
                    logger?.LogInformation($"{Thread.CurrentThread.ManagedThreadId} {dbg} Cancel Event port.Dispose() {port.PortName}   {port.BaudRate}");
                    port.Dispose();// Close();

                    rxDisposeEvent.Set();
                });
            }
            else rxDisposeEvent.Set();
        }
        protected override void EndRead()
        {
            // WaitHandle.WaitAll(new[] { rxDisposeEvent, rxEndEvent });
            // if (!IsOpen) Open();

            if (IsReading)
            {
                logger?.LogInformation($"{Thread.CurrentThread.ManagedThreadId} {dbg}  EndRead(BEGIN): IsReading =True");

                if (Cts != null)
                {
                    if (!Cts.IsCancellationRequested)
                    {
                        Cts.Cancel();
                        logger?.LogInformation($"{Thread.CurrentThread.ManagedThreadId} {dbg} EndRead(CONTINUE): Cts.Cancel()");
                    }
                    WaitHandle.WaitAll(new[] { rxDisposeEvent, rxEndEvent });
                    try
                    {
                        Open();
                    }
                    catch (Exception ex)
                    {
                        Thread.Sleep(300);
                        logger?.LogInformation($"ERR {Thread.CurrentThread.ManagedThreadId} {dbg}  Open {ex}");
                        Open();
                    }

                }
                IsReading = false;
                logger?.LogInformation($"{Thread.CurrentThread.ManagedThreadId} {dbg}  EndRead(END) Wait ALL");
            }
            else
            {
                if (Cts != null && Cts.IsCancellationRequested)
                {
                    rxDisposeEvent.WaitOne();
                    Open();
                    logger?.LogInformation($"{Thread.CurrentThread.ManagedThreadId} {dbg} EndRead(END) Wait rxDisposeEvent Only ");
                }
                //  else
                //      App.logger?.LogInformation($"{Thread.CurrentThread.ManagedThreadId} {dbg} EndRead(END) EasyEnd ");
            }
            base.EndRead();
        }

        protected override Task Send(DataReq dataReq)
        {
            return Task.Run(async () =>
            {
                if (!IsOpen)
                {
                    logger?.LogInformation($"{Thread.CurrentThread.ManagedThreadId} {dbg} =====================");
                }
                await port.BaseStream.FlushAsync();
                await port.BaseStream.WriteAsync(dataReq.txBuf, 0, dataReq.txBuf.Length);
            });
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            int cnt = port.BytesToRead;

            logger?.LogTrace("I:{cnt}", cnt);

            if ((cnt == 0) || IsCanceled) return;
            if (cnt + offset > rxBuf.Length)
            {
                logger?.LogError("Buffer OverFlow rxBuf.Length {rxBufLength } < {cntoff} {off} {cnt} ",
                    rxBuf.Length, cnt + offset, offset, cnt);
                Array.Resize(ref rxBuf, offset + cnt);
            }
            int incnt = port.Read(rxBuf, offset, cnt);
            if (incnt == 0) return;
            lock (_lock)
            {
                oldOffset = offset;
                offset += incnt;
            }
            rxRowEvent.Set();
        }
        private async void NetReadThread(CancellationToken tok)
        {
            while (true)
            {
                try
                {
                    if (tok.IsCancellationRequested) return;
                    int cntin = currenRq != null ? currenRq.rxCount : rxBuf.Length;

                    if (!port.IsOpen || !IsReading || cntin <= 0 || Cts == null)
                    {
                        rxEndEvent.Set();
                        Thread.Sleep(100);
                        continue;
                    }
                    rxEndEvent.Reset();
                    try
                    {
                        int cntout = 0;
                        while (port.IsOpen && cntout == 0 && !Cts.IsCancellationRequested)
                        {
                            // if (offset > 200) App.logger?.LogInformation("I");
                            cntout = await port.BaseStream.ReadAsync(rxBuf, offset, cntin - offset, Cts.Token);
                            if (cntout == 0) logger?.LogInformation("Z");
                            if (cntout > 0 && cntout < 3)
                            {
                                Thread.Sleep(0);
                                cntout += await port.BaseStream.ReadAsync(rxBuf, offset + cntout, cntin - offset - cntout, Cts.Token);
                            }
                            //if (offset > 200) App.logger?.LogInformation("E");
                        }
                        if (Cts.IsCancellationRequested)
                        {
                            IsReading = false;
                            rxEndEvent.Set();
                            continue;
                        }
                        if (cntout + offset > rxBuf.Length) { throw new Exception("Buffer OverFlow"); }

                        lock (_lock)
                        {
                            oldOffset = offset;
                            offset += cntout;

                            if (offset >= cntin)
                            {
                                IsReading = false;
                            }
                            rxRowEvent.Set();
                        }

                        // App.logger?.LogTrace("I:{cnt} {off}", cntout, offset);

                    }
                    catch (Exception e)
                    {
                        IsReading = false;
                        logger?.LogInformation($"ERR {Thread.CurrentThread.ManagedThreadId} {dbg} NetReadThread exit rxEndEvent.Set() {e}");
                        rxEndEvent.Set();
                    }
                }
                catch (Exception e)
                {
                    logger?.LogError(e.Message, e);
                }
            }

        }

    }
}
