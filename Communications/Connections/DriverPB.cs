using Connections.Interface;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CRCModbusRTU;

namespace Connections
{
    /// <summary>
    /// Драйвер транзакций для протокола процесса бурения (onewire)
    /// </summary>
    public class TransactionDriverPB: AbstractTransactionDriver
    {
        //current buffer
        private volatile int offset = 0;
        // нужно для события монитора (не удалять!)
        private volatile int oldOffset = 0;
        private byte[] rxBuf = new byte[IConnection.MIN_RX_BUF];
        //// current CRC
        private volatile int crcOldOffset = 0;
        private volatile int crcOffset = 0;
        private ushort _crc;
        private int rxAll = 0;

        public override void BeginTransaction(AbstractConnection Conn, DataReq dataReq)
        {
            rxBuf = new byte[DataResp.RxCount(dataReq.rxCount)];
            oldOffset = 0;
            offset = 0;
            crcOldOffset = 0;
            crcOffset = 0;
            _crc = 0xFFFF;
            rxAll = dataReq.rxCount;
        }
        public async override Task<int> RowRead(AbstractConnection Conn, CancellationToken cancellationToken)
        {
            // if (offset > 200) App.logger?.LogInformation("I");
            int cntout = await Conn.ReadAsync(rxBuf, offset, rxAll - offset, cancellationToken);
            if (cntout == 0) Conn.logger?.LogInformation("Z");
            int last = rxAll - offset - cntout;
            if (cntout > 0 && cntout < 3 && last > 0) // slim
            {
                Thread.Sleep(0);
                cntout += await Conn.ReadAsync(rxBuf, offset + cntout, last, cancellationToken);
            }
            return cntout;
            //if (offset > 200) App.logger?.LogInformation("E");
        }

        public override void Produce(AbstractConnection Conn, int Count)
        {
            if (rxAll - offset <= 0)
            {
                Conn.logger?.LogInformation("CNT = 0 !!!");
                return;
            }
            if (Count + offset > rxBuf.Length) { throw new Exception("Buffer OverFlow"); }

            if (Count > 0)
            {
                lock (Conn)
                {
                    oldOffset = offset;
                    offset += Count;
                    if (offset >= rxAll) Conn.IsReading = false;
                    Conn.ProdusEvent.Set();
                }
            }
        }

        public override void Consume(AbstractConnection Conn)
        {
            lock (Conn)
            {
                crcOldOffset = crcOffset;
                crcOffset = offset;
                int n = crcOffset - crcOldOffset;
                if (n > 0) _crc = Crc.ComputeCrc(_crc, rxBuf, crcOldOffset, n);
                // двойные события возможны, их игнорируем
                else return;
            }

            //if (of > 30000) logger?.LogInformation($"[Old: {crcOldOffset} New: {crcOffset} {of}]");


            if (crcOffset >= Conn.CurrenRq?.rxCount)
            { 
                if (_crc == 0) Conn.EndReadEvent.Set();
            }

            Conn.OnRowDataEvent(rxBuf, oldOffset, offset);
        }

        public override DataResp EndTransaction(AbstractConnection Conn, DataReq dataReq, int EndStatus)
        {
            int cnt;
            if (EndStatus == 1) cnt = -2; // hAbort
            else if (EndStatus != 0) cnt = -1; // dataReq.timout
            else cnt = offset; //EndReadEvent

            if (dataReq.rxCount != offset)
                Conn.logger?.LogInformation($"{Thread.CurrentThread.ManagedThreadId} {Conn.dbg} ReadTask {dataReq.rxCount} != {offset}");
            else if (cnt < 0)
                Conn.logger?.LogInformation($"{Thread.CurrentThread.ManagedThreadId} {Conn.dbg} ReadTask BAD CRC!!!  0 != {_crc:x4}");

            var r = new DataResp(dataReq, rxBuf, cnt);
            // time out for monitor
            if (cnt < 0) Conn.OnRowDataEvent(rxBuf, 0, cnt);
            dataReq.OnResponse?.Invoke(this, r);
            return r;
        }

    }
}
