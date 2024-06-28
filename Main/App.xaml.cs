using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using TextBlockLogging;
using Core;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Windows;
using ExceptionExtensions;
using System.Text.RegularExpressions;
using Main.ViewModels;
using System.Collections;
using Main.Views.Pane;
using System.Windows.Data;
using System.Windows.Input;

namespace Main
{
    using Connections;
    using Main.Models;
    using Properties;
    using System.ComponentModel;
    using System.IO;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, IServiceProvider, IMenuKeyGestureService
    {
        //public AbstractConnection AbstractConnection { get; set; } = new SerialConn() { PortName = "COM1" };
        private static  IHost _host = null!;
        private static ILogger? _logger;
        private static GlobalSettings opt = new();
        private List<KeyBinding>? keyBindings = null;
        public App() 
        {
            #region Handle Exceptions
            // optional: hooking up some more handlers
            // remember that you need to hook up additional handlers when 
            // logging from other dispatchers, shedulers, or applications
            //Dispatcher.UnhandledException +=
            //    (sender, args) => Dispatcher_OnUnhandledException(args);
            DispatcherUnhandledException +=
                (sender, args) => DispatcherOnUnhandledException(args);
            // this is the line you really want 
            AppDomain.CurrentDomain.UnhandledException +=
                (sender, args) => CurrentDomainOnUnhandledException(args);
            // optional: hooking up some more handlers
            // remember that you need to hook up additional handlers when 
            TaskScheduler.UnobservedTaskException +=
                (sender, args) => TaskSchedulerOnUnobservedTaskException(args);
            #endregion

            //TODO: load plugin 
            // RUN ServiceContainer.ADD

            // создаем хост приложения
            var hostbuilder = Host.CreateDefaultBuilder();
            // внедряем сервисы
            hostbuilder.ConfigureServices((context, services) =>
            {
                services.AddSingleton<GlobalSettings>(opt);
                context.Configuration.GetSection("GlobalSettings").Bind(opt);

                System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo(opt.Culture);
                System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo(opt.Culture);

                services.AddSingleton<IServiceProvider>(this);
                services.AddSingleton<IMenuKeyGestureService>(this);

                Core.ServicesRoot.Register(services);   
                ExceptLog.ServicesRoot.Register(context.Configuration, services, 
                    opt.Logging.Box.Info, opt.Logging.Box.Trace, opt.Logging.Box.Error,
                    opt.Logging.File.Info, opt.Logging.File.Trace, opt.Logging.File.Error);
                Communications.ServicesRoot.Register(services);
                WpfDialogs.ServicesRoot.Register(services);
                Services.ServicesRoot.Register(services,opt);

            //TODO: load plugin 
              //  ServiceContainer.Apply(services);
            });
            _host = hostbuilder.Build();
            _logger = _host.Services.GetRequiredService<ILogger<App>>();
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            MenuTemplateSelector.Dictionary.MergedDictionaries.Add(new ResourceDictionary()
            {
                Source = new Uri("pack://application:,,,/Views/MenusResource.xaml")
            });
            FormResource.Dictionary.MergedDictionaries.Add(new ResourceDictionary()
            {
                Source = new Uri("pack://application:,,,/Views/FormsResource.xaml")
            });
            _host.Start();

            MainWindow = _host.Services.GetRequiredService<MainWindow>();
            var ms = _host.Services.GetRequiredService<IMenuItemServer>();
            // static menus
            var madds = _host.Services.GetRequiredService< IEnumerable<IMenuItemClient>>();
            foreach ( var madd in madds ) madd.AddStaticMenus(ms);
            // formVM generator
            var frs = _host.Services.GetRequiredService<IEnumerable<IFormsRegistrator>>();
            foreach (var fr in frs) fr.Register();

            if (keyBindings !=  null) foreach ( var kb in keyBindings ) MainWindow.InputBindings.Add(kb);
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
        }
        protected override void OnExit(ExitEventArgs e)
        {
            _host.StopAsync();
            _host.Dispose();

            base.OnExit(e);
        }
        public object? GetService(Type serviceType)=> _host!.Services.GetService(serviceType);

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
            if (!(e is CancelDialogException)) _logger?.LogError((int)larg, e, message);
            if (larg.Dialog || _logger == null)
            {
                string strError = opt.Culture == "ru-RU" ? "Ошибка" : "Error";
                string caption = ((strError + "- " + e.ToString()).Split(':')[0]).Replace('-', ':');
                MessageBox.Show(larg.LogStack ? e.ToString() : e.Message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private static void TaskSchedulerOnUnobservedTaskException(UnobservedTaskExceptionEventArgs args)
        {
            LogError(args.Exception, args.Exception.Message);
            args.SetObserved();
        }

        //private static void CurrentOnDispatcherUnhandledException(DispatcherUnhandledExceptionEventArgs args)
        //{
        //    LogError(args.Exception, args.Exception.Message);
        //    args.Handled = true;
        //}

        private static void Dispatcher_OnUnhandledException(DispatcherUnhandledExceptionEventArgs args)
        {
            LogError(args.Exception, args.Exception.Message);
            args.Handled = true;
        }
        private static void DispatcherOnUnhandledException(DispatcherUnhandledExceptionEventArgs args)
        {
            LogError(args.Exception, args.Exception.Message);
            args.Handled = true;
        }

        private static void CurrentDomainOnUnhandledException(UnhandledExceptionEventArgs args)
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
