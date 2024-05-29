using Connections.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkProgMain.ViewModels
{
    public class ConnectionServer
    {
        public ConnectionServer() { }
    }
    internal class MonitorVM: TextLogVM
    {
        private IConnection _connection = null!;
        public required IConnection Connection 
        { get
            {
                //if (_connection == null)
                    return _connection;
            }
            set
            {
                _connection = value;
            } 
        }
    }
}
