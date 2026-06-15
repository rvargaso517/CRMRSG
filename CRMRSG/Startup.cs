using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(CRMRSG.Startup))]

namespace CRMRSG
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Este comando mágico es el que crea la ruta /signalr/hubs que te está dando 404
            app.MapSignalR();
        }
    }
}