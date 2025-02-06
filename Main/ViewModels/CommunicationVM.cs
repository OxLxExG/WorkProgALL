using CommunityToolkit.Mvvm.Input;
using Connections;
using Connections.Interface;
using Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Input;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Main.ViewModels
{
    //for extention
    public static class CommunicationVM
    {
    }
    public abstract class ConVM : VMBase
    {
        ICommand? _ShowMonitor;
        [XmlIgnore] public ICommand? ShowMonitor { get => _ShowMonitor; set => SetProperty(ref _ShowMonitor, value); }

        [XmlIgnore] public bool VisibilityMonitor
        {
            get
            {
                GlobalSettings opt = ServiceProvider.GetRequiredService<GlobalSettings>();
                return opt.Logging.Box.Monitor;
            }
        }

    }
    public class NopConVM : ConVM;
    public abstract class SNConVM : ConVM
    {

        private WeakReference<AbstractConnection>? _wcon;

        void OnShowMonitor()
        {
            var f = (MonitorVM) DockManagerVM.AddOrGetandShow($"{nameof(MonitorVM)}.{ConID()}", FormAddedFrom.User);
            f.BindConnection();
        }
        protected AbstractConnection? con
        {
            get 
            {
                AbstractConnection? c = null;
                _wcon?.TryGetTarget(out c);
                return c;
            }
            private set 
            {

                if (_wcon != null && _wcon.TryGetTarget(out var c) && c == value) return;

                _wcon = new WeakReference<AbstractConnection>(value!);

                if (VisibilityMonitor)
                {
                    ShowMonitor = new RelayCommand(OnShowMonitor);

                    var f = (MonitorVM?)DockManagerVM.Contains($"{nameof(MonitorVM)}.{ConID()}");
                    if (f != null) f.BindConnection();
                }
            }
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
            }
            con = (AbstractConnection)c;
        }

        [XmlIgnore] public IConnection? Connection => con != null ? con as IConnection : null;       
    }
    public class SerialVM : SNConVM
    {
        protected override string ConID() => _PortName;
        protected override IAbstractConnection Create() => new SerialConn { PortName = _PortName, BaudRate = _BaudRate, };

        string _PortName = "COM1";
        public string PortName
        {
            get => _PortName; 
            set
            {
                if (SetProperty(ref _PortName, value)) UpdateConn();
                else if (con == null) UpdateConn();
                if (con != null) Serial.BaudRate = _BaudRate;
            }
        }
        [XmlIgnore] public bool SerializePortName = true;
        public bool ShouldSerializePortName() => SerializePortName;

        int _BaudRate = 9600;
        public int BaudRate
        {
            get => _BaudRate; set
            {
                if (SetProperty(ref _BaudRate, value)) 
                    if (con != null) Serial.BaudRate = _BaudRate;
            }
        }
        [XmlIgnore] public bool SerializeBaudRate = true;
        public bool ShouldSerializeBaudRate() => SerializeBaudRate;

        [XmlIgnore] public SerialConn Serial
        {
            get
            {
                if (con == null) UpdateConn();
                return (SerialConn) con!;
            }
        }
    }
    public class NetVM : SNConVM
    {
        protected override string ConID() => $"{_Host}:{_Port}";
        protected override IAbstractConnection Create() => new NetConn { Host = _Host, Port = _Port, };

        string _Host = "192.168.4.1";
        public string Host
        {
            get => _Host; set
            {
                if (SetProperty(ref _Host, value)) 
                    if (con != null) UpdateConn();
            }
        }
        [XmlIgnore] public bool SerializeHost = true;
        public bool ShouldSerializeHost() => SerializeHost;

        int _Port = 5000;
        public int Port
        {
            get => _Port; set
            {
                if (SetProperty(ref _Port, value)) 
                    if (con != null) UpdateConn();
            }
        }
        [XmlIgnore] public bool SerializePort = true;
        public bool ShouldSerializePort() => SerializePort;

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
