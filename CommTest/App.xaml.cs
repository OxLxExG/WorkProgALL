using ExceptionExtensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using Core;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace SerialPortTest
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, IServiceProvider
    {
        //const string culture = "en-US"; //Where this is the culture you want to use
        public static GlobalSettings opt = new(); //Where this is the culture you want to use
        public static ServiceCollection services = new();
        public static ServiceProvider? serviceProvider { get; private set; }
        public static ILogger? logger;
        // public static ILogger? textBloklog;

        public object? GetService(Type serviceType) => serviceProvider?.GetService(serviceType);

        protected override void OnStartup(StartupEventArgs e)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            services.AddSingleton<GlobalSettings>(opt);
            configuration.GetSection("GlobalSettings").Bind(opt);

            System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo(opt.Culture);
            System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo(opt.Culture);

            //services.AddSingleton<IServiceProvider>(this);



            //services.AddSingleton<ILogTextBlockService, LogTextBlockService>();

            //services.AddTransient<Loader>();
            //services.Configure<LoadSettings>((opt) => configuration.GetSection("Loader").Bind(opt));
            //var bld = services.BuildServiceProvider();
            //Settings = bld.GetRequiredService<Loader>().LoadSettings;

            //System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo(Settings.Culture);
            //System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo(Settings.Culture);

            //ExceptLog.ServicesRoot.Register(configuration, services, opt.Logging.Box.Trace);
            //ExceptLog.ServicesRoot.Register(configuration, services,
            //    opt.Logging.Box.Info, opt.Logging.Box.Trace, opt.Logging.Box.Error,
            //    opt.Logging.File.Info, opt.Logging.File.Trace, opt.Logging.File.Error);

            Communications.ServicesRoot.Register(services);

            //services.AddLogging(builder =>
            //{
            //    builder.AddConfiguration(configuration.GetSection("Logging"));
            //    if (Settings.BoxInfo) builder.AddTextBlock<InfoTextBlockLoggerProvider, TextBlockOptions, InfoTextBlockLoggerFormatter>();
            //    if (Settings.BoxTrace) builder.AddTextBlock<TraceTextBlockLoggerProvider, TextBlockOptions, TraceTextBlockLoggerFormatter>();
            //    if (Settings.BoxError) builder.AddTextBlock<ExcTextBlockLoggerProvider, TextBlockOptions, ExcTextBlockLoggerFormatter>();
            //    if (Settings.FileError) builder.AddFile<FileLoggerProvider>(configure: o => o.RootPath = AppContext.BaseDirectory);
            //    if (Settings.FileTrace) builder.AddFile<TraceFileLoggerProvider>(configure: o => o.RootPath = AppContext.BaseDirectory);
            //    if (Settings.FileInfo) builder.AddFile<InfoFileLoggerProvider>(configure: o => o.RootPath = AppContext.BaseDirectory);
            //    //.AddSimpleConsole()
            //    builder.SetMinimumLevel(LogLevel.Debug);
            //});

            serviceProvider = services.BuildServiceProvider();
            logger = serviceProvider.GetRequiredService<ILogger<App>>();
            RegisterGlobalExceptionHandling();

            base.OnStartup(e);
        }

        private void RegisterGlobalExceptionHandling()
        {
            // this is the line you really want 
            AppDomain.CurrentDomain.UnhandledException +=
                (sender, args) => CurrentDomainOnUnhandledException(args);

            // optional: hooking up some more handlers
            // remember that you need to hook up additional handlers when 
            // logging from other dispatchers, shedulers, or applications

            //   Current.Dispatcher.UnhandledException +=
            //       (sender, args) => DispatcherOnUnhandledException(args, log);

            Current.DispatcherUnhandledException +=
                (sender, args) => CurrentOnDispatcherUnhandledException(args);

            TaskScheduler.UnobservedTaskException +=
                (sender, args) => TaskSchedulerOnUnobservedTaskException(args);
        }

        public static void LogError(Exception e, string message)
        {
            //using (logger?.BeginScope("SCOPE ROOT"))
            //{
            //    using (logger?.BeginScope("SCOPE APP"))
            //    {
            //        logger?.LogInformation("INFO exec : public static void LogError(Exception e, string message)");
            //    }
            //}
            if (App.opt.Culture == "ru-RU") e.UpdateMessage();
            var larg = e.GetExceptionExceptionLogFlags();
            logger?.LogError((int)larg, e, message);
            if (larg.Dialog)
            {
                string strError = SerialPortTest.Properties.Resources.strError;
                string caption = ((strError + "- " + e.ToString()).Split(':')[0]).Replace('-', ':');
                MessageBox.Show(larg.LogStack ? e.ToString() : e.Message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private static void TaskSchedulerOnUnobservedTaskException(UnobservedTaskExceptionEventArgs args)
        {
            LogError(args.Exception, args.Exception.Message);
            args.SetObserved();
        }

        private static void CurrentOnDispatcherUnhandledException(DispatcherUnhandledExceptionEventArgs args)
        {
            LogError(args.Exception, args.Exception.Message);
            args.Handled = true;
        }

        //    private static void DispatcherOnUnhandledException(DispatcherUnhandledExceptionEventArgs args, ILogger log)
        //    {
        //        MessageBox.Show(args.Exception.Message, $"Ошибка {args.Exception}".Split(':')[0], MessageBoxButton.OK, MessageBoxImage.Error);
        ////        log.LogError(args.Exception, args.Exception.Message);
        //        args.Handled = true;
        //    }

        private static void CurrentDomainOnUnhandledException(UnhandledExceptionEventArgs args)
        {
            var exception = args.ExceptionObject as Exception;
            var terminatingMessage = args.IsTerminating ? " The application is terminating." : string.Empty;
            var exceptionMessage = exception?.Message ?? "An unmanaged exception occured.";
            var message = string.Concat(exceptionMessage, terminatingMessage);
            LogError(exception!, message);
        }
    }
}
