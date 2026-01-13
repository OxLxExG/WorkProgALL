using Connections.Interface;
using System.Net.Sockets;

namespace Connections
{
    public class NetConn : AbstractConnection, INetConnection, IDisposable
    {
        private string host;
        private int port;
        private Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        static NetConn()
        {
            _ConnectionTypes.Add(typeof(NetConn));
        }
        public override string? ToString()
        {
            return $"{Host}:{Port}";
        }

        public NetConn() : base()
        {
            host = "192.168.4.1";
            port = 5000;
        }
        public string Host
        {
            get => host;
            set
            {
                if (IsOpen) throw new Exception("errror");
                host = value;
            }
        }
        public int Port
        {
            get => port;
            set
            {
                if (IsOpen) throw new Exception("errror");
                port = value;
            }
        }
        public override bool IsOpen { get { return socket.Connected; } }
        public override Task Close(int timout = 5000)
        {
            socket.Close();
            return base.Close(timout);
        }
        public override Task Open(int timout, bool RxNeed = true    )
        {
            return Task.Run(async () =>
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                using (CtsCancel = new CancellationTokenSource())
                {
                    try
                    {
                        await base.Open(timout);

                        RxProduserThread!.Name = "====netThread====";

                        await socket.ConnectAsync(host, port, CtsCancel.Token);
                    }
                    finally
                    {
                        CtsCancel = null;
                        if (!socket.Connected) socket.Close();
                    }
                }
            });
        }
        public override Task Send(DataReq dataReq)
        {
            return Task.Run(async () =>
            {
                using (CtsCancel = new CancellationTokenSource(dataReq.timout))
                {
                    try
                    {
                        await socket.SendAsync(new ArraySegment<byte>(dataReq.txBuf), SocketFlags.None, CtsCancel.Token);
                    }
                    finally
                    {
                        CtsCancel = null;
                    }
                }
            });
        }
        private bool IsDisposed
        {
            get
            {
                try
                {
                    var r = socket.RemoteEndPoint;
                    return false;
                }
                catch (ObjectDisposedException)
                {
                    return true;
                }
            }
        }
        public override async Task<int> ReadAsync(byte[] buffer, int offs, int count, CancellationToken cancellationToken)
        {
            var m = new Memory<byte>(buffer, offs, count);
            return await socket.ReceiveAsync(m, SocketFlags.None, cancellationToken);
        }

        //protected override async Task<int> DoRead(int cntin)
        //{
        //    int cntout = 0;
        //    //var m = new Memory<byte>(rxBuf, offset, cntin - offset);
        //    while (IsCanceled && cntout == 0)
        //    {
        //        cntout =  await ReadAsync(rxBuf, offset, cntin - offset, Cts != null ? Cts.Token : default)
        //            //await socket.ReceiveAsync(m, SocketFlags.None, Cts != null ? Cts.Token : default);
        //    }
        //    return cntout;
        //}

        //protected override async void ProduserThreadRun(CancellationToken token)
        //{
        //    while (true)
        //    {
        //        try
        //        {
        //            if (token.IsCancellationRequested) return;
        //            int cntin = currenRq != null ? currenRq.rxCount : rxBuf.Length;
        //            if (!socket.Connected || !IsReading || cntin <= 0)
        //            {
        //                continue;
        //            }
        //            try
        //            {
        //                int cntout = 0;
        //                var cnt = cntin - offset;
        //                if (cnt == 0)
        //                {
        //                    logger?.LogInformation("CNT = 0 !!!");
        //                    continue;
        //                }

        //                var m = new Memory<byte>(rxBuf, offset, cnt);
        //                while (socket.Connected && cntout == 0)
        //                {
        //                    cntout = await socket.ReceiveAsync(m, SocketFlags.None, Cts != null ? Cts.Token : default);
        //                }

        //                if (cntout + offset > rxBuf.Length) { throw new Exception("Buffer OverFlow"); }


        //                if (Cts != null && Cts.IsCancellationRequested)
        //                {
        //                    IsReading = false;
        //                    continue;
        //                }
        //                lock (_lock)
        //                {
        //                    oldOffset = offset;
        //                    offset += cntout;
        //                }

        //                if (offset >= cntin)
        //                {
        //                    IsReading = false;
        //                    logger?.LogInformation("IsReading END: {cnt} {off}", cntout, offset);
        //                }
        //                ProdusEvent.Set();
        //                // App.logger?.LogTrace("I:{cnt} {off}", cntout, offset);
        //            }
        //            catch (Exception e)
        //            {
        //                logger?.LogInformation($"ERR {Thread.CurrentThread.ManagedThreadId} {dbg} {e}");
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            logger?.LogError(e.Message, e);
        //        }
        //    }

        //}
    }
}
