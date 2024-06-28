using Core;
using Microsoft.Win32;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Tests
{
    public class ViewModelMain : VMBase
    {
        int Cnt = 0;
        public ViewModelMain() 
        {
            PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == nameof(cbi3IsSelected))
                {
                    if (cbi3IsSelected) cbxIsEnable = false;
                }
                else if (e.PropertyName == nameof(cbi2IsSelected))
                {
                    if (cbi2IsSelected) cbxIsEnable = true;
                }
            };
        }


        private bool _cbxIsEnable = true;
        public bool cbxIsEnable
        { get => _cbxIsEnable;
            set 
            {
                SetProperty(ref _cbxIsEnable, value);
                if (MainWindow.Inst != null) MainWindow.Inst.textBlock.Text = $"{Cnt++}> cbxIsEnable: {_cbxIsEnable}";
            }
        }

        private bool _cbxIsChecked = true;
        public bool cbxIsChecked { get => _cbxIsChecked; set => SetProperty(ref _cbxIsChecked, value); }

        private bool _cbi1IsSelected;
        public bool cbi1IsSelected { get => _cbi1IsSelected; set => SetProperty(ref _cbi1IsSelected, value); }

        private bool _cbi2IsSelected;
        public bool cbi2IsSelected { get => _cbi2IsSelected; set => SetProperty(ref _cbi2IsSelected, value); }

        private bool _cbi3IsSelected;
        public bool cbi3IsSelected { get => _cbi3IsSelected; set => SetProperty(ref _cbi3IsSelected, value); }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Inst;
        public MainWindow()
        {

            InitializeComponent();
            Inst = this;

          // var f =new OpenFolderDialog();
           // f.ShowDialog();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ((ViewModelMain)DataContext).cbxIsEnable = !((ViewModelMain)DataContext).cbxIsEnable;
        }

        private void cbx_Checked(object sender, RoutedEventArgs e)
        {
            // cbx.IsEnabled = false;// нельзя только управление привязкой or  Mode=TwoWay!!!
            ((ViewModelMain)DataContext).cbxIsEnable = false; // правильный подход
        }
    }
}