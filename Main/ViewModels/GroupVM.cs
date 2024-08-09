using Core;
using Main.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Main.ViewModels
{
    //internal class GroupVM: VMBase
    //{
    //    ObservableCollection<VisitDocument> VisitDocs { get; set; } = new ObservableCollection<VisitDocument>();
    //    public bool ShouldSerializeVisitDocs() => VisitDocs.Count > 0;
    //}

    public class GroupDocument : ComplexFileDocumentVM
    {
       const string EXT = "vstgrp";

        /// <summary>
        /// Model .vstgrp
        /// грузятся из файла
        /// 
        /// LocalPathOpenAverFiles - модели
        /// VM - ChildDocuments потомки VMBaseFileDocument TODO:factory VM M
        /// 
        /// </summary>
        [XmlIgnore] public new GroupFile? Model { get => (GroupFile?)base.Model; set => base.Model = value; }

        /// <summary>
        /// ViewModel .vstgrpvm
        /// грузятся из файлов
        /// визитов (заездов)
        /// </summary>
        [XmlIgnore] public ObservableCollection<VisitDocument> VisitDocs { get; set; } = new ObservableCollection<VisitDocument>();

        public static XmlSerializer Serializer => new XmlSerializer(typeof(GroupDocument));

        public GroupDocument()         
        {
            lastClosedFiles = ServiceProvider.GetRequiredService<LastGroupMenuVM>();
            ChildlastClosedFiles = ServiceProvider.GetRequiredService<LastFileMenuVM>();

            Title = Properties.Resources.tProjectGroup;
            DefaultTitle = Title;

            //InitialDirectory = ProjectFile.WorkDirs.Count > 0 ? ProjectFile.WorkDirs[0]! : ProjectFile.RootDir;
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

            DockManagerVM.FormAdded += FormAddedEvent;
            DockManagerVM.ActiveDocumentChanging += ActiveDocumentChangingEvent;
            DockManagerVM.FormVisibleChanged += FormVisibleChanged;
        }
        public static GroupDocument CreateAndSave(string file)
        {
            var v = new GroupDocument();
            v.Model = new();
            v.FileFullName = file;
            v.IsDirty = true;
            v.lastClosedFiles?.UserOpenFile(file);
            v.Save();
            return v;
        }

        public static GroupDocument Load(string file)
        {
            GroupFile model;
            // load M
            using (var fs = new StreamReader(file, false))
            {
                model = (GroupFile)GroupFile.Serializer.Deserialize(fs)!;
            }
            // load VM
            GroupDocument d;
            var vvm = Models.ProjectFile.GetTmpFile(file, ".grpvm");
            if (File.Exists(vvm))
            {
                using (var fs = new StreamReader(vvm, false))
                {
                    d = (GroupDocument)Serializer.Deserialize(fs)!;
                }
            }
            else
                d = new GroupDocument();
            ///
            /// Загружаем визиты(заезды)
            ///
            var wd = Path.GetDirectoryName(file);
            foreach (var lp in model.LocalPathVisit)
            {
                string fullPath = lp.FullPath(wd!);// Path.GetFullPath(wd +"\\"+ lp);
                if (File.Exists(fullPath))
                {
                    d.VisitDocs.Add(VisitDocument.Load(fullPath, false));
                }
                else d.VisitDocs.Add(new VisitDocument() { FileNotFound = true, FileFullName = fullPath, });
            }
            d.Model = model;
            d.FileFullName = file;
            d.lastClosedFiles?.UserOpenFile(file);
            return d;
        }
        protected override void SaveWithDialog()
        {
            IList<SaveFileItem>? files = null;// new List<SaveFileItem>();

            bool saveVM = NeedAnySave || IsVMDirty;

            if (IsDirty)
            {
                if (files == null) files = new List<SaveFileItem>();
                files.Add(new SaveFileItem { DrityFileName = DrityFileName});
            }

            foreach (var d in VisitDocs)
            {
                if (d.DrityOrHasChildDrity())
                {
                    if (files == null) files = new List<SaveFileItem>();
                    files.Add(d.GenerateDrityTree()!);
                    saveVM = saveVM || d.DrityOrHasChildVMDrity() || d.NeedAnySave;
                }
            }

            if (files != null)   
            {
                var sf = ServiceProvider.GetRequiredService<ISaveFilesDialog>();

                switch (sf.Show(files))
                {
                    case BoxResult.Yes:
                        Save();
                        break;

                    case BoxResult.Cancel:
                        throw new CancelDialogException();
                }
            }
            else if (saveVM)
            {
                Save();
            }
        }
        protected override void SaveModelAndViewModel()
        {
            var l = ServiceProvider.GetRequiredService<ILogger<VisitDocument>>();
            l.LogTrace("Save VM={} M={} '{}'", IsVMDirty, IsDirty, FileFullName);

            // save child
            foreach (var d in VisitDocs) { d.Save(); }
            // save VM
            if (IsVMDirty)
            {
                var vvm = Models.ProjectFile.GetTmpFile(FileFullName, ".grpvm");
                using (var fs = new StreamWriter(vvm, false))
                {
                    Serializer.Serialize(fs, this);
                }
            }
            // save M
            if (IsDirty)
            {               
                using (var fs = new StreamWriter(FileFullName, false))
                {
                    GroupFile.Serializer.Serialize(fs, Model);
                }
            }
            base.SaveModelAndViewModel();
        }
        private int ContainsVisit(string visitfilefull)
        {
            for(int i=0; i<VisitDocs.Count; i++)
            {
                VisitDocument d = VisitDocs[i];
                if (d.FileFullName.IsSameFiles(visitfilefull)) return i;
            }
            return -1;
        }
        public void RemoveVisit(VisitDocument v)
        {
            int i =VisitDocs.IndexOf(v);
            if (i > -1) RemoveVisit(i);
        }

        public void RemoveVisit(int i)
        {
            // remove VM 
            VisitDocs.RemoveAt(i);
            // remove M
            Model?.LocalPathVisit.RemoveAt(i);
            IsDirty = true;
        }
        public void AddVisit(VisitDocument visit)
        {
            string VisitRelpath = visit.FileFullName.Relative(FilePath);
            int i = ContainsVisit(visit.FileFullName);
            if ( i>=0 )
            {
                RemoveVisit(i);
            }          
            // add Model
            Model?.LocalPathVisit.Add(VisitRelpath);
            // add VM
            VisitDocs.Add(visit);
            IsDirty = true;            
        }

        public DockManagerSerialize DockManagerSerialize { get; set; } = new DockManagerSerialize();
        public override void Remove(bool UserCloseFile = true)
        {
           DockManagerVM.FormVisibleChanged -= FormVisibleChanged;
           DockManagerVM.FormAdded -= FormAddedEvent;
           DockManagerVM.ActiveDocumentChanging -= ActiveDocumentChangingEvent;
           base.Remove(UserCloseFile);
           foreach (var v in VisitDocs) v.Remove(false); // user remove group ! bat visit
        }
        void FormVisibleChanged(VMBaseForm? vMBaseForm) => IsVMDirty = vMBaseForm != null;
        void FormAddedEvent(object? sender, FormAddedEventArg e) => NeedAnySave = e.formAddedFrom == FormAddedFrom.User;
        void ActiveDocumentChangingEvent(object? sender, ActiveDocumentChangedEventArgs e) => IsVMDirty = e.OldActive != null;
    }

}
