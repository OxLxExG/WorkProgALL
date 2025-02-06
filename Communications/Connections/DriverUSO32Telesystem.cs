using Connections.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Windows;

namespace Connections
{
    public delegate void OnUSO32DataHandler(DriverUSO32Telesystem sender, int data);

    public class DriverUSO32Telesystem : AbstractTransactionDriver
    {
        public ILogger? logger { get; set; }
        static object lockObj = new object();
        public static string[] FindUSO32SerialPort()
        {
            string[] ports = SerialPort.GetPortNames();
            List<string> USO32Ports = new List<string>();
            IConnectionServer? cs = (Application.Current as IServiceProvider)?.GetRequiredService<IConnectionServer>();

            foreach (string port in ports) 
            {
                var ac = cs?.Get(port);
                if (ac != null && ac.IsLocked) continue;
                lock (lockObj)
                {
                                        
                    var p = new SerialPort(port, 57600);
                    try
                    {
                        if (p.IsOpen) continue;
                        p.Open();
                        p.Write(new byte[] { 0xAA }, 0, 1);
                        if (ac is AbstractConnection a) a.OnRowSendEvent(new byte[] { 0xAA }, 0, 1);
                        Thread.Sleep(1);
                        var btr = p.BytesToRead;
                        var r = new byte[btr];
                        int rn = p.Read(r, 0, btr);
                        if (ac is AbstractConnection ra) ra.OnRowDataEvent(r, 0, rn);
                        if (rn == 1 && r[0] == 0x55) USO32Ports.Add(port);
                        p.Close();
                    }
                    finally
                    {
                        p.Dispose();
                    }
                };
            }
            return USO32Ports.ToArray();
        }
        public event OnUSO32DataHandler? OnUSO32Data;

        private byte[] rxBuf = new byte[IConnection.MIN_RX_BUF];   
        private ConcurrentQueue<byte[]> rxQ = new ConcurrentQueue<byte[]>();
        public bool Terminate { get; set; } = false;

        bool SetHP;
        bool ClrHP;
        Action? DoneHP;
        public void DoSetHP(Action Done)
        {
            DoneHP = Done;
            SetHP = true;
        }
        public void DoClrHP(Action Done)
        {
            DoneHP = Done;
            ClrHP = true;
        }

        private int ADCIdx;

        private bool Wachdog = false;
        private bool WachdogFlag = false;
        private Timer? timer = null;

        private void OnTimer(object? state)
        {
            if (WachdogFlag) WachdogFlag = false;
            else
            {
                Wachdog = true;
                timer?.Dispose();
                timer = null;
                (state as AbstractConnection)?.Cancel();
            }                    
        }

        public override void BeginTransaction(AbstractConnection Conn, DataReq dataReq)
        {

            int c = dataReq.txBuf[0] - 0x90;
            int fq = dataReq.txBuf[1];
            if (dataReq.txBuf.Length != 2 && 
                c >= 0 && c < 6 && fq <= 0x16 && dataReq.timout != -1) throw new Exception("USO32 Invalid Transaction Request");
            if (Conn is SerialConn serialConn)
            {
                serialConn.BaudRate = 57600;
            }
            Terminate = false;
            Terminated = false;
            ADCIdx = -1;
            rxQ.Clear();
            Wachdog = false;
            WachdogFlag = false;
            timer = new Timer(OnTimer, Conn, 1000, 300);
        }
        public async override Task<int> RowRead(AbstractConnection Conn, CancellationToken cancellationToken)
        {
            int n = await Conn.ReadAsync(rxBuf, 0, rxBuf.Length, cancellationToken);            
            if (n == 1 && !Terminate)
            {
                Thread.Sleep(0);
                n += await Conn.ReadAsync(rxBuf, 1, rxBuf.Length-1, cancellationToken);
            }
            WachdogFlag = true;
            return n;
        }
        private bool Terminated = false;
        public override async void Produce(AbstractConnection Conn, int Count)
        {
            var received = new byte[Count];
            Array.Copy(rxBuf, 0, received, 0, Count);
            rxQ.Enqueue(received);
            Conn.ProdusEvent.Set();

            if (Terminate && !Terminated)
            {
                await Conn.Send(new DataReq(new byte[] { 0x10 }, 1));
                Conn.OnRowSendEvent(new byte[] { 0x10 }, 0, 1);
            }
            if (SetHP)
            {
                var d = new DataReq(new byte[] { 0x71 }, 1);
                await Conn.Send(d);
                Conn.OnRowSendEvent(d.txBuf, 0, d.txBuf.Length);
                SetHP = false;
                DoneHP?.Invoke();
            }
            if (ClrHP)
            {
                var d = new DataReq(new byte[] { 0x72 }, 1);
                await Conn.Send(d);
                Conn.OnRowSendEvent(d.txBuf, 0, d.txBuf.Length);
                ClrHP = false;
                DoneHP?.Invoke();
            }
        }
        private byte[] ADCData = new byte[4];
        public override void Consume(AbstractConnection Conn)
        {
            while (rxQ.TryDequeue(out var received))
            {
                foreach (var di in received )
                {
                    if (Terminate && di == 0x55 && !Terminated)
                    {
                        Terminated = true;
                        Conn.EndReadEvent.Set();
                        break;
                    }
                    if (ADCIdx == -1)
                    {
                        if (di == 0) ADCIdx = 0;
                        continue;
                    }
                    if (ADCIdx < 4 && ADCIdx >= 0)
                    {
                        ADCData[ADCIdx++] = di;
                        continue;
                    }
                    if (ADCIdx == 4 && di == 0xff)
                    {
                        OnUSO32Data?.Invoke(this, BinaryPrimitives.ReverseEndianness(BitConverter.ToInt32(ADCData, 0)));
                        ADCIdx = -1;
                    }
                    else
                    {
                        ADCIdx = -1;

                        for (int i = 0; i < 4; i++)
                        {
                            if (ADCData[i] == 0)
                            {
                                for (int k = 0, j = i + 1; j < 4; j++, k++)
                                {
                                    ADCData[k] = ADCData[j];
                                }
                                ADCIdx = 3 - i;
                                break;
                            }
                        }
                    }
                }
                Conn.OnRowDataEvent(received, 0, received.Length);
            }
        }
        public override DataResp EndTransaction(AbstractConnection Conn, DataReq dataReq, int EndStatus)
        {
            DataResp r;
            timer?.Dispose();
            timer = null;

            if (EndStatus == 1) r = new DataResp(dataReq, -1);
            else r = new DataResp(dataReq, new byte[] { 0x55}, 1);

            dataReq.OnResponse?.Invoke(Conn, r);

            return r;

        }

    }
}
