using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Core
{
    public interface IMenuKeyGestureService
    {
        public void Register(ICommand command, string Gesture);
        public void Register(string CommandPath, string Gesture);
    }
    public interface IMenuItemClient
    {
        public void AddStaticMenus(IMenuItemServer s);
        //  public void RemoveAddStaticMenus();
    }
    public static class MenuService
    {
        public static T Create<T>(rootMenu root)
            where T: MenuItemVM,new()
        {
           return new T
            {
                Header = root.Header,
                ContentID = root.ContentID,
                Priority = root.Priority,
            };
        }
    }
    ///// <summary>
    ///// основа меню сепараторы и меню
    ///// </summary>
    //public abstract class MenuItemBaseVM: VMBase
    //{
    //    public int Priority { get; set; } = 100;
    //    public int Group => Priority / 100;
    //}
    ///// <summary>
    ///// Separator появляется автоматически между разными группами 
    ///// (см Group Priority) (использовать IMenuItemServer)
    ///// </summary>
    //public class SeparatorVM : MenuItemBaseVM { }
    /// <summary>
    /// простое меню для подменю
    /// </summary>
    public class MenuItemVM : PriorityItemBase
    {

        public MenuItemVM() { }
        public MenuItemVM(rootMenu root) : this()
        {
            Header = root.Header;
            ContentID = root.ContentID;
            Priority = root.Priority;
        }

        #region Properties
        public ObservableCollection<PriorityItem> Items { get; } = new ObservableCollection<PriorityItem>();

        public bool IsCheckable { get; set; }

        #region Header
        private string _header = string.Empty;
        public string Header
        {
            get => _header; 
            set
            {
                if (value != _header)
                {
                    _header = value;
                    OnPropertyChanged(nameof(Header));
                }
            }
        }
        #endregion

        #region Icon

        private Image? _icon;
        public Image? Icon
        {
              
            get
            {
                if (_icon == null && !String.IsNullOrWhiteSpace(IconSource))
                {
                    _icon = new Image { Source = new BitmapImage(new Uri(IconSource)) };
                }
                return _icon;
            }
        }
        #endregion

        #region IsChecked
        private bool _IsChecked;
        public bool IsChecked
        {
            get { return _IsChecked; }
            set
            {
                if (_IsChecked != value)
                {
                    _IsChecked = value;
                    OnPropertyChanged(nameof(IsChecked));
                }
            }
        }
        #endregion

        #endregion
    }
    /// <summary>
    ///  МЕНЮ команда
    /// </summary>
    public class CommandMenuItemVM : MenuItemVM
    {
        private ICommand? _Command;
        public ICommand? Command 
        { 
            get=>_Command; 
            set 
            {
                _Command = value;
                RegisterGesture();
            } 
        }

        private string? _InputGestureText;
        public CommandMenuItemVM() : base() { }
        public CommandMenuItemVM(rootMenu root) : base(root) { }
        public string? InputGestureText
        {
            get => _InputGestureText;
            set
            {
                _InputGestureText = value;
                RegisterGesture();
            }
        }
        private void RegisterGesture()
        {
            if (!string.IsNullOrWhiteSpace(InputGestureText) && Command != null) 
                ServiceProvider.GetRequiredService<IMenuKeyGestureService>().Register(Command, InputGestureText);
        }
    }
    /// <summary>
    /// меню с перехватом открытия подменю
    /// </summary>
    public class OnSubMenuOpenMenuItemVM : MenuItemVM
    {
        #region OnSubMenuAction
        public Action? OnSubMenuAction = null;

        private bool _isSubmenuOpen = false;

        public bool IsSubmenuOpen
        { 
            get { return _isSubmenuOpen; }
            set
            {
                if (!_isSubmenuOpen && value)
                {
                    OnSubMenuAction?.Invoke();
                }
                _isSubmenuOpen = value;
            }
        }
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    internal class ClosedFileItem : CommandMenuItemVM
    {
        internal ClosedFileItem(Action<ClosedFileItem> ToOwner, int idx, string fileName) : base()
        {
            ContentID = fileName;
            string name = Path.GetFileName(fileName) ?? string.Empty;
            string path = Path.GetDirectoryName(fileName) ?? string.Empty;
            Header = $"{idx}. {name} ({path})";
            Command = new RelayCommand(() => ToOwner(this));
        }
    }
    public class OpenClosedFileException : Exception { };
    public abstract class LastClosedFilesVM : MenuItemVM
    {   // ложный подход т.к. время жизни меню безконечно и обработчики могут обнулиться
        /// <summary>
        /// событие для открытия файла из истории
        /// </summary>
        //public event EventHandler<OpenClosedFileEventArg>? AfterOpenClosedFileEvent;
        /// <summary>
        /// событие для сохранения файла или отмены открытия файла из истории (исключением)
        /// </summary>
        //public event EventHandler<OpenClosedFileEventArg>? BeforeOpenClosedFileEvent;
       
        protected abstract StringCollection lastClosedFiles { get; }
        protected abstract void SaveClosedFiles();
        /// <summary>
        /// при удалении пользователем документа
        /// </summary>
        public void UserCloseFile(string file)
        {
            if (string.IsNullOrEmpty(file)) return;
            if (lastClosedFiles.Contains(file)) lastClosedFiles.Remove(file);
            lastClosedFiles.Insert(0, file);
            while (lastClosedFiles.Count > 10) lastClosedFiles.RemoveAt(lastClosedFiles.Count - 1);
            SaveClosedFiles();
            UpdateSubMenus();
        }
        /// <summary>
        /// при открытии пользователем документа
        /// </summary>
        public void UserOpenFile(string file)
        {
            if (lastClosedFiles.Contains(file))
            {
                lastClosedFiles.Remove(file);
                SaveClosedFiles();
                UpdateSubMenus();
            }
        }
        /// <summary>
        /// пользователь открывает файл из истории
        /// </summary>
        /// <param name="item"></param>         
        private void OnClickItem(ClosedFileItem item)
        {
            try
            {
                BeforeOpenClosedFileEvent(item.ContentID!); 
            }
            catch (OpenClosedFileException)
            {
                return;
            }
            UserOpenFile(item.ContentID!);
            AfterOpenClosedFileEvent(item.ContentID!);
        }
        protected abstract void BeforeOpenClosedFileEvent(string file);
        protected abstract void AfterOpenClosedFileEvent(string file);
        protected virtual void CheckEnable()
        {
            IsEnable = Items.Count > 0;
        }
        protected void UpdateSubMenus()
        {
            if (lastClosedFiles == null)
            {
                IsEnable = false;
                return;
            }
            bool needSave = false;
            for (int i = lastClosedFiles.Count - 1; i >= 0; i--)
            {
                if (File.Exists(lastClosedFiles[i])) continue;
                lastClosedFiles.RemoveAt(i);
                needSave = true;
            }
            if (needSave) SaveClosedFiles();
            Items.Clear();
            for (int i = 0; i < lastClosedFiles.Count; i++)
            {
                Items.Add(new ClosedFileItem(OnClickItem, i + 1, lastClosedFiles[i]!));
            }
            CheckEnable();
        }
    }

    //public abstract class MenuFileVM : MenuItemVM
    //{
    //    public string? Title { get; set; }
    //    public string? InitialDirectory { get; set; }
    //    public string? Filter { get; set; }
    //    public bool ValidateNames { get; set; }
    //    public IList<object>? CustomPlaces { get; set; }
    //    public bool CheckPathExists { get; set; }
    //    public bool CheckFileExists { get; set; }
    //    public bool AddExtension { get; set; }
    //    public string? DefaultExt { get; set; }
    //    public Action<string>? OnSelectFileAction = null;
    //}
    //public class MenuOpenFileVM : MenuFileVM
    //{
    //    public bool ReadOnlyChecked { get; set; }
    //    public bool ShowReadOnly { get; set; }
    //}
    //public class MenuSaveFileVM : MenuFileVM
    //{
    //    public bool CreatePrompt { get; set; }
    //    public bool OverwritePrompt { get; set; }
    //}
    //public class MessageBoxMenuVM : MenuItemVM
    //{
    //    public bool ShowBox { get; set; }
    //    public enum BoxButton
    //    {
    //        OK = 0,
    //        OKCancel = 1,
    //        YesNoCancel = 3,
    //        YesNo = 4
    //    }
    //    public enum BoxImage
    //    {
    //        None = 0,
    //        //
    //        // Сводка:
    //        //     The message box contains a symbol consisting of white X in a circle with a red
    //        //     background.
    //        Error = 16,
    //        //
    //        // Сводка:
    //        //     The message box contains a symbol consisting of a question mark in a circle.
    //        //     The question mark message icon is no longer recommended because it does not clearly
    //        //     represent a specific type of message and because the phrasing of a message as
    //        //     a question could apply to any message type. In addition, users can confuse the
    //        //     question mark symbol with a help information symbol. Therefore, do not use this
    //        //     question mark symbol in your message boxes. The system continues to support its
    //        //     inclusion only for backward compatibility.
    //        Question = 32,
    //        //
    //        // Сводка:
    //        //     The message box contains a symbol consisting of an exclamation point in a triangle
    //        //     with a yellow background.
    //        Warning = 48,
    //        //
    //        // Сводка:
    //        //     The message box contains a symbol consisting of a lowercase letter i in a circle.
    //        Information = 64
    //    }
    //    public enum BoxResult
    //    {
    //        //
    //        // Сводка:
    //        //     The message box returns no result.
    //        None = 0,
    //        //
    //        // Сводка:
    //        //     The result value of the message box is OK.
    //        OK = 1,
    //        //
    //        // Сводка:
    //        //     The result value of the message box is Cancel.
    //        Cancel = 2,
    //        //
    //        // Сводка:
    //        //     The result value of the message box is Yes.
    //        Yes = 6,
    //        //
    //        // Сводка:
    //        //     The result value of the message box is No.
    //        No = 7
    //    }
    //    //
    //    // Сводка:
    //    //     Specifies special display options for a message box.
    //    [Flags]
    //    public enum BoxOptions
    //    {
    //        //
    //        // Сводка:
    //        //     No options are set.
    //        None = 0,
    //        //
    //        // Сводка:
    //        //     The message box is displayed on the default desktop of the interactive window
    //        //     station. Specifies that the message box is displayed from a .NET Windows Service
    //        //     application in order to notify the user of an event.
    //        DefaultDesktopOnly = 131072,
    //        //
    //        // Сводка:
    //        //     The message box text and title bar caption are right-aligned.
    //        RightAlign = 524288,
    //        //
    //        // Сводка:
    //        //     All text, buttons, icons, and title bars are displayed right-to-left.
    //        RtlReading = 1048576,
    //        //
    //        // Сводка:
    //        //     The message box is displayed on the currently active desktop even if a user is
    //        //     not logged on to the computer. Specifies that the message box is displayed from
    //        //     a .NET Windows Service application in order to notify the user of an event.
    //        ServiceNotification = 2097152
    //    }
    //    public string Text { get; set; } = string.Empty;
    //    public string Caption { get; set; } = string.Empty;
    //    public BoxButton Button { get; set; }
    //    public BoxImage Image { get; set; }
    //    public BoxResult DefaultResult { get; set; }
    //    public BoxOptions Options { get; set; }
    //    public Action<BoxResult>? OnBoxResult { get; set; }
    //}


    /// <summary>
    /// привазка моделей к представлению: 
    /// 
    /// SeparatorVM
    /// MenuItemVM
    /// CommandMenuItemVM
    /// OnSubMenuOpenMenuItemVM
    /// 
    /// имя модели = ключ представления в словаре
    /// 
    /// к Dictionary добавляем пользовательские представления  Dictionary.MergedDictionaries.Add(....) 
    /// TODO: написать сервис
    /// 
    /// </summary>
    //public class ItemContainerStyleSelectorVM : StyleSelector
    //{
    //    private static ResourceDictionary _dictionary;
    //    static ItemContainerStyleSelectorVM()
    //    {
    //        _dictionary = new ResourceDictionary();
    //        _dictionary.Source = new Uri("Core;component/MenusResource.xaml", UriKind.Relative);
    //    }
    //    public override Style SelectStyle(object item, DependencyObject container)
    //    {

    //        var name = item == null ? null : item.GetType().Name;
    //        if (name != null && _dictionary.Contains(name))
    //        {
    //            return (Style)_dictionary[name];
    //        }
    //        return null!; 
    //    }
    //}

    //public class ExMenuItem: MenuItem
    //{
    //    public ExMenuItem() { }
    //    protected override DependencyObject GetContainerForItemOverride()=> new ExMenuItem(); // Required to preserve the item type in all the hierarchy
    //    protected override bool IsItemItsOwnContainerOverride(object item)=> item is ExMenuItem;
    //    #region Dependency Properties: Command Target Parameter
    //    public static readonly DependencyProperty SubmenuOpenedCommandProperty =
    //        DependencyProperty.Register(
    //            "SubmenuOpenedCommand",
    //            typeof(ICommand),
    //            typeof(ExMenuItem));
    //    public ICommand SubmenuOpenedCommand
    //    {
    //        get=> (ICommand)GetValue(SubmenuOpenedCommandProperty);
    //        set=> SetValue(SubmenuOpenedCommandProperty, value);
    //    }
    //    public static readonly DependencyProperty SubmenuOpenedTargetProperty =
    //        DependencyProperty.Register(
    //            "SubmenuOpenedTarget",
    //            typeof(IInputElement),
    //            typeof(ExMenuItem));
    //    public IInputElement SubmenuOpenedTarget
    //    {
    //        get => (IInputElement)GetValue(SubmenuOpenedTargetProperty);
    //        set=> SetValue(SubmenuOpenedTargetProperty, value);
    //    }
    //    public static readonly DependencyProperty SubmenuOpenedParameterProperty =
    //        DependencyProperty.Register(
    //            "SubmenuOpenedParameter",
    //            typeof(object),
    //            typeof(ExMenuItem));
    //    public object SubmenuOpenedParameter
    //    {
    //        get => GetValue(SubmenuOpenedParameterProperty);
    //        set=> SetValue(SubmenuOpenedParameterProperty, value);
    //    }
    //    #endregion
    //    protected override void OnSubmenuOpened(RoutedEventArgs e)
    //    {
    //        base.OnSubmenuOpened(e);

    //        if (this.SubmenuOpenedCommand != null)
    //        {
    //            RoutedCommand? command = SubmenuOpenedCommand as RoutedCommand;

    //            if (command != null) command.Execute(SubmenuOpenedParameter, SubmenuOpenedTarget);                

    //            else SubmenuOpenedCommand.Execute(SubmenuOpenedParameter);                
    //        }
    //    }
    //}
    //public class MenuEX: Menu
    //{
    //    public MenuEX():base() { }
    //    protected override DependencyObject GetContainerForItemOverride()=> new ExMenuItem();
    //    protected override bool IsItemItsOwnContainerOverride(object item)=> item is ExMenuItem;
    //}

}
