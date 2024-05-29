//using Core;
using FileLogging;
using Karambolo.Extensions.Logging.File;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    public static class ServicesRoot
    {
        public static void Register(IConfiguration Configuration, IServiceCollection services, 
            bool BoxInfo, bool BoxTrace, bool BoxError,
            bool FileInfo, bool FileTrace, bool FileError)
        {
            services.AddLogging(builder =>
            {
                builder.AddConfiguration(Configuration.GetSection("Logging"));
                if (BoxInfo) builder.AddTextBlock<InfoTextBlockLoggerProvider, TextBlockOptions, InfoTextBlockLoggerFormatter>();
                if (BoxTrace) builder.AddTextBlock<TraceTextBlockLoggerProvider, TextBlockOptions, TraceTextBlockLoggerFormatter>();
                if (BoxError) builder.AddTextBlock<ExcTextBlockLoggerProvider, TextBlockOptions, ExcTextBlockLoggerFormatter>();
                if (FileError) builder.AddFile<FileLoggerProvider>(configure: o => o.RootPath = AppContext.BaseDirectory);
                if (FileTrace) builder.AddFile<TraceFileLoggerProvider>(configure: o => o.RootPath = AppContext.BaseDirectory);
                if (FileInfo) builder.AddFile<InfoFileLoggerProvider>(configure: o => o.RootPath = AppContext.BaseDirectory);
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            services.AddSingleton<ILogTextBlockService, LogTextBlockService>();
        }
    }
}
