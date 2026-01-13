using CommunityToolkit.Mvvm.Input;
using Connections;
using Core;
using Global;
using HorizontDrilling.Models;
using HorizontDrilling.Properties;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace HorizontDrilling.ViewModels
{
    using static HorizontDrilling.Models.MMenus;


    public class RootNotNullCommandMenuItemVM : CommandMenuItemVM
    {
        public RootNotNullCommandMenuItemVM(rootMenu root) : base(root)
        {
            RootFileDocumentVM.StaticPropertyChanged += (o, e) => IsEnable = RootFileDocumentVM.Instance != null;
        }
    }
    public class RootIsGroupCommandMenuItemVM : CommandMenuItemVM
    {
        public RootIsGroupCommandMenuItemVM(rootMenu root) : base(root)
        {
            RootFileDocumentVM.StaticPropertyChanged += (o, e) => IsEnable = RootFileDocumentVM.Instance is GroupDocument;
        }
    }
    //internal class ProjectsSingleMenuFactory : IMenuItemClient
    //{
    //    public void AddStaticMenus(IMenuItemServer s)
    //    {
    //        s.Add(RootMenusID.NFile, new LastSettingsMenuVM[]
    //        {
    //           // VMBase.ServiceProvider.GetRequiredService<LastSingleVisitMenuVM>(),
    //            VMBase.ServiceProvider.GetRequiredService<LastFileMenuVM>()
    //        });
    //        //s.Add(NewProject.ParentStaticRootID, new CommandMenuItemVM(NewProject)
    //        //{
    //        //    IconSource = "pack://application:,,,/Images/NewProject.PNG",
    //        //    Command = new RelayCommand(() =>
    //        //    {
    //        //       // ProjectsGroupMenuFactory.Helper(() => RootFileDocumentVM.Instance = (RootFileDocumentVM?)RootFileDocumentVM.InstanceFactory!.CreateNew());
    //        //    })
    //        //});
    //        //s.Add(OpenProject.ParentStaticRootID, new CommandMenuItemVM(OpenProject)
    //        //{
    //        //    IconSource = "pack://application:,,,/Images/OpenProject.PNG",
    //        //    Command = new RelayCommand(() =>
    //        //    {
    //        //        //var file = RootFileDocumentVM.InstanceFactory!.LoadDialog();
    //        //        //if (file != string.Empty) 
    //        //        //    ProjectsGroupMenuFactory.Helper(() => RootFileDocumentVM.Instance = (RootFileDocumentVM?)RootFileDocumentVM.InstanceFactory!.LoadNew(file));
    //        //    })
    //        //});
    //    }
    //}
    [RegService(typeof(IMenuItemClient), IsSingle: false)]
    internal class ProjectsGroupMenuFactory : IMenuItemClient
    {
        internal static IFileOpenDialog GetFileOpenDialog()
        {
            var od = VMBase.FOpen();
            //od.Title = Resources.nfile_Open_ALL;

            od.InitialDirectory = ProjectFile.WorkDirs.Count > 0 ? ProjectFile.WorkDirs[0]! : ProjectFile.RootDir;
            //od.Filter = $"{Resources.nfile_Flt_ALL} (.vst, .vstgrp)|*.vst;*.vstgrp";

            object[] o = new object[]
                {
                                $"{Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments)}\\Горизонт\\WorkProg\\Projects\\",
                                new Guid("FDD39AD0-238F-46AF-ADB4-6C85480369C7"),//документы
                                new Guid("1777F761-68AD-4D8A-87BD-30B759FA33DD"),//избрвнное
                };
            var o2 = ProjectFile.WorkDirs.Cast<object>().ToArray();
            od.CustomPlaces = o2.Concat(o).ToArray();
            return od;
        }
        //private static readonly Type[] obj = {
        //                    typeof(DevicePB),
        //                    typeof(DeviceTelesystem),
        //                    typeof(DeviceTelesystem2),
        //                    typeof(SerialConn),
        //                    typeof(BusPB),
        //                };
        //private static readonly Type[] objVM = {
        //                    typeof(DevicePBVM),
        //                    typeof(DeviceT1VM),
        //                    typeof(DeviceT2VM),
        //                     typeof(BusPBVM),
        //                };

        //internal static void Helper(Action AssignInstance)
        //{
        //    RootFileDocumentVM.Instance?.Remove();
        //    RootFileDocumentVM.Instance = null;
        //    DockManagerVM.Clear();
        //    AssignInstance();
        //    //RootFileDocumentVM.Instance?.AddChildForm(DockManagerVM.AddOrGetandShow(nameof(ProjectsExplorerVM), FormAddedFrom.DeSerialize));
        //}
        public void AddStaticMenus(IMenuItemServer s)
        {
            s.Add(RootMenusID.NFile, new LastSettingsMenuVM[]
            {
                VMBase.ServiceProvider.GetRequiredService<LastGroupMenuVM>(),
                VMBase.ServiceProvider.GetRequiredService<LastFileMenuVM>()
            });
            //s.Add(NewProjectGroup.ParentStaticRootID, new CommandMenuItemVM(NewProjectGroup)
            //{
            //    IconSource = "pack://application:,,,/Images/NewProject.PNG",
            //    //Command = new RelayCommand(() => 
            //    //    Helper(() => RootFileDocumentVM.Instance = (RootFileDocumentVM) RootFileDocumentVM.InstanceFactory!.CreateNew()))
            //});
            //s.Add(OpenProjectGroup.ParentStaticRootID, new CommandMenuItemVM(OpenProjectGroup)
            //{
            //    IconSource = "pack://application:,,,/Images/OpenProject.PNG",
            //    //Command = new RelayCommand(() =>
            //    //{
            //    //    var file = RootFileDocumentVM.InstanceFactory!.LoadDialog();
            //    //    if (file != string.Empty) Helper(() =>
            //    //        RootFileDocumentVM.Instance = RootFileDocumentVM.InstanceFactory!.LoadNew(file) as RootFileDocumentVM);
            //    //})
            // }); 
            s.Add(NewProject.ParentStaticRootID, new CommandMenuItemVM(NewProject)
            {
                IconSource = "pack://application:,,,/Images/NewProject.PNG",
                InputGestureText = "Ctrl+V",
                Command = new RelayCommand(() =>
                {
                    ProjectFile.CreateNewProject(VMBase.ServiceProvider.GetRequiredService<ICreateNewVisitDialog>());

                    //var nnk = BinaryParser.Parse(BinaryParser.Meta_NNK);
                    //var cal = BinaryParser.Parse(BinaryParser.Meta_CAL);
                    //var ind = BinaryParser.Parse(BinaryParser.Meta_Ind);
                    //string ProjName = "D:\\Projects\\C#\\WorkProgMain\\bin\\Project.xml";
                    //string ProjNameVM = "D:\\Projects\\C#\\WorkProgMain\\bin\\ProjectVM.xml";
                    //var visOut = new Visit();
                    //var visOutVM = new VisitDocument();

                    //using (MemoryStream ms = new MemoryStream())
                    //{
                    //    XmlWriterSettings sttngs = new XmlWriterSettings();
                    //    sttngs.Indent = true;

                    //    using (var w = XmlWriter.Create(ms, sttngs))
                    //    {

                    //        w.WriteProcessingInstruction("xml-stylesheet", $"type=\"text/xsl\" href =\"{StructDef.SLTSTR}\"");
                    //        var t = new Trip{Parent = visOut};
                    //        AbstractConnection sp = new SerialConn() { PortName = "COM1" };
                    //        var p = new Pipe { connectionObj = sp, Parent = t };
                    //        var bs = new Bus { Parent = p, Name = "Accoustics T+R" };
                    //        var d1 = new DeviceTelesystem { Parent = bs };
                    //        var d5 = new DeviceTelesystem2 { Parent = bs };
                    //        var bs2 = new BusPB { Parent = p, Name = "vik" };
                    //        var d12 = new DevicePB { Parent = bs2, metaData = cal };
                    //        var d111 = new DevicePB { Parent = bs2, metaData = nnk };
                    //        var d3 = new DevicePB { Parent = bs2, metaData = ind };
                    //        visOut.UpdateParent(null);
                    //        visOutVM.VisitVM = visOut;
                    //        using (var file = File.Create(ProjNameVM))
                    //        {
                    //            XmlSerializer xsvm = new XmlSerializer(typeof(VisitDocument), null, objVM, null, null, null);
                    //            xsvm.Serialize(file, visOutVM);
                    //        }

                    //        XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                    //        //  ns.Add(StructDef.NS_PX, StructDef.NAMESPACE);
                    //        // ns.Add(Visit.NS_PX, Visit.NS);
                    //        ns.Add("xsi", XmlSchema.InstanceNamespace);
                    //        XmlSerializer xs = new XmlSerializer(typeof(Visit), null, obj.ToArray(), null, Visit.NS, null);
                    //        xs.Serialize(w, visOut, ns);
                    //    }

                    //    ms.Position = 0;
                    //    XDocument doc = XDocument.Load(new XmlTextReader(ms));
                    //    doc.Root?.Add(new XAttribute(XName.Get("schemaLocation", XmlSchema.InstanceNamespace),
                    //        $"{StructDef.NAMESPACE} {StructDef.SCHLOC} {Visit.NS} {Visit.SCH}"));
                    //    doc.Save(ProjName);
                    //}
                })
            });
            s.Add(OpenProject.ParentStaticRootID, new CommandMenuItemVM(OpenALL)
            {
                IconSource = "pack://application:,,,/Images/OpenProject.PNG",
                Command = new RelayCommand(() =>
                {
                    string file = string.Empty;
                    var od = GetFileOpenDialog();
                    od.Title = Resources.nfile_Open_ALL;
                    od.Filter = $"{Resources.nfile_Flt_ALL} (.vst, .vstgrp)|*.vst;*.vstgrp";
                    if (od.ShowDialog(s => file = s))
                    {
                        ProjectFile.CloseRoot(true);
                        ProjectFile.CreateOldProject(file);
                    }
                    //string ProjName = "D:\\Projects\\C#\\WorkProgMain\\bin\\Project.xml";
                    //Visit? visIn = null;
                    //using (var file = File.OpenRead(ProjName))
                    //{
                    //    XmlSerializer xs = new XmlSerializer(typeof(Visit), null, obj.ToArray(), null, Visit.NS, null);
                    //    visIn = (Visit?)xs.Deserialize(file);
                    //    visIn?.UpdateParent(null);
                    //    visIn?.UpdateParent(null);
                    //    visIn?.UpdateParent(null);
                    //}
                    //VisitVM? visVMIn = null;
                    //string ProjNameVM = "D:\\Projects\\C#\\WorkProgMain\\bin\\ProjectVM.xml";
                    //using (var file = File.OpenRead(ProjNameVM))
                    //{
                    //    XmlSerializer xs = new XmlSerializer(typeof(VisitVM), null, objVM, null, null, null);
                    //    visVMIn = (VisitVM?)xs.Deserialize(file);
                    //    if (visVMIn != null)
                    //    {
                    //        visVMIn.SetModel(visIn);
                    //        visVMIn.IsExpanded = true;
                    //    }
                    //}
                })
            });
            s.Add(AddProject.ParentStaticRootID, new CommandMenuItemVM(AddNewProject)
            {
//                IconSource = "pack://application:,,,/Images/OpenProject.PNG",
                Command = new RelayCommand(() =>
                {
                    ProjectFile.AddNewProject(VMBase.ServiceProvider.GetRequiredService<ICreateNewVisitDialog>());
                })
            });
            s.Add(AddProject.ParentStaticRootID, new CommandMenuItemVM(AddProject)
            {
  //              IconSource = "pack://application:,,,/Images/OpenProject.PNG",
                Command = new RelayCommand(() =>
                {
                    string file = string.Empty;
                    var od = GetFileOpenDialog();
                    od.Title = Resources.nfile_Open_ALL;
                    od.Filter = $"{Resources.nfile_Flt_ALL} (.vst)|*.vst";
                    if (od.ShowDialog(s => file = s))
                    {
                        ProjectFile.Add(file);
                    }
                })
            });
            var add = s.Find(AddProject.ParentStaticRootID);
            RootFileDocumentVM.StaticPropertyChanged += (o, e) =>  add!.IsEnable = RootFileDocumentVM.Instance is GroupDocument;
        }
    }

    //public class DocumentFileVM: VMBaseFileDocument
    //{
    //    public DocumentFileVM() 
    //    {
    //        lastClosedFiles = ServiceProvider.GetRequiredService<LastFileMenuVM>();
    //    }

    //}
    //public class VisitFileVM : ComplexFileDocumentVM
    //{
    //    internal const string EXT = "vst";
    //    public VisitFileVM() 
    //    {
    //       // lastClosedFiles = ServiceProvider.GetRequiredService<LastVisitMenuVM>();
    //        ChildlastClosedFiles = ServiceProvider.GetRequiredService<LastFileMenuVM>();
    //    }
    //}

    //public abstract class RootabctractVM: RootFileDocumentVM
    //{
    //    public override void Remove(bool UserCloseFile = true)
    //    {
    //        base.Remove(UserCloseFile);
    //        if (UserCloseFile)
    //        {
    //            Settings.Default.CurrentRoot = string.Empty;
    //            Settings.Default.Save();
    //        }
    //    }
    //    public  void UpdateSettingsCurrentRoot()
    //    {
    //        if (Settings.Default.CurrentRoot != FileFullName)
    //        {
    //            lastClosedFiles?.UserCloseFile(Settings.Default.CurrentRoot);
    //            Settings.Default.CurrentRoot = FileFullName;
    //            Settings.Default.Save();
    //        }
    //    }
    //    protected override void SaveModelAndViewModel()
    //    {
    //        base.SaveModelAndViewModel();
    //        XmlSerializer xser = new XmlSerializer(GetType());

    //        using (var fs = new StreamWriter(FileFullName, false))
    //        {
    //            xser.Serialize(fs, this);
    //        }
    //        UpdateSettingsCurrentRoot();
    //    }
    //}
    //public class VisitSingleVM : RootabctractVM
    //{
    //    internal const string EXT = "xpr";
    //    public VisitSingleVM()
    //    {
            
    //        Title = Properties.Resources.tProject;
    //        DefaultTitle = Title;

    //        var s = ServiceProvider.GetRequiredService<GlobalSettings>();
    //        var ss = Properties.Settings.Default.ProjectsDir;
    //        if (!string.IsNullOrEmpty(ss)) InitialDirectory = ss;
    //        else if (s.ProjectDir != string.Empty) InitialDirectory = s.ProjectDir;
    //        else
    //        {
    //            var se = Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
    //            InitialDirectory = $"{se}\\Горизонт\\WorkProg\\Projects\\";
    //        }
    //        Filter = $"{Title} (.{EXT})|*.{EXT}";

    //        CustomPlaces = new object[]
    //        {
    //            InitialDirectory,
    //            $"{Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments)}\\Горизонт\\WorkProg\\Projects\\",
    //            new Guid("FDD39AD0-238F-46AF-ADB4-6C85480369C7"),//документы
    //            new Guid("1777F761-68AD-4D8A-87BD-30B759FA33DD"),//избрвнное
    //        };

    //        DefaultExt = EXT;

    //        lastClosedFiles = ServiceProvider.GetRequiredService<LastSingleVisitMenuVM>();
    //        ChildlastClosedFiles = ServiceProvider.GetRequiredService<LastFileMenuVM>();
    //    }
    //    public override void Remove(bool UserCloseFile = true)
    //    {
    //        base.Remove(UserCloseFile);
    //        if (Settings.Default.ProjectsDir != this.FilePath)
    //        {
    //            Settings.Default.ProjectsDir = this.FilePath;
    //            Settings.Default.Save();
    //        }
    //    }
    //}
    //public class RootDocFactory<T> : DocumentFactory where T : RootabctractVM, new()
    //{
    //    public string fileRootName = string.Empty;

    //    public override VMBaseFileDocument CreateNew()
    //    {
    //        T t = new T();
    //        Directory.CreateDirectory(t.InitialDirectory);

    //        int i = 1;
    //        string s;
    //        while (File.Exists(s = $"{t.InitialDirectory}\\{fileRootName}{i}.{t.DefaultExt}")) i++;

    //        t.FileFullName = s;
    //        t.UpdateParent(null);
    //        return t;
    //    }

    //    public override string LoadDialog()
    //    {
    //        string file = string.Empty;
    //        (new T()).OpenDialog(s => file = s);
    //        return file;
    //    }

    //    public override VMBaseFileDocument? LoadNew(string file)
    //    {
    //        XmlSerializer xser = new XmlSerializer(typeof(T));
    //        using (var fs = new FileStream(file, FileMode.Open))
    //        {
    //            T? t = (T?)xser.Deserialize(fs);
    //            if (t != null)
    //            {
    //                t.FileFullName = file;
    //                t.UpdateParent(null);
    //                t.IsNew = false;
    //                t.IsDirty = false;
    //                t.lastClosedFiles?.UserOpenFile(file);
    //                t.UpdateSettingsCurrentRoot();
    //                return t;
    //            }
    //            return null;
    //        }
    //    }
    //}

    //public class DocFactory<T,M>: DocumentFactory 
    //    where T : VMBaseFileDocument, new()
    //    where M: new()
    //{
    //    public string  fileRootName = string.Empty;
    //    public  override VMBaseFileDocument CreateNew()
    //    {
    //        T t = new T();
    //        Directory.CreateDirectory(t.InitialDirectory);
    //        t.Model = new M();
    //        int i = 1;
    //        string s;
    //        while (File.Exists(s = $"{t.InitialDirectory}\\{fileRootName}{i}.{t.DefaultExt}")) i++;

    //        t.FileFullName = s;
    //        t.UpdateParent(null);
    //        return t;
    //    }
    //    public override VMBaseFileDocument? LoadNew(string file)
    //    {
    //        XmlSerializer xser = new XmlSerializer(typeof(M));
    //        using (var fs = new FileStream(file, FileMode.Open))
    //        {
    //            M? m = (M?)xser.Deserialize(fs);
    //            if (m != null)
    //            {
    //                T t = new T();
    //                t.Model = m;
    //                t.FileFullName = file;
    //                t.UpdateParent(null);
    //                t.IsNew = false;
    //                t.IsDirty = false;
    //                t.lastClosedFiles?.UserOpenFile(file);
    //                return t;
    //            }
    //            return null;
    //        }
    //    }
    //    public override string LoadDialog()
    //    {
    //        string file = string.Empty;
    //        (new T()).OpenDialog(s => file = s);
    //        return file;    
    //    }
    //}

    //public class VisitFactory : RootDocFactory<VisitSingleVM>
    //{
    //    public VisitFactory() { fileRootName = "Visit"; }
    //}

    //public class GroupFactory : RootDocFactory<VisitsGroupVM>
    //{
    //    public GroupFactory() { fileRootName = "VisitGroup"; }
    //}
//    public class VisitsGroupVM : RootabctractVM
//    {
//        internal const string EXT = "xpg";
//        public StringCollection LastClosedVisits { get; set; } = new StringCollection();
//        public bool ShouldSerializeLastClosedVisits() => LastClosedVisits.Count > 0;

//        #region Visits
//      //  private ObservableCollection<VisitFileVM>? _Visits;
//        public ObservableCollection<VisitFileVM> Visits { get; set; } = new ObservableCollection<VisitFileVM>();
//        //{
//        //    get
//        //    {
//        //        if (_Visits == null) _Visits = new ObservableCollection<VisitFileVM>();

//        //        return _Visits;
//        //    }
//        //    set { _Visits = value; }
//        //}
////        public bool ShouldSerializeVisits() => _Visits != null && _Visits.Count > 0;
//        public bool ShouldSerializeVisits() => Visits.Count > 0;
//        #endregion

//        #region prop lastClosedVisits Visits
//        protected LastClosedFilesVM? _lastClosedVisits = null;
//        /// <summary>
//        /// сам файл
//        /// </summary>
//        protected LastClosedFilesVM? lastClosedVisits
//        {
//            get => _lastClosedVisits;
//            set
//            {
//                if (_lastClosedVisits != value)
//                {
//                    //if (_lastClosedVisits != null)
//                    //{
//                    //    _lastClosedVisits.BeforeOpenClosedFileEvent -= BeforeOpenClosedFile;
//                    //    _lastClosedVisits.AfterOpenClosedFileEvent -= AfterOpenClosedFile;
//                    //}
//                    //if (value != null)
//                    //{
//                    //    value.BeforeOpenClosedFileEvent += BeforeOpenClosedFile;
//                    //    value.AfterOpenClosedFileEvent += AfterOpenClosedFile;
//                    //}
//                    _lastClosedVisits = value;
//                }
//            }
//        }
//        //protected void BeforeOpenClosedVisit(object? o, OpenClosedFileEventArg e)
//        //{
            
//        //}
//        //protected void AfterOpenClosedVisit(object? o, OpenClosedFileEventArg e)
//        //{
            
//        //}
//        #endregion

//        public static void AddVisit(string file)
//        {
//            if (Instance is VisitsGroupVM pgi)
//            {
//                DocFactory<VisitFileVM,Visit> f = new DocFactory<VisitFileVM, Visit> { fileRootName = "Visit" };
//                var d = f.LoadNew(file);
//                if (d!= null) pgi.AddChild(d);
//            }
//        }        
//        public VisitsGroupVM() : base()
//        {
//            Title = Properties.Resources.tProjectGroup;
//            DefaultTitle = Title;

//            var s = ServiceProvider.GetRequiredService<GlobalSettings>();
//            var ss = Properties.Settings.Default.ProjectGroupDir;
//            if (!string.IsNullOrEmpty(ss)) InitialDirectory = ss;
//            else if (s.GroupDir != string.Empty) InitialDirectory = s.GroupDir;
//            else
//            {
//                var se = Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
//                InitialDirectory = $"{se}\\Горизонт\\WorkProg\\ProjectsGroup\\";
//            }
//            Filter = $"{Title} (.{EXT})|*.{EXT}";

//            CustomPlaces = new object[]
//            { 
//                InitialDirectory,
//                $"{Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments)}\\Горизонт\\WorkProg\\Projects\\",
//                new Guid("FDD39AD0-238F-46AF-ADB4-6C85480369C7"),//документы
//                new Guid("1777F761-68AD-4D8A-87BD-30B759FA33DD"),//избрвнное
//            };

//            DefaultExt = EXT;

//            lastClosedFiles = ServiceProvider.GetRequiredService<LastGroupMenuVM>();
//            ChildlastClosedFiles = ServiceProvider.GetRequiredService<LastFileMenuVM>();
//            lastClosedVisits = ServiceProvider.GetRequiredService<LastVisitMenuVM>();
//        }
//    }
}
