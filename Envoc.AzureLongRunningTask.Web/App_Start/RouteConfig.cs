using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Envoc.AzureLongRunningTask.AzureCommon.Persistance;
using Envoc.AzureLongRunningTask.AzureCommon.Persistance.Blob;
using Envoc.AzureLongRunningTask.AzureCommon.Persistance.Queues;
using Envoc.AzureLongRunningTask.Web.Models;
using Envoc.AzureLongRunningTask.Web.Services;
using SimpleInjector;
using SimpleInjector.Extensions;
using SimpleInjector.Integration.Web.Mvc;

namespace Envoc.AzureLongRunningTask.Web
{
    public class IocRegistrar
    {
        public static void RegisterContainers()
        {
            var container = new Container();
            container.RegisterMvcControllers(Assembly.GetExecutingAssembly());
            container.RegisterMvcAttributeFilterProvider();

            container.Register(() => new AzureContext());
            container.RegisterOpenGeneric(typeof(IQueueContext<>), typeof(QueueContext<>));
            container.RegisterOpenGeneric(typeof(IStorageContext<>), typeof(StorageContext<>));
            container.RegisterOpenGeneric(typeof(IList<>), typeof(StaticList<>));
            container.Register<ImageProcessService>();
            container.Register<ImageUploadService>();

            container.Verify();
            DependencyResolver.SetResolver(new SimpleInjectorDependencyResolver(container));
        }
    }

    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
