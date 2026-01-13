using Global;
using HorizontDrilling.ViewModels;
using Loggin;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

namespace HorizontDrilling.Views
{

    public class VmToBoxConverter : IValueConverter
    {
        public static RichTextBox GetLogTextBlock(TextLogVM vm)
        {
            var t = LogBoxContainer.GetOrCteate(vm.ContentID!);
            vm.InitLogger(((Paragraph) t.Document.Blocks.FirstBlock).Inlines.Clear);
            return t;
        }

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            //If the value is null, don't return anything
            if (value == null) return Binding.DoNothing;

            if (targetType == typeof(object) && typeof(TextLogVM).IsAssignableFrom(value.GetType()))
            {
                return GetLogTextBlock((TextLogVM)value);
            }
            //Type conversion is not supported
            throw new NotSupportedException(
                string.Format("Cannot convert from <{0}> to <{1}> using <ToLogTextBlockConverter>.",
                value.GetType(), targetType));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException("This method is not supported.");
        }

        #endregion
    }

    /// <summary>
    /// Логика взаимодействия для ExceptLogUC.xaml
    /// </summary>    
    public partial class TextLogUC : UserControl
    {
        public TextLogUC()
        {
            InitializeComponent();
        }
    }
}
