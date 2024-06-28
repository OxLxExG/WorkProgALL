using Core;
using Main.ViewModels;
using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using MS.Internal;

namespace Main.Views
{

    public class TreeViewEx: TreeView
    {
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new TreeViewItemSubHeader();// { SubHeaderTemplateSelector = SubHeaderTemplateSelector.Instance };
        }
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            if (element is TreeViewItemSubHeader h && h.SubHeaderTemplate == null && h.SubHeader == null)
            {
                if (SubHeaderTemplateSelector.Instance.SelectTemplate(item, h) is DataTemplate dt)
                {
                    DependencyObject o = dt.LoadContent();

                    if (o is FrameworkElement f) f.DataContext = item;

                    h.SubHeader = o;
                }
            }
        }
    }
    public class TreeViewItemSubHeader: TreeViewItem
    {
        public static readonly DependencyProperty HasSubHeaderProperty
            = DependencyProperty.Register("HasSubHeader", typeof(bool), typeof(TreeViewItemSubHeader), null);
        public static readonly DependencyProperty SubHeaderProperty
            = DependencyProperty.Register("SubHeader", typeof(object), typeof(TreeViewItemSubHeader), new FrameworkPropertyMetadata(
                                (object)null!,
                                new PropertyChangedCallback(OnSubHeaderChanged)));

        public TreeViewItemSubHeader()
        {
            this.DefaultStyleKey = typeof(TreeViewItemSubHeader);
        }

        [Bindable(true)]
        public object SubHeader
        {
            get { return GetValue(SubHeaderProperty); }
            set { SetValue(SubHeaderProperty, value); }
        }
        [Bindable(false)]
        [Browsable(false)]
        public bool HasSubHeader
        {
            get => (bool) GetValue(HasSubHeaderProperty);
            protected set => SetValue(HasSubHeaderProperty, value);
        }

        [Bindable(true)]
        public static readonly DependencyProperty SubHeaderTemplateProperty =
                DependencyProperty.Register(
                        "SubHeaderTemplate",
                        typeof(DataTemplate),
                        typeof(TreeViewItemSubHeader),null);

        [Bindable(true), Category("Content")]
        public DataTemplate SubHeaderTemplate
        {
            get { return (DataTemplate)GetValue(SubHeaderTemplateProperty); }
            set { SetValue(SubHeaderTemplateProperty, value); }
        }
        protected static void OnSubHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) 
        {
            TreeViewItemSubHeader ctrl = (TreeViewItemSubHeader)d;

            if (e.NewValue != null) ((FrameworkElement)e.NewValue).DataContext = ctrl.DataContext;

            ctrl.SetValue(HasSubHeaderProperty, (e.NewValue != null) ? true : false);
        }
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new TreeViewItemSubHeader();// { SubHeaderTemplateSelector = SubHeaderTemplateSelector.Instance };
        }
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            if (element is TreeViewItemSubHeader h && h.SubHeaderTemplate == null && h.SubHeader == null)
            {
                if (SubHeaderTemplateSelector.Instance.SelectTemplate(item, h) is DataTemplate dt)
                {
                    DependencyObject o = dt.LoadContent();

                    if (o is FrameworkElement f) f.DataContext = item;

                    h.SubHeader = o;
                }
            }
        }

    }

    public class ContainerStyleSelector : StyleSelector
    {
        public override Style SelectStyle(object item, DependencyObject container)
        {
            var res = AnyResuorceSelector.Get(HeaderTemplateSelector.Dictionary, item, "Style");
            return (res != null) ? (Style)res : base.SelectStyle(item, container);
        }
    }

    public class SubHeaderTemplateSelector : DataTemplateSelector
    {
        static SubHeaderTemplateSelector _Instance = null!;
        public static SubHeaderTemplateSelector Instance
        {
            get 
            {
                if (_Instance == null) _Instance = new SubHeaderTemplateSelector();
                return _Instance;
            }
        }
        public override DataTemplate SelectTemplate(object item, DependencyObject parentItemsControl)
        {
            var res = AnyResuorceSelector.Get(HeaderTemplateSelector.Dictionary, item, "SubHeader");
            if (res is HierarchicalDataTemplate r)
            {
                return r;
            }
            return (res != null) ? (DataTemplate)res : base.SelectTemplate(item, parentItemsControl);
        }
    }
    public class HeaderTemplateSelector : DataTemplateSelector
    {
        private static ResourceDictionary _dictionary;
        public static ResourceDictionary Dictionary => _dictionary;
        static HeaderTemplateSelector()
        {
            _dictionary = new ResourceDictionary();
            _dictionary.Source = new Uri("pack://application:,,,/Views/ProjectExplorerDictionary.xaml");
        }
        public override DataTemplate SelectTemplate(object item, DependencyObject parentItemsControl)
        {
            var res = AnyResuorceSelector.Get(_dictionary, item, "Header");
            if (res is HierarchicalDataTemplate r)
            {
                return r;
            }
            return (res != null) ? (DataTemplate)res : base.SelectTemplate(item, parentItemsControl);
        }
    }

    [ValueConversion(typeof(object), typeof(IEnumerable))]
    public class InstanceConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is GroupDocument g)// && targetType == typeof(IEnumerable))
            {
                return g.VisitDocs;
            }
            else if (value is VisitDocument v)
            {
                if (v.IsRoot) return new VisitDocument[] { v };
                else return v.VisitVM.Items;
            }
            else if (value is ComplexVisitItemVM c)
            {
                return c.GetType().GetProperty("Items")!.GetValue(c, null)!;
            }
            else return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    //[ValueConversion(typeof(ComplexFileDocumentVM), typeof(IEnumerable))]
    //public class VisitConverter : IValueConverter
    //{

    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        if (value is VisitDocument g)// && targetType == typeof(IEnumerable))
    //        {
    //            return g.VisitVM.Items;
    //        }
    //        else if (value is ComplexVisitItemVM v)
    //        {
    //            return v.GetType().GetProperty("Items")!.GetValue(v, null)!; 
    //        }
    //        else return Binding.DoNothing;
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    /// <summary>
    /// Логика взаимодействия для ProjectsExplorerUC.xaml
    /// </summary>
    public partial class ProjectsExplorerUC : UserControl
    {
        public ProjectsExplorerUC()
        {
            InitializeComponent();
        }
    }
}
