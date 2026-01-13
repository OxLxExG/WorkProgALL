using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Global
{
    public enum AdvancedRegs
    {
        Not,
        Form
    }
    [AttributeUsage(AttributeTargets.Class)]
    public class RegServiceAttribute : Attribute
    {
        public RegServiceAttribute(Type? Interface, bool IsSingle = true, AdvancedRegs Advanced = AdvancedRegs.Not)
        {
            this.Interface = Interface;
            this.IsSingle = IsSingle;
            this.Advanced = Advanced;
        }
        public Type? Interface { get; }
        public bool IsSingle { get; }
        public AdvancedRegs Advanced { get; }

        public static Action<IConfiguration, IServiceCollection, Type, RegServiceAttribute>? RegisterForm { get; set; } = null;
    }

    public abstract class AbstractServiceRegister
    {
        public abstract void Register(IConfiguration context, IServiceCollection services);

        protected void RegisterServicesFormAttr(Assembly assembly, IConfiguration context, IServiceCollection services)
        {
            var types = assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.GetCustomAttributes(typeof(RegServiceAttribute), true).Any());
            foreach (var type in types)
            {
                var attr = type.GetCustomAttributes(typeof(RegServiceAttribute), true).FirstOrDefault() as RegServiceAttribute;
                if (attr != null)
                {
                    if (attr.Advanced == AdvancedRegs.Form)
                    {
                        if (RegServiceAttribute.RegisterForm == null) throw new ArgumentNullException(nameof(RegServiceAttribute.RegisterForm)); 
                        RegServiceAttribute.RegisterForm(context, services, type, attr);
                    }
                    else
                        Register(context, services, type, attr);
                }
            }
        }
        private void Register(IConfiguration context, IServiceCollection services, Type type, RegServiceAttribute attr)
        {
            var lt = attr.IsSingle ? ServiceLifetime.Singleton : ServiceLifetime.Transient;
            services.Add(new ServiceDescriptor(attr.Interface ?? type, type, lt));
        }
    }
}
