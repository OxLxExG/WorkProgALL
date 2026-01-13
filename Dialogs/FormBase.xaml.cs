using CommunityToolkit.Mvvm.Input;
using Core;
using Global;
using Microsoft.Extensions.DependencyInjection;
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
using System.Xml.Linq;

namespace WpfDialogs
{
    [RegService(typeof(IMenuItemClient))]
    public class FormBaseFactory : IMenuItemClient
    {
        private static int n = 1;

        //static IMenuItemServer? _menuItemServer;
        static CommandMenuItemVM? _menuCreate = null;
        public FormBaseFactory() 
        {
            if (_menuCreate == null)
            {
                _menuCreate = new CommandMenuItemVM();
                _menuCreate.ContentID = "FormBaseFactory";
                _menuCreate.Header = "Add Doc 10";
                _menuCreate.Priority = 2001;
                _menuCreate.Command = new RelayCommand(() =>
                {
                    DockManagerVM.Add(new DocumentVM 
                    { 
                        Title =$"Doc {n++}",
                        ContentID = $"CID_{n}",
                        ToolTip=$"ToolTip {n}",
                        IconSource = "pack://application:,,,/Images/HTabGroup.png"

                }, FormAddedFrom.User);
                    
                    //var f = _mainWindow.AddToLayout<FormBase>(false, true);
                    //f.Name = $"FormLogg{n}";
                    //f.Layout!.Title = $"Form Logg{n++} {_menuCreate.Priority}";
                    //f.Layout!.ContentId = f.Name;
                });

            }
        }
        void IMenuItemClient.AddStaticMenus(IMenuItemServer s)
        {
            if (_menuCreate != null) s.Add(RootMenusID.NShow, new[] { _menuCreate, });
        }
    }
    /// <summary>
    /// Логика взаимодействия для FormBase.xaml
    /// </summary>
    [RegService(null)]
    public partial class FormBase : DockUserControl
    {
        public FormBase()
        {

            InitializeComponent();
            NClear.Click += NClear_Click;
            NFreeze.Click += NFreeze_Click;
            NClose.Click += NClose_Click;
            this.Loaded += new RoutedEventHandler(FormBase_Loaded);
            this.Unloaded += new RoutedEventHandler(FormBase_Unloaded);
        }

        private void NClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void FormBase_Unloaded(object sender, RoutedEventArgs e)
        {
           // throw new NotImplementedException();
        }

        private void FormBase_Loaded(object sender, RoutedEventArgs e)
        {
          //  throw new NotImplementedException();
        }

        private void NFreeze_Click(object sender, RoutedEventArgs e)
        {
          //  throw new NotImplementedException();
        }
        private void NClear_Click(object sender, RoutedEventArgs e)
        {
           data.Inlines.Clear();
        }
    }
}
