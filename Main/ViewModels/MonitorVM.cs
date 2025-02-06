using Connections.Interface;
using Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Extensions.Logging.Console;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TextBlockLogging;
using Connections;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Threading;
using System.Windows.Controls;
using ScottPlot;

namespace Main.ViewModels
{
    public class ConnectionServer
    {
        public ConnectionServer() { }
    }
    public class MonitorVM: TextLogVM
    {
        public int QueueLength { get; set; } = 30;
        public int TextBlockLength {  get; set; } = 400;
        public int DataLen { get; set; } = 30;

        WeakReference<CheckToolButton>? _txonly;
        bool _TxOnly;        
        [XmlIgnore] public bool TxOnly { get => _TxOnly; 
            set 
            {
                if (SetProperty(ref _TxOnly, value))
                 {
                    if (_txonly != null && _txonly.TryGetTarget(out var t) && t != null) t.IsChecked = value;
                    if (monitor != null && con != null)
                    {
                        if (_TxOnly) con.OnRowDataHandler -= monitor.onRxData;
                        else con.OnRowDataHandler += monitor.onRxData;
                    }
                }
            }
        }
        
        protected override void ActivateDynItems()
        {
            base.ActivateDynItems();
            var ctb = new CheckToolButton
            {
                ToolTip = new ToolTip { Content = Properties.Resources.m_Freeze + " Rx (" + Title + ")" },
                ContentID = "txOnly" + ContentID,
                IconSource = "pack://application:,,,/Images/TxOnly.png",
                Priority = 1003,
                IsChecked = _TxOnly,
                Command = new RelayCommand(()=> TxOnly = !TxOnly),
            };
            _txonly = new WeakReference<CheckToolButton>(ctb);
            ToolBarServer.Add("ToolGlyph", ctb);
            DynAdapter.DynamicItems.Add(ctb);
        }

        TextBlockMonitor? monitor;

        private WeakReference<AbstractConnection>? _wcon;
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

                if (_wcon != null)
                {
                    if (_wcon.TryGetTarget(out var c))
                    {
                        if (c == value) return;
                        else if (c != null && monitor != null)
                        {
                            c.OnRowDataHandler -= monitor.onRxData;
                            c.OnRowSendHandler -= monitor.onTxData;
                        }
                    }
                }
                if (value != null && monitor != null)
                { 
                    value.OnRowDataHandler += monitor.onRxData;
                    value.OnRowSendHandler += monitor.onTxData;
                }
                if (value == null) _wcon = null;
                else _wcon = new WeakReference<AbstractConnection>(value);
            }
        }

        public MonitorVM() 
        {
            IconSource = new Uri("pack://application:,,,/Images/Monitor.png");
            CanClose = true;
            PropertyChanged += OnPropertyChangedEvent;
        }        
        public void BindConnection()
        {
            IConnectionServer cs = ServiceProvider.GetRequiredService<IConnectionServer>();
            var c = cs.Get(Title);
            if (c == null) throw new Exception($"{Title} not found");
            if (c is AbstractConnection ac) con = ac;
        }

        public override void Close()
        {
            if (monitor != null && con != null)
            {
                con.OnRowDataHandler -= monitor.onRxData;
                con.OnRowSendHandler -= monitor.onTxData;
            }

            PropertyChanged -= OnPropertyChangedEvent;
            base.Close();
        }
        void OnPropertyChangedEvent(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ContentID")
            {
                GlobalSettings opt = ServiceProvider.GetRequiredService<GlobalSettings>();
                if (!opt.Logging.Box.Monitor)
                {
                    Task.Run(() => { Thread.Sleep(10); Close(); });
                    return;
                }

                Title = ContentID!.Split(".")[1];
                ToolTip = Title;

                monitor = new TextBlockMonitor(ContentID,
                    ServiceProvider.GetRequiredService<ILogTextBlockService>(),
                    ConsoleLoggerQueueFullMode.DropWrite, QueueLength, TextBlockLength, DataLen);
            }
        }
    }
}
