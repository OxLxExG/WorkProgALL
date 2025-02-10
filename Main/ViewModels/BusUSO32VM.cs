using Communications;
using CommunityToolkit.Mvvm.Input;
using Connections;
using Connections.Interface;
using Connections.Uso32;
using Core;
using Main.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml.Linq;
using System.Xml.Serialization;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Main.ViewModels
{
    public class TelesysVM : DeviceVM
    {
        public TelesysVM() 
        {
            DynAdapter.OnActivateDynItems += ActivateDynItems;
            DynAdapter.OnDeActivateDynItems += DeActivateDynItems;
        }
        //protected override void Remove()
        //{
        //    base.Remove();
        //    DeActivateDynItems();
        //}
        private void DeActivateDynItems()
        {
            var l = ServiceProvider.GetRequiredService<ILogger<TelesysVM>>();
            l.LogTrace("~~DeActivateDynItems {} ", ContentID);

            if (Parent is BusUSO32VM buso)
            {
                //buso.DeActivateDynItems();
                buso.DynAdapter.UserDeactivate();
            }
            
        }

        private void ActivateDynItems()
        {
            if (Parent is BusUSO32VM buso)
            {
                buso.DynAdapter.UserActivate();
            }
            var m = new MenuItemVM { ContentID = "Tlsys1", Header = "Setup Telesys", Priority = 2 };
            MenuItemServer.Add("ROOT", m);
            //var range = new PriorityItemBase[]  { m };
            DynAdapter.DynamicItems.Add(m);

            var l = ServiceProvider.GetRequiredService<ILogger<TelesysVM>>();
            l.LogTrace("ActivateDynItems {} ", ContentID);
        }
    }
    public enum USO32_State {
        USO32_Idle_FindPort = 0, // ожидание  по таймеру 
        USO32_FindPort,//   ОБМЕН  по таймеру
        USO32_FoundPort,// найден порт idle? stop timer

        USO32_Start,// нажатие на кнопку togle RUN или USO32_FindPort по таймеру или старт(RUN)
        USO32_Run0, // <0
        USO32_Run1, // >0

        USO32_Terminate, // отпускание кнопки togle RUN запретить событие USO32_Run 
        USO32_Terminated, //idle? разрешить событие USO32_Run 

        USO32_SetHP, // при нажатии кнопки SetHP запретить событие USO32_Run
                     // пока флаг DriverUSO32Telesystem.SetHP = true затем разрешить событие USO32_Run 
        USO32_ClearHP, // при нажатии кнопки ClearHP запретить событие USO32_Run
                       // пока флаг DriverUSO32Telesystem.ClrHP = true затем разрешить событие USO32_Run

        USO32_ErrorPort, // idle?
    }

    public delegate void OnUSODataHandler(BaseBusUSOVM sender, int data);
    public class BaseBusUSOVM: BusVM
    {
        public event OnUSODataHandler? OnUSODataEvent;
        protected static void InvokeUSODataEvent(BaseBusUSOVM sender, int data) => sender.OnUSODataEvent?.Invoke(sender, data);
    }
    public class BusUSO32VM : BaseBusUSOVM
    {
        #region Name
        const string NAME = "USO32";
        public override bool ShouldSerializeName() => Name != NAME;
        #endregion

        #region Low Pass Filter
        static Dictionary<string, int> FRQ = Enum.GetValues<Uso32_Freqs>().Cast<int>().ToDictionary<int, string>(
            n => Enum.GetName<Uso32_Freqs>((Uso32_Freqs)n)!
                    .Replace("fq", "")
                    .Replace("_", "."));
        public string Freq { get => tbxFQ.Text; set => tbxFQ.Text = value; }
        public bool ShouldSerializeFreq() => Freq != "20Hz";
        [XmlIgnore] public ToolComboBox tbxFQ { get; init; }
        ToolComboBox initLPF()
        {
           var r = new ToolComboBox
            {
                ToolTip = new ToolTip { Content = $"USO32 FQ" },
                ContentID = "FQ" + ContentID,
                Priority = 110,
                ItemsSource = FRQ.Keys.ToArray().Order(Comparer<string>.Create(
                    (x, y) => (int)((float.Parse(y.Replace("Hz", "")) - float.Parse(x.Replace("Hz", ""))) * 10))),
                Text = "20Hz",
            };
            r.PropertyChanged += (s, e) => SetDrity(true);
            return r;
        }
        #endregion

        #region Bias
        static Dictionary<string, int> AMP = Enum.GetValues<Uso32_Bias>().Cast<int>().ToDictionary<int, string>(
            n => Enum.GetName<Uso32_Bias>((Uso32_Bias)n)!
                    .Replace("b", ""));
        [XmlIgnore] public ToolComboBox tbxAMP { get; init; }
        public string Amp { get => tbxAMP.Text; set => tbxAMP.Text = value; }
        public bool ShouldSerializeAmp() => Amp != "1";
        ToolComboBox initAmp()
        {
            var r = new ToolComboBox
            {
                ToolTip = new ToolTip { Content = $"USO32 AMP" },
                ContentID = "AMP" + ContentID,
                Priority = 111,
                ItemsSource = AMP.Keys.ToArray(),
                Text = "1",
            };
            r.PropertyChanged += (s, e) => SetDrity(true);
            return r;
        }
        #endregion

        #region visibility FQA
        bool _ShowInToolFQA = true;
        public bool ShowInToolBarFQA
        {
            get => _ShowInToolFQA;
            set
            {
                if (_ShowInToolFQA != value)
                {
                    SetDrity(true);

                    _ShowInToolFQA = value;
                    if (value)
                    {
                        tbxFQ.Visibility = Visibility.Visible;
                        tbxAMP.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        tbxFQ.Visibility = Visibility.Collapsed;
                        tbxAMP.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }
        private void fqa_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsChecked" && sender is MenuItemVM m)
            {
                ShowInToolBarFQA = m.IsChecked;
            }
        }
        public bool ShouldSerializeShowInToolBarFQA() => _ShowInToolFQA == false;
        #endregion

        #region HP
        bool _ShowInToolBarHP;
        public bool ShowInToolBarHP { get=>_ShowInToolBarHP; 
            set
            {
                if (_ShowInToolBarHP != value)
                {
                    SetDrity(true);

                    _ShowInToolBarHP = value;
                    if (value)
                    {
                        ctbSetHP.Visibility = Visibility.Visible;
                        ctbClrHP.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        ctbSetHP.Visibility = Visibility.Collapsed;
                        ctbClrHP.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }
        private void Shp_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsChecked" && sender is MenuItemVM m)
            {
                ShowInToolBarHP = m.IsChecked;
            }
        }

        public bool ShouldSerializeShowInToolBarHP() => _ShowInToolBarHP == true;
        protected CheckToolButton ctbSetHP { get; init; }
        protected CheckToolButton ctbClrHP { get; init; }
        void OnBtnHP(USO32_State s)
        {
            StatusRunEnable = false;
            AutomatUpdateSate(s);
        }
        CheckToolButton initHP(string icon, string toolTip, int priority, USO32_State s)
        {
            return new CheckToolButton
            {
                ToolTip = new ToolTip { Content = toolTip },
                ContentID = "HP" + ContentID,
                IconSource = icon,
                Priority = priority,
                IsEnable = false,
                Visibility = Visibility.Collapsed,
                Command = new RelayCommand(() => OnBtnHP(s)),
            };
        }
        #endregion

        #region Start State Button
        #region USO State
        static Dictionary<USO32_State, string> ICS = new Dictionary<USO32_State, string>
        {
            {USO32_State.USO32_Idle_FindPort, "pack://application:,,,/Images/FindPortidle.png"},
            {USO32_State.USO32_FindPort, "pack://application:,,,/Images/FindPort.png" },
            {USO32_State.USO32_FoundPort, "pack://application:,,,/Images/FoundPort.png"},

            {USO32_State.USO32_Start, "pack://application:,,,/Images/Uso32Start.png"},
            {USO32_State.USO32_Run0, "pack://application:,,,/Images/Uso32Run0.png"},
            {USO32_State.USO32_Run1, "pack://application:,,,/Images/Uso32Run1.png"},

            {USO32_State.USO32_Terminate, "pack://application:,,,/Images/Uso32Terminate.png"},
            {USO32_State.USO32_Terminated, "pack://application:,,,/Images/Uso32Terminated.png"},

            {USO32_State.USO32_SetHP, "pack://application:,,,/Images/Uso32SetHP.png"},
            {USO32_State.USO32_ClearHP, "pack://application:,,,/Images/Uso32ClrHP.png"},

            {USO32_State.USO32_ErrorPort, "pack://application:,,,/Images/Uso32Err.png"},
        };
        static Dictionary<USO32_State, string> TCS = new Dictionary<USO32_State, string>
        {
            {USO32_State.USO32_Idle_FindPort,  "USO32: Weit to find port"},
            {USO32_State.USO32_FindPort, "USO32: Find port now" } ,
            {USO32_State.USO32_FoundPort, "USO32: Port found" },

            {USO32_State.USO32_Start, "USO32: Start" },
            {USO32_State.USO32_Run0, "USO32: <0" },
            {USO32_State.USO32_Run1, "USO32: >0" },

            {USO32_State.USO32_Terminate, "USO32: stop"},
            {USO32_State.USO32_Terminated, "USO32: Terminated"},

            {USO32_State.USO32_SetHP, "USO32: On HP now"},
            {USO32_State.USO32_ClearHP, "USO32: Off HP now"},

            {USO32_State.USO32_ErrorPort, "USO32: Error port"},
        };

        USO32_State _State;
        [XmlIgnore]
        public USO32_State State
        {
            get => _State;
            set
            {
                if (SetProperty(ref _State, value))
                {
                    if (Application.Current != null)
                        Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
                            () => ctbStart.IconSource = ICS[value]);
                    

                    if (value == USO32_State.USO32_Run0 || value == USO32_State.USO32_Run1) return;

                    if (Application.Current != null)
                        Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
                            () => ctbStart.ToolTip = new ToolTip { Content = TCS[value] });
                    else
                    {
                        FindPortTimer?.Dispose();
                        FindPortTimer = null;
                    }
                }
            }
        }
        #endregion

        protected CheckToolButton ctbStart { get; init; }
        CheckToolButton initStart()
        {
            return new CheckToolButton
            {
                ContentID = "RUN" + ContentID,
                Priority = 101,
                Command = new RelayCommand(OnBtnStart),
            };
        }
        bool IsStartUso { get; set; }
        void OnBtnStart()
        {
            IsStartUso = !IsStartUso;
            if (IsStartUso == false) StatusRunEnable = false;

            var l = ServiceProvider.GetRequiredService<ILogger<BusUSO32VM>>();
            l.LogTrace("---OnBtnStart--- {} IsStartUso={} StatusRunEnable={} ", Name, IsStartUso, StatusRunEnable);

            AutomatUpdateSate(USO32_State.USO32_Start);
        }
        #endregion

        #region Show Osc
        [XmlIgnore] public ICommand OscCommand { get; init; } 
        void DoOscCommand()
        {
           var f = DockManagerVM.AddOrGetandShow($"{nameof(OscUSO32VM)}.{ContentID!}", FormAddedFrom.User);            
           GetRoot()?.AddChildForm(f);
        }
        #endregion

        static BusUSO32VM()
        {
            ConnectionCash.logger = VMBase.ServiceProvider.GetRequiredService<ILogger<BusUSO32VM>>();
        }
        public BusUSO32VM()
        {
            ContentID = Guid.NewGuid().ToString("D");
            Name = NAME;
            DynAdapter.OnActivateDynItems += ActivateDynItems;
            DynAdapter.OnDeActivateDynItems += DeActivateDynItems;

            OscCommand = new RelayCommand(DoOscCommand);

            tbxFQ = initLPF();
            tbxAMP = initAmp();
            ctbStart = initStart();
            ctbSetHP = initHP("pack://application:,,,/Images/Uso32SetHP.png", "USO32 Вкл HP", 102, USO32_State.USO32_SetHP);
            ctbClrHP = initHP("pack://application:,,,/Images/Uso32ClrHP.png", "USO32 Выкл HP", 103, USO32_State.USO32_ClearHP);

            State = USO32_State.USO32_Terminated;
        }
        SerialVM? serialVM { get => (VMConn is SerialVM s) ? s : null; set => VMConn = value; }
        public override bool ShouldSerializeVMConn() => false;

        string? foundPortName;
        DriverUSO32Telesystem driverUSO32 = new();
        private System.Threading.Timer? FindPortTimer = null;


        bool StatusRunEnable;
        private readonly Stopwatch stopwatch = new();
        int dataOld;
        void OnUSO32Data(DriverUSO32Telesystem sender, int data) 
        {
            if (!IsStartUso) return;
            if (StatusRunEnable && IsActivateDynItems)// && stopwatch.ElapsedMilliseconds > 300)
            {
                if (data < -1000)
                {
                    if (dataOld >= 0)
                    {
                        State = USO32_State.USO32_Run0;
                        //AutomatUpdateSate(USO32_State.USO32_Run0);
                        stopwatch.Restart();
                        dataOld = data;
                    }
                }
                else if (data > 1000)
                {
                    if (dataOld <= 0)
                    {
                        State = USO32_State.USO32_Run1;
                        //AutomatUpdateSate(USO32_State.USO32_Run1);
                        stopwatch.Restart();
                        dataOld = data;
                    }
                }
                else
                {
                    if (State != USO32_State.USO32_Start && stopwatch.IsRunning && stopwatch.ElapsedMilliseconds > 2000)
                    {
                        State = USO32_State.USO32_Start;
                        stopwatch.Stop();
                    }
                }
            }
            InvokeUSODataEvent(this, data);
        }

        bool Treminated;
        void OnEndUso32(object Sender, DataResp arg)
        {
            Treminated = true;

            if (serialVM != null && serialVM.Serial != null) 
                serialVM.Serial.Driver = null;   

            if (disposedValue) return;
            tbxFQ.IsEnable = true;
            tbxAMP.IsEnable = true;
            ctbClrHP.IsEnable = false;
            ctbSetHP.IsEnable = false;
            if (ctbStart.IsChecked)
            {
                IsStartUso = false;
                ctbStart.IsChecked = false;
            }

            if (arg.rxCount == -1) 
                AutomatUpdateSate(USO32_State.USO32_ErrorPort);
            else
                AutomatUpdateSate(USO32_State.USO32_Terminated);
            VMConn = new NopConVM();
        }
        void AutomatUpdateSate(USO32_State newState)
        {
            var old = State; State = newState;
            switch (State)
            {
                case USO32_State.USO32_Idle_FindPort:
                    if (FindPortTimer == null && old != USO32_State.USO32_FoundPort) 
                        FindPortTimer = new System.Threading.Timer(_=> AutomatUpdateSate(USO32_State.USO32_FindPort), null, 1000, 2000);
                    break;
                case USO32_State.USO32_FindPort:
                    Task.Run(() =>
                    {
                        if (DriverUSO32Telesystem.IsLocked) return;
                        Thread.Sleep(500);
                        var fp = DriverUSO32Telesystem.FindUSO32SerialPort();
                        if (fp.Length > 0)
                        {
                            FindPortTimer?.Dispose();
                            FindPortTimer = null;
                            foundPortName = fp[0];
                            AutomatUpdateSate(USO32_State.USO32_FoundPort);
                        }
                        else
                        {
                            foundPortName = null;
                            if (VMConn != null && !(VMConn is NopConVM)) VMConn = new NopConVM();
                            AutomatUpdateSate(USO32_State.USO32_Idle_FindPort);
                        }
                    });
                    break;

                case USO32_State.USO32_FoundPort:
                    serialVM = new SerialVM { PortName = foundPortName!, BaudRate = 57600};
                    break;

                case USO32_State.USO32_Start:
                    if (IsStartUso)
                    {
                        if (serialVM != null && serialVM.Serial != null && old == USO32_State.USO32_FoundPort)
                        {
                            try
                            {
                                StatusRunEnable = true;
                                Treminated = false;
                                //CheckOsc = false;
                                serialVM.Serial.Driver = driverUSO32;
                                ProtocolUSO32.StartUSO32(serialVM.Serial,
                                            (Uso32_Bias)Enum.Parse(typeof(Uso32_Bias), "b" + tbxAMP.Text),
                                            (Uso32_Freqs)Enum.Parse(typeof(Uso32_Freqs), "fq" + tbxFQ.Text.Replace(".", "_")),
                                            OnUSO32Data, OnEndUso32);
                                tbxFQ.IsEnable = false;
                                tbxAMP.IsEnable = false;
                                ctbClrHP.IsEnable = true;
                                ctbSetHP.IsEnable = true;
                            }
                            catch
                            {
                                ctbStart.IsChecked = false;
                                IsStartUso = false;
                                AutomatUpdateSate(USO32_State.USO32_ErrorPort);
                                throw;
                            }
                        }
                        else
                        { 
                            ctbStart.IsChecked = false;
                            IsStartUso = false;
                            AutomatUpdateSate(USO32_State.USO32_Idle_FindPort);
                        }
                    }
                    else 
                    {
                        if (old == USO32_State.USO32_Run0 || old == USO32_State.USO32_Run1 || old == USO32_State.USO32_Start)
                        {
                            if (driverUSO32 != null)
                            {
                                var l = ServiceProvider.GetRequiredService<ILogger<BusUSO32VM>>();
                                l.LogTrace("---TERMINATE--- {} ", Name);

                                driverUSO32.Terminate = true;
                            }
                            AutomatUpdateSate(USO32_State.USO32_Terminate);                            
                        }
                        else
                            AutomatUpdateSate(USO32_State.USO32_Idle_FindPort);
                    }
                    break;
                //case USO32_State.USO32_Run0:
                //    break;
                //case USO32_State.USO32_Run1:
                //    break;
                //case USO32_State.USO32_Terminate:                     
                //    break;
                //case USO32_State.USO32_Terminated:
                //    break;
                case USO32_State.USO32_SetHP:
                    driverUSO32?.DoSetHP( ()=> Task.Run( () => { Thread.Sleep(500); StatusRunEnable = true; ctbSetHP.IsChecked = false; }));
                    break;
                case USO32_State.USO32_ClearHP:
                    driverUSO32?.DoClrHP(() => Task.Run( ()=> { Thread.Sleep(500); StatusRunEnable = true; ctbClrHP.IsChecked = false; })); 
                    break;
                //case USO32_State.USO32_ErrorPort:
                //    break;
            }
        }
        
        bool IsActivateDynItems = false;
        public void ActivateDynItems()
        {
            IsActivateDynItems = true;

            ToolItem[] tia = { ctbStart, ctbSetHP, ctbClrHP, tbxAMP, tbxFQ };
            ToolBarServer.Add("ToolGlyph", tia);
            DynAdapter.DynamicItems.AddRange(tia);

            var root = new MenuItemVM { ContentID = "USO32", Header = Name, Priority = 1 };
            var shp = new MenuItemVM { ContentID = "USO32SHOWHP", 
                Header = "show HP", 
                Priority = 101, 
                IsCheckable=true,
                IsChecked = ShowInToolBarHP };

            shp.PropertyChanged += Shp_PropertyChanged;
            var fqa = new MenuItemVM
            {
                ContentID = "USO32SHOWFQA",
                Header = "show FQA",
                Priority = 100,
                IsCheckable = true,
                IsChecked = ShowInToolBarFQA
            };

            fqa.PropertyChanged += fqa_PropertyChanged;

            //DockManagerVM.ActiveDocument = DockManagerVM.Contains(nameof(ProjectsExplorerVM))!;
            MenuItemServer.Add("ROOT", root);
            MenuItemServer.Add("USO32", [shp, fqa]);
            //var range = new PriorityItemBase[]  { m };
            DynAdapter.DynamicItems.Add(root);
            var l = ServiceProvider.GetRequiredService<ILogger<BusUSO32VM>>();
            l.LogTrace("ActivateDynItems {} ", ContentID);
        }

        public void DeActivateDynItems()
        {
            IsActivateDynItems = false;
            var l = ServiceProvider.GetRequiredService<ILogger<BusUSO32VM>>();
            l.LogTrace("~~DeActivateDynItems {} ", ContentID);
        }
        protected override void Remove()
        {
            if (!DelEnable) return;
            base.Remove();
            DockManagerVM.Remove($"{nameof(OscUSO32VM)}.{ContentID!}");
        }
        protected override void Dispose(bool disposing)
        {
            if (disposedValue) return;
            base.Dispose(disposing);
            if (IsStartUso && driverUSO32 != null)
            {
                IsStartUso = false;
                driverUSO32.Terminate = true;
                int cnt = 0;
                while (!Treminated && cnt++ < 10) Thread.Sleep(100);
            }            
            //НЕЛЬЗЯ!!! может быть общий ресурс 
            //serialVM?.Serial?.Dispose();            
        }
    }
}
