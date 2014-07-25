using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace Envoc.AzureLongRunningTask.Web
{
    public class NotificationHub : Hub
    {
        internal static void SendNotification(string userId, ClientNotification message)
        {
            GlobalHost.ConnectionManager.GetHubContext<NotificationHub>().Clients.User(userId).process(message);
        }
    }

    public class ClientNotification
    {
        public string Type { get; set; }
        public object Content { get; set; }
    }
}