using Global;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace WpfDialogs
{
    public class ServiceRegister : AbstractServiceRegister
    {
        public override void Register(IConfiguration context, IServiceCollection services)
        {
            RegisterServicesFormAttr(Assembly.GetExecutingAssembly(), context, services);
            //services.AddTransient<FormBase>();
            //services.AddSingleton<IMenuItemClient, FormBaseFactory>();
            //services.AddSingleton<IMenuItemClient, DocRoot>();
        }
    }
}
