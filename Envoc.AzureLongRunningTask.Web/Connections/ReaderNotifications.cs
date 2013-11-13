using Microsoft.AspNet.SignalR;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Envoc.AzureLongRunningTask.Web.Connections
{
    public class ReaderNotifications : PersistentConnection
    {
        private static readonly object ConnectionSync = new object();
        private static readonly Dictionary<string, List<string>> Connections = new Dictionary<string, List<string>>();

        public static void SendMessage(string userId, ClientNotification message)
        {
            var context = GlobalHost.ConnectionManager.GetConnectionContext<ReaderNotifications>();
            var connection = context.Connection;
            var ids = GetConnectionIds(userId);
            foreach (var id in ids)
            {
                connection.Send(id, message);
            }
        }
        
        protected override Task OnConnected(IRequest request, string connectionId)
        {
            AddConnection(request, connectionId);
            return base.OnConnected(request, connectionId);
        }

        protected override Task OnReconnected(IRequest request, string connectionId)
        {
            AddConnection(request, connectionId);
            return base.OnReconnected(request, connectionId);
        }

        protected override Task OnDisconnected(IRequest request, string connectionId)
        {
            RemoveConnection(request, connectionId);
            return base.OnDisconnected(request, connectionId);
        }

        private static List<string> GetConnectionIds(string userId)
        {
            lock (ConnectionSync)
            {
                return Connections.ContainsKey(userId) ? Connections[userId].ToList() : new List<string>();
            }
        }

        private static void RemoveConnection(IRequest request, string connectionId)
        {
            if (request == null || request.User == null || string.IsNullOrEmpty(request.User.Identity.Name))
            {
                return;
            }

            var userId = request.User.Identity.Name;

            lock (ConnectionSync)
            {
                if (!Connections.ContainsKey(userId))
                {
                    return;
                }

                Connections[userId].Remove(connectionId);
            }
        }

        private static void AddConnection(IRequest request, string connectionId)
        {
            if (request == null || request.User == null || string.IsNullOrEmpty(request.User.Identity.Name))
            {
                return;
            }
            var userId = request.User.Identity.Name;
            lock (ConnectionSync)
            {
                if (Connections.ContainsKey(userId) && !Connections[userId].Contains(connectionId))
                {
                    Connections[userId].Add(connectionId);
                }

                if (!Connections.ContainsKey(userId))
                {
                    Connections[userId] = new List<string> { connectionId };
                }
            }
        }
    }

    public class ClientNotification
    {
        public string Type { get; set; }
        public object Content { get; set; }
    }
}