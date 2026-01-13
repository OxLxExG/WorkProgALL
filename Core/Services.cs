using Global;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public class ServiceRegister : AbstractServiceRegister
    {
        public override void Register(IConfiguration context, IServiceCollection services)
        {
            RegisterServicesFormAttr(Assembly.GetExecutingAssembly(), context, services);
            //services.AddSingleton<IMenuItemServer, MenuServer>();
            //services.AddSingleton<IToolServer, ToolServer>();
            //services.AddTransient<IMessageBox, MsgBox>();
            //services.AddTransient<IFileOpenDialog, FileOpenView>();
            //services.AddTransient<IFileSaveDialog, FileSaveView>();
        }
    }
}
