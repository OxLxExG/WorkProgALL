using CommunityToolkit.Mvvm.Input;
using Core;
using Main.Models;
using Main.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Main.ViewModels
{
    public abstract class ItemVM: VMBase
    {
        public bool IsExpanded { get; set; } = true;
        public bool ShouldSerializeIsExpanded() => !IsExpanded;
        public bool IsSelected { get; set; }
        public bool ShouldSerializeIsSelected() => IsSelected;
        [XmlIgnore] public ObservableCollection<MenuItemVM> CItems { get; set; } = new ObservableCollection<MenuItemVM>();        
        public ItemVM() 
        { 
        }
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
            while (p is ComplexVM c) { p = c.Parent; }
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
            if (parent != null)
            {
                ComplexVM? m = parent.Target as ComplexVM;
                m?.ItemsRemove(this);
            }
        }
        #endregion
    }
    public abstract class ComplexVM: ItemVM
    {
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
                    foreach(var  d in dvs) tr.ItemsRemove(d);
                }
                // vm
                Items.Remove(item);
                if (Items.Count == 0) Items = null;
                SetDrity();
            }
        }
        public void ItemsAdd(ItemVM item)
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
    internal class ItemMprop
    {
        private WeakReference<ItemM>? _wr_model;
        internal virtual void SetModel(ItemM? value)
        {
            if (value == null) _wr_model = null;
            else
            if (_wr_model == null) _wr_model = new WeakReference<ItemM>(value);
            else
            {
                ItemM? m;
                if (_wr_model.TryGetTarget(out m) && m != value)
                {
                    _wr_model = new WeakReference<ItemM>(value);
                }
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
        [XmlIgnore] public ItemM? Model { get => itemMprop.GetModel(); set => itemMprop.SetModel(value); }
    }
    public abstract class ComplexModelVM : ComplexVM
    {
        private readonly ItemMprop itemMprop = new ItemMprop();
        [XmlIgnore] public ItemM? Model { get => itemMprop.GetModel(); set => itemMprop.SetModel(value); }    
    }
    /// <summary>
    /// Items => Devises
    /// model----
    /// </summary>
    public class BusVM : ComplexVM
    {
        
    }
    /// <summary>
    /// Items => Buses
    /// model----
    /// </summary>
    public class PipeVM : ComplexVM
    {

    }
    /// <summary>
    /// Items => Pipes, Devices(unconnected),
    /// Model Trip 
    /// </summary>
    public class TripVM : ComplexModelVM
    {

    }
    /// <summary>
    /// Items => Trips
    /// model visit
    /// </summary>
    public class VisitVM: ComplexModelVM 
    {
            
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
        public HeaderHelper(VisitDocument owner) { this.wr = new WeakReference<VisitDocument>(owner); }
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
                g.RemoveVisit(this);
            }
        }

        [XmlIgnore] public ObservableCollection<MenuItemVM> CItems { get; set; } = new ObservableCollection<MenuItemVM>();

        public bool IsSettingsExpanded { get; set; } = true;
        public bool ShouldSerializeIsSettingsExpanded() => !IsSettingsExpanded;
        [XmlIgnore]
        public new Visit? Model
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
        public bool TripExpanded
        {
            get => _TripExpanded; set
            {
                SetProperty(ref _TripExpanded, value);
            }
        }
        public bool ShouldSerializeTripExpanded() => !FileNotFound && !TripExpanded;

        bool _DockExpanded = true;
        public bool DockExpanded
        {
            get => _DockExpanded; set

                => SetProperty(ref _DockExpanded, value);
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
            Visit model = null!;
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
            var vvm = Models.ProjectFile.GetTmpFile(visitModelFile, ".vstvm");
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
                var dms = Models.ProjectFile.GetTmpFile(FileFullName, ".vstdm");
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
