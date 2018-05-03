using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebRemote
{
    public class WebRemoteHub : Hub
    {
        public override Task OnConnected()
        {
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            return base.OnDisconnected(stopCalled);
        }

        public override Task OnReconnected()
        {
            return base.OnReconnected();
        }
    }

    public class WebRemoteClients
    {
        private IHubContext _hubContext = GlobalHost.ConnectionManager.GetHubContext<WebRemoteHub>();

        public static void SendScreenCapture()
        {
        }
    }
}
