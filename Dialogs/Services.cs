using Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfDialogs
{
    public static class ServicesRoot
    {
        public static void Register(IServiceCollection services)
        {
            services.AddTransient<FormBase>();
            services.AddSingleton<IMenuItemClient, FormBaseFactory>();
            services.AddSingleton<IMenuItemClient, DocRoot>();
        }
    }
}
