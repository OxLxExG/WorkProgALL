using Main.Models;
using Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Serialization;
using Connections;
using System.IO;
using System.Windows.Media.Media3D;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Main.ViewModels
{
    public abstract class VisitItemVM : VMBase
    {
        public bool IsExpanded { get; set; }
        public bool IsSelected { get; set; }
        #region Factory
        protected static readonly Dictionary<Type, Func<ModelVisitItem, VisitItemVM>> _factory = new();

        protected static void AddFactory(Type tp, Func<ModelVisitItem, VisitItemVM> FactoryFunc)
        {
            if (!_factory.ContainsKey(tp))
                _factory.Add(tp, FactoryFunc);
        }
        protected static void AddFactory<M, VM>()
            where VM : VisitItemVM, new()
            where M : ModelVisitItem
        {
            var tp = typeof(M);
            if (!_factory.ContainsKey(tp))
                _factory.Add(tp, m => new VM()
                {
                    model = (M)m
                });
        }
        protected static VisitItemVM GetBaseVisitItemVM(ModelVisitItem m)
        {
            Func<ModelVisitItem, VisitItemVM> f;
            if (_factory.TryGetValue(m.GetType(), out f!)) ///TODO: Recur Base Type Check ????
                return f(m);
            throw new ArgumentException();
        }
        internal static T GetVM<T>(ModelVisitItem m)
            where T : VisitItemVM
        {
            var r = GetBaseVisitItemVM(m);
            if (r is T t) //ERROR then bus
                return t;
            throw new ArgumentException();
        }
        #endregion
        internal abstract void SetModel(ModelVisitItem? m);// => _model = m;

        protected ModelVisitItem? _model;
        [XmlIgnore] public ModelVisitItem? model { get => _model;
            set
            {
                if (_model == value) return;
                SetModel(value);
            }
        }
    }
    public abstract class SimpleVisitItemVM : VisitItemVM
    {
        internal override void SetModel(ModelVisitItem? m)
        {
            if (m is Device)
            {
                if (string.IsNullOrEmpty(ContentID)) ContentID = m.Id.ToString("D");
                _model = m;
            }
            else throw new ArgumentException();
        }
        [XmlIgnore] public new Device? model { get => (Device?)base.model; set => base.model = value; }
    }
    public abstract class ComplexVisitItemVM : VisitItemVM
    {
        //public abstract void ItemsRemove(VisitItemVM item);
        //public abstract void ItemsAdd(VisitItemVM item);
        public abstract void ItemsRemove(ModelVisitItem item);
        public abstract VisitItemVM ItemsAdd(ModelVisitItem item);
        public abstract bool ContainsModel(string modelID);
    }
    public abstract class ComplexVisitItemVM<CHILD, CHILDVM> : ComplexVisitItemVM
        where CHILD : ModelVisitItem
        where CHILDVM : VisitItemVM, new()
    {
        internal override void SetModel(ModelVisitItem? m)
        {
            if (m is ComplexModelVisitItem<CHILD> mm) SetModel(mm);
            else throw new ArgumentException();
        }
        internal void SetModel(ComplexModelVisitItem<CHILD>? m)
        {
            if (_model != null || m == null) throw new InvalidOperationException();
            _model = m;
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
                for (var i = Items.Count-1;i >= 0;i--)
                {
                    var item = Items[i];
                    if (item.model == null) Items.RemoveAt(i);  
                }
            }
        }
        [XmlIgnore] public new ComplexModelVisitItem<CHILD>? model
        {
            get => (ComplexModelVisitItem<CHILD>?)base.model;
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
        public override void ItemsRemove(ModelVisitItem item)
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
        public override VisitItemVM ItemsAdd(ModelVisitItem item)
        {
            CHILDVM? vm;
            if (item is CHILD m)
            {
                vm = GetViewModel(m.Id.ToString("D"));
                if (vm == null)
                {
                    vm = GetVM<CHILDVM>(m);
                    model?.ItemsAdd(m);
                    Items.Add(vm);
                }
                return vm;
            }
            else throw new InvalidOperationException();
        }
        public CHILDVM Add(ModelVisitItem item)=> (CHILDVM) this.ItemsAdd(item);
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
    public class TripVM : ComplexVisitItemVM<Pipe, PipeVM>
    {
      [XmlIgnore]  public Trip? trip { get => (Trip?)model; set => model = value; }
    }
    public class VisitVM : ComplexVisitItemVM<Trip, TripVM>
    {

        public static implicit operator VisitVM(Visit d)
        {
            var r = new VisitVM();
            r.SetModel(d);
            return r;
        }
        static VisitVM() 
        {
            AddFactory<Visit, VisitVM>();
            AddFactory<Trip, TripVM>();
            AddFactory<Pipe, PipeVM>();
            AddFactory<Bus, BusVM>();
            AddFactory<BusPB, BusPBVM>();
            AddFactory<DevicePB, DevicePBVM>();
            AddFactory<DeviceTelesystem, DeviceT1VM>();
            AddFactory<DeviceTelesystem2, DeviceT2VM>();
        }
        public void CheckTree()
        {

        }
    }

    public class HeaderHelper: VMBase
    {
        protected VisitDocument owner;
        public virtual bool IsExpanded { get => owner.TripExpanded; set => owner.TripExpanded = value; }
        public bool IsSelected { get; set; }
        public virtual string Header => "Trips";
        public virtual IEnumerable? Items => owner.VisitVM.Items; 
        public HeaderHelper(VisitDocument owner) { this.owner = owner;  }
    }
    public class HeaderDockHelper : HeaderHelper
    {
        public override bool IsExpanded { get => owner.DockExpanded; set => owner.DockExpanded = value; }
        public override string Header => "Documents";
        public override IEnumerable? Items => owner.ChildDocuments;
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

        [XmlIgnore] public new Visit? Model 
        { 
            get => (Visit?)base.Model; 
            set 
            {
                if (base.Model != value)
                {
                    base.Model = value;

                    VisitVM.SetModel(value);
                }
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
            }
        }
        public static XmlSerializer Serializer => new XmlSerializer(typeof(VisitDocument), null, new[]
        {
                            typeof(DevicePBVM),
                            typeof(DeviceT1VM),
                            typeof(DeviceT2VM),
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
            Visit model;
            // load M
            using (var fs = new StreamReader(visitModelFile, false))
            {
                model = (Visit)Visit.Serializer.Deserialize(fs)!;
            }
            // load VM
            VisitDocument d;
            var vvm = ProjectFile.GetTmpFile(visitModelFile, ".vstvm");
            if (File.Exists(vvm))
            {
                using (var fs = new StreamReader(vvm, false))
                {
                    d = (VisitDocument)Serializer.Deserialize(fs)!;
                }
            } 
            else 
                d = new VisitDocument();

            if (Isroot)
            {
                var dms = ProjectFile.GetTmpFile(visitModelFile, ".vstdm");
                if (File.Exists(dms))
                {
                    using (var fs = new StreamReader(dms, false))
                    {
                        var s = new XmlSerializer(typeof(DockManagerSerialize));
                        s.Deserialize(fs);
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
            try
            {

                // save VM
                if (IsVMDirty)
                {
                    var vvm = ProjectFile.GetTmpFile(FileFullName, ".vstvm");
                    using (var fs = new StreamWriter(vvm, false))
                    {
                        Serializer.Serialize(fs, this);
                    }
                }
                if (IsRoot && (IsVMDirty || NeedAnySave))
                {
                    var dms = ProjectFile.GetTmpFile(FileFullName, ".vstdm");
                    using (var fs = new StreamWriter(dms, false))
                    {
                        DockManagerSerialize d = new DockManagerSerialize();
                        (new XmlSerializer(typeof(DockManagerSerialize))).Serialize(fs, d);
                    }
                }
                // save M
                if (IsDirty)
                {
                    using (var fs = new StreamWriter(FileFullName, false))
                    {
                        Visit.Serializer.Serialize(fs, Model);
                    }
                }
                base.SaveModelAndViewModel();
            }
            catch
            {
               //TODO: Logg error
            }
        }
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
        public override void Remove(bool UserCloseFile = true)
        {
            if (IsRoot)
            {
                DockManagerVM.FormVisibleChanged -= FormVisibleChanged;
                DockManagerVM.FormAdded -= FormAddedEvent;
                DockManagerVM.ActiveDocumentChanging -= ActiveDocumentChangingEvent;
            }
            base.Remove(UserCloseFile);
        }
        void FormVisibleChanged(VMBaseForm? vMBaseForm) => NeedAnySave = vMBaseForm != null;
        void FormAddedEvent(object? sender, FormAddedEventArg e) => NeedAnySave = e.formAddedFrom == FormAddedFrom.User;
        void ActiveDocumentChangingEvent(object? sender, ActiveDocumentChangedEventArgs e) => NeedAnySave = e.OldActive != null;
    }
}
