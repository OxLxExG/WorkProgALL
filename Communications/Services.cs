using Connections.Interface;
using Microsoft.Extensions.DependencyInjection;
using Global;
using System.Text;
using Serilog;
using Microsoft.Extensions.Configuration;

namespace Communications
{
    #region IConnectionServer
    internal class ConectionItem
    {
        public List<WeakReference> Subscrubers = new List<WeakReference>();
        public IAbstractConnection conection { get; }
        public string Id { get; }
        public ConectionItem(IAbstractConnection con, string id, object Subscruber)
        {
            conection = con;
            Id = id;
            Subscrubers.Add(new WeakReference(Subscruber));
        }
        public void Add(object Subscruber)
        {
            var s = Subscrubers.FirstOrDefault(w => w.Target == Subscruber);
            if (s == null) Subscrubers.Add(new WeakReference(Subscruber));
        }
    }

    //[RegService(typeof(IConnectionServer), IsSingle:false)]
    public class ConnectionCash: IConnectionServer
    {
        static private List<ConectionItem> conections = new ();

        static public ILogger? logger { get; set; }

        public IAbstractConnection? Get(string ConnectionID, object Subscruber)
        {
            ConectionItem? f = conections.FirstOrDefault(c => c.Id == ConnectionID);
            if ( f != null)
            {
                f.Add(Subscruber);
            }
            conections.RemoveAll(c =>
            {
                c.Subscrubers.RemoveAll(w => w.Target == null);
                return c.Subscrubers.Count == 0;
            });
            return f?.conection;
        }

        public IAbstractConnection? Get(string ConnectionID)
        {
            return conections.FirstOrDefault(c => c.Id == ConnectionID)?.conection;
        }

        public void Set(string ConnectionID, IAbstractConnection Connection, object Subscruber)
        {
            if (Get(ConnectionID, Subscruber) != null) 
                throw new FlagsArgumentOutOfRangeException("ConnectionID", ConnectionID, "Used");
            conections.Add(new ConectionItem(Connection, ConnectionID, Subscruber));

            if (logger != null)
            {
                foreach (var c in conections)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var item in c.Subscrubers)
                    {
                        sb.Append(item.ToString());
                    }
                    logger.Debug("{} = {}", c.Id, sb);
                }
            }
        }
    }
    #endregion

    public class ServiceRegister: AbstractServiceRegister
    {
        public override void Register(IConfiguration context, IServiceCollection services)
        {
            services.AddTransient<IConnectionServer, ConnectionCash>();
        }
    }
}
