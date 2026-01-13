using CommunityToolkit.Mvvm.Input;
using Core;
using Global;

namespace WpfDialogs
{

    /// <summary>
    /// Логика взаимодействия для DocRoot.xaml
    /// </summary>
    [RegService(typeof(IMenuItemClient), IsSingle: false)]
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
