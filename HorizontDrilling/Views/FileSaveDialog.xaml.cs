using AdonisUI.Controls;
using Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace HorizontDrilling.Views
{
    //[ValueConversion(typeof(bool), typeof(Visibility))]
    //public class DrityToVisibilityConverter : IValueConverter
    //{

    //    #region IValueConverter Members 
    //    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    //    {
    //        if (value is bool && targetType == typeof(Visibility))
    //        {
    //            bool val = (bool)value;
    //            return val? Visibility.Visible: Visibility.Collapsed;
    //        }
    //        return Visibility.Collapsed;
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    //    {

    //        if (value is Visibility && targetType == typeof(bool))
    //        {
    //            Visibility val = (Visibility)value;
    //            if (val == Visibility.Visible)
    //                return true;
    //            else
    //                return false;
    //        }
    //        throw new ArgumentException("Invalid argument/return type. Expected argument: Visibility and return type: bool");
    //    }
    //    #endregion
    //}

    internal static class WindowExtensions
    {
        // from winuser.h
        private const int GWL_STYLE = -16,
                          WS_MAXIMIZEBOX = 0x10000,
                          WS_MINIMIZEBOX = 0x20000;

        [DllImport("user32.dll")]
        extern private static int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        extern private static int SetWindowLong(IntPtr hwnd, int index, int value);

        internal static void HideMinimizeAndMaximizeButtons(this Window window)
        {
            IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
            var currentStyle = GetWindowLong(hwnd, GWL_STYLE);

            SetWindowLong(hwnd, GWL_STYLE, (currentStyle & ~WS_MAXIMIZEBOX & ~WS_MINIMIZEBOX));
        }
    }
    /// <summary>
    /// Логика взаимодействия для FileSaveDialog.xaml
    /// </summary>
    public partial class FileSaveDialog : AdonisWindow
    {
        public BoxResult Result { get; set; } = BoxResult.Cancel;
        public FileSaveDialog()
        {
            InitializeComponent();
            this.Owner = App.Current.MainWindow;
            SourceInitialized += (o, e) =>
            {
                MinimizeButton.Visibility = Visibility.Collapsed;
                MaximizeRestoreButton.Visibility = Visibility.Collapsed;
                //this.HideMinimizeAndMaximizeButtons();
            };
        }

        private void btNoSave_Click(object sender, RoutedEventArgs e)
        {
            Result = BoxResult.No;
            Close();
        }

        private void btSave_Click(object sender, RoutedEventArgs e)
        {
            Result = BoxResult.Yes;
            this.DialogResult = true;
            Close();
        }

        private void btCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
