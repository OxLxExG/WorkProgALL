using CommunityToolkit.Mvvm.Input;
using Global;
using Serilog;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml.Serialization;
using Microsoft.Extensions.Configuration;

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
    public class FormsRegistrator : IFormsRegistrator
    {
        FormsRegistrator(Type type)
        {
            this.type = type;
        }

        Type type { get; }

        void IFormsRegistrator.Register() => DockManagerVM.Register(type, type.Name, ()=> (VMBaseForm) VMBase.ServiceProvider.GetRequiredService(type));
        public static void RegFormAction(IConfiguration context, IServiceCollection services, Type type, RegServiceAttribute attr)
        {
            services.AddTransient(type);
            services.AddTransient(typeof(IFormsRegistrator), s => new FormsRegistrator(type));
        }
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
    public class DynamicItemsAdapter: IDisposable
    {
        public delegate void ActivateHandler();
        public event ActivateHandler? OnActivateDynItems;
        public event ActivateHandler? OnDeActivateDynItems;

        public List<PriorityItemBase> DynamicItems = new List<PriorityItemBase>();
        private DispatcherTimer _Activatetimer;
        private DispatcherTimer _DeActivatetimer;
        private bool disposedValue;

        public void ActivateMenu()
        {
            _DeActivatetimer.Stop();
            _Activatetimer.Start();
        }
        public void DeActivateMenu()
        {
            _Activatetimer.Stop();
            _DeActivatetimer.Start();
        }

        public DynamicItemsAdapter()
        {
            _Activatetimer = new();
            _Activatetimer.Interval = new(TimeSpan.TicksPerMillisecond * 500);
            _Activatetimer.Tick += (s, e) => UserActivate();

            _DeActivatetimer = new();
            _DeActivatetimer.Interval = new(TimeSpan.TicksPerMillisecond * 100);
            _DeActivatetimer.Tick += (s, e) => UserDeactivate();

            //OnActivate += ActivateMenu;
            //OnDeActivate += DeActivateMenu;
        }
        public void UserActivate()
        {
            _Activatetimer.Stop();
            if (DynamicItems.Count == 0) OnActivateDynItems?.Invoke();
        }

        public void UserDeactivate() 
        {
            _DeActivatetimer.Stop();
            if (DynamicItems.Count > 0) OnDeActivateDynItems?.Invoke();
            if (DynamicItems.Count > 0)
            {
                VMBase.ToolBarServer.Remove(DynamicItems.OfType<ToolItem>());
                VMBase.MenuItemServer.Remove(DynamicItems.OfType<MenuItemVM>());
                DynamicItems.Clear();
            }
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: освободить управляемое состояние (управляемые объекты)
                }

                // TODO: освободить неуправляемые ресурсы (неуправляемые объекты) и переопределить метод завершения
                // TODO: установить значение NULL для больших полей
                _Activatetimer.Stop();
                UserDeactivate();

                disposedValue = true;
            }
        }

        // // TODO: переопределить метод завершения, только если "Dispose(bool disposing)" содержит код для освобождения неуправляемых ресурсов
        ~DynamicItemsAdapter()
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
    public abstract class VMBaseForm : VMBase
    {
        //public delegate void OnCloseHandler(object sender);
        //public static event OnCloseHandler? OnClose;
        private DynamicItemsAdapter? dynamicItemsAdapter;
        public DynamicItemsAdapter DynAdapter 
        { 
            get 
            {
                if (dynamicItemsAdapter == null) dynamicItemsAdapter = new DynamicItemsAdapter();
                return dynamicItemsAdapter;
            } 
        }
        #region Close
        public virtual void Close()
        {
            if (dynamicItemsAdapter != null) dynamicItemsAdapter.Dispose();
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
        [XmlIgnore] public string Title
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

        [XmlIgnore] public bool _isVisible = true;
        [XmlIgnore] public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (value != _isVisible)
                {
                    _isVisible = value;

                    if (!_isVisible) dynamicItemsAdapter?.DeActivateMenu();
                    else if (_isActive) dynamicItemsAdapter?.ActivateMenu();

                    OnPropertyChanged(nameof(IsVisible));
                    OnVisibleChange(this);
                }
            }
        }
        #endregion

        #region Icon        
        string? _IconSource;
        [XmlIgnore] public string? IconSource
        {
            get=> _IconSource;
            set=> SetProperty(ref _IconSource, value);
        }
        //ImageSource? _IconSource;
        //[XmlIgnore] public object? IconSource
        //{
        //    get => _IconSource;
        //    set
        //    {
        //        if (value is string s)
        //        {
        //            if (Uri.TryCreate(s, UriKind.Absolute, out var uriResult))
        //            {
        //                _IconSource = new BitmapImage(uriResult);
        //            }
        //            else
        //            {
        //                FontFamily fontFamily = new FontFamily("Segoe Fluent Icons");


        //                var SymbolSize = 16;

        //                var textBlock = new TextBlock
        //                {
        //                    /// дллжно быть в xaml Style
        //                    //Foreground = Application.Current.Resources[AdonisUI.Brushes.ForegroundBrush] as SolidColorBrush,
        //                    //Background = Application.Current.Resources[AdonisUI.Brushes.Layer1BackgroundBrush] as SolidColorBrush,

        //                    FontFamily = fontFamily,
        //                    Text = s,
        //                };

        //                var brush = new VisualBrush
        //                {
        //                    Visual = textBlock,
        //                    Stretch = Stretch.Uniform

        //                };


        //                var drawing = new GeometryDrawing
        //                { 
        //                    Brush = brush,
        //                    Geometry = new RectangleGeometry(
        //                    new Rect(0, 0, SymbolSize, SymbolSize))
        //                };

        //                _IconSource = new DrawingImage(drawing);
        //            }
        //        }
        //        else
        //        if (_IconSource != value && _IconSource is ImageSource si)
        //        {
        //            _IconSource = si;
        //            //OnPropertyChanged(nameof(IconSource));
        //        }
        //    }

        [XmlIgnore] public bool IconSourceEnable => _IconSource != null;
        [XmlIgnore] public bool IconSourceIsUri => _IconSource != null && Uri.TryCreate(_IconSource, UriKind.Absolute, out var _);
        [XmlIgnore] public bool IconSourceIsNotUri => _IconSource != null && !Uri.TryCreate(_IconSource, UriKind.Absolute, out var _);
        #endregion

        #region IsSelected

        protected delegate void SelectHandler();
        protected event SelectHandler? OnSelect;
        protected event SelectHandler? OnDeSelect;
        private bool _isSelected = false;
        [XmlIgnore] public bool IsSelected
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

        //private event ActivateHandler? OnActivate;
        //private event ActivateHandler? OnDeActivate;
        private bool _isActive = false;
        [XmlIgnore] public bool IsActive
        {
            get { return _isActive; }
            set
            {
                if (_isActive != value)
                {
                    if (_isActive && !value)
                        dynamicItemsAdapter?.DeActivateMenu(); // OnDeActivate?.Invoke();
                    else
                        dynamicItemsAdapter?.ActivateMenu(); // OnActivate?.Invoke();

                    _isActive = value;
                    OnPropertyChanged(nameof(IsActive));
                }
            }
        }

        #endregion

        #region FloatingWidth
        [XmlIgnore] public double FloatingWidth { get; set; } = 0.0;

        #endregion

        #region FloatingHeight

        [XmlIgnore] public double FloatingHeight { get; set; } = 0.0;

        #endregion

        #region FloatingLeft

        [XmlIgnore] public double FloatingLeft { get; set; } = 0.0;

        #endregion

        #region FloatingTop

        [XmlIgnore] public double FloatingTop { get; set; } = 0.0;
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
        [XmlIgnore]
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
        [XmlIgnore] public bool CanFloat { get; set; } = true;
        #endregion

        #region ToolTip
        [XmlIgnore] public bool ToolTipEnable=> ToolTip != null;
        [XmlIgnore] public string? ToolTip { get; set; } = null;
        #endregion

    }
    public class DocumentVM : VMBaseForm
    {
        #region Description
        [XmlIgnore] public string? Description { get; set; } = null;

        [XmlIgnore] public bool DescriptionEnable => Description != null;
        #endregion

        #region CanMove
        [XmlIgnore] public bool CanMove { get; set; } = true;
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
        [XmlIgnore] public double AutoHideWidth { get; set; } = 200;

        #endregion

        #region AutoHideMinWidth

        [XmlIgnore] public double AutoHideMinWidth { get; set; } = 20;

        #endregion

        #region AutoHideHeight

        [XmlIgnore] public double AutoHideHeight { get; set; } = 200;
        #endregion

        #region AutoHideMinHeight

        [XmlIgnore] public double AutoHideMinHeight { get; set; } = 20;

        #endregion

        #region CanHide

        [XmlIgnore] public bool CanHide { get; set; } = true;

        #endregion

        #region CanAutoHide

        [XmlIgnore] public bool CanAutoHide { get; set; }=true;

        #endregion

        #region CanDockAsTabbedDocument

        [XmlIgnore] public bool CanDockAsTabbedDocument { get; set; } = true;
        #endregion

        #region ShowStrategy
        [XmlIgnore] public ShowStrategy? ShowStrategy { get; set; } = null;
        #endregion
    }

    public class ActiveDocumentChangedEventArgs: EventArgs
    {
        public VMBaseForm OldActive = null!;
        public VMBaseForm NewActive = null!;
    }
    public enum FormAddedFrom
    {
        User,
        DeSerialize,
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
                if (_instance == null) throw new Exception("_instance == null");                
                return _instance;
            }
            //set
            //{
            //    if (_instance != null) Clear();
            //    //if (value != null && _instance != null && _instance != value) 
            //    _instance = value; 
            //}
        }
        #endregion
        public DockManagerVM() { if (_instance == null) _instance = this; }

        #region Docs and Tools properties
        [XmlArray("Documents")]
        public ObservableCollection<DocumentVM> _docs { get; set; }  = new ObservableCollection<DocumentVM>();
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
        [XmlArray("Tools")]
        public ObservableCollection<ToolVM> _tools { get; set; } = new ObservableCollection<ToolVM>();
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
        private static FormAddedFrom _state = FormAddedFrom.User;
        public static FormAddedFrom State { get => _state; set => _state = value; }
        public static void OnVisibleChange(VMBaseForm? sender)
        {
           if (_state == FormAddedFrom.User) FormVisibleChanged?.Invoke(sender);
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
            _state = formAddedFrom;
            if (vmbase is ToolVM t) Instance._tools.Add(t);
            else if (vmbase is DocumentVM d) Instance._docs.Add(d);
            FormAdded?.Invoke(vmbase, new FormAddedEventArg(formAddedFrom));
            return vmbase;
        }
        public static VMBaseForm AddOrGet(string ContentID, FormAddedFrom formAddedFrom)
        {
            _state = formAddedFrom;
            var c = Contains(ContentID);
            if (c != null)
            {
                //                if (c.IsClosed) c.ResetState();
                return c;
            }
            // создаем новую форму 
            string RootContentID = SplitID(ContentID)[0];
            var FormVMGenerator = _RegForm.GetValueOrDefault(RootContentID);
            if (FormVMGenerator == null)
                throw new ArgumentOutOfRangeException(nameof(FormVMGenerator), "FormVMGenerator == null", null);
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
        static List<Type> _SerializerTypes = new();
        //    /// <summary>
        //    /// регистрируем генератор модели представления
        //    /// </summary>
        //    /// <param name="RootContentID"> RootContentID.AnyData.AnyData...) </param>
        //    /// <param name="RegFunc">генератор модели представления</param>
        public static void Register<T>(string RootContentID, Func<VMBaseForm> RegFunc) where T : VMBaseForm
        {
            _RegForm.TryAdd(RootContentID, RegFunc);
            if (!_SerializerTypes.Contains(typeof(T))) _SerializerTypes.Add(typeof(T));
        }
        public static void Register(Type type, string RootContentID, Func<VMBaseForm> RegFunc)
        {
            _RegForm.TryAdd(RootContentID, RegFunc);
            if (!_SerializerTypes.Contains(type)) _SerializerTypes.Add(type);
        }
        public static XmlSerializer Serializer => new XmlSerializer(typeof(DockManagerVM), null, _SerializerTypes.ToArray(), null, null, null);

        public static void Serialize(StreamWriter fs)
        {
            Serializer.Serialize(fs, Instance);
        }

        public static void DeSerialize(StreamReader fs)
        {
            DockManagerVM tmp = (DockManagerVM) DockManagerVM.Serializer.Deserialize(fs)!;
            Instance._tools.Clear();
            Instance._docs.Clear();
            foreach (var t in tmp._tools) Instance._tools.Add(t);
            foreach (var d in tmp._docs) Instance._docs.Add(d);
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

                    //var l = ServiceProvider.GetRequiredService<ILogger<VMBaseForm>>();
                    //var s = Instance._activeDocument != null ? Instance._activeDocument.ContentID : "NUL";
                    //l.LogTrace(" ActiveDocument {0} => {1} ", s, value.ContentID);
                    Logger.Trace?.Debug(" ActiveDocument {OldActive} => {NewActive} ", 
                        Instance._activeDocument?.ContentID ?? "NUL", value.ContentID);

                    Instance._activeDocument = value;
                    Instance.OnPropertyChanged(nameof(ActiveDocument));
                }
            }
        }
        public static event EventHandler<ActiveDocumentChangedEventArgs>? ActiveDocumentChanging;
        #endregion

    }

}
