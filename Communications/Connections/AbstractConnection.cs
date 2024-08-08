using Connections.Interface;
using CRCModbusRTU;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//using System.Windows.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.Intrinsics.Arm;
using System.Xml.Serialization;
using System.Diagnostics.Tracing;

namespace Connections //Horizont.Drilling.Connections
{
    public abstract class AbstractConnection : IConnection, IAbstractConnection, IDisposable
    {   //current buffer
        protected volatile int offset = 0;
        // нужно для события монитора (не удалять!)
        protected volatile int oldOffset = 0;
        protected byte[] rxBuf;
        protected readonly AutoResetEvent rxRowEvent = new AutoResetEvent(false);
        // current CRC
        private volatile int crcOldOffset = 0;
        private volatile int crcOffset = 0;
        private ushort _crc;
        protected readonly AutoResetEvent crcOk = new AutoResetEvent(false);
        protected Thread? readthread;

        protected DataReq? currenRq;
        protected CancellationTokenSource? Cts;
        public CancellationTokenSource? CancelTokenSource { get { return Cts; } }

        protected CancellationTokenSource? Ctsreadthread;
        protected bool IsReading;
        protected bool disposed = false;
        protected object _lock = new object();
        private int running = 0;
        // private IDisposable? _scope;
        [XmlIgnore]
        public string dbg = "";
        // interface
        public bool Lock()
        {
            int r = Interlocked.CompareExchange(ref running, 1, 0);
            return r == 0;
        }
        public void UnLock()
        {
            Interlocked.Exchange(ref running, 0);
        }
        public bool IsLocked { get { return running == 1; } }
        public virtual void Cancel()
        {
            logger?.LogInformation($"{Thread.CurrentThread.ManagedThreadId} {dbg} Cts.Cancel()");
            canceled = true;
            Cts?.Cancel();
        }
        protected bool canceled;
        public bool IsCanceled => canceled;
        public virtual Task Close(int timout = 5000)
        {
            if (readthread != null && Ctsreadthread != null)
            {
                Ctsreadthread.Cancel();
                rxRowEvent.Set();
                while (readthread.IsAlive && --timout > 0) Thread.Sleep(1);
                Ctsreadthread.Dispose();
                Ctsreadthread = null;
                readthread = null;
            }
            return Task.CompletedTask;
        }
        public virtual Task Open(int timout = 10000)
        {
            if (readthread != null && Ctsreadthread != null) return Task.CompletedTask; 
            Ctsreadthread = new();
            readthread = new Thread(() => rxBufferThread(Ctsreadthread.Token));
            readthread.IsBackground = true;
            readthread.Start();
            return Task.CompletedTask;
        }

        public async Task<DataResp> SendAsync(DataReq dataReq)
        {
            BeginTransaction(dataReq);
            await Send(dataReq);
           // App.logger?.LogTrace("SE:{}", dataReq.rxCount);
            OnRowSendHandler?.Invoke(dataReq.txBuf, 0, dataReq.txBuf.Length);
            return await Read(dataReq);
        }
        protected abstract Task Send(DataReq dataReq);
        protected virtual void BeginRead()
        {
            canceled = false;
            if (currenRq!.rxCount > 0)
            {
                IsReading = true;
                Cts = new CancellationTokenSource();
            }
        }
        protected virtual void EndRead()
        {
            Cts?.Dispose();
            Cts = null;
            IsReading = false;
        }
        protected virtual Task<DataResp> Read(DataReq dataReq)
        {
            return Task.Run(() =>
            {
                BeginRead();  
                var hCts = Cts != null? Cts.Token.WaitHandle: crcOk;
                int s = WaitHandle.WaitAny(new[] { crcOk, hCts }, dataReq.timout);
                int cnt;
                if (s == 1) cnt = -2; // abort
                else if (s != 0) cnt = -1; // timout
                else cnt = offset; //crcGood

                if (dataReq.rxCount != offset) 
                    logger?.LogInformation($"{Thread.CurrentThread.ManagedThreadId} {dbg} ReadTask {dataReq.rxCount} != {offset}");
                else if (cnt < 0) 
                    logger?.LogInformation($"{Thread.CurrentThread.ManagedThreadId} {dbg} ReadTask BAD CRC!!! 0 != {_crc:x4}");

                var r = new DataResp(dataReq, rxBuf, cnt);
                // time out for monitor
                if (cnt < 0) OnRowDataHandler.Invoke(rxBuf, 0, cnt);
                dataReq.OnResponse?.Invoke(this, r);
                EndRead();
                return r;
            });
        }
        public abstract bool IsOpen { get; }
        // row data events
        public delegate void OnRowNewDataHandler(byte[] buf, int oldof, int of);
        public event OnRowNewDataHandler? OnRowSendHandler;
        public event OnRowNewDataHandler OnRowDataHandler;
        //constructor
        [XmlIgnore]
        public ILogger? logger { get; set; }
        public AbstractConnection()
        {
            rxBuf = new byte[IConnection.MIN_RX_BUF];
            OnRowDataHandler += OnRowDataEvent;
        }
        // dectructor
        ~AbstractConnection() 
        { 
            Dispose(disposing: false); 
        }
        public void Dispose() 
        { 
            Dispose(disposing: true);  
            GC.SuppressFinalize(this); 
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;
            disposed = true;
            OnRowDataHandler -= OnRowDataEvent;
            IsReading = false;
            Cts?.Dispose();
            rxRowEvent?.Dispose();
            crcOk?.Dispose();
        }
        // reset offsets, crc - helper
        protected void BeginTransaction(DataReq dataReq )
        {            
            lock (_lock)
            {
                logger?.LogInformation($"{Thread.CurrentThread.ManagedThreadId} {dbg} ===> begin--- {dataReq.rxCount}");
                oldOffset = 0;
                offset = 0;
                crcOldOffset = 0;
                crcOffset = 0;
                _crc = 0xFFFF;
                rxBuf = new byte[DataResp.RxCount(dataReq.rxCount)];
                currenRq = dataReq;
            }
        }
        private void OnRowDataEvent(byte[] buf, int oldof, int of)
        {
            if (of <= 0) return;

            lock (_lock)
            {
                crcOldOffset = crcOffset;
                crcOffset = of;
            }

            //if (of > 30000) logger?.LogInformation($"[Old: {crcOldOffset} New: {crcOffset} {of}]");

            var n = crcOffset - crcOldOffset;
            if (n > 0) _crc = Crc.ComputeCrc(_crc, buf, crcOldOffset, crcOffset - crcOldOffset);
            // двойные события возможны, их игнорируем
            else return;

            if (of >= currenRq?.rxCount)
                if (_crc == 0) crcOk.Set();
        }
        private void rxBufferThread(CancellationToken tok)
        {
          //  var scope = App.logger?.BeginScope(this);

            while (true)
            {
                rxRowEvent.WaitOne();

                if (tok.IsCancellationRequested) {
                    //scope?.Dispose();
                    return; }

                try
                {
                    int locof, locOld;
                    lock (_lock)
                    {
                        locOld = oldOffset;
                        locof = offset;
                    }
                    OnRowDataHandler.Invoke(rxBuf, locOld, locof);
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
        // массив производных типов для сериализации
        protected static List<Type> _ConnectionTypes = new List<Type>();
        public static Type[] ConnectionTypes { get => _ConnectionTypes.ToArray(); }
    }
}
