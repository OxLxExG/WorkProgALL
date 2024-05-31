using Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.Intrinsics.X86;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Navigation;
using System.Xml;
using Main.ViewModels;
using WpfDialogs;
using Xceed.Wpf.AvalonDock;
using Xceed.Wpf.AvalonDock.Controls;
using Xceed.Wpf.AvalonDock.Layout;
using Xceed.Wpf.AvalonDock.Layout.Serialization;

namespace Main
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
    public partial class MainWindow : Window, IDockManagerSerialization
    {
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
            }
        }

        private void WindowMain_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
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

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowMain.WindowState = WindowState.Minimized;
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            WindowMain.WindowState = WindowState.Normal;
            RootFileDocumentVM.SetDrity();
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowMain.WindowState = WindowState.Maximized;
            RootFileDocumentVM.SetDrity();
        }

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
