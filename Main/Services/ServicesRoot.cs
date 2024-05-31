using Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Main.ViewModels;
using Main.Views;

namespace Main.Services
{
    internal static class ServicesRoot
    {
        public static void Register(IServiceCollection services, GlobalSettings opt) 
        {
            services.AddSingleton<MainWindow>();
            services.AddSingleton<IDockManagerSerialization, MainWindow>(sp=> (MainWindow) Application.Current.MainWindow);

            services.AddSingleton<MainVindowVM>();

            //services.AddSingleton<IFormsServer>(sp => DockManagerVM.Instance);

            services.AddSingleton<MenuVM>();

            if (opt.UseGroup)
            {
                services.AddSingleton<LastGroupMenuVM>();
                services.AddSingleton<LastVisitMenuVM>();
            }
            else services.AddSingleton<LastSingleVisitMenuVM>();
            services.AddSingleton<LastFileMenuVM>();

            if (opt.UseGroup) services.AddTransient<IMenuItemClient, ProjectsGroupMenuFactory>();
            else services.AddTransient<IMenuItemClient, ProjectsSingleMenuFactory>();
            services.AddTransient<VisitsGroupVM>();
            services.AddTransient<ISaveFilesDialog, ServiceSaveDialog>();
            services.AddTransient<ICreateNewVisitDialog, CreateNewVisitDialog>();

            //services.AddTransient<MenuItemVM>();
            //services.AddTransient<CommandMenuItemVM>();
            //services.AddTransient<OnSubMenuOpenMenuItemVM>();

            services.AddTransient<IMenuItemClient, ProjectsExplorerMenuFactory>();
            services.RegisterForm<ProjectsExplorerVM>();


            if (opt.Logging.Box.Error)
            {
                services.RegisterForm<ExceptLogVM>();
                services.AddTransient<IMenuItemClient, ExceptLogMenuFactory>();
            }
            if (opt.Logging.Box.Trace)
            {
                services.RegisterForm<TraceLogVM>();
                services.AddTransient<IMenuItemClient, TraceLogMenuFactory>();
            }
            if (opt.Logging.Box.Info)
            {
                services.RegisterForm<InfoLogVM>();
                services.AddTransient<IMenuItemClient, InfoLogMenuFactory>();
            }
            // services.AddTransient<IStaticContentClient,ProjectsExplorerFactory>();
        }
    }
}
