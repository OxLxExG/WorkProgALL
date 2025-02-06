using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;


namespace Connections.Interface
{
    public interface IAbstractConnection : IDisposable
    {
        Task Open(int timout = 10000, bool RxNeed = true);
        Task Close(int timout = 5000);
        bool IsOpen { get; }

        void Cancel();
        bool IsCanceled { get; }

        // для захвата соединения
        bool Lock();
        void UnLock();
        bool IsLocked { get; }
    }
    public interface ISSDConnection : IAbstractConnection
    {
        char Letter { get; set; }
        long SSDSize { get; }
        uint SectorSize { get; }
        SafeFileHandle? handle { get; }
        CancellationToken CancelToken { get; }
    }

    public delegate void OnResponseEvent(object Sender, DataResp arg);

    public record DataReq(byte[] txBuf, int rxCount, OnResponseEvent? OnResponse = null, int timout = 500)
    {
        public DataReq(DataReq r, OnResponseEvent ev, int timout = 500) : this(r.txBuf, r.rxCount, ev, timout) { }
    }

    public record DataResp(DataReq Req, byte[] rxBuf, int rxCount)
    {
        public DataResp(DataReq Req, int rxCount) : this(Req, new byte[RxCount(rxCount)], rxCount) { }

        public static int RxCount(int cnt)
        {
            return cnt < IConnection.MIN_RX_BUF ? IConnection.MIN_RX_BUF : cnt;
        }
    }

    /// <summary>
    /// UDP Socket or SerialPort
    /// </summary>
    public interface IConnection : IAbstractConnection
    {
        const int MIN_RX_BUF = 4096;
        // низкоуровневые методы
        Task Send(DataReq dataReq);
        Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);
        // высокоуровневые методы 
        Task<DataResp> Transaction(DataReq dataReq);
    }
    public interface INetConnection : IConnection
    {
        int Port { get; set; }
        string Host { get; set; }
    }
    public interface ISerialConnection : IConnection
    {
        //int DefaultBaudRate { get; set; }?
        int BaudRate { get; set; }
        string PortName { get; set; }
    }
    public interface IConnectionServer
    {
        IAbstractConnection? Get(string ConnectionID, object Subscruber);
        IAbstractConnection? Get(string ConnectionID);
        void Set(string ConnectionID, IAbstractConnection Connection, object Subscruber);

    }
}
