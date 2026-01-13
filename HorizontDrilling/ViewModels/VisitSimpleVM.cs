using Communications;
using CommunityToolkit.Mvvm.Input;
using Core;
using HorizontDrilling.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Xml.Serialization;

namespace HorizontDrilling.ViewModels
{
    using HorizontDrilling.Properties;
    public abstract class ItemVM: VMBase, IDisposable
    {
        public static implicit operator ItemVM(ItemM m)
        {
            if (m is Visit v) return new VisitVM { Model = v } ;
            else if (m is Trip t) return new TripVM { Model = t };
            else if (m is Device d)
            {
                var dvm = VisitVM.GetVM(d);
                if (dvm != null) return dvm;
            }
            throw new InvalidCastException();
        }
        public ItemVM()
        {
            CItems.Add(new CommandMenuItemVM
            {
                Header = $"Delete {ModelName}",
                Command = new RelayCommand(Remove),
                ContentID = "DEL",
                //IsEnable = DelEnable,
                Priority = 10000,
            });
        }
        [XmlIgnore] public bool DelEnable 
        { 
            get 
            { 
                if (this is ComplexVM c)
                {
                    if (c.Items == null || c.Items.Count == 0) return true;
                }
                else return true;

                return false; 
            } 
        }
        [XmlIgnore] public virtual string ModelName => string.Empty;

        private bool _IsExpanded = true;
        public bool IsExpanded { get => _IsExpanded; set
            {
                if (SetProperty(ref _IsExpanded, value))
                {
                    SetDrity(true);
                }
            }
        }
        public bool ShouldSerializeIsExpanded() => !_IsExpanded;

        private DynamicItemsAdapter? dynamicItemsAdapter;
        public DynamicItemsAdapter DynAdapter
        {
            get
            {
                if (dynamicItemsAdapter == null) dynamicItemsAdapter = new DynamicItemsAdapter();
                return dynamicItemsAdapter;
            }
        }

        private bool _IsSelected;
        public bool IsSelected { get=> _IsSelected; 
            set 
            { 
                if (SetProperty(ref _IsSelected, value))
                {
                    if  (_IsSelected) dynamicItemsAdapter?.ActivateMenu();
                    else dynamicItemsAdapter?.DeActivateMenu();
                }
            } 
        }
        public bool ShouldSerializeIsSelected() => _IsSelected;
        [XmlIgnore] public ObservableCollection<MenuItemVM> CItems { get; set; } = new ObservableCollection<MenuItemVM>();        

        #region Parent
        private WeakReference? parent;
        protected bool disposedValue;

        public virtual void SetParent(object? par)
        {
            if (par == null) parent = null;
            else parent = new WeakReference(par);
        }
        [XmlIgnore] public object? Parent
        {
            get
            {
                if (parent == null) return null;
                return parent.Target;
            }
            set => SetParent(value);
        }
        protected VMBaseFileDocument? GetRoot()
        {
            object? p = Parent;
            while (p is ComplexVM c) { p = c.Parent; }
            if (p is VMBaseFileDocument v) return v;
            else return null;
        }
        protected void SetDrity(bool VMOnly = false)
        {
            var v = GetRoot();
            if (v != null)
            {
                if (VMOnly) v.IsVMDirty = true;
                else v.IsDirty = true;
            }
        }
        protected virtual void Remove()
        {
            if (!DelEnable) return;

            if (dynamicItemsAdapter != null) dynamicItemsAdapter.Dispose();

            if (parent != null)
            {
                ComplexVM? m = parent.Target as ComplexVM;
                m?.ItemsRemove(this);
            }
            Dispose();
        }
        #endregion

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: освободить управляемое состояние (управляемые объекты)
                }
                if (dynamicItemsAdapter != null) dynamicItemsAdapter.Dispose();
                // TODO: освободить неуправляемые ресурсы (неуправляемые объекты) и переопределить метод завершения
                // TODO: установить значение NULL для больших полей
                disposedValue = true;
            }
        }

        // // TODO: переопределить метод завершения, только если "Dispose(bool disposing)" содержит код для освобождения неуправляемых ресурсов
        ~ItemVM()
        {
            // Не изменяйте этот код. Разместите код очистки в методе "Dispose(bool disposing)".
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Не изменяйте этот код. Разместите код очистки в методе "Dispose(bool disposing)".
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

    }
    public abstract class ComplexVM: ItemVM
    {
        public VMBase? Find(string id)
        {
            if (Items != null)
            {
                foreach (var i in Items)
                {
                    if (i.ContentID == id) return i;
                    if (i is ComplexVM c)
                    {
                        var r = c.Find(id);
                        if (r != null) return r;
                    }
                }
            }
            return null;
        }
        public IEnumerable<T>? Find<T>() where T : VMBase
        {
            List<T>? res = new List<T>();
            if (Items != null)
            {
                foreach (var i in Items)
                {
                    if (i is T t) res.Add(t);
                    if (i is ComplexVM c)
                    {
                        var r = c.Find<T>();
                        if (r != null) res.AddRange(r);
                    }
                }
            }
            return res.Count == 0 ? null : res;
        }
        /// <summary>
        /// loaded event action
        /// </summary>
        public void RemoveChildEmptyModel()
        {
            if (Items == null || Items.Count == 0)
            {       // auto remove empty BUS,PIPE
                if (!(this is ComplexModelVM) && (Parent is ComplexVM p)) p.ItemsRemove(this);
                return;
            }
            for (int i = Items.Count - 1; i >= 0; i--)
            {
                var vm = Items[i];

                if ((vm is ComplexModelVM cm && cm.Model == null) || (vm is DeviceVM d && d.Model == null))
                {
                    Items.RemoveAt(i);
                    SetDrity();
                }
                else
                    if (vm is ComplexVM cvm) cvm.RemoveChildEmptyModel();
            }
        }


        private (Trip trip, Device[] devs ) FindTripAndChildDevs(ItemVM vm)
        {
            ComplexVM? p = (ComplexVM?)vm.Parent;
            while (p != null && !(p is TripVM))
            {
                p = (ComplexVM?)p.Parent;
            }
            if (p is TripVM tvm && tvm.Model is Trip t)
            {
                List<Device> devices = new List<Device>();

                void recur(ItemVM root)
                {
                    if (root is DeviceVM dvm && dvm.Model is Device d) devices.Add(d);
                    else if (root is ComplexVM c && c.Items != null)
                    {
                        foreach (var i in c.Items) recur(i);
                    }
                }
                recur(vm);

                return (t, devices.ToArray());
            }
            throw new NotImplementedException();
        }
        /// <summary>
        /// Items-Visit,Trip,pipe,bus VMs
        /// </summary>
        /// <param name="item">tripVM,PipeVM,BusVM,DevVM</param>
        public void ItemsRemove(ItemVM item)
        {
            if (Items != null)
            {
                //удаляем рейс из заезда (модель)
                if (item is TripVM tm && tm.Model is Trip t && this is VisitVM vm && vm.Model is Visit v)
                {
                    v.ItemsRemove(t);
                }
                // (модель) нахдим родительский рейс и дочерние приборы, их удаляем
                if (!(item is TripVM))
                {
                    var (tr,dvs) = FindTripAndChildDevs(item);
                    foreach (var d in dvs)
                    {                        
                        tr.ItemsRemove(d);
                    }
                }
                // vm
                Items.Remove(item);
                if (Items.Count == 0)
                {
                    Items = null;
                    // remove bus,pipe if not child devices
                    /// неверно ???
                   // if (!(this is ComplexModelVM) && (Parent is ComplexVM p)) p.ItemsRemove(this); 
                    /// правильно ???
                    this.Remove();
                }

                SetDrity();
            }
        }
        public ItemVM ItemsAdd(ItemVM item)
        {
            if (Items == null) Items = new ObservableCollection<ItemVM> { item };
            else if (!Items.Contains(item)) Items.Add(item);
            item.Parent = this;

            if (item is TripVM tm && tm.Model is Trip t && this is VisitVM vm && vm.Model is Visit v)
            {
                v.ItemsAdd(t);
            }
            if (!(item is TripVM))
            {
                var (tr, dvs) = FindTripAndChildDevs(item);
                foreach (var d in dvs) tr.ItemsAdd(d);
            }
            SetDrity();
            return item;
        }
        public T Add<T>(T item) where T : ItemVM
        {
            ItemsAdd(item);
            return item;
        }
        public ObservableCollection<ItemVM>? Items {  get; set; }
        public bool ShouldSerializeItems() => Items != null && Items.Count > 0;
        public override void SetParent(object? parent)
        {
            base.SetParent(parent);

            if (Items != null)
                foreach(var i in Items) 
                    i.SetParent(this);
        }
    }
    public static class ItemVMmodelEx
    {
        public static void SetModel(this ItemVM vm, ItemM m)
        {
            if (vm is ComplexModelVM cm) cm.Model = m;
            else if (vm is DeviceVM dm) dm.Model = m;
        }
    }
    internal class ItemMprop
    {
        private WeakReference<ItemM>? _wr_model;
        internal virtual void SetModel(ItemVM vm, ItemM? value)
        {
            if (value == null) _wr_model = null;
            else
            {                  
                _wr_model = new WeakReference<ItemM>(value);
                vm.ContentID = value.Id.ToString("D");
            }
        }
        internal ItemM? GetModel()
        {
            ItemM? m = null;
                if (_wr_model != null)
                {
                    _wr_model.TryGetTarget(out m);
                }
                return m;
        }
    }

    /// <summary>
    /// Items-----simple
    /// Model- Device
    /// </summary>
    public abstract class DeviceVM : ItemVM
    {

        private readonly ItemMprop itemMprop = new ItemMprop();
        [XmlIgnore] public ItemM? Model { get => itemMprop.GetModel(); set => itemMprop.SetModel(this,value); }
    }
    public class DevicePBVM: DeviceVM
    {
        public DevicePBVM() 
        {
            DynAdapter.OnActivateDynItems += ActivateDynItems;
        }
        bool Freeze;

        private void ActivateDynItems()
        {
            if (Parent is BusPBVM bvm)
            {
                var ftb = new CheckToolButton
                {

                    ToolTip = new ToolTip { Content = $"Циклоопрос устройства {Model?.Name}" },
                    ContentID = "Cycle" + ContentID,
                    //IconSource = "pack://application:,,,/Images/CycleDev.png",
                    Content = new Image { Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Images/CycleDev.png")) },
                    Priority = 101,
                    IsChecked = Freeze,
                    Command = new RelayCommand(() => Freeze = !Freeze),
                };
                ToolBarServer.Add("ToolGlyph", ftb);
                DynAdapter.DynamicItems.Add(ftb);
            }
        }

        [XmlIgnore] public string MaskPort => Model != null ? Convert.ToString(Model.SupportUartSpeed,2) : string.Empty;
        [XmlIgnore] public override string ModelName => "Device PB";
        [XmlIgnore] public new DevicePB? Model { get =>(DevicePB?) base.Model; set => base.Model = value; }
    }
    public abstract class ComplexModelVM : ComplexVM
    {
        private readonly ItemMprop itemMprop = new ItemMprop();
        [XmlIgnore] public ItemM? Model { get => itemMprop.GetModel(); set => itemMprop.SetModel(this,value); }    
    }
    /// <summary>
    /// Items => Devises
    /// model----
    /// </summary>
    public class BusVM : ComplexVM
    {
        [XmlIgnore] public override string ModelName => "BUS";

        string _Name = string.Empty;
        public string Name 
        {
            get => _Name;
            set
            {
                _Name = value;
                if (Name != string.Empty)
                {
                    string rootName = _Name.Split('(')[0];
                    var bs = RootFileDocumentVM.Find<BusVM>();
                    if (bs != null)
                    {

                        int cnt = 0;
                        bool BadName = false;
                        do
                        {
                            BadName = false;
                            foreach (var b in bs)
                            {
                                if (b != this && _Name == b.Name)
                                {
                                    cnt++;
                                    _Name = $"{rootName}({cnt})";
                                    BadName = true;
                                    break;
                                }
                            }
                        } while (BadName);
                    }
                }
            }
        } 
        public virtual bool ShouldSerializeName() => Name != string.Empty;

        ConVM? _VMConn;
        public ConVM? VMConn { get=> _VMConn; set 
            { 
                if (SetProperty(ref _VMConn,value) && _VMConn != null)
                {
                    _VMConn.PropertyChanged += (o,e) => SetDrity(true);
                }
            } 
        }
        public virtual bool ShouldSerializeVMConn() => VMConn != null;

    }
    public class BusPBVM : BusVM
    {
        const string PBNAME = "ПБ 1ware";
        static BusPBVM()
        {
           // ConnectionCash.logger = VMBase.ServiceProvider.GetRequiredService<ILogger<BusPBVM>>();
        }
        public BusPBVM() 
        { 
            
           // ContentID = (Guid.NewGuid()).ToString("D");
            Name = PBNAME;
            DynAdapter.OnActivateDynItems += ActivateDynItems;
            DynAdapter.OnDeActivateDynItems += DeActivateDynItems;
        }
        private void DeActivateDynItems()
        {
          //  var l = ServiceProvider.GetRequiredService<ILogger<BusPBVM>>();
          //  l.LogTrace("~~DeActivateDynItems {} ", ContentID);
        }
        bool Freeze;
        private void ActivateDynItems()
        {
            if (Items ==  null) return;
            StringBuilder sb = new StringBuilder();
            foreach (var di in Items!) if (di is DevicePBVM pb && pb.Model is DevicePB d)  { sb.Append($" {d.Name}"); }
            var ftb = new CheckToolButton
            {

                ToolTip = new ToolTip { Content = $"Циклоопрос устройств: {sb}" },
                ContentID = "Cycle" + ContentID,
                Content = new Image { Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Images/Cycle.png")) },
                Priority = 101,
                IsChecked = Freeze,
                // binding  buttonVM.Command=>formVM.Freeze=>buttonVM.IsChecked
                Command = new RelayCommand(() => Freeze = !Freeze),
            };

            ToolBarServer.Add("ToolGlyph", ftb);
            DynAdapter.DynamicItems.Add(ftb);

            var m = new MenuItemVM { ContentID = "bus", Header = "bus", Priority = 1 };
            MenuItemServer.Add("ROOT", m);
            //var range = new PriorityItemBase[]  { m };
            DynAdapter.DynamicItems.Add(m);
          //  var l = ServiceProvider.GetRequiredService<ILogger<BusPBVM>>();
          //  l.LogTrace("ActivateDynItems {} ", ContentID);
        }

        public override bool ShouldSerializeName() => Name != PBNAME;

        #region Interval
        private int _Interval = 2100;
        public int Interval { get => _Interval; set { if (SetProperty(ref _Interval, value)) SetDrity(true); } }
        public bool ShouldSerializeInterval() => _Interval != 2100;
        #endregion
    }
    /// <summary>
    /// Items => buses, Devices(unconnected),
    /// Model Trip 
    /// </summary>
    public class TripVM : ComplexModelVM
    {
        [XmlIgnore] public override string ModelName => "Trip";
        [XmlIgnore] public Trip trip => (Trip) Model!; 
    }
    /// <summary>
    /// Items => Trips
    /// model visit
    /// </summary>
    public class VisitVM: ComplexModelVM 
    {
        [XmlIgnore] public override string ModelName => "Visit";
        [XmlIgnore] public Visit visit => (Visit)Model!;
        static VisitVM()
        {
            AddFactory<DevicePB, DevicePBVM>();
            AddFactory<DeviceTelesystem, TelesysVM>();
            //AddFactory<DeviceTelesystem2, DeviceT2VM>();
        }
        #region Factory
        protected static readonly Dictionary<Type, Func<Device, DeviceVM>> _factory = new();
        public static void AddFactory(Type tp, Func<Device, DeviceVM> FactoryFunc)
        {
            if (!_factory.ContainsKey(tp))
                _factory.Add(tp, FactoryFunc);
        }
        protected static void AddFactory<M, VM>()
            where VM : DeviceVM, new()
            where M : Device
        {
            var tp = typeof(M);
            if (!_factory.ContainsKey(tp))
                _factory.Add(tp, m => new VM()
                {
                    Model = m
                });
        }
        public static DeviceVM GetVM(Device m)
        {
            Func<Device, DeviceVM> f;
            Type? tp = m.GetType();

            //while (tp != null)
            //{
            if (_factory.TryGetValue(tp, out f!)) ///TODO: Recur Base Type Check ????
                return f(m);
            //    tp = tp.BaseType;               
            //}
            throw new ArgumentException();
        }
        #endregion
    }

    /// <summary>
    /// ***********  visit  document **********************
    /// </summary>

    public class HeaderHelper : VMBase
    {
        private WeakReference<VisitDocument> wr;

        protected VisitDocument? owner
        {
            get
            {
                VisitDocument? d = null;
                wr.TryGetTarget(out d);
                return d;
            }
        }

        public virtual bool IsExpanded
        {
            get => owner == null ? true : owner.TripExpanded; set { if (owner != null) owner.TripExpanded = value; }
        }
        public bool IsSelected { get; set; }
        public virtual string Header => Resources.tTrips;
        public virtual IEnumerable? Items => owner?.VisitVM.Items;
        public HeaderHelper(VisitDocument owner) { this.wr = new WeakReference<VisitDocument>(owner); }
    }
    public class HeaderDockHelper : HeaderHelper
    {
        public override bool IsExpanded { get => owner == null ? true : owner.DockExpanded; set { if (owner != null) owner.DockExpanded = value; } }
        public override string Header => Resources.tDocs;
        public override IEnumerable? Items => owner?.ChildDocuments;
        public HeaderDockHelper(VisitDocument owner) : base(owner) { }
    }
    public class HeaderFileHelper : HeaderHelper
    {
        public override bool IsExpanded { get; set; }
        public override string Header => "File Not Found";
        public override IEnumerable? Items => null;
        public HeaderFileHelper(VisitDocument owner) : base(owner) { owner.IsExpanded = true; }
    }

    /// <summary>
    /// if file not found items = null
    /// Items[0] => visitVM
    /// </summary>
    public class VisitDocument : ComplexFileDocumentVM
    {
        const string EXT = "vst";

        public VisitDocument() : base()
        {
            lastClosedFiles = ServiceProvider.GetRequiredService<LastGroupMenuVM>();
            ChildlastClosedFiles = ServiceProvider.GetRequiredService<LastFileMenuVM>();

            Title = Properties.Resources.tProject;
            DefaultTitle = Title;

            CItems.Add(new CommandMenuItemVM
            {
                Header = "Delete",
                Command = new RelayCommand(()=> Remove()),
                ContentID = "DEL",
                Priority = 10000,
            }
            );

            //InitialDirectory = ProjectFile.WorkDirs.Count > 0 ? ProjectFile.WorkDirs[0]! : ProjectFile.RootDir;

            ////var s = ServiceProvider.GetRequiredService<GlobalSettings>();
            ////var ss = Properties.Settings.Default.ProjectGroupDir;
            ////if (!string.IsNullOrEmpty(ss)) InitialDirectory = ss;
            ////else if (s.GroupDir != string.Empty) InitialDirectory = s.GroupDir;
            ////else
            ////{
            ////    var se = Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
            ////    InitialDirectory = $"{se}\\Горизонт\\WorkProg\\ProjectsGroup\\";
            ////}
            //Filter = $"{Title} (.{EXT})|*.{EXT}";

            //object[] o = new object[]
            //    {
            //            $"{Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments)}\\Горизонт\\WorkProg\\Projects\\",
            //            new Guid("FDD39AD0-238F-46AF-ADB4-6C85480369C7"),//документы
            //            new Guid("1777F761-68AD-4D8A-87BD-30B759FA33DD"),//избрвнное
            //    };
            //var o2 = ProjectFile.WorkDirs.Cast<object>().ToArray();

            //CustomPlaces = o2.Concat(o).ToArray();  

            //DefaultExt = EXT;

        }

        //protected void CmdRemove()
        //{
        //    Remove( );
        //    //if (RootFileDocumentVM.Instance is GroupDocument g)
        //    //{
        //    //    g.RemoveVisit(this);
        //    //}
        //}

        [XmlIgnore] public ObservableCollection<MenuItemVM> CItems { get; set; } = new ObservableCollection<MenuItemVM>();

        bool _IsSettingsExpanded = true;
        public bool IsSettingsExpanded { get=>_IsSettingsExpanded; set => SetPropertyWithDrity(ref _IsSettingsExpanded, value); } 
        public bool ShouldSerializeIsSettingsExpanded() => !_IsSettingsExpanded;

        /// <summary>
        /// loaded or new event action 
        /// </summary>
        public void SetModel(Visit visit)
        {
            var ID = visit.Id.ToString("D");
            if (string.IsNullOrEmpty(VisitVM.ContentID))
            {
                //новый заезд
                VisitVM.ContentID = ID;
                VisitVM.Model = visit;
                if (visit.Items != null)
                {
                    ///
                    /// Добавляем рейсы, и к ресам сразу приборы
                    /// это происходит при открытии модели на другом компе (создать флаг для VM оконченного заезда)
                    ///
                    foreach (var vi in visit.Items) if (vi is Trip t)
                    {
                        TripVM tripVM = new TripVM { Model = t };
                        VisitVM.ItemsAdd(tripVM);
                        if (t.Items  != null) 
                                foreach(var ti in t.Items) if (ti is Device d)
                                   tripVM.ItemsAdd(VisitVM.GetVM(d));
                    }
                }
            }
            else // bad model
               if (VisitVM.ContentID != ID) throw new InvalidOperationException("bad model ContentID != ID");
            else
            {
                /// событие при загрузке
                /// только добавляем ВМ если нет
                /// 
                VisitVM.Model = visit;
                if (visit.Items == null) return;
                else foreach (var titem in visit.Items) if (titem is Trip t)
                        {
                            /// add or check trips
                            TripVM? tripvm = getVM(VisitVM.Items, t) as TripVM;
                            if (tripvm == null)
                            {
                                tripvm = new TripVM { Model = t };
                                VisitVM.ItemsAdd(tripvm);
                            }
                            if (t.Items != null) foreach (var ditem in t.Items)
                                {
                                    DeviceVM? dvm = DeepGetVM(tripvm.Items, ditem);
                                    if (dvm == null && ditem is Device d)
                                    {
                                        tripvm.ItemsAdd(VisitVM.GetVM(d));
                                    }
                                }
                        }
                static ItemVM? getVM(ObservableCollection<ItemVM>? RootItems, ItemM model)
                {
                    var rz = RootItems == null ? null : RootItems.FirstOrDefault((vm) => model.Id.ToString("D") == vm.ContentID);
                    rz?.SetModel(model);
                    return rz;
                }
                static DeviceVM? DeepGetVM(ObservableCollection<ItemVM>? RootItems, ItemM model)
                {
                    var rz = getVM(RootItems, model);
                    if (rz != null && rz is DeviceVM dvm) return dvm;
                    if (RootItems != null)
                        foreach (var ri in RootItems)
                            if (ri is ComplexVM cri)
                            {
                                var rzi = DeepGetVM(cri.Items, model);
                                if (rzi != null) return rzi;
                            }
                    return null;
                }
            }

        }
        public void ClearModel()
        {
            VisitVM.Model = null;
        }
        public override IEnumerable<T>? Find<T>()
        {
            return _VisitVM?.Find<T>();
        }

        public override VMBase? Find(string id)
        {
            if (_VisitVM != null)
            {
                if (_VisitVM.ContentID == id) return VisitVM;
                if (_VisitVM.Items != null)
                    foreach (var i in _VisitVM.Items)
                    {
                        if (i.ContentID == id) return i;
                        if (i is ComplexVM c)
                        {
                            var r = c.Find(id);
                            if (r != null) return r;
                        }
                    }
            }
            return null;
        }
            [XmlIgnore] public new Visit? Model
        {
            get => (Visit?)base.Model;
            set
            {
                if (base.Model != value)
                {
                    base.Model = value;

                    if (value == null) ClearModel();
                    else
                        SetModel(value);
                }
                VisitVM.SetParent(this);
                VisitVM.RemoveChildEmptyModel();
            }
        }
        bool _TripExpanded = true;
        public bool TripExpanded
        {
            get => _TripExpanded; set
            {
                SetPropertyWithDrity(ref _TripExpanded, value);
            }
        }
        public bool ShouldSerializeTripExpanded() => !FileNotFound && !TripExpanded;

        bool _DockExpanded = true;
        public bool DockExpanded
        {
            get => _DockExpanded; set

                => SetPropertyWithDrity(ref _DockExpanded, value);
        }
        public bool ShouldSerializeDockExpanded() => !FileNotFound && !DockExpanded;

        bool _FileNotFound;
        [XmlIgnore]
        public override bool FileNotFound
        {
            get => _FileNotFound; set
            {
                if (SetProperty(ref _FileNotFound, value)) _Items = Update_Items();
            }
        }


        HeaderHelper[]? _Items;
        private HeaderHelper[] Update_Items()
        {
            if (FileNotFound) return new HeaderHelper[] { new HeaderFileHelper(this) };
            else return new HeaderHelper[]
            {
                    new HeaderHelper(this),
                    new HeaderDockHelper( this)
            };
        }
        [XmlIgnore]
        public HeaderHelper[] Items
        {
            get
            {
                if (_Items == null)
                {
                    _Items = Update_Items();
                }
                return _Items;
            }
        }

        private VisitVM? _VisitVM;
        public VisitVM VisitVM
        {
            get
            {
                if (_VisitVM == null)
                    _VisitVM = new VisitVM();

                return _VisitVM;
            }
            set
            {
                _VisitVM = value;
                _VisitVM.SetParent(this);
               // VisitVM.RemoveChildEmptyModel(); ????
            }
        }
        public static XmlSerializer Serializer => new XmlSerializer(typeof(VisitDocument), null, new[]
        {
                            typeof(TelesysVM),
                            typeof(DevicePBVM),
                            typeof(DeviceVM),
                            typeof(VisitVM),
                            typeof(TripVM),
                            typeof(SerialVM),
                            typeof(NopConVM),
                            typeof(NetVM),
                            typeof(BusVM),
                            typeof(BusPBVM),
                            typeof(BusUSO32VM),
         }, null, null, null);
        public static VisitDocument CreateAndSave(string visitModelFile, bool isroot)
        {
            var v = new VisitDocument();
            v.Model = new();
            v.FileFullName = visitModelFile;
            v.IsDirty = true;
            v.IsRoot = isroot;
            v.lastClosedFiles?.UserOpenFile(visitModelFile);
            v.Save();
            return v;
        }
        public static VisitDocument Load(string visitModelFile, bool Isroot)
        {
            FormAddedFrom SaveState = DockManagerVM.State;
            Visit VisitModel = null!;
            VisitDocument visitDocumentVM = null!;
            try
            {
                DockManagerVM.State = FormAddedFrom.DeSerialize;
                // load M
                try
                {
                    using (var fs = new StreamReader(visitModelFile, false))
                    {
                        VisitModel = (Visit)Visit.Serializer.Deserialize(fs)!;
                    }
                    if (VisitModel == null) throw new IOException($"BAD File {visitModelFile} can't load Visit!");
                }
                catch (Exception e)
                {
                    App.LogError(e, e.Message);
                }

                // load VM
                var vvm = Models.ProjectFile.GetTmpFile(visitModelFile, ".vstvm");
                if (File.Exists(vvm))
                {
                    try
                    {
                        using (var fs = new StreamReader(vvm, false))
                        {
                            visitDocumentVM = (VisitDocument)Serializer.Deserialize(fs)!;
                        }
                    }
                    catch (Exception e)
                    {
                        App.LogError(e, e.Message);
                    }
                }
                else
                    visitDocumentVM = new VisitDocument();

                if (Isroot)
                {   // load ViewModel DockManagerVM
                    var dmvms = Models.ProjectFile.GetTmpFile(visitModelFile, ".vstdmvm");
                    if (File.Exists(dmvms))
                    {
                        try
                        {
                            using (var fs = new StreamReader(dmvms, false))
                            {
                                DockManagerVM.DeSerialize(fs);
                            }
                        }
                        catch (Exception e)
                        {
                            App.LogError(e, e.Message);
                        }
                    }

                    // load Xceed DockManager                                               
                    var dms = Models.ProjectFile.GetTmpFile(visitModelFile, ".vstdm");
                    if (File.Exists(dms))
                    {
                        try
                        {
                            using (var fs = new StreamReader(dms, false))
                            {
                                var s = new XmlSerializer(typeof(DockManagerSerialize));
                                s.Deserialize(fs);
                            }
                        }
                        catch (Exception e)
                        {
                            App.LogError(e, e.Message);
                        }
                    }
                }

                if (visitDocumentVM == null) visitDocumentVM = new VisitDocument();//?? if (File.Exists(vvm)) need dialog to recreate visit doc

                visitDocumentVM.Model = VisitModel;
                visitDocumentVM.FileFullName = visitModelFile;
                visitDocumentVM.IsRoot = Isroot;
                visitDocumentVM.lastClosedFiles?.UserOpenFile(visitModelFile);
            }
            finally
            {
                DockManagerVM.State = SaveState;
            }
            return visitDocumentVM;
        }
        protected override void SaveModelAndViewModel()
        {
           // var l = ServiceProvider.GetRequiredService<ILogger<VisitDocument>>();
           // l.LogTrace("Save VM={} M={} '{}'", IsVMDirty, IsDirty, FileFullName);

            // save VM
            if (IsVMDirty)
            {
                var vvm = Models.ProjectFile.GetTmpFile(FileFullName, ".vstvm");
                try
                {
                    using (var fs = new StreamWriter(vvm, false))
                    {
                        Serializer.Serialize(fs, this);
                    }
                }
                catch (Exception e)
                {
                    App.LogError(e, e.Message);
                }
            }

            if (IsRoot && (IsVMDirty || NeedAnySave))
            {
                try
                {   // xceed DocManager
                    var dms = Models.ProjectFile.GetTmpFile(FileFullName, ".vstdm");
                    using (var fs = new StreamWriter(dms, false))
                    {
                        DockManagerSerialize d = new DockManagerSerialize();
                        (new XmlSerializer(typeof(DockManagerSerialize))).Serialize(fs, d);
                    }
                    // ViewModel DockManager
                    var dmvms = Models.ProjectFile.GetTmpFile(FileFullName, ".vstdmvm");
                    using (var fs = new StreamWriter(dmvms, false))
                    {
                        DockManagerVM.Serialize(fs);
                    }
                }
                catch (Exception e)
                {
                    App.LogError(e, e.Message);
                }
            }
            // save M
            if (IsDirty)
            {
                try
                {
                    using (var fs = new StreamWriter(FileFullName, false))
                    {
                        Visit.Serializer.Serialize(fs, Model);
                    }
                }
                catch (Exception e)
                {
                    App.LogError(e, e.Message);
                }
            }
            base.SaveModelAndViewModel();
        }
        #region Root
        bool _IsRoot;
        [XmlIgnore]
        public bool IsRoot
        {
            get => _IsRoot;
            set
            {
                if (_IsRoot != value)
                {
                    _IsRoot = value;
                    if (_IsRoot)
                    {
                        DockManagerVM.FormAdded += FormAddedEvent;
                        DockManagerVM.ActiveDocumentChanging += ActiveDocumentChangingEvent;
                        DockManagerVM.FormVisibleChanged += FormVisibleChanged;
                    }
                }
            }
        }
        #endregion
        public override void Remove(bool UserCloseFile = true)
        {
            if (UserCloseFile)
            {
                while (ChildFormIDs != null && ChildFormIDs.Count > 0)
                {
                    DockManagerVM.Remove(ChildFormIDs[0]!);
                }
            }

            if (IsRoot)
            {
                DockManagerVM.FormVisibleChanged -= FormVisibleChanged;
                DockManagerVM.FormAdded -= FormAddedEvent;
                DockManagerVM.ActiveDocumentChanging -= ActiveDocumentChangingEvent;
            }
            else if (UserCloseFile && RootFileDocumentVM.Instance is GroupDocument g) g.RemoveVisit(this);
            base.Remove(UserCloseFile);

            void Disp(ItemVM? root)
            {
                if (root == null) return;
                if (root is ComplexVM c && c.Items != null) 
                    foreach (var i in c.Items) Disp(i);
                root.Dispose();
            }
            Disp(_VisitVM);

            Model = null;
            //VisitVM.ClearModel();
            _Items = null;
        }
        void FormVisibleChanged(VMBaseForm? vMBaseForm) => NeedAnySave = vMBaseForm != null;
        void FormAddedEvent(object? sender, FormAddedEventArg e) => NeedAnySave = e.formAddedFrom == FormAddedFrom.User;
        void ActiveDocumentChangingEvent(object? sender, ActiveDocumentChangedEventArgs e) => NeedAnySave = e.OldActive != null;
    }
}
