using Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TextBlockLogging;
using WorkProgMain.ViewModels;
using WpfDialogs;

namespace WorkProgMain.Views
{

    public class ToLogTextBlockConverter : IValueConverter
    {
        /// <summary>
        /// создать или найти представление LogTextBlock по vm.ContentID
        /// используя сервис ILogTextBlockService
        /// ILogTextBlockService - хранилище LogTextBlock ов
        /// </summary>
        /// <param name="vm">модель представления (Trace, Log Except возможно monitor)</param>
        /// <returns></returns>
        public static LogTextBlock GetLogTextBlock(TextLogVM vm)
        {
            ILogTextBlockService s = VMBase.ServiceProvider.GetRequiredService<ILogTextBlockService>();

            var t = s.GetLogTextBlock(vm.ContentID!);

            if (t == null)
            {
                t = new LogTextBlock();
                s.SetLogTextBlock(vm.ContentID!, t);
            }
            BindingOperations.ClearBinding(t, LogTextBlock.FreezeProperty);
            Binding binding = new Binding();
            binding.Source = vm;
            binding.Path = new PropertyPath("Freeze");
            binding.Mode = BindingMode.OneWay;
            t.SetBinding(LogTextBlock.FreezeProperty, binding);
            // ?будет утечка памяти если LogTextBlock t будет удален?
            vm.OnClear += t.Inlines.Clear;

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
        /// <summary>
        /// тестовое меню
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void menu_Click_Err(object sender, RoutedEventArgs e)
        {
            var l = VMBase.ServiceProvider.GetRequiredService<ILogger<TextLogUC>>();
            l.LogInformation("LogInformation");
            l.LogTrace("msg {}", sender);
            throw new NotImplementedException();    
        }
    }       
}
