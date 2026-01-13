using Connections.Interface;
using Serilog;
using Serilog.Core;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace Connections //Horizont.Drilling.Connections
{
    public delegate void OnRowNewDataHandler(byte[] buf, int oldof, int of);

    /// <summary>
    /// высокоуровневый драйвер транзакций ПБ. УСО телесистемы
    /// </summary>
    public abstract class AbstractTransactionDriver
    {
        public abstract void BeginTransaction(AbstractConnection Conn, DataReq dataReq);
        public abstract Task<int> RowRead(AbstractConnection Conn, CancellationToken cancellationToken);
        public abstract void Produce(AbstractConnection Conn, int Count);
        public abstract void Consume(AbstractConnection Conn);
        public abstract DataResp EndTransaction(AbstractConnection Conn, DataReq dataReq, int EndStatus);
    }


    public abstract class AbstractConnection :INotifyPropertyChanged, IConnection, IAbstractConnection, IDisposable
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected bool SetProperty<T>([NotNullIfNotNull(nameof(newValue))] ref T field, T newValue, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue))
            {
                return false;
            }
            field = newValue;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            return true;
        }

        public readonly AutoResetEvent ProdusEvent = new AutoResetEvent(false);
        public readonly AutoResetEvent EndReadEvent = new AutoResetEvent(false);
        protected readonly AutoResetEvent rxEndBad = new AutoResetEvent(false);

        protected Thread? RxConsumerThread { get; set; }
        protected Thread? RxProduserThread { get; set; }
        protected CancellationTokenSource? CtsConsumerProduser { get; set; }

        public AbstractTransactionDriver? _Driver;
        public AbstractTransactionDriver? Driver { get => _Driver;
            set 
            { 
                if (value != null && _Driver != null && _Driver != value)
                    throw new Exception("Driver already set");
                _Driver = value; 
            }
        }
        public void OnRowDataEvent(byte[] buf, int oldof, int of) => monitor?.Debug("Rx {buf}", buf);        
        public void OnRowSendEvent(byte[] buf, int oldof, int of) => monitor?.Information("{Tx} {buf}", "Tx",buf);
        
        public DataReq? CurrenRq { get; set; }
        public bool IsReading { get; set; }
        
        // private IDisposable? _scope;
        [XmlIgnore]
        public string dbg = "";

        [XmlIgnore]
        public ILogger? logger { get; set; }

        [XmlIgnore]
        public ILogger? monitor { get; set; }

        [XmlIgnore]
        public LoggingLevelSwitch? monitorLevel { get; set; }


        // interface
        public abstract bool IsOpen { get; }

        #region LOCK
        private int running = 0;
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
        #endregion

        #region Cancel
        protected CancellationTokenSource? CtsCancel { get; set; }
        public virtual void Cancel()
        {
            logger?.Information($"{Thread.CurrentThread.ManagedThreadId} {dbg} Cts.Cancel()");
            canceled = true;
            CtsCancel?.Cancel();
        }
        protected bool canceled;
        public bool IsCanceled => canceled;
        #endregion

        #region OPEN CLOSE
        public virtual Task Close(int timout = 5000)
        {
            if (RxConsumerThread != null && CtsConsumerProduser != null && RxProduserThread != null)
            {
                CtsConsumerProduser.Cancel();
                ProdusEvent.Set();
                while (RxProduserThread.IsAlive && --timout > 0) Thread.Sleep(1);
                while (RxConsumerThread.IsAlive && --timout > 0) Thread.Sleep(1);
                CtsConsumerProduser.Dispose();
                CtsConsumerProduser = null;
                RxConsumerThread = null;
                RxProduserThread = null;
            }
            return Task.CompletedTask;
        }
        public virtual Task Open(int timout = 10000, bool RxNeed = true)
        {
            if (RxNeed)
            {
                if (RxConsumerThread != null && CtsConsumerProduser != null &&
                    RxProduserThread != null) return Task.CompletedTask;
                CtsConsumerProduser = new();
                RxConsumerThread = new Thread(() => ConsumerThreadRun(CtsConsumerProduser.Token));
                RxConsumerThread.IsBackground = true;
                RxConsumerThread.Start();

                RxProduserThread = new Thread(() => ProduserThreadRun(CtsConsumerProduser.Token));
                RxProduserThread.IsBackground = true;
                RxProduserThread.Start();
            }
            return Task.CompletedTask;
        }
        #endregion

        // low level methods
        public abstract Task Send(DataReq dataReq);
        public abstract Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);

        public async Task<DataResp> Transaction(DataReq dataReq)
        {
            logger?.Information($"{Thread.CurrentThread.ManagedThreadId} {dbg} ===> begin--- {dataReq.rxCount}");
            lock (this)
            {
                CurrenRq = dataReq;
                Driver?.BeginTransaction(this, dataReq);
            }
            await Send(dataReq);
            OnRowSendEvent(dataReq.txBuf, 0, dataReq.txBuf.Length);
            return await Read(dataReq);
        }
        protected virtual void BeginRead()
        {
            canceled = false;
            if (CurrenRq!.rxCount > 0)
            {
                IsReading = true;
                CtsCancel = new CancellationTokenSource();
            }
        }
        protected virtual void EndRead()
        {
            CtsCancel?.Dispose();
            CtsCancel = null;
            IsReading = false;
        }
        private Task<DataResp> Read(DataReq dataReq)
        {
            return Task.Run(() =>
            {
                BeginRead();  
                var hAbort = CtsCancel != null? CtsCancel.Token.WaitHandle: EndReadEvent;
                int s = WaitHandle.WaitAny(new[] { EndReadEvent, hAbort }, dataReq.timout);

                var r = Driver!.EndTransaction(this,dataReq, s);

                EndRead();
                return r;
            });
        }

        // dectructor
        #region Dispose
        ~AbstractConnection() 
        { 
            Dispose(disposing: false); 
        }
        public void Dispose() 
        { 
            Dispose(disposing: true);  
            GC.SuppressFinalize(this); 
        }
        protected bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;
            disposed = true;
            IsReading = false;
            CtsCancel?.Dispose();
            rxEndBad.Dispose();
            ProdusEvent?.Dispose();
            EndReadEvent?.Dispose();
        }
        #endregion
        private async void ProduserThreadRun(CancellationToken token)
        {
            while (true)
            {
                try
                {
                    if (token.IsCancellationRequested) return;                   

                    if (!IsOpen || !IsReading || CtsCancel == null)
                    {
                        continue;
                    }
                    try
                    {
                        int cntout = 0;
                        while (IsOpen && cntout == 0 && CtsCancel != null && !CtsCancel.IsCancellationRequested && Driver != null)
                        {
                            cntout = await Driver.RowRead(this, CtsCancel.Token);
                        }

                        Driver?.Produce(this, cntout);

                        if (CtsCancel != null && CtsCancel.IsCancellationRequested)
                        {
                            IsReading = false;
                            rxEndBad.Set();
                            continue;
                        }
                    }
                    catch (Exception e)
                    {
                        IsReading = false;
                        logger?.Information($"ERR {Thread.CurrentThread.ManagedThreadId} {dbg} Produser exit rxEndBad.Set() {e}");
                        rxEndBad.Set();
                    }
                }
                catch (Exception e)
                {
                    logger?.Error(e.Message, e);
                }
            }

        }
        private void ConsumerThreadRun(CancellationToken tok)
        {
            //  var scope = App.logger?.BeginScope(this);
            while (true)
            {
                ProdusEvent.WaitOne();

                if (tok.IsCancellationRequested) 
                {
                    //scope?.Dispose();
                    return; 
                }

                try
                {
                    Driver?.Consume(this);
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
