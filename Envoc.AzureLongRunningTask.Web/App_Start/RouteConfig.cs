using System.Web.Mvc;
using System.Web.Routing;
using Envoc.AzureLongRunningTask.Web.Connections;

namespace Envoc.AzureLongRunningTask.Web.App_Start
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // SignalR support
            RouteTable.Routes.MapConnection<ReaderNotifications>("readernotifications", "/readernotifications");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}