using HorizontDrilling.ViewModels;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace HorizontDrilling.Views
{
    /// <summary>
    /// Логика взаимодействия для MonitorUC.xaml
    /// </summary>
    public partial class MonitorUC : UserControl
    {
        public MonitorUC()
        {
            InitializeComponent();
        }
        MonitorVM? _vm;
        bool timer = false;
        private void Box_Loaded(object sender, RoutedEventArgs e)
        {
            _vm = DataContext as MonitorVM;
            _vm?.InitLogger(((Paragraph)Box.Document.Blocks.FirstBlock).Inlines.Clear);
            _vm?.CreateMonitor(Box);
        }
        private void Box_Unloaded(object sender, RoutedEventArgs e)
        {
           _vm?.DisposeMonitor();
           _vm = null;
        }

        private void Box_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!timer)
            {
                timer = true;
                Task.Delay(250, CancellationToken.None).ContinueWith(t =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Box.ScrollToEnd();
                        timer = false;
                    });
                });
            }
        }
    }
}
