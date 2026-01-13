using CommunityToolkit.Mvvm.Input;
using Connections;
using Connections.Interface;
using Core;
using Global;
using Loggin;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Sinks.RichTextBox.Themes;
using System;
using System.Windows.Input;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace HorizontDrilling.ViewModels
{
    //for extention
    public static class CommunicationVM
    {
    }
    public abstract class ConVM : VMBase
    {
        protected virtual void OnShowMonitor() { }

        public ConVM()
        {
           if (VisibilityMonitor) ShowMonitor = new RelayCommand(OnShowMonitor);
        }
        ICommand? _ShowMonitor;
        [XmlIgnore] public ICommand? ShowMonitor { get => _ShowMonitor; set => SetProperty(ref _ShowMonitor, value); }

        [XmlIgnore] public bool VisibilityMonitor
        {
            get
            {
                StdLoggs opt = ServiceProvider.GetRequiredService<StdLoggs>();
                return opt.Box.Monitor;
            }
        }

    }
    public class NopConVM : ConVM;
    
    public abstract class SNConVM : ConVM
    {
        public SNConVM()  :base()
        {
            UpdateConn();            
        }
        protected override void OnShowMonitor()=> DockManagerVM.AddOrGetandShow($"{nameof(MonitorVM)}@{ConID()}", FormAddedFrom.User);
        protected abstract string ConID();
        protected abstract IAbstractConnection Create();

        //public void DisposeMonitor()
        //{
        //    IConnectionServer cs = ServiceProvider.GetRequiredService<IConnectionServer>();
        //    var c = cs.Get(ConID(), this);
        //    if (c is AbstractConnection ac)
        //    {
        //        ac.monitor = null;
        //        ac.monitorLevel = null;
        //    }
        //}
        protected AbstractConnection? UpdateConn()
        {
            IConnectionServer cs = ServiceProvider.GetRequiredService<IConnectionServer>();
            var c = cs.Get(ConID(), this);
            if (c == null)
            {
                c = Create();
                cs.Set(ConID(), c, this);
            }
            //if (c is AbstractConnection ac)
            //{
            //    if (ac.monitor == null)
            //    {
            //        ac.monitorLevel = new Serilog.Core.LoggingLevelSwitch(Logger.LevelMonitor);
            //        ac.monitor = new LoggerConfiguration()
            //        .MinimumLevel.ControlledBy(ac.monitorLevel)
            //        .WriteTo.RichTextBox(LogBoxContainer.GetOrCteate($"MonitorVM@{ConID()}"), 
            //        outputTemplate: "{Timestamp:ss.fff} {Message:lj}{NewLine}",
            //        theme: RichTextBoxConsoleTheme.Monitor,
            //        syncRoot: new object())
            //        .CreateLogger();
            //    }
            //}
            return c as AbstractConnection;
        }
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
                SetProperty(ref _PortName, value); 
                Serial.BaudRate = _BaudRate;
            }
        }
        [XmlIgnore] public bool SerializePortName = true;
        public bool ShouldSerializePortName() => SerializePortName;

        int _BaudRate = 9600;
        public int BaudRate
        {
            get => _BaudRate; set
            {
                SetProperty(ref _BaudRate, value); 
                Serial.BaudRate = _BaudRate;
            }
        }
        [XmlIgnore] public bool SerializeBaudRate = true;
        public bool ShouldSerializeBaudRate() => SerializeBaudRate;

        [XmlIgnore] public SerialConn Serial
        {
            get
            {
                return (SerialConn) UpdateConn()!;
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
                SetProperty(ref _Host, value); 
                UpdateConn();
            }
        }
        [XmlIgnore] public bool SerializeHost = true;
        public bool ShouldSerializeHost() => SerializeHost;

        int _Port = 5000;
        public int Port
        {
            get => _Port; set
            {
                SetProperty(ref _Port, value); 
                UpdateConn();
            }
        }
        [XmlIgnore] public bool SerializePort = true;
        public bool ShouldSerializePort() => SerializePort;

        [XmlIgnore] public NetConn Net
        {
            get
            {
                return (NetConn)UpdateConn()!;
            }
        }
    }

}
