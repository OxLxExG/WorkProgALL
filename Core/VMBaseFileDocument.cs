using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace Core
{
    public enum UserFileAction
    {
        LOAD_ON_PROG_OPEN,
        OPEN,
        SAVE_AS,
        NEW_ITEM,
        REMOVE,
        SAVE_ON_PROG_CLOSE,
    }
    //public class UserFileEventArg : EventArgs
    //{
    //    public string? File;
    //    public VMBaseFileDocument? target;
    //    public UserFileAction action;
    //}

    public class CancelDialogException : Exception { };
    /// <summary>
    /// VM для oдного файла Model для ProjectsExplorerVM
    /// 
    /// User UI with event: Menu, ContextMenu, ToolButton, OpenProgram, CloseProgram
    /// 
    /// Action:
    /// 
    ///  OpenProgram: 
    ///     LOAD (ProjectGroup or Project)
    ///     
    ///  Menu, ContextMenu, ToolButton:
    ///     OPEN_DIALOG_ITEM (openDialog addToParent or replaceRoot add-replace UI) (parentDrity)   
    ///     NEW_DIALOG_ITEM   (new addToParent or replaceRoot add-replace UI) (parentDrity)   
    ///     SAVEAS (close add to lastfiles with old name and save new file
    ///     REMOVE_DIALOG_ITEM (RemoveFromParent) (CloseFileSaveDialog?)(parentDrity)   
    ///     
    ///  CloseProgram: 
    ///     SAVE (CloseFileSaveDialog?)
    ///     
    ///   CloseRootFileSaveDialog Yes
    ///                           - DialogSave new-Drity files (Assign FileFullName) if cancel then no change
    ///                           -- remove new-noDrity files 
    ///                           --- Save Drity files
    /// </summary>
   
    public class VMBaseFileDocument: VMBase
    {
        // public static event EventHandler<UserFileEventArg>? UserFileEventEvent;
        public VMBaseFileDocument() 
        {
            PropertyChanged += (o,e) => IsVMDirty = true;
            DockManagerVM.FormsCleared += FormsClearedEvent;
            DockManagerVM.FormClosed += FormClosedEvent;
        }
        protected void FormsClearedEvent(object? sender, EventArgs e)
        {
            if (ChildFormIDs != null)
            {
                ChildFormIDs.Clear();
                ChildFormIDs = null;
                IsVMDirty = true;
            }
        }
        protected void FormClosedEvent(object? sender, EventArgs e)
        {
            if (sender is VMBaseForm f && ChildFormIDs != null && ChildFormIDs.Contains(f.ContentID))
                RemoveChildForm(f);
        }        
        //public bool IsRootDocument => parent == null;
        [XmlIgnore] public virtual LastClosedFilesVM? lastClosedFiles {  get; set; }

        #region Child Forms
        public StringCollection? ChildFormIDs { get; set; }
        public bool ShouldSerializeChildFormIDs() => ChildFormIDs != null && ChildFormIDs.Count > 0;
        public virtual void AddChildForm(VMBaseForm childForm)
        {
            if (ChildFormIDs == null) ChildFormIDs = new();
            ChildFormIDs.Add(childForm.ContentID);
            IsVMDirty = true;
        }
        public virtual void RemoveChildForm(VMBaseForm childForm)
        {
            if (ChildFormIDs != null)
            {
                ChildFormIDs.Remove(childForm.ContentID);
                if (ChildFormIDs.Count == 0) ChildFormIDs = null;
                IsVMDirty = true;
            }
        }
        #endregion

        private ComplexFileDocumentVM? parent {get; set;}
        public virtual void UpdateParent(ComplexFileDocumentVM? root) => parent = root;
        public bool UnloadedModel { get; set; }
        public bool ShouldSerializeUnloadedModel() => UnloadedModel == true;
        [XmlIgnore]public virtual bool FileNotFound { get; set; }
        /// <summary>
        /// То что умеет себя сохранять в файл Visit, WitsML
        /// </summary>
        [XmlIgnore] public object? Model { get; set; }
        public bool HasIcon => IconSource != string.Empty;
        public string IconSource { get; set; } = string.Empty;
        public bool ShouldSerializeIconSource() => IconSource != string.Empty;

        protected string DefaultTitle = string.Empty;
        /// <summary>
        /// DrityFileName or user title? 
        /// </summary>
        public string Title { get; set; } = string.Empty;
        public bool ShouldSerializeTitle() => Title != DefaultTitle;

        bool _IsActive;
        public bool IsActive { get=>_IsActive; set => SetProperty(ref _IsActive, value); }
        public bool ShouldSerializeIsActive() => IsActive;
        public bool CanActive { get; set; }
        public bool ShouldSerializeCanActive() => CanActive;
        public bool IsReadOnly { get; set; }
        public bool ShouldSerializeIsReadOnly() => IsReadOnly;
        
        bool _IsExpanded;
        public bool IsExpanded { get=>_IsExpanded; set => SetProperty(ref _IsExpanded, value); }
        public bool ShouldSerializeIsExpanded() => IsExpanded;
       
        bool _IsSelected;
        public bool IsSelected { get=> _IsSelected; set => SetProperty(ref _IsSelected, value); }
        public bool ShouldSerializeIsSelected() => IsSelected;


        private bool _IsDirty;
        /// <summary>
        /// model changed cport i t.d.
        /// </summary>
        [XmlIgnore]
        public bool IsDirty
        {
            get => _IsDirty;
            set
            {
                if (SetProperty(ref _IsDirty, value))
                {
                    if (value) IsVMDirty = value; 
                }
            }
        }
   
        private bool _IsVMDirty;
        /// <summary>
        /// view model or this class changed expand port settings i t.d.
        /// </summary>
        [XmlIgnore] public bool IsVMDirty { get => _IsVMDirty; set => SetProperty(ref _IsVMDirty, value); }
        [XmlIgnore] public bool IsNew { get; set; } = true;

        private string _FileFullName = string.Empty;
        [XmlIgnore] public string FileFullName { get => _FileFullName;
            set 
            {
               if (SetProperty(ref _FileFullName, value))
                {
                    IsDirty = false;
                    IsNew = false;
                }
            }
        }
        public string FilePath => Path.GetDirectoryName(FileFullName) ?? string.Empty;
        public string FileName => Path.GetFileNameWithoutExtension(FileFullName) ;
        public string DrityFileName => Path.GetFileName(FileFullName) + (IsDirty ? "*" : "");

        #region Prop File Dialog 
        /// <summary>
        /// last saved file directory ? 
        /// </summary>
        //[XmlIgnore] public string InitialDirectory { get; set; } = string.Empty; 
        //[XmlIgnore] public string Filter { get; set; } = string.Empty;
        //[XmlIgnore] public bool ValidateNames { get; set; } = true;
        //[XmlIgnore] public IList<object>? CustomPlaces { get; set; }
        //[XmlIgnore] public bool CheckPathExists { get; set; } = true;
        //[XmlIgnore] public bool CheckFileExists { get; set; }
        //[XmlIgnore] public bool AddExtension { get; set; }= true;
        //[XmlIgnore] public string DefaultExt { get; set; } = string.Empty;

        //[XmlIgnore] public bool ReadOnlyChecked { get; set; }= true;
        //[XmlIgnore] public bool ShowReadOnly { get; set; }

        //[XmlIgnore] public bool CreatePrompt { get; set; }
        //[XmlIgnore] public bool OverwritePrompt { get; set; } = true;
        #endregion

        protected virtual void LoadModel() { }
        protected virtual void SaveModelAndViewModel() 
        {
            NeedAnySave = false;
            IsDirty = false;
            IsVMDirty = false;
        }
        //private void AssignDialog(IFileDialog f)
        //{
        //    f.InitialDirectory = InitialDirectory;
        //    f.Filter = Filter;
        //    f.DefaultExt = DefaultExt;
        //    f.ValidateNames = ValidateNames;
        //    if (CustomPlaces != null) f.CustomPlaces = CustomPlaces;
        //    f.CheckPathExists = CheckPathExists;
        //    f.CheckFileExists = CheckFileExists;
        //    f.AddExtension = AddExtension;
        //    f.FileName = FileFullName;

        //}
        //public bool OpenDialog(Action<string> SetName)
        //{
        //    var f = FOpen();
        //    AssignDialog(f);
        //    f.Title = $"Open {Title} dialog";
        //    f.ReadOnlyChecked = ReadOnlyChecked;
        //    f.ShowReadOnly = ShowReadOnly;
        //    if (f.ShowDialog())
        //    {
        //        SetName(f.FileName);
        //        return true;
        //    }
        //    return false;
        //}
        //protected bool SaveDialog(Action<string> SetName)
        //{
        //    var f = FSave();
        //    AssignDialog(f);
        //    f.Title = $"Save {Title} dialog";
        //    var p = Path.GetFullPath(FilePath);
        //    if (CustomPlaces?.FirstOrDefault(o => o is string s && s.IsSameFiles(p)) == default)
        //    {
        //        CustomPlaces?.Add(FilePath);
        //    }            
        //    if (CustomPlaces != null) f.CustomPlaces = CustomPlaces;
        //    f.CreatePrompt = CreatePrompt;
        //    f.OverwritePrompt = OverwritePrompt;
        //    f.FileName = FileName;
        //    if (f.ShowDialog())
        //    {
        //        SetName(f.FileName);
        //        return true;
        //    } return false;
        //}
        protected virtual IFileSaveDialog iSaveDialog => FSave(); 
        public virtual void Save()
        {
            if (IsNew && !IsDirty) return;
            
            if (IsNew && IsDirty) 
            {
                if (iSaveDialog.ShowDialog(newName=> FileFullName = newName))
                {
                    IsNew = false;
                }
                else throw new CancelDialogException();
            }
           SaveModelAndViewModel();
        }
        protected bool NeedAnySave;
        protected void SaveWithDialog()
        {
            if (this.DrityOrHasChildDrity())
            {
                var sf = ServiceProvider.GetRequiredService<ISaveFilesDialog>();

                switch (sf.Show(new[] { this.GenerateDrityTree()! }))
                {
                    case BoxResult.Yes:
                        Save();
                        break;

                    case BoxResult.Cancel:
                        throw new CancelDialogException();
                }
            }
            else if (this.DrityOrHasChildVMDrity() || NeedAnySave)
            {  
                Save(); 
            }
        }
        public virtual void ClearForms()
        {
            if (ChildFormIDs != null)
            {
                foreach (var _id in ChildFormIDs)
                    if (!string.IsNullOrEmpty(_id))
                    {
                        DockManagerVM.Remove(_id);
                        IsVMDirty = true;
                    }
            }
        }
        public virtual void Remove(bool UserCloseFile = true)
        {
            SaveWithDialog();
            if (parent != null) 
            {
                parent.RemoveChild(this);
               // RootFileDocumentVM.SetDrity();
            }
            DockManagerVM.FormsCleared -= FormsClearedEvent;
            DockManagerVM.FormClosed -= FormClosedEvent;
            if (!IsNew && UserCloseFile) lastClosedFiles?.UserCloseFile(FileFullName);
            if (UserCloseFile) ClearForms();
        }
        public virtual void SaveAs()
        {
            SaveWithDialog();
            iSaveDialog.ShowDialog(f =>
            {
                if (f != FileFullName)
                {
                    lastClosedFiles?.UserCloseFile(FileFullName);
                    FileFullName = f;
                    // TODO: create DIR
                    SaveModelAndViewModel();
                    if (parent != null) parent.IsDirty = true;
                   // RootFileDocumentVM.SetDrity();
                }
            }); 
        }
    }
    public abstract class ComplexFileDocumentVM : VMBaseFileDocument
    {
        #region prop lastClosedFiles сам файл
        //protected LastClosedFilesVM? _lastClosedFiles = null;
        ///// <summary>
        ///// сам файл
        ///// </summary>
        //protected override LastClosedFilesVM? lastClosedFiles
        //{
        //    get => _lastClosedFiles;
        //    set
        //    {
        //        if (_lastClosedFiles != value)
        //        {
        //            if (_lastClosedFiles != null)
        //            {
        //                _lastClosedFiles.BeforeOpenClosedFileEvent -= BeforeOpenClosedFile;
        //                _lastClosedFiles.AfterOpenClosedFileEvent -= AfterOpenClosedFile;
        //            }
        //            if (value != null)
        //            {
        //                value.BeforeOpenClosedFileEvent += BeforeOpenClosedFile;
        //                value.AfterOpenClosedFileEvent += AfterOpenClosedFile;
        //            }
        //            _lastClosedFiles = value;
        //        }
        //    }
        //}
        //protected virtual void BeforeOpenClosedFile(object? o, OpenClosedFileEventArg e) 
        //{
        //    if (e.file == FileFullName) throw new OpenClosedFileException();
        //}
        //protected virtual void AfterOpenClosedFile(object? o, OpenClosedFileEventArg e) 
        //{
        //    lastClosedFiles = null;
        //}
        #endregion

        #region prop ChildDocuments
        public ObservableCollection<VMBaseFileDocument>? ChildDocuments { get; set; }
        public bool ShouldSerializeChildDocuments() => ChildDocuments != null && ChildDocuments.Count > 0;

        public virtual void AddChild(VMBaseFileDocument d)
        {
            if (ChildDocuments == null) ChildDocuments = new ObservableCollection<VMBaseFileDocument>();
            ChildDocuments.Add(d);
            IsDirty = true;
            //RootFileDocumentVM.SetDrity();
            d.UpdateParent(this);
            lastClosedFiles?.UserOpenFile(d.FileFullName);
        }
        public virtual void RemoveChild(VMBaseFileDocument d)
        {
            if (ChildDocuments != null)
            {
                ChildDocuments.Remove(d);
                IsDirty = true;
               // RootFileDocumentVM.SetDrity();
                if (ChildDocuments.Count == 0) ChildDocuments = null;
            }
        }
        #endregion

        #region prop ChildlastClosedFiles дочерние файлы
        //protected LastClosedFilesVM? _ChildlastClosedFiles = null;
        /// <summary>
        /// дочерние файлы
        /// </summary>
        protected LastClosedFilesVM? ChildlastClosedFiles { get; set; }
        //{
        //    get => _ChildlastClosedFiles;
        //    set
        //    {
        //        if (_ChildlastClosedFiles != value)
        //        {
        //            //if (_ChildlastClosedFiles != null)
        //            //{
        //            //    _ChildlastClosedFiles.BeforeOpenClosedFileEvent -= ChildBeforeOpenClosedFile;
        //            //    _ChildlastClosedFiles.AfterOpenClosedFileEvent -= ChildAfterOpenClosedFile;
        //            //}
        //            //if (value != null)
        //            //{
        //            //    value.BeforeOpenClosedFileEvent += ChildBeforeOpenClosedFile;
        //            //    value.AfterOpenClosedFileEvent += ChildAfterOpenClosedFile;
        //            //}
        //            _ChildlastClosedFiles = value;
        //        }
        //    }
        //}
        //protected virtual void ChildBeforeOpenClosedFile(object? o, OpenClosedFileEventArg e)
        //{
        //    if (_ChildDocuments != null && _ChildDocuments.FirstOrDefault(d => d.FileFullName == e.file) != null) throw new CancelDialogException();
        //}
        //protected virtual void ChildAfterOpenClosedFile(object? o, OpenClosedFileEventArg e) { }
        #endregion

        public override void Save()
        {
            if (ChildDocuments != null) foreach (var dc in ChildDocuments) dc.Save();
            base.Save();
        }
        public override void UpdateParent(ComplexFileDocumentVM? root)
        {
            base.UpdateParent(root);
            if (ChildDocuments != null)
                foreach (var d in ChildDocuments) d.UpdateParent(this);
        }
        public override void ClearForms()
        {
            if (ChildDocuments != null)
                foreach (var d in ChildDocuments) d.ClearForms();
            base.ClearForms();
        }

        public override void Remove(bool UserCloseFile = true)
        {
            if (ChildDocuments != null && UserCloseFile)
                foreach (var d in ChildDocuments) d.ClearForms();
            base.Remove(UserCloseFile);
        }
    }

    public class DockManagerSerialize : IXmlSerializable
    {
        public XmlSchema? GetSchema() => null;
        public void ReadXml(XmlReader reader)
        {
            var s = VMBase.ServiceProvider.GetRequiredService<IDockManagerSerialization>();
            if (reader.LocalName == nameof(DockManagerSerialize) &&
                reader.Read() &&
                reader.NodeType == XmlNodeType.Element)
            {
                s.Deserialize(reader);
                reader.Read();// end element
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            var s = VMBase.ServiceProvider.GetRequiredService<IDockManagerSerialization>();
            s.Serialize(writer);
        }
    }

    //public abstract class DocumentFactory//<T> where T : RootFileDocumentVM, new()
    //{
    //    public abstract VMBaseFileDocument? LoadNew(string file);
    //    public abstract string LoadDialog();
    //    public abstract VMBaseFileDocument CreateNew();
    //}
    public static class RootFileDocumentVM
    {
        public static event PropertyChangedEventHandler? StaticPropertyChanged;

        #region prop InstanceFactory
        //private static DocumentFactory? _instanceFactory;
        //public static DocumentFactory? InstanceFactory 
        //{
        //    get=>_instanceFactory; 
        //    set 
        //    {
        //        if(_instanceFactory != value) 
        //        {
        //            _instanceFactory = value;
        //            StaticPropertyChanged?.Invoke(value,new PropertyChangedEventArgs(nameof(InstanceFactory)));
        //        }
        //    } 
        //}
        #endregion

        #region prop Instance
        private static void InstanceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            StaticPropertyChanged?.Invoke(sender, new PropertyChangedEventArgs(e.PropertyName));
        }
        private static ComplexFileDocumentVM? _instance;
        public static ComplexFileDocumentVM? Instance 
        { 
            get=>_instance; 
            set 
            {
                if (_instance != value)
                {
                    if (_instance != null) _instance.PropertyChanged -= InstanceOnPropertyChanged;  
                    _instance = value;
                    if (_instance != null) _instance.PropertyChanged += InstanceOnPropertyChanged;
                    StaticPropertyChanged?.Invoke(value, new PropertyChangedEventArgs(nameof(Instance)));
                }
            }
        }
        #endregion
        //public RootFileDocumentVM(): base() 
        //{            
        //    DockManagerVM.FormAdded += FormAddedEvent;
        //    DockManagerVM.ActiveDocumentChanging += ActiveDocumentChangingEvent;
        //    DockManagerVM.FormVisibleChanged += FormVisibleChanged;
        //}
        //void FormVisibleChanged(VMBaseForm? vMBaseForm) => IsVMDirty = vMBaseForm != null;
        //void FormAddedEvent(object? sender, FormAddedEventArg e) => IsVMDirty = e.formAddedFrom == FormAddedFrom.User;
        //void ActiveDocumentChangingEvent(object? sender, ActiveDocumentChangedEventArgs e) => IsVMDirty = e.OldActive != null;
        //public override void Remove(bool UserCloseFile = true)
        //{
        //    DockManagerVM.FormVisibleChanged -= FormVisibleChanged;
        //    DockManagerVM.FormAdded -= FormAddedEvent;
        //    DockManagerVM.ActiveDocumentChanging -= ActiveDocumentChangingEvent;
        //    base.Remove(UserCloseFile);
        //}
        //protected override void FormsClearedEvent(object? sender, EventArgs e)
        //{
        //    base.FormsClearedEvent(sender, e);
        //    IsVMDirty = true;
        //}
        //protected override void FormClosedEvent(object? sender, EventArgs e)
        //{
        //    base.FormClosedEvent(sender, e);
        //    IsVMDirty = true;
        //}
        public static void SetDrity()
        {
            if (Instance != null)
            {
                Instance.IsDirty = true;
            }
        }
        public static void SetVMDrity()
        {
            if (Instance != null)
            {
                Instance.IsVMDirty = true;
            }
        }
        //public DockManagerSerialize DockManagerSerialize { get; set; } = new DockManagerSerialize();
    }

}
