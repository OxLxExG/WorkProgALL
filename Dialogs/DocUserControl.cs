using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;
using Xceed.Wpf.AvalonDock.Layout;
using Core;
using Xceed.Wpf.AvalonDock;

namespace WpfDialogs
{
    public class DockUserControl : UserControl
    {
        public static DependencyProperty CanCloseProperty;
        public static DependencyProperty CanHideProperty;
        public static DependencyProperty CanAutoHideProperty;
        public static DependencyProperty CanFloatProperty;
        public static DependencyProperty CanDockAsTabbedDocumentProperty;
        public static DependencyProperty ShowStrategyProperty;
        public static DependencyProperty IsDocumentProperty;
        public static DependencyProperty FloatingStrategyProperty;
        public static DependencyProperty TitleProperty;

        static DockUserControl()
        {
            CanCloseProperty = DependencyProperty.Register("CanClose", typeof(bool), typeof(DockUserControl));
            CanHideProperty = DependencyProperty.Register("CanHide", typeof(bool), typeof(DockUserControl));
            CanAutoHideProperty = DependencyProperty.Register("CanAutoHide", typeof(bool), typeof(DockUserControl));
            CanDockAsTabbedDocumentProperty = DependencyProperty.Register("CanDockAsTabbedDocument", typeof(bool), typeof(DockUserControl));
            CanFloatProperty = DependencyProperty.Register("CanFloat", typeof(bool), typeof(DockUserControl));
            ShowStrategyProperty = DependencyProperty.Register("ShowStrategy", typeof(AnchorableShowStrategy),
                typeof(DockUserControl), new PropertyMetadata(AnchorableShowStrategy.Most));
            IsDocumentProperty = DependencyProperty.Register("IsDocument", typeof(bool), typeof(DockUserControl));
            FloatingStrategyProperty = DependencyProperty.Register("FloatingStrategy", typeof(bool), typeof(DockUserControl));
            TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(DockUserControl));
        }
        //IServiceProvider _serviceProvider;
         public DockUserControl()
        {
            CanDockAsTabbedDocument = true;
            CanClose = true;
            CanAutoHide = true;
            CanFloat = true;
            CanHide = true;
        }
        private LayoutAnchorable? _layout;
        public void AddToLayout(DockingManager dockingManager, EventHandler? IsVisibleChanged = null)
        {
            _layout = new LayoutAnchorable();
            _layout.Content = this;
            _layout.Closed += (s,e)=> _layout = null;
            OnAddLayout(_layout);
            if (IsVisibleChanged != null) _layout.IsVisibleChanged += IsVisibleChanged;
            try
            {
                _layout.AddToLayout(dockingManager, ShowStrategy);
            }
            catch 
            {
                
            }
            finally
            {
                if (FloatingStrategy) _layout.Float();
                else _layout.Show();
            }
        }
        public LayoutAnchorable? Layout => _layout;// (Container.dockingManager!.Layout.Descendents().OfType<LayoutAnchorable>().FirstOrDefault((l) => l.Content == this));
        public void Close() => Layout?.Close();
        
        #region Properties
        public AnchorableShowStrategy ShowStrategy
        {
            get { return (AnchorableShowStrategy)GetValue(ShowStrategyProperty); }
            set { SetValue(ShowStrategyProperty, value); }
        }
        public bool IsDocument
        {
            get { return (bool)GetValue(IsDocumentProperty); }
            set { SetValue(IsDocumentProperty, value); }
        }
        public bool FloatingStrategy
        {
            get { return (bool)GetValue(FloatingStrategyProperty); }
            set { SetValue(FloatingStrategyProperty, value); }
        }
        public bool CanClose
        {
            get { return (bool)GetValue(CanCloseProperty); }
            set { SetValue(CanCloseProperty, value); }
        }
        public bool CanHide
        {
            get { return (bool)GetValue(CanHideProperty); }
            set { SetValue(CanHideProperty, value); }
        }
        public bool CanAutoHide
        {
            get { return (bool)GetValue(CanAutoHideProperty); }
            set { SetValue(CanAutoHideProperty, value); }
        }
        public bool CanFloat
        {
            get { return (bool)GetValue(CanFloatProperty); }
            set { SetValue(CanFloatProperty, value); }
        }
        public bool CanDockAsTabbedDocument
        {
            get { return (bool)GetValue(CanDockAsTabbedDocumentProperty); }
            set { SetValue(CanDockAsTabbedDocumentProperty, value); }
        }
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }
        #endregion
        private void OnAddLayout(LayoutAnchorable? layout)
        {
            if (layout != null)
            {
                layout.Title = Title;
                layout.FloatingWidth = Width;
                layout.FloatingHeight = Height;
                layout.AutoHideHeight = Height;
                layout.AutoHideWidth = Width;
                layout.CanClose = CanClose;
                layout.CanHide = CanHide;
                layout.CanDockAsTabbedDocument = CanDockAsTabbedDocument;
                layout.CanFloat = CanFloat;
                layout.CanAutoHide = CanAutoHide;
                layout.FloatingTop = (SystemParameters.PrimaryScreenHeight - Height) / 2;
                layout.FloatingLeft = (SystemParameters.PrimaryScreenWidth - Width) / 2;
                Width = double.NaN;
                Height = double.NaN;
                Binding binding = new Binding();
                binding.Source = layout; // элемент-источник
                binding.Path = new PropertyPath("Title"); // свойство элемента-источника
                binding.Mode = BindingMode.TwoWay;
                SetBinding(TitleProperty, binding); // установка привязки для элемента-приемника                
            }
        }
    }
}
