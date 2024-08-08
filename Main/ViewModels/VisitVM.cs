using CommunityToolkit.Mvvm.Input;
using Core;
using Main.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Main.ViewModels
{
    public class ItemVM : VMBase
    {
        #region Parent
        private WeakReference? parent;
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
            while (p is ComplexVisitItemVM c) { p = c.Parent; }
            if (p is VMBaseFileDocument v) return v;
            else return null;
        }
        protected void SetDrity()
        {
            var v = GetRoot();
            if (v != null) v.IsDirty = true;
        }
        protected void Remove()
        {
            if (parent != null && model != null)
            {
                SetDrity();
                ComplexVisitItemVM? m = parent.Target as ComplexVisitItemVM;
                m?.ItemsRemove(model);

            }
        }
        #endregion
        public bool IsExpanded { get; set; } = true;
        public bool ShouldSerializeIsExpanded() => !IsExpanded;
        public bool IsSelected { get; set; }
        public bool ShouldSerializeIsSelected() => IsSelected;
        [XmlIgnore] public ObservableCollection<MenuItemVM> CItems { get; set; } = new ObservableCollection<MenuItemVM>();
        
        public ItemVM() 
        {
            CItems.Add(new CommandMenuItemVM
            {
                Header = "Delete",
                Command = new RelayCommand(Remove),
                ContentID = "DEL",
                Priority = 10000,
            });
        }

        #region Factory
        protected static readonly Dictionary<Type, Func<ModelItem, ItemVM>> _factory = new();

        protected static void AddFactory(Type tp, Func<ModelItem, ItemVM> FactoryFunc)
        {
            if (!_factory.ContainsKey(tp))
                _factory.Add(tp, FactoryFunc);
        }
        protected static void AddFactory<M, VM>()
            where VM : ItemVM, new()
            where M : ModelItem
        {
            var tp = typeof(M);
            if (!_factory.ContainsKey(tp))
                _factory.Add(tp, m => new VM()
                {
                    model = (M)m
                });
        }
        protected static ItemVM GetBaseVisitItemVM(ModelItem m)
        {
            Func<ModelItem, ItemVM> f;
            if (_factory.TryGetValue(m.GetType(), out f!)) ///TODO: Recur Base Type Check ????
                return f(m);
            throw new ArgumentException();
        }
        internal static T GetVM<T>(ModelItem m)
            where T : ItemVM
        {
            var r = GetBaseVisitItemVM(m);
            if (r is T t) //ERROR then bus
                return t;
            throw new ArgumentException();
        }
        #endregion
        #region Model
        internal virtual void SetModel(ModelItem? value)
        {
            if (value == null) _wr_model = null;
            else
            if (_wr_model == null) _wr_model = new WeakReference<ModelItem>(value);
            else
            {
                ModelItem? m;
                if (_wr_model.TryGetTarget(out m) && m != value)
                {
                    _wr_model = new WeakReference<ModelItem>(value);
                }
            }
        }
        internal void ClearModel() => _wr_model = null;

        private WeakReference<ModelItem>? _wr_model;
        [XmlIgnore] public ModelItem? model
        {
            get
            {
                ModelItem? m = null;
                if (_wr_model != null)
                {
                    _wr_model.TryGetTarget(out m);
                }
                return m;
            }
            set
            {
                SetModel(value);
            }
        }
        #endregion
    }
    public abstract class SimpleVisitItemVM : ItemVM
    {
        internal override void SetModel(ModelItem? m)
        {
            if (m is Device)
            {
                if (string.IsNullOrEmpty(ContentID)) ContentID = m.Id.ToString("D");
                base.SetModel(m);
            }
            else throw new ArgumentException();
        }
        [XmlIgnore] public new Device? model { get => (Device?)base.model; set => base.model = value; }
    }
    public abstract class ComplexVisitItemVM : ItemVM
    {
        public abstract void RemoveChildEmptyModel();
        public abstract void ItemsRemove(ModelItem item);
        public abstract ItemVM ItemsAdd(ModelItem item);
        public abstract bool ContainsModel(string modelID);
    }
    public abstract class ComplexVisitItemVM<CHILD, CHILDVM> : ComplexVisitItemVM
        where CHILD : ModelItem
        where CHILDVM : ItemVM, new()
    {
        public override void RemoveChildEmptyModel()
        {
            for(int i = Items.Count-1; i >= 0; i--)
            {
                var vm = Items[i];

                if (vm.model == null)
                {
                    Items.RemoveAt(i);
                    SetDrity();
                }
                else
                    if (Items[i] is ComplexVisitItemVM m) m.RemoveChildEmptyModel();
            }
        }
        public override void SetParent(object? par)
        {
            foreach (var i in Items) i.SetParent(this);
            base.SetParent(par);
        }
        internal override void SetModel(ModelItem? m)
        {
            if (m is ComplexModelItem<CHILD> mm) SetModel(mm);
            else throw new ArgumentException();
        }
        internal void SetModel(ComplexModelItem<CHILD>? m)
        {
            if (model != null || m == null) throw new InvalidOperationException();
            base.SetModel(m);
            var ID = m!.Id.ToString("D");
            if (string.IsNullOrEmpty(ContentID))
            {
                ContentID = ID;
                Items = new ObservableCollection<CHILDVM>(m.Items.Select(GetVM<CHILDVM>));
            }
            else if (ContentID != ID) throw new InvalidOperationException();
            else
            {
                foreach (var item in m.Items)
                {
                    var id = item.Id.ToString("D");
                    var md = GetViewModel(id);
                    if (md == null)
                    {
                        Add(item);
                    }
                    else md.model = item;
                }
                for (var i = Items.Count - 1; i >= 0; i--)
                {
                    var item = Items[i];
                    if (item.model == null) Items.RemoveAt(i);
                }
            }
        }
        [XmlIgnore] public new ComplexModelItem<CHILD>? model
        {
            get => (ComplexModelItem<CHILD>?)base.model;
            set => base.model = value;
        }

        //[XmlIgnore]
        public ObservableCollection<CHILDVM> Items { get; set; } = new ObservableCollection<CHILDVM>();
        public bool ShouldSerializeItems() => Items.Count > 0;
        public CHILDVM? GetViewModel(string modelID) => Items.FirstOrDefault(vm => vm.ContentID == modelID);
        public override bool ContainsModel(string modelID) => GetViewModel(modelID) != null;
        //public override void ItemsAdd(VisitItemVM item)
        //{
        //    if (item is CHILDVM t)
        //    {
        //        if (!Items.Contains(t)) Items.Add(t);
        //    }
        //    else throw new InvalidOperationException();
        //}
        //public override void ItemsRemove(VisitItemVM item)
        //{
        //    if (item is CHILDVM t) Items.Remove(t);
        //    else throw new InvalidOperationException();
        //}
        public override void ItemsRemove(ModelItem item)
        {
            if (item is CHILD m)
            {
                var vm = GetViewModel(m.Id.ToString("D"));
                if (vm != null)
                {
                    Items.Remove(vm);
                }
                model?.ItemsRemove(m);
            }
            else throw new InvalidOperationException();
        }
        public override ItemVM ItemsAdd(ModelItem item)
        {
            CHILDVM? vm;
            if (item is CHILD m)
            {
                vm = GetViewModel(m.Id.ToString("D"));
                if (vm == null)
                {
                    vm = GetVM<CHILDVM>(m);
                    model?.ItemsAdd(m);
                    vm.SetParent(this);
                    Items.Add(vm);
                }
                return vm;
            }
            else throw new InvalidOperationException();
        }
        public CHILDVM Add(ModelItem item) => (CHILDVM)this.ItemsAdd(item);
    }



    public class DeviceVM : SimpleVisitItemVM;
    public class DevicePBVM : DeviceVM;
    public class DeviceT1VM : DeviceVM;
    public class DeviceT2VM : DeviceVM;
    public class BusVM : ComplexVisitItemVM<Device, DeviceVM>
    {
        [XmlIgnore] public Bus? bus { get => (Bus?)model; set => model = value; }
    }
    public class BusPBVM : BusVM;// ComplexBaseVisitItemVM<DevicePB, DevicePBVM>;
    public class PipeVM : ComplexVisitItemVM<Bus, BusVM>
    {
        [XmlIgnore] public Pipe? pipe { get => (Pipe?)model; set => model = value; }
    }
    public class SerialPipeVM : PipeVM
    {
        [XmlIgnore] public new SerialPipe? pipe { get => (SerialPipe?)model; set => model = value; }
    }
    public class NetPipeVM : PipeVM
    {
        [XmlIgnore] public new NetPipe? pipe { get => (NetPipe?)model; set => model = value; }
    }
    public class TripVM : ComplexVisitItemVM<Pipe, PipeVM>
    {
        [XmlIgnore] public Trip? trip { get => (Trip?)model; set => model = value; }
    }
    public class VisitVM : ComplexVisitItemVM<Trip, TripVM>
    {

        public static implicit operator VisitVM(Visit d)
        {
            var r = new VisitVM();
            r.SetModel(d);
            r.SetParent(null);
            return r;
        }
        static VisitVM()
        {
            AddFactory<Visit, VisitVM>();
            AddFactory<Trip, TripVM>();
            AddFactory<Pipe, PipeVM>();
            AddFactory<SerialPipe, SerialPipeVM>();
            AddFactory<NetPipe, NetPipeVM>();
            AddFactory<Bus, BusVM>();
            AddFactory<BusPB, BusPBVM>();
            AddFactory<DevicePB, DevicePBVM>();
            AddFactory<DeviceTelesystem, DeviceT1VM>();
            AddFactory<DeviceTelesystem2, DeviceT2VM>();
        }
        public void RemoveEmptyModel()
        {

        }
    }

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
        public virtual string Header => "Trips";
        public virtual IEnumerable? Items => owner?.VisitVM.Items; 
        public HeaderHelper(VisitDocument owner) { this.wr = new WeakReference<VisitDocument>(owner);  }
    }
    public class HeaderDockHelper : HeaderHelper
    {
        public override bool IsExpanded { get => owner == null ? true : owner.DockExpanded; set { if (owner != null) owner.DockExpanded = value; } }
        public override string Header => "Documents";
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
                Command = new RelayCommand(Remove),
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

        protected void Remove()
        {
            if (RootFileDocumentVM.Instance is GroupDocument g)
            {
                g.RemoveVisit( this );
            }
        }

        [XmlIgnore] public ObservableCollection<MenuItemVM> CItems { get; set; } = new ObservableCollection<MenuItemVM>();

        public bool IsSettingsExpanded { get; set; } = true;
        public bool ShouldSerializeIsSettingsExpanded() => !IsSettingsExpanded;
        [XmlIgnore] public new Visit? Model 
        { 
            get => (Visit?)base.Model; 
            set 
            {
                if (base.Model != value)
                {
                    base.Model = value;

                    if (value == null) VisitVM.ClearModel();
                    else
                        VisitVM.SetModel(value);
                }
                VisitVM.SetParent(this);
                VisitVM.RemoveChildEmptyModel();
            }
        }
        bool _TripExpanded = true;
        public bool TripExpanded { get => _TripExpanded; set 
            {
                SetProperty(ref _TripExpanded, value);
            } 
        }
        public bool ShouldSerializeTripExpanded() => !FileNotFound && !TripExpanded;

        bool _DockExpanded = true;
        public bool DockExpanded { get=> _DockExpanded; set 
            
                =>SetProperty(ref _DockExpanded, value);             
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
                VisitVM.RemoveChildEmptyModel();
            }
        }
        public static XmlSerializer Serializer => new XmlSerializer(typeof(VisitDocument), null, new[]
        {
                            typeof(DevicePBVM),
                            typeof(DeviceT1VM),
                            typeof(DeviceT2VM),
                            typeof(SerialPipeVM),
                            typeof(NetPipeVM),
                             typeof(BusPBVM),
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
            Visit model =null!;
            VisitDocument d = null!;
            // load M
            try
            {
                using (var fs = new StreamReader(visitModelFile, false))
                {
                    model = (Visit)Visit.Serializer.Deserialize(fs)!;
                }
                if (model == null) throw new IOException($"BAD File {visitModelFile} can't load Visit!");
            }
            catch (Exception e)
            {
                App.LogError(e, e.Message);
            }

            // load VM
            var vvm = ProjectFile.GetTmpFile(visitModelFile, ".vstvm");
            if (File.Exists(vvm))
            {
                try
                {
                    using (var fs = new StreamReader(vvm, false))
                    {
                        d = (VisitDocument)Serializer.Deserialize(fs)!;
                    }
                }
                catch (Exception e)
                {
                    App.LogError(e, e.Message);
                }
            }
            else
                d = new VisitDocument();

            if (Isroot)
            {
                var dms = ProjectFile.GetTmpFile(visitModelFile, ".vstdm");
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


            d.Model = model;
            d.FileFullName = visitModelFile;
            d.IsRoot = Isroot;
            d.lastClosedFiles?.UserOpenFile(visitModelFile);
            return d;
        }
        protected override void SaveModelAndViewModel()
        {
            var l = ServiceProvider.GetRequiredService<ILogger<VisitDocument>>();
            l.LogTrace("Save VM={} M={} '{}'", IsVMDirty, IsDirty, FileFullName);

            // save VM
            if (IsVMDirty)
            {
                var vvm = ProjectFile.GetTmpFile(FileFullName, ".vstvm");
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
                var dms = ProjectFile.GetTmpFile(FileFullName, ".vstdm");
                try
                {
                    using (var fs = new StreamWriter(dms, false))
                    {
                        DockManagerSerialize d = new DockManagerSerialize();
                        (new XmlSerializer(typeof(DockManagerSerialize))).Serialize(fs, d);
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
                    if (IsRoot)
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
            if (IsRoot)
            {
                DockManagerVM.FormVisibleChanged -= FormVisibleChanged;
                DockManagerVM.FormAdded -= FormAddedEvent;
                DockManagerVM.ActiveDocumentChanging -= ActiveDocumentChangingEvent;
            }
            else if (UserCloseFile && RootFileDocumentVM.Instance is GroupDocument g) g.RemoveVisit(this);
            base.Remove(UserCloseFile);
            Model = null;
            //VisitVM.ClearModel();
            _Items = null;
        }
        void FormVisibleChanged(VMBaseForm? vMBaseForm) => NeedAnySave = vMBaseForm != null;
        void FormAddedEvent(object? sender, FormAddedEventArg e) => NeedAnySave = e.formAddedFrom == FormAddedFrom.User;
        void ActiveDocumentChangingEvent(object? sender, ActiveDocumentChangedEventArgs e) => NeedAnySave = e.OldActive != null;
    }
}
