using Global;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Reflection;

namespace Loggin
{
    public record StdLogg(bool Error, bool Info, bool Trace, bool Monitor)
    {
        public StdLogg() : this(true, false, false, false) { }
    }
    [RegService(typeof(StdLoggs))]
    public record StdLoggs(StdLogg Box, StdLogg File)
    {
        public StdLoggs() : this(new(), new()) { }
    }

    public class ServiceRegister : AbstractServiceRegister
    {
        public override void Register(IConfiguration context, IServiceCollection services)
        {
            RegisterServicesFormAttr(Assembly.GetExecutingAssembly(), context, services);

            var settings = context.GetSection("StdLoggs").Get<StdLoggs>() ?? new StdLoggs();

            services.AddSingleton<StdLoggs>(settings);

            var lcError = new LoggerConfiguration();

            lcError.MinimumLevel.ControlledBy(Logger.ErrorLevel);

            lcError.WriteTo.File("logs/error.log",
                                rollingInterval: RollingInterval.Day,
                                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error);

            if (settings.Box.Error)
            {
                lcError.WriteTo.RichTextBox(
                    LogBoxContainer.GetOrCteate("ExceptLogVM"));

            }
            Log.Logger = lcError.CreateLogger();

            if (settings.Box.Info || settings.File.Info)
            {
                var lcInfo = new LoggerConfiguration();

                lcInfo.MinimumLevel.ControlledBy(Logger.InfoLevel);

                if (settings.File.Info)
                {
                    lcInfo.WriteTo.File("logs/info.log",
                                        rollingInterval: RollingInterval.Day,
                                        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information);
                }
                if (settings.Box.Info)
                {
                    lcInfo.WriteTo.RichTextBox(
                    LogBoxContainer.GetOrCteate("InfoLogVM"));
                }
                Logger.Info = lcInfo.CreateLogger();
            }

            if (settings.Box.Trace)
            {
                Logger.Trace = new LoggerConfiguration()
                    .MinimumLevel.ControlledBy(Logger.TraceLevel)
                    .WriteTo.RichTextBox(LogBoxContainer.GetOrCteate("TraceLogVM")).MinimumLevel.Verbose()
                    .CreateLogger();
            }
        }
    }
}
