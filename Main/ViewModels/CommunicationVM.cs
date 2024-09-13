using Connections;
using Connections.Interface;
using Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Xml.Serialization;

namespace Main.ViewModels
{
    //for extention
    public static class CommunicationVM
    {
    }
    public abstract class ConVM : VMBase
    {
        private WeakReference<AbstractConnection>? _wcon;

        protected AbstractConnection? con
        {
            get 
            {
                AbstractConnection? c = null;
                _wcon?.TryGetTarget(out c);
                return c;
            } 
            private set => _wcon = new WeakReference<AbstractConnection>(value!);
            
        }
        protected abstract string ConID();
        protected abstract IAbstractConnection Create();

        protected void UpdateConn()
        {
            IConnectionServer cs = ServiceProvider.GetRequiredService<IConnectionServer>();
            var c = cs.Get(ConID(), this);
            if (c == null)
            {
                c = Create();
                cs.Set(ConID(), c, this);
                con = (AbstractConnection) c;
            }
        }

        [XmlIgnore] public IConnection? Connection => con != null ? con as IConnection : null;
    }
    public class SerialVM : ConVM
    {
        protected override string ConID() => _PortName;
        protected override IAbstractConnection Create() => new SerialConn { BaudRate = _BaudRate, PortName = _PortName, };

        string _PortName = "COM1";
        [XmlIgnore] public string PortName
        {
            get => _PortName; 
            set
            {
                if (SetProperty(ref _PortName, value)) UpdateConn();
                    if (con != null) UpdateConn(); 
            }
        }
        int _BaudRate = 125000;
        [XmlIgnore] public int BaudRate
        {
            get => _BaudRate; set
            {
                if (SetProperty(ref _BaudRate, value)) 
                    if (con != null) Serial.BaudRate = _BaudRate;
            }
        }
        [XmlIgnore] public SerialConn Serial
        {
            get
            {
                if (con == null) UpdateConn();
                return (SerialConn) con!;
            }
        }
    }
    public class NetVM : ConVM
    {
        protected override string ConID() => $"{_Host}:{_Port}";
        protected override IAbstractConnection Create() => new NetConn { Host = _Host, Port = _Port, };

        string _Host = "192.168.4.1";
        [XmlIgnore] public string Host
        {
            get => _Host; set
            {
                if (SetProperty(ref _Host, value)) 
                    if (con != null) UpdateConn();
            }
        }
        int _Port = 5000;
        [XmlIgnore] public int Port
        {
            get => _Port; set
            {
                if (SetProperty(ref _Port, value)) 
                    if (con != null) UpdateConn();
            }
        }
        [XmlIgnore] public NetConn Net
        {
            get
            {
                if (con == null) UpdateConn(); 
                return (NetConn)con!;
            }
        }
    }

}
