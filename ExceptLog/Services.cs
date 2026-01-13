using Global;
using FileLogging;
using Karambolo.Extensions.Logging.File;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;
using TextBlockLogging;

namespace ExceptLog
{
    public class ServiceRegister: AbstractServiceRegister
    {
        public override void Register(HostBuilderContext context, IServiceCollection services) 
        {
            var settings = context.Configuration.GetSection("StdLoggs").Get<StdLoggs>();
            if (settings != null) services.AddLogging(builder =>
            {
                services.AddSingleton<StdLoggs>(settings);
                builder.AddConfiguration(context.Configuration.GetSection("Logging"));
                if (settings.Box.Info) builder.AddTextBlock<InfoTextBlockLoggerProvider, TextBlockOptions, InfoTextBlockLoggerFormatter>();
                if (settings.Box.Trace) builder.AddTextBlock<TraceTextBlockLoggerProvider, TextBlockOptions, TraceTextBlockLoggerFormatter>();
                if (settings.Box.Error) builder.AddTextBlock<ExcTextBlockLoggerProvider, TextBlockOptions, ExcTextBlockLoggerFormatter>();
                if (settings.File.Error) builder.AddFile<FileLoggerProvider>(configure: o => o.RootPath = AppContext.BaseDirectory);
                if (settings.File.Trace) builder.AddFile<TraceFileLoggerProvider>(configure: o => o.RootPath = AppContext.BaseDirectory);
                if (settings.File.Info) builder.AddFile<InfoFileLoggerProvider>(configure: o => o.RootPath = AppContext.BaseDirectory);
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            services.AddSingleton<ILogTextBlockService, LogTextBlockService>();
        }
    }
}
