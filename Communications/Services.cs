using Connections.Interface;
using ExceptionExtensions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;

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
    public class ConnectionCash: IConnectionServer
    {
        static private List<ConectionItem> conections = new ();

        public IAbstractConnection? Get(string ConnectionID, object Subscruber)
        {
            var f = conections.FirstOrDefault(c => c.Id == ConnectionID);
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

        public void Set(string ConnectionID, IAbstractConnection Connection, object Subscruber)
        {
            if (Get(ConnectionID, Subscruber) != null) 
                throw new FlagsArgumentOutOfRangeException("ConnectionID", ConnectionID, "Used");
            conections.Add(new ConectionItem(Connection, ConnectionID, Subscruber));
        }
    }
    #endregion

    public static class ServicesRoot
    {
        public static void Register(IServiceCollection services)
        {
            services.AddTransient<IConnectionServer, ConnectionCash>();
        }
    }
}
