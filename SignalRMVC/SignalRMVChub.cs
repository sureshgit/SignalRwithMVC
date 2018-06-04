using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace InternetNow
{
    public class SignalRMVCHub : Hub
    {
        public void Hello()
        {
            Clients.All.hello();
        }

        public void UpdateCount()
        {
            Clients.All.UpdateCount(1);
        }
    }
}