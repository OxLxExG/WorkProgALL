using Core;
using Global;
using HorizontDrilling.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Windows;
using Loggin;

namespace HorizontDrilling.Services
{
    public class ServiceRegister : AbstractServiceRegister
    {
        public override void Register(IConfiguration context, IServiceCollection services) 
        {
            RegisterServicesFormAttr(Assembly.GetExecutingAssembly(), context, services);

            //services.AddSingleton<MainWindow>();
            services.AddSingleton<IDockManagerSerialization, MainWindow>(sp=> (MainWindow) Application.Current.MainWindow);

            //services.AddSingleton<MainVindowVM>();

            //services.AddSingleton<IFormsServer>(sp => DockManagerVM.Instance);

            //services.AddSingleton<MenuVM>();

            //if (opt.UseGroup)
            //{
                //services.AddSingleton<LastGroupMenuVM>();
              //  services.AddSingleton<LastVisitMenuVM>();
            //}
            //else services.AddSingleton<LastSingleVisitMenuVM>();

            //services.AddSingleton<LastFileMenuVM>();

            //services.AddTransient<IMenuItemClient, ProjectsGroupMenuFactory>();
            //services.AddTransient<IMenuItemClient, ThemeFactory>();
            //services.AddTransient<IMenuItemClient, ProjectsSingleMenuFactory>();
            //services.AddTransient<VisitsGroupVM>();
            //services.AddTransient<ISaveFilesDialog, ServiceSaveDialog>();
            //services.AddTransient<ICreateNewVisitDialog, NewVisitDialog>();

            //services.AddTransient<MenuItemVM>();
            //services.AddTransient<CommandMenuItemVM>();
            //services.AddTransient<OnSubMenuOpenMenuItemVM>();

            //services.AddTransient<IMenuItemClient, ProjectsExplorerMenuFactory>();
            //services.RegisterForm<ProjectsExplorerVM>();

           // services.RegisterForm<OscUSO32VM>();
           // services.RegisterForm<MonitorVM>();

            var settings = context.GetSection("StdLoggs").Get<StdLoggs>();

            if (settings != null)
            {
                if (settings.Box.Error)
                {
                    services.RegisterForm<ExceptLogVM>();
                    services.AddTransient<IMenuItemClient, ExceptLogMenuFactory>();
                }
                if (settings.Box.Trace)
                {
                    services.RegisterForm<TraceLogVM>();
                    services.AddTransient<IMenuItemClient, TraceLogMenuFactory>();
                }
                if (settings.Box.Info)
                {
                    services.RegisterForm<InfoLogVM>();
                    services.AddTransient<IMenuItemClient, InfoLogMenuFactory>();
                }
            }
            // services.AddTransient<IStaticContentClient,ProjectsExplorerFactory>();
        }
    }
}
