using CommunityToolkit.Mvvm.Input;
using Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Main.ViewModels
{
    public class ProjectsExplorerMenuFactory : IMenuItemClient
    {
        private static void SetBinding(PriorityItemBase item)
        {
            RootFileDocumentVM.StaticPropertyChanged += (o, e) => item.IsEnable = RootFileDocumentVM.Instance != null;
        }
        void IMenuItemClient.AddStaticMenus(IMenuItemServer mis)
        {
            IToolServer ts = VMBase.ServiceProvider.GetRequiredService<IToolServer>();

            var tb = new ToolButton
            {
                IsEnable = false,
                ToolTip = new ToolTip { Content = $"{Properties.Resources.m_Show} {Properties.Resources.tProjectExplorer}" },
                ContentID = "CidShowProjectsExplorerT",
                IconSource = "pack://application:,,,/Images/Project.PNG",
                Priority = -1000,
                Command = new RelayCommand(ProjectsExplorerVM.Show)
            };
            var tm = new CommandMenuItemVM
            {
                IsEnable = false,
                ContentID = "CidShowProjectsExplorerM",
                InputGestureText = "Ctrl+R",
                Header = Properties.Resources.tProjectExplorer,
                IconSource = "pack://application:,,,/Images/Project.PNG",
                Priority = 0,
                Command = new RelayCommand(ProjectsExplorerVM.Show)
            };
            SetBinding(tb);

            SetBinding(tm);

            ts.Add("ToolGlyph", tb);

            mis.Add(RootMenusID.NShow, tm);
        }
    }
    //public class ProjectsExplorerFactory //: IStaticContentClient
    //{
    //    private readonly IServiceProvider _serviceProvider;
    //    public ProjectsExplorerFactory(IServiceProvider sp) => _serviceProvider = sp;
    //    public object Content => _serviceProvider.GetRequiredService<ProjectsExplorer>();
    //    public ShowStrategy? AnchorableShowStrategy => ShowStrategy.Left;        
    //}
    public class ProjectsExplorerVM : ToolVM
    {
        public static void Show() => DockManagerVM.AddOrGetandShow(nameof(ProjectsExplorerVM), FormAddedFrom.User);

        public ProjectsExplorerVM()
        {

            Title = Properties.Resources.tProjectExplorer;
            IconSource = new Uri("pack://application:,,,/Images/Project16.png");
            CanFloat = false;
            CanDockAsTabbedDocument = false;
            CanClose = false;
            ShowStrategy = Core.ShowStrategy.Left;  
            ContentID = nameof(ProjectsExplorerVM);
            ToolTip = Properties.Resources.tProjectExplorer;
        }
    }
}
