using CommunityToolkit.Mvvm.Input;
using Core;
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
using Xceed.Wpf.AvalonDock.Layout;

namespace WpfDialogs
{
    
    /// <summary>
    /// Логика взаимодействия для DocRoot.xaml
    /// </summary>
    public partial class DocRoot : DockUserControl, IMenuItemClient
    {
        static CommandMenuItemVM? _menuCreate;
        public DocRoot()
        {
            if (_menuCreate == null)
            {
                _menuCreate = new CommandMenuItemVM();
                _menuCreate.ContentID = "globalDocRoot";
                _menuCreate.Header = "BLA 0";
                _menuCreate.Priority = 2000;
                _menuCreate.Command = new RelayCommand(() => DockManagerVM.AddOrGetandShow(nameof(ToolVM), FormAddedFrom.User
            //                {
            //VMBaseForms.CreateAndShow(nameof(ToolVM), () => new ToolVM 
            //{ 
            //    Title = "bla0", 
            //    ContentID = nameof(ToolVM) 
            //});
            //    }
            ));      
            }
            InitializeComponent();
        }
        void IMenuItemClient.AddStaticMenus(IMenuItemServer _menuItemServer)
        {
            _menuItemServer.Add(RootMenusID.NShow, new[]{ DocRoot._menuCreate!, });
        }
    }
}
