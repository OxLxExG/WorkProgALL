using HorizontDrilling.ViewModels;
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
using Xceed.Wpf.AvalonDock.Controls;

namespace HorizontDrilling.Views
{
    /// <summary>
    /// Логика взаимодействия для VisitUI.xaml
    /// </summary>
    public partial class VisitUI : UserControl
    {
        public VisitUI()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            //var dd = VisualParent;

            //DataContext = this.FindVisualAncestor<TreeViewItemSubHeader>().DataContext;  

            //DataContext = d.DataContext;
            if (sender is VisitDocument v)
            {
                //v.IsActive = true;
            }
        }
    }
}
