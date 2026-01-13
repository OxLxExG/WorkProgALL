using CommunityToolkit.Mvvm.Input;
using Connections;
using Connections.Interface;
using Connections.Uso32;
using Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Serialization;

namespace HorizontDrilling.ViewModels
{
    using HorizontDrilling.Properties;
    using Microsoft.Extensions.DependencyInjection;

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
           // var l = ServiceProvider.GetRequiredService<ILogger<TelesysVM>>();
           // Log.Trace("~~DeActivateDynItems {} ", ContentID);

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
            var m = new MenuItemVM { ContentID = "TLS1", Header = Resources.strTelesystem, Priority = 2 };
            var m1 = new MenuItemVM { ContentID = "Tlsys2", Header = "Setup Telesys2", Priority = 200 };
            var m2 = new MenuItemVM { ContentID = "Tlsys3", Header = "Setup Telesys3", Priority = 201 };
            MenuItemServer.Add("ROOT", m);
            MenuItemServer.Add("TLS1", [m1,m2]);
            //var range = new PriorityItemBase[]  { m };
            DynAdapter.DynamicItems.Add(m);

           // var l = ServiceProvider.GetRequiredService<ILogger<TelesysVM>>();
           // l.LogTrace("ActivateDynItems {} ", ContentID);
        }
    }
    public enum USO32_State {
        USO32_Idle_FindPort = 0, // ожидание  по таймеру 
                                 //  USO32_FindPort,//   ОБМЕН  по таймеру
        USO32_FoundPort,// найден порт idle? stop timer

        USO32_VZerro,// 0  нажатие на кнопку togle RUN или USO32_FindPort по таймеру или старт(RUN)
        USO32_VMinus, // <0
        USO32_VPlus, // >0

        USO32_Terminate, // отпускание кнопки togle RUN запретить событие USO32_Run 
        USO32_Terminated, //idle? разрешить событие USO32_Run 

        USO32_SetHP, // при нажатии кнопки SetHP запретить событие USO32_Run
                     // пока флаг DriverUSO32Telesystem.SetHP = true затем разрешить событие USO32_Run 
        USO32_ClearHP, // при нажатии кнопки ClearHP запретить событие USO32_Run
                       // пока флаг DriverUSO32Telesystem.ClrHP = true затем разрешить событие USO32_Run

        USO32_ErrorPort, // idle?
    }

    public class Uso32StartToolButton: CheckToolButton
    {
        public Uso32StartToolButton()
        {
            ContentID = "RUN";
            FontSize = 24;
            Size = 40;
            Priority = 101;            
        }
        USO32_State _State;
        public USO32_State State { get => _State; set => SetProperty(ref _State, value); }
        public string Name { get; set; } = string.Empty;
        public string tt_Error { get => string.Format(Resources.uso_Error, Name); }
        public string tt_Run { get => string.Format(Resources.uso_Run, Name); }
        public string tt_FindPort { get => string.Format(Resources.uso_FindPort, Name); }
        public string tt_FoundPort { get => string.Format(Resources.uso_FoundPort, Name); }
        public string tt_Terminated { get => string.Format(Resources.uso_Terminated, Name); }

    }

    public delegate void OnUSODataHandler(BaseBusUSOVM sender, int data);
    public class BaseBusUSOVM: BusVM
    {
        public event OnUSODataHandler? OnUSODataEvent;

        // данные для телесистемы
        protected static void InvokeUSODataEvent(BaseBusUSOVM sender, int data) => sender.OnUSODataEvent?.Invoke(sender, data);

        /// интевфейс для управления УСО телесистемой 
         
        /// АРУ
        /// нериализованно
        //[XmlIgnore] public float AruGain { get; set; }

        /// Частота фильтра УСО подстраивается под частоту телесистемы
        protected float _TelesystemFQ = 10;
        [XmlIgnore] public float TelesystemFQ { get => _TelesystemFQ; set => SetProperty(ref _TelesystemFQ, value); }
    }
    public class BusUSO32VM : BaseBusUSOVM
    {
        #region Name
        string NAME = Resources.uso_Capt;
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
                ToolTip = new ToolTip { Content = string.Format(Resources.uso_LowPasFilter, Name) },
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

        #region Gain
        static Dictionary<string, int> AMP = Enum.GetValues<Uso32_Gain>().Cast<int>().ToDictionary<int, string>(
            n => Enum.GetName<Uso32_Gain>((Uso32_Gain)n)!
                    .Replace("b", ""));
        [XmlIgnore] public ToolComboBox tbxAMP { get; init; }
        public string Amp { get => tbxAMP.Text; set => tbxAMP.Text = value; }
        public bool ShouldSerializeAmp() => Amp != "1";
        ToolComboBox initAmp()
        {
            var r = new ToolComboBox
            {
                ToolTip = new ToolTip { Content = string.Format(Resources.uso_Gain, Name) },
                ContentID = "AMP" + ContentID,
                Priority = 111,
                ItemsSource = AMP.Keys.ToArray(),
                Text = "1",
            };
            r.PropertyChanged += (s, e) =>
            {
                SetDrity(true);
                if (e.PropertyName == "Text")
                {
                    OnPropertyChanged("Amp");
                }
            };
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
                    ctbSetHP.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
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
        void OnBtnHP()
        {
            StatusRunEnable = false;
            AutomatUpdateSate(ctbSetHP.IsChecked? USO32_State.USO32_SetHP: USO32_State.USO32_ClearHP);
        }
        CheckToolButton initHP()
        {
            return new CheckToolButton
            {
                ToolTip = new ToolTip { Content = string.Format(Resources.uso_HiPasFilter, Name) },
                ContentID = "HP" + ContentID,
                ContentTemplate = Views.Icons.HPOn,
                Priority = 103,
                Size = 40,
                IsEnable = false,
                Visibility = Visibility.Collapsed,
                Command = new RelayCommand(OnBtnHP),
            };
        }
        #endregion

        #region Start State Button

        protected Uso32StartToolButton ctbStart { get; init; }
        Uso32StartToolButton initStart()
        {
            return new Uso32StartToolButton
            {
                Command = new RelayCommand(OnBtnStart),
                Name = Name,
            };
        }
        bool IsStartUso { get; set; }
        void OnBtnStart()
        {
            IsStartUso = !IsStartUso;
            if (IsStartUso == false) StatusRunEnable = false;

           // var l = ServiceProvider.GetRequiredService<ILogger<BusUSO32VM>>();
           // l.LogTrace("---OnBtnStart--- {} IsStartUso={} StatusRunEnable={} ", Name, IsStartUso, StatusRunEnable);

            AutomatUpdateSate(USO32_State.USO32_VZerro);
        }
        #endregion

        #region Show Osc
        protected ToolButton ctbShowOsc { get; init; }
        ToolButton initShowOsc()
        {
            return new ToolButton
            {
                ContentID = "SOW" + ContentID,
                ToolTip = new ToolTip { Content = string.Format(Resources.uso_ShowOsc, Name) },
                FontSize = 24,
                Size = 40,
                Priority = 102,
                Content = "\ue9d9",
                Command = OscCommand,
            };
        }
        [XmlIgnore] public ICommand OscCommand { get; init; } 
        void DoOscCommand()
        {
           var f = DockManagerVM.AddOrGetandShow($"{nameof(OscUSO32VM)}@{ContentID!}", FormAddedFrom.User);            
           GetRoot()?.AddChildForm(f);
        }
        #endregion

        static BusUSO32VM()
        {
          //  ConnectionCash.logger = VMBase.ServiceProvider.GetRequiredService<ILogger<BusUSO32VM>>();
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
            ctbShowOsc = initShowOsc();
            ctbSetHP = initHP();

            ctbStart.State = USO32_State.USO32_Terminated;
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
                        ctbStart.State = USO32_State.USO32_VMinus;
                        stopwatch.Restart();
                        dataOld = data;
                    }
                }
                else if (data > 1000)
                {
                    if (dataOld <= 0)
                    {
                        ctbStart.State = USO32_State.USO32_VPlus;
                        stopwatch.Restart();
                        dataOld = data;
                    }
                }
                else
                {
                    if (ctbStart.State != USO32_State.USO32_VZerro && stopwatch.IsRunning && stopwatch.ElapsedMilliseconds > 2000)
                    {
                        ctbStart.State = USO32_State.USO32_VZerro;
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
            ctbSetHP.IsEnable = false;
            ctbSetHP.IsChecked = false;

            if (ctbStart.IsChecked)
            {
                IsStartUso = false;
                ctbStart.IsChecked = false;
            }
            AutomatUpdateSate(arg.rxCount == -1? USO32_State.USO32_ErrorPort : USO32_State.USO32_Terminated);
            VMConn = new NopConVM();
        }
        void AutomatUpdateSate(USO32_State newState)
        {
            var old = ctbStart.State; ctbStart.State = newState;
            switch (newState)
            {
                case USO32_State.USO32_Idle_FindPort:
                    if (FindPortTimer == null && old != USO32_State.USO32_FoundPort) 
                        FindPortTimer = new System.Threading.Timer(_=> Task.Run(() =>
                        {
                            if (DriverUSO32Telesystem.IsLocked) return;
                            Thread.Sleep(500);
                            IConnectionServer? cs = (Application.Current as IServiceProvider)?.GetRequiredService<IConnectionServer>();

                            var fp = DriverUSO32Telesystem.FindUSO32SerialPort(cs);
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
                        }), null, 1000, 2000);
                    break;

                case USO32_State.USO32_FoundPort:
                    serialVM = new SerialVM { PortName = foundPortName!, BaudRate = 57600};
                    break;

                case USO32_State.USO32_VZerro:
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
                                            (Uso32_Gain)Enum.Parse(typeof(Uso32_Gain), "b" + tbxAMP.Text),
                                            (Uso32_Freqs)Enum.Parse(typeof(Uso32_Freqs), "fq" + tbxFQ.Text.Replace(".", "_")),
                                            OnUSO32Data, OnEndUso32);
                                tbxFQ.IsEnable = false;
                                tbxAMP.IsEnable = false;
                                ctbSetHP.IsEnable = true;
                                ctbSetHP.IsChecked = false;
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
                        if (old == USO32_State.USO32_VPlus || old == USO32_State.USO32_VMinus || old == USO32_State.USO32_VZerro)
                        {
                            if (driverUSO32 != null)
                            {
                               // var l = ServiceProvider.GetRequiredService<ILogger<BusUSO32VM>>();
                              //  l.LogTrace("---TERMINATE--- {} ", Name);

                                driverUSO32.Terminate = true;
                            }
                            AutomatUpdateSate(USO32_State.USO32_Terminate);                            
                        }
                        else
                            AutomatUpdateSate(USO32_State.USO32_Idle_FindPort);
                    }
                    break;
                case USO32_State.USO32_SetHP:
                    driverUSO32?.DoSetHP( ()=> Task.Run( () => { Thread.Sleep(500); StatusRunEnable = true;}));
                    break;
                case USO32_State.USO32_ClearHP:
                    driverUSO32?.DoClrHP(() => Task.Run( ()=> { Thread.Sleep(500); StatusRunEnable = true;})); 
                    break;
            }
        }
        
        bool IsActivateDynItems = false;
        public void ActivateDynItems()
        {
            IsActivateDynItems = true;

            ToolItem[] tia = { ctbStart, ctbShowOsc, ctbSetHP, tbxAMP, tbxFQ };
            ToolBarServer.Add("ToolGlyph", tia);
            DynAdapter.DynamicItems.AddRange(tia);

            var root = new MenuItemVM { ContentID = "USO32", Header = Name, Priority = 1 };
            var shp = new MenuItemVM { ContentID = "USO32SHOWHP", 
                Header = Resources.hShowHPF, 
                Priority = 101, 
                IsCheckable=true,
                IsChecked = ShowInToolBarHP };

            shp.PropertyChanged += Shp_PropertyChanged;
            var fqa = new MenuItemVM
            {
                ContentID = "USO32SHOWFQA",
                Header = Resources.hShowLpfAmp,
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
          //  var l = ServiceProvider.GetRequiredService<ILogger<BusUSO32VM>>();
          //  l.LogTrace("ActivateDynItems {} ", ContentID);
        }

        public void DeActivateDynItems()
        {
            IsActivateDynItems = false;
          //  var l = ServiceProvider.GetRequiredService<ILogger<BusUSO32VM>>();
           // l.LogTrace("~~DeActivateDynItems {} ", ContentID);
        }
        protected override void Remove()
        {
            if (!DelEnable) return;
            base.Remove();
            DockManagerVM.Remove($"{nameof(OscUSO32VM)}@{ContentID!}");
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
