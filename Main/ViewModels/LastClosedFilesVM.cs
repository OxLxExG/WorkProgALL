using Core;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Main.ViewModels
{
    using static Main.Models.MMenus;

    internal abstract class LastSettingsMenuVM : LastClosedFilesVM
    {
        public LastSettingsMenuVM(rootMenu m)
        {
            ContentID = m.ContentID;
            Header = m.Header;
            Priority = m.Priority;
            Task.Run(UpdateSubMenus);
        }        
    }
    internal abstract class LastRootMenuVM : LastSettingsMenuVM
    {
        public LastRootMenuVM(rootMenu m) : base(m) { }
        protected override void BeforeOpenClosedFileEvent(string file)
        {
            RootFileDocumentVM.Instance?.Remove();
            RootFileDocumentVM.Instance = null;
            DockManagerVM.Clear();
        }
        protected override void AfterOpenClosedFileEvent(string file)
        {
            RootFileDocumentVM.Instance = (RootFileDocumentVM?)RootFileDocumentVM.InstanceFactory!.LoadNew(file);
        }
        protected override void SaveClosedFiles() => Properties.Settings.Default.Save();
    }

    internal class LastGroupMenuVM : LastRootMenuVM
    {
        public LastGroupMenuVM() : base(LastVisitGroups) { }
        protected override StringCollection lastClosedFiles { get => Properties.Settings.Default.ClosedProjectGroup; }
    }
    internal class LastSingleVisitMenuVM : LastRootMenuVM
    {
        public LastSingleVisitMenuVM() : base(LastVisits) { }
        protected override StringCollection lastClosedFiles { get => Properties.Settings.Default.ClosedProjects; }
    }
    internal class LastVisitMenuVM : LastSettingsMenuVM
    {
        public LastVisitMenuVM() : base(LastVisits)
        {
            RootFileDocumentVM.StaticPropertyChanged += (o, e) => CheckEnable();
        }
        protected override void CheckEnable()
        {
            IsEnable = Items.Count > 0 && RootFileDocumentVM.Instance != null;
        }
        protected override StringCollection lastClosedFiles 
        {
            get 
            { if (RootFileDocumentVM.Instance is VisitsGroupVM pgi)
                {
                    return pgi.LastClosedVisits;
                }
                return null!;
            } 
        }
        protected override void AfterOpenClosedFileEvent(string file)
        {
            VisitsGroupVM.AddVisit(file);
        }
        protected override void BeforeOpenClosedFileEvent(string file) { }
        protected override void SaveClosedFiles()
        {
            RootFileDocumentVM.SetDrity();
        }
    }
    internal class LastFileMenuVM : LastSettingsMenuVM
    {
        public LastFileMenuVM() : base(LastFiles) { }
        protected override StringCollection lastClosedFiles { get => Properties.Settings.Default.ClosedFiles; }
        protected override void BeforeOpenClosedFileEvent(string file)
        {
            throw new NotImplementedException();
        }
        protected override void AfterOpenClosedFileEvent(string file)
        {
            throw new NotImplementedException();
        }
        protected override void SaveClosedFiles() => Properties.Settings.Default.Save();
    }

}
