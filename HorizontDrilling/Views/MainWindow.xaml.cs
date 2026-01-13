using AdonisUI;
using AdonisUI.Controls;
using Core;
using Global;
using HorizontDrilling.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Xml;
using Xceed.Wpf.AvalonDock.Layout;
using Xceed.Wpf.AvalonDock.Layout.Serialization;

namespace HorizontDrilling
{
    [ValueConversion(typeof(VMBaseForm), typeof(LayoutContent))]
    public class VMtoLayoutContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && targetType == typeof(LayoutContent)) 
            {
                LayoutRoot lr = VMBase.ServiceProvider.GetRequiredService<MainWindow>().dockManager.Layout;
                return lr.Descendents().OfType<LayoutContent>().FirstOrDefault(lc => lc.Content == value) ?? Binding.DoNothing;
            }
            return Binding.DoNothing;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is LayoutContent lc) return lc.Content;
            else return Binding.DoNothing; 
        }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [RegService(null)]
    public partial class MainWindow : AdonisWindow, IDockManagerSerialization
    {
        protected override void CloseClick(object sender, RoutedEventArgs e)
        {
            CloseProgramCommand.Shutdown();
        }
        //public bool IsDark
        //{
        //    get => (bool)GetValue(IsDarkProperty);
        //    set => SetValue(IsDarkProperty, value);
        //}

        //public static readonly DependencyProperty IsDarkProperty = DependencyProperty.Register("IsDark", typeof(bool), typeof(MainWindow), new PropertyMetadata(false, OnIsDarkChanged));

        //private static void OnIsDarkChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        //{
        //    ((MainWindow)d).ChangeTheme((bool)e.OldValue);
        //}

        //private void ChangeTheme(bool oldValue)
        //{
        //    ResourceLocator.SetColorScheme(Application.Current.Resources, oldValue ? ResourceLocator.LightColorScheme : ResourceLocator.DarkColorScheme);
        //}

        public MainWindow(MainVindowVM vm)
        {
            InitializeComponent();

            DataContext = vm;

            VMBaseForm.OnFocus += (o) =>
            {
                var w = dockManager.FloatingWindows.FirstOrDefault(
                    w => w.Model.Descendents().OfType<LayoutContent>().FirstOrDefault(
                        lc => lc.Content == o) != null);
                w?.Focus();
            };
        }

        private void WindowMain_Loaded(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.Height > 0 && Properties.Settings.Default.Width > 0)
            {
                WindowMain.Top = Properties.Settings.Default.Top;
                WindowMain.Left = Properties.Settings.Default.Left;
                WindowMain.Height = Properties.Settings.Default.Height;
                WindowMain.Width = Properties.Settings.Default.Width;
                if (Properties.Settings.Default.Maximized) WindowMain.WindowState = WindowState.Maximized;
                toolBarMenu.Band = Properties.Settings.Default.ToolBarMenu_Band;
                toolBarMenu.BandIndex = Properties.Settings.Default.ToolBarMenu_Index;
                toolBarButtonGliph.Band = Properties.Settings.Default.ToolBarGlyph_Band;
                toolBarButtonGliph.BandIndex = Properties.Settings.Default.ToolBarGlyph_Index;
                toolBarButtonText.Band = Properties.Settings.Default.ToolBarTxt_Band;
                toolBarButtonText.BandIndex = Properties.Settings.Default.ToolBarTxt_Index;
                //using (TextReader reader = new StringReader(Properties.Settings.Default.XMLDockManager))
                //{
                //    var serializer = new XmlLayoutSerializer(dockManager);

                //    serializer.LayoutSerializationCallback += (s, args) =>
                //        args.Content = DockManagerVM.AddOrGet(args.Model.ContentId, FormAddedFrom.DeSerialize);

                //    serializer.Deserialize(reader);
                //}
            }
        }

        private void WindowMain_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //using (StringWriter textWriter = new StringWriter())
            //{
            //    var serializer = new XmlLayoutSerializer(dockManager);
            //    serializer.Serialize(textWriter);
            //    Properties.Settings.Default.XMLDockManager = textWriter.ToString();
            //}             
            Properties.Settings.Default.Top = WindowMain.RestoreBounds.Top;
            Properties.Settings.Default.Left = WindowMain.RestoreBounds.Left;
            Properties.Settings.Default.Height = WindowMain.RestoreBounds.Height;
            Properties.Settings.Default.Width = WindowMain.RestoreBounds.Width;
            Properties.Settings.Default.Maximized = WindowMain.WindowState == WindowState.Maximized;
            Properties.Settings.Default.ToolBarMenu_Band = toolBarMenu.Band;
            Properties.Settings.Default.ToolBarMenu_Index = toolBarMenu.BandIndex;
            Properties.Settings.Default.ToolBarGlyph_Band = toolBarButtonGliph.Band;
            Properties.Settings.Default.ToolBarGlyph_Index = toolBarButtonGliph.BandIndex;
            Properties.Settings.Default.ToolBarTxt_Band = toolBarButtonText.Band;
            Properties.Settings.Default.ToolBarTxt_Index = toolBarButtonText.BandIndex;
            Properties.Settings.Default.Save();
        }

        //private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        //{
        //    WindowMain.WindowState = WindowState.Minimized;
        //}

        //private void RestoreButton_Click(object sender, RoutedEventArgs e)
        //{
        //    WindowMain.WindowState = WindowState.Normal;
        //    RootFileDocumentVM.SetVMDrity();
        //}

        //private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        //{
        //    WindowMain.WindowState = WindowState.Maximized;
        //    RootFileDocumentVM.SetVMDrity();
        //}

        public void Serialize(XmlWriter writer)
        {
            var serializer = new XmlLayoutSerializer(dockManager);
            serializer.Serialize(writer);
        }

        public void Deserialize(XmlReader reader)
        {
            var serializer = new XmlLayoutSerializer(dockManager);

            serializer.LayoutSerializationCallback += (s, args) =>
                args.Content = DockManagerVM.AddOrGet(args.Model.ContentId, FormAddedFrom.DeSerialize);

            serializer.Deserialize(reader);
        }

        public void Serialize(string File)
        {
            var serializer = new XmlLayoutSerializer(dockManager);
            serializer.Serialize(File);
        }

        public void Deserialize(string File)
        {
            var serializer = new XmlLayoutSerializer(dockManager);

            serializer.LayoutSerializationCallback += (s, args) =>
                args.Content = DockManagerVM.AddOrGet(args.Model.ContentId, FormAddedFrom.DeSerialize);

            serializer.Deserialize(File);
        }
    }
}
