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
        public readonly SerialPort port = new SerialPort();

        static SerialConn()
        {
            _ConnectionTypes.Add(typeof(SerialConn));
        }
        public SerialConn() : base()
        {
            port.BaudRate = 125000;
        }
        public override string? ToString()
        {
            return PortName;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposed) return;
            port.Dispose();
            rxDisposeEvent.Dispose();
            base.Dispose(disposing);
        }
        public int BaudRate
        {
            get { return port.BaudRate; }
            set { port.BaudRate = value; }
        }
        public string PortName
        {
            get { return port.PortName; }
            set { port.PortName = value; }
        }

        public override bool IsOpen => port.IsOpen;

        public override Task Close(int timout = 2000)
        {
            logger?.LogInformation($"{Thread.CurrentThread.ManagedThreadId} {dbg} CLOSE {port.PortName}   {port.BaudRate}");
            port.Close();
            return base.Close(timout);
        }

        public override Task Open(int timout = 500, bool RxNeed = true)
        {
            logger?.LogInformation($"{Thread.CurrentThread.ManagedThreadId} {dbg} OPEN {port.PortName}   {port.BaudRate}");
            port.Open();
            PortDisposed = false;
            return base.Open(timout, RxNeed);
        }
        protected readonly AutoResetEvent rxDisposeEvent = new AutoResetEvent(false);

        private volatile bool PortDisposed = false;
        protected override void BeginRead()
        {
            base.BeginRead();
            if (CtsCancel != null)
            {
                rxDisposeEvent.Reset();
                CtsCancel.Token.Register(() =>
                {
                    logger?.LogInformation($"{Thread.CurrentThread.ManagedThreadId} {dbg} Cancel Event port.Dispose() {port.PortName}   {port.BaudRate}");
                    PortDisposed = true;
                    port.Dispose();// Close();
                    rxDisposeEvent.Set();
                });
            }
            else rxDisposeEvent.Set();
            rxEndBad.Reset();
        }
        protected override void EndRead()
        {
            if (IsReading)
            {
                logger?.LogInformation($"ERR {Thread.CurrentThread.ManagedThreadId} {dbg} EndRead(BEGIN): IsReading =True");

                if (CtsCancel != null)
                {
                    if (!CtsCancel.IsCancellationRequested)
                    {
                        CtsCancel.Cancel();
                        logger?.LogInformation($"ERR {Thread.CurrentThread.ManagedThreadId} {dbg} EndRead(CONTINUE): Cts.Cancel()");
                    }
                    WaitHandle.WaitAll(new[] { rxDisposeEvent, rxEndBad });
                    try
                    {
                        Open();
                    }
                    catch (Exception ex)
                    {
                        Thread.Sleep(300);
                        logger?.LogInformation($"ERR {Thread.CurrentThread.ManagedThreadId} {dbg} ERR_Open {ex}");
                        Open();
                    }

                }
                IsReading = false;
                logger?.LogInformation($"ERR  {Thread.CurrentThread.ManagedThreadId} {dbg}  EndRead(END) Wait ALL");
            }
            else
            {
                if (CtsCancel != null && CtsCancel.IsCancellationRequested)
                {
                    rxDisposeEvent.WaitOne();
                    Open();
                    logger?.LogInformation($"ERR  {Thread.CurrentThread.ManagedThreadId} {dbg} EndRead(END) Wait rxDisposeEvent Only ");
                }
                else
                    logger?.LogInformation($"{Thread.CurrentThread.ManagedThreadId} {dbg} EndRead(END) EasyEnd ");
            }
            base.EndRead();
        }

        public override Task Send(DataReq dataReq)
        {
            return Task.Run(async () =>
            {
                //if (!IsOpen)
                //{
                //    logger?.LogInformation($"{Thread.CurrentThread.ManagedThreadId} {dbg} =====================");
                //}
                if (!PortDisposed) await port.BaseStream.FlushAsync();
                if (!PortDisposed) await port.BaseStream.WriteAsync(dataReq.txBuf, 0, dataReq.txBuf.Length);
            });
        }
        public override async Task<int> ReadAsync(byte[] buffer, int offs, int count, CancellationToken cancellationToken)
        {
            if (!PortDisposed) return await port.BaseStream.ReadAsync(buffer, offs, count, cancellationToken);
            else return 0;
        }
    }
}
