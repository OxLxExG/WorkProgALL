using CommunityToolkit.Mvvm.Input;
using Connections;
using Connections.Interface;
using Core;
using Global;
using Loggin;
using Microsoft.Extensions.DependencyInjection;
using ScottPlot;
using Serilog;
using Serilog.Sinks.RichTextBox.Themes;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Xml.Serialization;
//using ScottPlot;

namespace HorizontDrilling.ViewModels
{
    [RegService(null, IsSingle: false, Advanced: AdvancedRegs.Form)]
    public class MonitorVM : TextLogVM
    {
        public int QueueLength { get; set; } = 30;
        public int TextBlockLength { get; set; } = 400;
        public int DataLen { get; set; } = 30;

        WeakReference<CheckToolButton>? _txonlyCTB;
        bool _TxOnly;
        protected override void FreezeLogger()
        {
            var c = con;
            if (c != null && c.monitorLevel != null)
                c.monitorLevel.MinimumLevel = Freeze ? Logger.LevelStop : TxOnly ? Logger.LevelMonitorTx : Logger.LevelMonitor;
        }

        [XmlIgnore] public bool TxOnly { get => _TxOnly;
            set
            {
                if (SetProperty(ref _TxOnly, value))
                {
                    if (_txonlyCTB != null && _txonlyCTB.TryGetTarget(out var t) && t != null) t.IsChecked = value;
                    FreezeLogger();
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
                //IconSource = "pack://application:,,,/Images/TxOnly.png",
                Content = "pack://application:,,,/Images/TxOnly.png",
                Size = 40,
                Priority = 1003,
                IsChecked = _TxOnly,
                Command = new RelayCommand(() => TxOnly = !TxOnly),
            };
            _txonlyCTB = new WeakReference<CheckToolButton>(ctb);
            ToolBarServer.Add("ToolGlyph", ctb);
            DynAdapter.DynamicItems.Add(ctb);
        }
        //CommandMenuItemVM crx;
        public MonitorVM()
        {            
            IconSource = "\ue7f8";// "pack://application:,,,/Images/Monitor16.png";
            CanClose = true;
            PropertyChanged += OnPropertyChangedEvent;
            var crx = new CommandMenuItemVM
            {
                Header = Properties.Resources.m_Freeze + " Rx",
                IconSource = "pack://application:,,,/Images/TxOnly.png",
                IsChecked = _TxOnly,
                IsCheckable = true,
                Command = new RelayCommand(() => TxOnly = !TxOnly),

            };
            CItems.Insert(0, new Core.Separator());
            CItems.Insert(0, crx);
        }
        protected AbstractConnection? con 
        { 
            get 
            {
                IConnectionServer cs = ServiceProvider.GetRequiredService<IConnectionServer>();
                var c = cs.Get(Title);
                if (c == null) throw new Exception($"{Title} not found");
                if (c is AbstractConnection ac)
                {
                    return ac;
                }
                return null; 
            } 
        }
        public void CreateMonitor( RichTextBox Box )
        {
            var c = con;
            if (c is AbstractConnection ac && !string.IsNullOrEmpty(ContentID))
            {
                if (ac.monitor == null)
                {
                    ac.monitorLevel = new Serilog.Core.LoggingLevelSwitch(Logger.LevelMonitor);
                    ac.monitor = new LoggerConfiguration()
                    .MinimumLevel.ControlledBy(ac.monitorLevel)
                    //.WriteTo.RichTextBox(LogBoxContainer.GetOrCteate(ContentID),
                    .WriteTo.RichTextBox(Box,
                    outputTemplate: "{NewLine}{Timestamp:ss.fff} {Message:lj}",
                    theme: RichTextBoxConsoleTheme.Monitor,
                    syncRoot: new object())
                    .CreateLogger();
                    FreezeLogger();
                }
            }
        }
        public void DisposeMonitor()
        {
            var c = con;
            if (c != null)
            {
                c.monitor = null;
                c.monitorLevel = null;
            }
        }

        public override void Close()
        {
            PropertyChanged -= OnPropertyChangedEvent;
            DisposeMonitor();
            LogBoxContainer.Remove(ContentID!);
            base.Close();
        }
        void OnPropertyChangedEvent(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ContentID")
            {
                StdLoggs opt = ServiceProvider.GetRequiredService<StdLoggs>();
                if (!opt.Box.Monitor)
                {
                    Task.Run(() => { Thread.Sleep(10); Close(); });
                    return;
                }
                Title = ContentIDs![1];
                ToolTip = Title;
                //CreateMonitor();
                //FreezeLogger();
            }
        }
    }
}
