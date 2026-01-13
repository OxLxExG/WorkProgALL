using Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace HorizontDrilling
{
    using Global;
    using HorizontDrilling.Models;
    using Properties;
    using System.IO;

    record GlobalCulture(string Culture)
    {
        public GlobalCulture() : this("en-US") { }
    }
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, IServiceProvider, IMenuKeyGestureService
    {
        private List<KeyBinding>? keyBindings = null;
        static GlobalCulture opt = new();
        IServiceProvider serviceProvider;        
        public App()
        {
            #region Handle Exceptions
            // optional: hooking up some more handlers
            // remember that you need to hook up additional handlers when 
            // logging from other dispatchers, shedulers, or applications
            //Dispatcher.UnhandledException +=
            //    (sender, args) => Dispatcher_OnUnhandledException(args);
            DispatcherUnhandledException += DispatcherOnUnhandledException;
            // this is the line you really want 
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            // optional: hooking up some more handlers
            // remember that you need to hook up additional handlers when 
            TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
            #endregion

            IServiceCollection services = new ServiceCollection();
            // создаем хост приложения
            IConfigurationRoot ConfRoot = new ConfigurationBuilder()
                 .SetBasePath(Directory.GetCurrentDirectory())
                 .AddJsonFile("appsettings.json")
                 .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
                 .Build();

            // язык
            services.AddSingleton<GlobalCulture>(opt);
            ConfRoot.GetSection("GlobalCulture").Bind(opt);


            System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo(opt.Culture);
            System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo(opt.Culture);

            services.AddSingleton<IServiceProvider>(this);
            services.AddSingleton<IMenuKeyGestureService>(this);

            // регистрация сервисов библиотек
            RegServiceAttribute.RegisterForm = FormsRegistrator.RegFormAction;
            Type[] types = [    typeof(Core.ServiceRegister),
                                typeof(Communications.ServiceRegister),
                                //typeof(ExceptLog.ServiceRegister),
                                typeof(Loggin.ServiceRegister),
                                typeof(HorizontDrilling.Services.ServiceRegister),
                                typeof(WpfDialogs.ServiceRegister),
                              ];
            foreach (var type in types)
            {
                var r = (AbstractServiceRegister?)Activator.CreateInstance(type);
                r?.Register(ConfRoot, services);
            }

            serviceProvider = services.BuildServiceProvider();
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            #region Добаим ресурсы приложения к ресурсам ядра
            ToolTemplateSelector.Dictionary.MergedDictionaries.Add(new ResourceDictionary()
            {
                Source = new Uri("pack://application:,,,/Views/USO32/Uso32Dictionary.xaml")
            });
            MenuTemplateSelector.Dictionary.MergedDictionaries.Add(new ResourceDictionary()
            {
                Source = new Uri("pack://application:,,,/Views/MenusResource.xaml")
            });
            FormResource.Dictionary.MergedDictionaries.Add(new ResourceDictionary()
            {
                Source = new Uri("pack://application:,,,/Views/FormsResource.xaml")
            });
            #endregion

            MainWindow = serviceProvider.GetRequiredService<MainWindow>();

            // static menus
            var ms = serviceProvider.GetRequiredService<IMenuItemServer>();
            var madds = serviceProvider.GetRequiredService<IEnumerable<IMenuItemClient>>();
            foreach (var madd in madds) madd.AddStaticMenus(ms);

            // formVM generator
            var frs = serviceProvider.GetRequiredService<IEnumerable<IFormsRegistrator>>();
            foreach (var fr in frs) fr.Register();

            // статические кнопки меню
            if (keyBindings != null) foreach (var kb in keyBindings) MainWindow.InputBindings.Add(kb);
            keyBindings = null;

            // static autocreate windows
            // var lay = _host.Services.GetRequiredService<IEnumerable<IStaticContentClient>>();
            // _host.Services.GetRequiredService<ILayoutServer>().AddToLayout(lay);

            // todo load data (projects)
            // todo load data (projects data + layout)
            // load  dynamic menus
            // TODO if not find
            //var pe = _host.Services.GetRequiredService<ProjectsExplorer>();
            //pe.IsVisibleChanged += ((MainVindowVM)this.MainWindow.DataContext).DocIsVisibleChanged;
            //pe.AddToLayout(((IMainWindow)this.MainWindow).DockManager, Xceed.Wpf.AvalonDock.Layout.AnchorableShowStrategy.Left);

            //RootFileDocumentVM.InstanceFactory = opt.UseGroup? new GroupFactory() : new VisitFactory();

            // загрузка корневого файла проекта 
            try
            {
                var rot = Settings.Default.CurrentRoot;
                if (!string.IsNullOrEmpty(rot) && File.Exists(rot))
                {
                    ProjectFile.LoadRoot(rot);// RootFileDocumentVM.InstanceFactory!.LoadNew(rot) as RootFileDocumentVM;
                }
            }
            catch
            {
                Settings.Default.CurrentRoot = string.Empty;
                Settings.Default.Save();
                throw;
            }
            finally
            {
                MainWindow.Show();

                VMBaseForm.OnVisibleChange(null);

                base.OnStartup(e);
            }
            //var path = System.Configuration.ConfigurationManager.OpenExeConfiguration(
            //    System.Configuration.ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
        }
        // IServiceProvider
        public object? GetService(Type serviceType) => serviceProvider.GetService(serviceType);

        #region IMenuKeyGestureService
        private void Addkb(KeyBinding kb)
        {
            if (keyBindings == null) keyBindings = new();
            keyBindings.Add(kb);
        }
        public void Register(ICommand command, string Gesture)
        {
            var g = (KeyGesture)(new KeyGestureConverter()).ConvertFrom(Gesture)!;
            Addkb(new KeyBinding(command, g));
        }
        public void Register(string CommandPath, string Gesture)
        {
            var kb = new KeyBinding { Gesture = (KeyGesture)(new KeyGestureConverter()).ConvertFrom(Gesture)! };
            BindingOperations.SetBinding(
                kb,
                InputBinding.CommandProperty,
                new Binding { Path = new PropertyPath(CommandPath) }
            );
            Addkb(kb);
        }
        #endregion

        #region LogExceptions
        public static void LogError(Exception e, string message)
        {
            //using (logger?.BeginScope("SCOPE ROOT"))
            //{
            //    using (logger?.BeginScope("SCOPE APP"))
            //    {
            //        logger?.LogInformation("INFO exec : public static void LogError(Exception e, string message)");
            //    }
            //}
            if (opt.Culture == "ru-RU") e.UpdateMessage();
            var larg = e.GetExceptionExceptionLogFlags();
            if (!(e is CancelDialogException)) Log.Error(e, message);
            if (larg.Dialog)
            {
                string strError = opt.Culture == "ru-RU" ? "Ошибка" : "Error";
                string caption = ((strError + "- " + e.ToString()).Split(':')[0]).Replace('-', ':');
                MessageBox.Show(larg.LogStack ? e.ToString() : e.Message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private static void TaskSchedulerOnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
        {
            LogError(args.Exception, args.Exception.Message);
            args.SetObserved();
        }

        //private static void CurrentOnDispatcherUnhandledException(DispatcherUnhandledExceptionEventArgs args)
        //{
        //    LogError(args.Exception, args.Exception.Message);
        //    args.Handled = true;
        //}

        private static void Dispatcher_OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
        {
            LogError(args.Exception, args.Exception.Message);
            args.Handled = true;
        }
        private static void DispatcherOnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
        {
            LogError(args.Exception, args.Exception.Message);
            args.Handled = true;
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            var exception = args.ExceptionObject as Exception;
            var terminatingMessage = args.IsTerminating ? " The application is terminating." : string.Empty;
            var exceptionMessage = exception?.Message ?? "An unmanaged exception occured.";
            var message = string.Concat(exceptionMessage, terminatingMessage);
            LogError(exception!, message);
        }
        #endregion
    }
}
