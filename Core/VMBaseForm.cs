using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace Core
{
    public interface IDockManagerSerialization
    {
        void Serialize(System.Xml.XmlWriter writer);
        void Deserialize(System.Xml.XmlReader reader);
        void Serialize(string File);
        void Deserialize(string File);
    }
    public interface IFormsRegistrator
    {
        void Register();
    }
    public class FormsRegistrator<T> : IFormsRegistrator where T : VMBaseForm
    {
        void IFormsRegistrator.Register() => DockManagerVM.Register<T>(typeof(T).Name, VMBase.ServiceProvider.GetRequiredService<T>);        
    }
    public static class ServicesFormsRegistratorExt
    {
        public static IServiceCollection RegisterForm<T>(this IServiceCollection s) where T : VMBaseForm
        {
            s.AddTransient<T>();
            s.AddTransient<IFormsRegistrator, FormsRegistrator<T>>();
            return s;
        }
    }

    /// <summary>
    /// <para>
    /// ОСНОВНАЯ ИДЕЯ ПРОГРАММЫ:
    /// </para>
    /// Каждое окно создает при активации свои кнопки управления и меню !
    /// <para>
    /// событие OnMenuActivate: создать tools, menus ,вручную добавить в DynamicItems,
    /// IToolServer, IMenuItemServer
    /// </para>
    /// <para>
    /// событие OnMenuDeActivate: обнулить внутренние ссылки, если есть, на tools, menus.
    /// Очищать DynamicItems и удалять tools, menus из IToolServer,IMenuItemServer не надо, 
    /// будет удалено автоматически
    /// </para>
    /// </summary>
    public abstract class VMBaseForm : VMBase
    {               
        //public delegate void OnCloseHandler(object sender);
        //public static event OnCloseHandler? OnClose;

        #region Dynamic Tools And Menus 
        protected List<PriorityItemBase> DynamicItems = new List<PriorityItemBase>();
        private DispatcherTimer _Activatetimer;
        private DispatcherTimer _DeActivatetimer;
        private void ActivateMenu()
        {
            _DeActivatetimer.Stop();
            _Activatetimer.Start();
        }
        private void DeActivateMenu()
        {
            _Activatetimer.Stop();
            _DeActivatetimer.Start();
        }

        public VMBaseForm()
        {
            _Activatetimer = new();
            _Activatetimer.Interval = new(TimeSpan.TicksPerMillisecond * 500);
            _Activatetimer.Tick += (s, e) => 
            { 
                _Activatetimer.Stop(); 
                if (DynamicItems.Count ==0) OnMenuActivate?.Invoke();
            };

            _DeActivatetimer = new();
            _DeActivatetimer.Interval = new(TimeSpan.TicksPerMillisecond * 100);
            _DeActivatetimer.Tick += (s, e) => 
            { 
                _DeActivatetimer.Stop();
                if (DynamicItems.Count > 0) OnMenuDeActivate?.Invoke();
                if (DynamicItems.Count > 0)
                {
                    ToolBarServer.Remove(DynamicItems.OfType<ToolButton>());
                    MenuItemServer.Remove(DynamicItems.OfType<MenuItemVM>());
                    DynamicItems.Clear();
                }
            };

            OnActivate += ActivateMenu;
            OnDeActivate += DeActivateMenu;
        }
        #endregion

        #region Close
        public virtual void Close()
        {
            DockManagerVM.Remove(this);
        }
        RelayCommand _closeCommand = null!;
        public ICommand CloseCommand
        {
            get
            {
                if (_closeCommand == null)
                {
                    _closeCommand = new RelayCommand(Close, ()=>CanClose);
                }

                return _closeCommand;
            }
        }
        #endregion

        #region Focus
        public delegate void OnFocusHandler(object sender);
        public static event OnFocusHandler? OnFocus;
        public void Focus()
        {
            OnFocus?.Invoke(this);
        }
        #endregion

        #region Title
        private string _title = string.Empty;
        public string Title
        {
            get => _title; set => SetProperty(ref _title, value);
            //{
            //    SetProperty(ref _title, value); 
            //    //if (value != _title)
            //    //{
            //    //    _title = value;
            //    //    OnPropertyChanged(nameof(Title));
            //    //}
            //}
        }
        #endregion

        #region IsVisible
        public static void OnVisibleChange(VMBaseForm? sender) => DockManagerVM.OnVisibleChange(sender);
        
        public bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (value != _isVisible)
                {
                    _isVisible = value;

                    if (!_isVisible) DeActivateMenu();
                    else if (_isActive) ActivateMenu();

                    OnPropertyChanged(nameof(IsVisible));
                    OnVisibleChange(this);
                }
            }
        }
        #endregion

        #region Icon
        [XmlIgnore] public Uri? IconSource
        {
            get;
            set;
        } = null;
        public bool IconSourceEnable => IconSource != null;
        #endregion

        #region IsSelected

        protected delegate void SelectHandler();
        protected event SelectHandler? OnSelect;
        protected event SelectHandler? OnDeSelect;
        private bool _isSelected = false;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    if (_isSelected && !value)
                        OnDeSelect?.Invoke();
                    else
                        OnSelect?.Invoke();

                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        #endregion

        #region IsActive

        protected delegate void ActivateHandler();
        private event ActivateHandler? OnActivate;
        private event ActivateHandler? OnDeActivate;
        protected event ActivateHandler? OnMenuActivate;
        protected event ActivateHandler? OnMenuDeActivate;
        private bool _isActive = false;
        public bool IsActive
        {
            get { return _isActive; }
            set
            {
                if (_isActive != value)
                {
                    if (_isActive && !value) 
                        OnDeActivate?.Invoke();
                    else 
                        OnActivate?.Invoke();

                    _isActive = value;
                    OnPropertyChanged(nameof(IsActive));
                }
            }
        }

        #endregion

        #region FloatingWidth
        public double FloatingWidth { get; set; } = 0.0;

        #endregion

        #region FloatingHeight

        public double FloatingHeight { get; set; } = 0.0;

        #endregion

        #region FloatingLeft

        public double FloatingLeft { get; set; } = 0.0;

        #endregion

        #region FloatingTop

        public double FloatingTop { get; set; } = 0.0;
        #endregion

        //#region IsFloating

        //private bool _isFloating = false;

        //public bool IsFloating
        //{
        //    get
        //    {
        //        return _isFloating;
        //    }
        //    internal set
        //    {
        //        if (_isFloating != value)
        //        {
        //            _isFloating = value;
        //            OnPropertyChanged("IsFloating");
        //        }
        //    }
        //}

        //#endregion

        #region CanClose
        bool _canClose = true;
        public bool CanClose
        { 
            get=> _canClose;
            set => SetProperty(ref _canClose, value); 
            //{
            //    if (_canClose != value)
            //    {
            //        _canClose = value;
            //        OnPropertyChanged(nameof(CanClose));
            //    }
            //}
        }

        #endregion

        #region CanFloat
        public bool CanFloat { get; set; } = true;
        #endregion

        #region ToolTip
        public bool ToolTipEnable=> ToolTip != null;
        public string? ToolTip { get; set; } = null;
        #endregion

    }
    public class DocumentVM : VMBaseForm
    {
        #region Description
        public string? Description { get; set; } = null;

        public bool DescriptionEnable => Description != null;
        #endregion

        #region CanMove
        public bool CanMove { get; set; } = true;
        #endregion

    }
    [Flags]
    public enum ShowStrategy : byte
    {
        Most = 0x0001,
        Left = 0x0002,
        Right = 0x0004,
        Top = 0x0010,
        Bottom = 0x0020,
    }
    public class ToolVM: VMBaseForm
    {
        public ToolVM(): base() 
        {
            CanClose = false;
        }
        #region AutoHideWidth
        public double AutoHideWidth { get; set; } = 200;

        #endregion

        #region AutoHideMinWidth

        public double AutoHideMinWidth { get; set; } = 20;

        #endregion

        #region AutoHideHeight

        public double AutoHideHeight { get; set; } = 200;
        #endregion

        #region AutoHideMinHeight

        public double AutoHideMinHeight { get; set; } = 20;

        #endregion

        #region CanHide

        public bool CanHide { get; set; } = true;

        #endregion

        #region CanAutoHide

        public bool CanAutoHide { get; set; }=true;

        #endregion

        #region CanDockAsTabbedDocument

        public bool CanDockAsTabbedDocument { get; set; } = true;
        #endregion

        #region ShowStrategy
        public ShowStrategy? ShowStrategy { get; set; } = null;
        #endregion
    }

    public class ActiveDocumentChangedEventArgs: EventArgs
    {
        public VMBaseForm OldActive = null!;
        public VMBaseForm NewActive = null!;
    }
    public enum FormAddedFrom
    {
        DeSerialize,
        User
    }
    public class FormAddedEventArg : EventArgs
    {
        public FormAddedFrom formAddedFrom;
        public FormAddedEventArg(FormAddedFrom formAddedFrom) { this.formAddedFrom = formAddedFrom; }
    }
    public class DockManagerVM : VMBase//, IFormsServer
    {
        #region Instance
        private static DockManagerVM? _instance;
        public static DockManagerVM Instance 
        { 
            get 
            { 
                if (_instance == null) _instance = new DockManagerVM();
                return _instance;
            } 
        }
        #endregion
        public DockManagerVM() { _instance = this; }

        #region Docs and Tools properties
        ObservableCollection<DocumentVM> _docs = new ObservableCollection<DocumentVM>();
        ReadOnlyObservableCollection<DocumentVM> _readonyDocs = null!;
        public static ReadOnlyObservableCollection<DocumentVM> Docs
        {
            get
            {
                if (Instance._readonyDocs == null)
                    Instance._readonyDocs = new ReadOnlyObservableCollection<DocumentVM>(Instance._docs);

                return Instance._readonyDocs;
            }
        }
        ObservableCollection<ToolVM> _tools = new ObservableCollection<ToolVM>();
        ReadOnlyObservableCollection<ToolVM> _readonyTools = null!;
        public static ReadOnlyObservableCollection<ToolVM> Tools
        {
            get
            {
                if (Instance._readonyTools == null)
                    Instance._readonyTools = new ReadOnlyObservableCollection<ToolVM>(Instance._tools);

                return Instance._readonyTools;
            }
        }
        #endregion

        public static event EventHandler? FormsCleared;
        public static event EventHandler<FormAddedEventArg>? FormAdded;
        public static event EventHandler? FormClosed;
        public delegate void OnVisibleChangedHandler(VMBaseForm? sender);
        public static event OnVisibleChangedHandler? FormVisibleChanged;

        public static void OnVisibleChange(VMBaseForm? sender)
        {
            FormVisibleChanged?.Invoke(sender);
        }
        public static void Clear()
        {
            Instance._tools.Clear();
            Instance._docs.Clear();
            Instance._activeDocument = null!;
            FormsCleared?.Invoke(Instance, EventArgs.Empty);
        }
        public static VMBaseForm? Contains(string id)
        {
            return (VMBaseForm?)
                    Instance._tools.FirstOrDefault(t => t.ContentID == id)
                    ??
                    Instance._docs.FirstOrDefault(t => t.ContentID == id);
        }
        public static VMBaseForm Add(VMBaseForm vmbase, FormAddedFrom formAddedFrom )
        {
            if (vmbase is ToolVM t) Instance._tools.Add(t);
            else if (vmbase is DocumentVM d) Instance._docs.Add(d);
            FormAdded?.Invoke(vmbase, new FormAddedEventArg(formAddedFrom));
            return vmbase;
        }
        public static VMBaseForm AddOrGet(string ContentID, FormAddedFrom formAddedFrom)
        {
            var c = Contains(ContentID);
            if (c != null)
            {
                //                if (c.IsClosed) c.ResetState();
                return c;
            }
            // создаем новую форму 
            string RootContentID = ContentID.Split('.', StringSplitOptions.RemoveEmptyEntries)[0];
            var FormVMGenerator = _RegForm.GetValueOrDefault(RootContentID);
            if (FormVMGenerator == null)
                throw new ArgumentOutOfRangeException(nameof(ContentID), "FormVMGenerator == null bad ID", ContentID);
            var form = FormVMGenerator();
            form.ContentID = ContentID;
            //
            Add(form, formAddedFrom);
            return form;
        }
        public static VMBaseForm AddOrGetandShow(string ContentID, FormAddedFrom formAddedFrom)
        {
            var r = AddOrGet(ContentID, formAddedFrom);
            r.IsVisible = true;
            r.Focus();
            r.IsActive = true;
            r.IsSelected = true;
            return r;
        }
        public static void Remove(VMBaseForm RemForm)
        {
            if (RemForm is ToolVM t && Tools.Contains(t)) Instance._tools.Remove(t);
            else if (RemForm is DocumentVM d && Docs.Contains(d)) Instance._docs.Remove(d);
            FormClosed?.Invoke(RemForm, EventArgs.Empty);
        }
        public static void Remove(string ContentID)
        {
            var f = Contains(ContentID);
            if (f != null) Remove(f);
        }
        static Dictionary<string, Func<VMBaseForm>> _RegForm = new();
        //    /// <summary>
        //    /// регистрируем генератор модели представления
        //    /// </summary>
        //    /// <param name="RootContentID"> RootContentID.AnyData.AnyData...) </param>
        //    /// <param name="RegFunc">генератор модели представления</param>
        public static void Register<T>(string RootContentID, Func<VMBaseForm> RegFunc) where T : VMBaseForm
        {
            _RegForm.TryAdd(RootContentID, RegFunc);
        }

        public static IEnumerable<ToolVM> Hiddens => Tools.Where(t => !t.IsVisible/* && !t.IsClosed*/);

        #region ActiveDocument
        private VMBaseForm _activeDocument = null!;
        public static VMBaseForm ActiveDocument
        {
            get { return Instance._activeDocument; }
            set
            {
                if (Instance._activeDocument != value)
                {
                    if (Instance._activeDocument != null)
                    {
                        ActiveDocumentChanging?.Invoke(Instance, new ActiveDocumentChangedEventArgs
                        {
                            OldActive = Instance._activeDocument,
                            NewActive = value
                        });
                    }

                    var l = ServiceProvider.GetRequiredService<ILogger<VMBaseForm>>();
                    var s = Instance._activeDocument != null ? Instance._activeDocument.ContentID : "NUL";
                    l.LogInformation(" ActiveDocument {0} => {1} ", s, value.ContentID);

                    Instance._activeDocument = value;
                    Instance.OnPropertyChanged(nameof(ActiveDocument));
                }
            }
        }
        public static event EventHandler<ActiveDocumentChangedEventArgs>? ActiveDocumentChanging;
        #endregion

    }

}
