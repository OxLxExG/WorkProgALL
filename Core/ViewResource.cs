using Global;
using Microsoft.Win32;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Core
{
    public static class ThemeEvent
    {
        static Dictionary<VMBaseForm, WeakReference<TextBlock>> datas = new Dictionary<VMBaseForm, WeakReference<TextBlock>>();
        public static void Register(VMBaseForm form, TextBlock tb)
        {
            datas.Remove(form);
            datas.Add(form, new WeakReference<TextBlock>(tb));
        }
        public static void DoThemeChanged(object? sender, bool e)
        {
            ThemeChangeEvent.DoThemeChanged(sender, e);

            for (var i = datas.Count-1; i >= 0; i--)  
            {
                var w = datas.ElementAt(i);
                if (w.Value.TryGetTarget(out var elem))
                {
                    elem.Foreground = Application.Current.Resources[AdonisUI.Brushes.ForegroundBrush] as SolidColorBrush;
                }
                else datas.Remove(w.Key);
            }
        }
    }

    //[MarkupExtensionReturnType(typeof(DrawingImage))]
    //public class IconImageExtension : StaticResourceExtension
    //{
    //    private static readonly FontFamily fontFamily
    //        = new FontFamily("Segoe Fluent Icons");

    //    public string SymbolCode { get; set; } = string.Empty;

    //    public double SymbolSize { get; set; } = 16;

    //    public override object ProvideValue(IServiceProvider serviceProvider)
    //    {
    //        var textBlock = new TextBlock
    //        {
    //            FontFamily = fontFamily,
    //            Text = SymbolCode,
    //        };

    //        var brush = new VisualBrush
    //        {
    //            Visual = textBlock,
    //            Stretch = Stretch.Uniform
    //        };

    //        var drawing = new GeometryDrawing
    //        {
    //            Brush = brush,
    //            Geometry = new RectangleGeometry(
    //                new Rect(0, 0, SymbolSize, SymbolSize))
    //        };

    //        return new DrawingImage(drawing);
    //    }
    //}


    [ValueConversion(typeof(object), typeof(object))]
    public class ContentTemplateButtonConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
            {
                if (value is ComponentResourceKey c)
                {
                    var dt = Application.Current.FindResource(c) as DataTemplate;
                    return dt ?? Binding.DoNothing;
                }
                return value;
            }
            return null!;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }    

    [ValueConversion(typeof(object), typeof(object))]
    public class ContentButtonConverter : IValueConverter
    {

        #region IValueConverter Members 
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
            {
                    if (value is string s)
                    {
                        if (Uri.TryCreate(s, UriKind.Absolute, out var uriResult))
                        {
                            var i = new Image
                            {
                                Source = new BitmapImage(uriResult),
                            };
                            return i;
                        }
                        else
                        {
                            return s;
                        }
                    }
                    return value;
            }
            return null!;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
           throw new NotImplementedException();
        }
        #endregion
    }


    [ValueConversion(typeof(VMBaseForm), typeof(ImageSourceConverter))]
    public class VMFormToImageSource : IValueConverter
    {

        #region IValueConverter Members 
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null && targetType == typeof(ImageSource))
            {
                if (value is VMBaseForm f)
                {
                    if (f.IconSourceEnable)
                    {
                        if (f.IconSourceIsUri) return new Uri(f.IconSource!);
                        else
                        {
                            var textBlock = new TextBlock
                            {                        
                                FontFamily = new FontFamily("Segoe Fluent Icons"),
                                Text = f.IconSource,
                            };
                            textBlock.Foreground = Application.Current.Resources[AdonisUI.Brushes.ForegroundBrush] as SolidColorBrush;

                            ThemeEvent.Register(f, textBlock);

                            var brush = new VisualBrush
                            {
                                Visual = textBlock,
                                Stretch = Stretch.Uniform
                            };

                            var drawing = new GeometryDrawing
                            {
                                Brush = brush,
                                Geometry = new RectangleGeometry(
                                    new Rect(0, 0, 16, 16))
                            };

                            return new DrawingImage(drawing);

                            //return new IconImageExtension { SymbolCode = f.IconSource! }.ProvideValue(null!);
                        }
                    }
                }
            }
            return Binding.DoNothing; 
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            return value;
        }
        #endregion
    }

    //public class DummiConverter : IValueConverter
    //{

    //    #region IValueConverter Members 
    //    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    //    {
    //        return value;
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    //    {
            
    //        return value;
    //    }
    //    #endregion
    //}

    public static class AnyResuorceSelector
    {
        public static object? Get(ResourceDictionary _dictionary, object item, string suffix = "")
        {
            if (item != null)
            {
                Type? type = item.GetType();

                while (type != null)
                {
                    if (_dictionary.Contains(type.Name + suffix))
                    {
                        return _dictionary[type.Name + suffix];
                    }
                    type = type.BaseType;
                }
            }
            return null;
        }
    }
    public class ToolTemplateSelector : DataTemplateSelector
    {
        private static ResourceDictionary _dictionary;
        public static ResourceDictionary Dictionary => _dictionary;
        static ToolTemplateSelector()
        {
            _dictionary = new ResourceDictionary();

            _dictionary.Source = new Uri("Core;component/ViewResourceTools.xaml", UriKind.Relative);
        }
        public override DataTemplate SelectTemplate(object item, DependencyObject parentItemsControl)
        {
            var res = AnyResuorceSelector.Get(_dictionary, item);
            return (res != null) ? (DataTemplate)res : base.SelectTemplate(item, parentItemsControl);
        }
    }
    public class MenuTemplateSelector : ItemContainerTemplateSelector
    {
        private static ResourceDictionary _dictionary;
        public static ResourceDictionary Dictionary => _dictionary;
        static MenuTemplateSelector()
        {
            _dictionary = new ResourceDictionary();

            _dictionary.Source = new Uri("Core;component/ViewResourceMenus.xaml", UriKind.Relative);
        }
        public override DataTemplate SelectTemplate(object item, ItemsControl parentItemsControl)
        {
            var res = AnyResuorceSelector.Get(_dictionary, item);
            return (res != null) ? (DataTemplate)res : base.SelectTemplate(item, parentItemsControl);
        }
    }

    public class FormResource
    {
        private static ResourceDictionary _dictionary;
        public static ResourceDictionary Dictionary => _dictionary;
        static FormResource()
        {
            _dictionary = new ResourceDictionary();

            _dictionary.Source = new Uri("Core;component/ViewResourceForms.xaml", UriKind.Relative);
        }
        public static object? Get(object item, string suffix) => AnyResuorceSelector.Get(_dictionary, item, suffix);
    }

    public class PanesStyleSelector : StyleSelector
    {
        public override System.Windows.Style SelectStyle(object item, System.Windows.DependencyObject container)
        {
            var res = FormResource.Get(item, "Style");
            return (res != null)?(Style) res : base.SelectStyle(item, container);
        }
    }
    public class PanesTemplateSelector : DataTemplateSelector
    {
        public override System.Windows.DataTemplate SelectTemplate(object item, System.Windows.DependencyObject container)
        {
            var res = FormResource.Get(item, "Template");
            return (res != null) ? (DataTemplate)res : base.SelectTemplate(item, container);
        }
    }

    internal abstract class FileView : IFileDialog
    {
        protected readonly FileDialog fileDialog;
        public string Title { get => fileDialog.Title; set => fileDialog.Title = value; }
        public string InitialDirectory { get => fileDialog.InitialDirectory; set => fileDialog.InitialDirectory = value; }
        public string Filter { get => fileDialog.Filter; set => fileDialog.Filter = value; }
        public bool ValidateNames { get => fileDialog.ValidateNames; set => fileDialog.ValidateNames = value; }
        public IList<object> CustomPlaces
        {
            get => (IList<object>)fileDialog.CustomPlaces;
            set => fileDialog.CustomPlaces = Array.ConvertAll(value.ToArray(), (o) =>
                                {
                                    if (o is string s) return new FileDialogCustomPlace(s);
                                    else if (o is Guid g) return new FileDialogCustomPlace(g);
                                    else return new FileDialogCustomPlace("no name");
                                });
        }
        public bool CheckPathExists { get => fileDialog.CheckPathExists; set => fileDialog.CheckPathExists = value; }
        public bool CheckFileExists { get => fileDialog.CheckFileExists; set => fileDialog.CheckFileExists = value; }
        public bool AddExtension { get => fileDialog.AddExtension; set => fileDialog.AddExtension = value; }
        public string DefaultExt { get => fileDialog.DefaultExt; set => fileDialog.DefaultExt = value; }
        public string[] FileNames => fileDialog.FileNames;
        public string FileName { get => fileDialog.FileName; set => fileDialog.FileName = value; }
        internal FileView(FileDialog fileDialog)
        {
            this.fileDialog = fileDialog;
        }
        public bool ShowDialog()
        {
            var r = fileDialog.ShowDialog();
            return r ?? false;
        }
        public bool ShowDialog(Action<string> res)
        {
            bool r = fileDialog.ShowDialog() ?? false;
            if (r) res(fileDialog.FileName);
            return r;
        }
    }
    [RegService(typeof(IFileOpenDialog),IsSingle:false)]
    internal sealed class FileOpenView : FileView, IFileOpenDialog
    {
        private OpenFileDialog fd => (OpenFileDialog) fileDialog;
        public FileOpenView() : base(new OpenFileDialog()) {}
        public bool ReadOnlyChecked { get => fd.ReadOnlyChecked; set => fd.ReadOnlyChecked = value; }
        public bool ShowReadOnly { get => fd.ShowReadOnly; set => fd.ShowReadOnly = value; }
    }
    [RegService(typeof(IFileSaveDialog), IsSingle: false)]
    internal sealed class FileSaveView : FileView, IFileSaveDialog
    {
        private SaveFileDialog fs => (SaveFileDialog) fileDialog;
        public FileSaveView() : base(new SaveFileDialog()) { }
        public bool CreatePrompt { get => fs.CreatePrompt; set => fs.CreatePrompt = value; }
        public bool OverwritePrompt { get => fs.OverwritePrompt; set => fs.OverwritePrompt = value; }
    }
    [RegService(typeof(IMessageBox), IsSingle: false)]
    internal sealed class MsgBox : IMessageBox
    {
        public BoxResult Show(string messageBoxText, string caption, BoxButton button, BoxImage icon, BoxResult defaultResult = BoxResult.None, BoxOptions options = BoxOptions.None)
        {
            return (BoxResult)MessageBox.Show(messageBoxText, caption, (MessageBoxButton)button, (MessageBoxImage)icon, (MessageBoxResult)defaultResult, (MessageBoxOptions)options);
        }
    }
    //public abstract class FileMenuItemView : MenuItem
    //{
    //    public FileDialog? FileDialog { get; set; }
    //    public FileMenuItemView()
    //    {
    //        Click += (o, e) =>
    //        {
    //            SetupDialog();
    //            if (FileDialog?.ShowDialog() == true)
    //            {
    //                //this.Command?.Execute(FileDialog.FileName);
    //                (this.DataContext as MenuFileVM)?.OnSelectFileAction?.Invoke(FileDialog.FileName);
    //            }
    //        };
    //    }
    //    protected virtual void SetupDialog()
    //    {
    //        if (FileDialog != null && DataContext is MenuFileVM mf)
    //        {
    //            FileDialog.Title = mf.Title ?? FileDialog.Title;
    //            FileDialog.InitialDirectory = mf.InitialDirectory ?? FileDialog.InitialDirectory;
    //            FileDialog.AddExtension = mf.AddExtension;
    //            FileDialog.CheckFileExists = mf.CheckFileExists;
    //            FileDialog.CheckPathExists = mf.CheckPathExists;
    //            FileDialog.DefaultExt = mf.DefaultExt ?? FileDialog.DefaultExt;
    //            FileDialog.Filter = mf.Filter ?? FileDialog.Filter;
    //            FileDialog.ValidateNames = mf.ValidateNames;
    //            if (mf.CustomPlaces != null)
    //            {
    //                FileDialog.CustomPlaces = Array.ConvertAll(mf.CustomPlaces.ToArray(), (o) =>
    //                {
    //                    if (o is string s) return new FileDialogCustomPlace(s);
    //                    else if (o is Guid g) return new FileDialogCustomPlace(g);
    //                    else return new FileDialogCustomPlace("no name");
    //                });
    //            }
    //        }
    //    }
    //}
    //public class FileOpenMenuItemView : FileMenuItemView
    //{
    //    protected override void SetupDialog()
    //    {
    //        this.FileDialog = new OpenFileDialog();
    //        base.SetupDialog();
    //        if (DataContext is MenuOpenFileVM m && FileDialog is OpenFileDialog f)
    //        {
    //            f.ReadOnlyChecked = m.ReadOnlyChecked;
    //            f.ShowReadOnly = m.ShowReadOnly;
    //        }
    //    }
    //}
    //public class FileSaveMenuItemView : FileMenuItemView
    //{
    //    protected override void SetupDialog()
    //    {
    //        this.FileDialog = new SaveFileDialog();
    //        base.SetupDialog();
    //        if (DataContext is MenuSaveFileVM m && FileDialog is SaveFileDialog f)
    //        {
    //            f.CreatePrompt = m.CreatePrompt;
    //            f.OverwritePrompt = m.OverwritePrompt;
    //        }
    //    }
    //}

    //public class MessageBoxMenuItem: MenuItem
    //{
    //    public MessageBoxMenuItem()
    //    {
    //        Click += (o, e) =>
    //        {
    //            if (this.DataContext is MessageBoxMenuVM dc)
    //            {
    //                if (dc.ShowBox)
    //                {
    //                    MessageBoxButton b = (MessageBoxButton)dc.Button;
    //                    MessageBoxImage i = (MessageBoxImage)dc.Image;
    //                    MessageBoxResult defa = (MessageBoxResult)dc.DefaultResult;
    //                    MessageBoxOptions opt = (MessageBoxOptions)dc.Options;
    //                    dc.OnBoxResult?.Invoke((MessageBoxMenuVM.BoxResult)MessageBox.Show(dc.Text, dc.Caption, b, i, defa, opt));
    //                }
    //                else dc.OnBoxResult?.Invoke(dc.DefaultResult);
    //            }
    //        };
    //    }
    //}
}
