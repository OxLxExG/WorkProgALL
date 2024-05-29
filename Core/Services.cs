using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public static class ServicesRoot
    {
        public static void Register(IServiceCollection services)
        {
            services.AddSingleton<IMenuItemServer, MenuServer>();
            services.AddSingleton<IToolServer, ToolServer>();
            services.AddTransient<IMessageBox, MsgBox>();
            services.AddTransient<IFileOpenDialog, FileOpenView>();
            services.AddTransient<IFileSaveDialog, FileSaveView>();
        }
    }
}
