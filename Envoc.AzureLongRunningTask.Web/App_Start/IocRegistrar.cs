using System.Collections.Generic;
using System.Reflection;
using System.Web.Mvc;
using Envoc.AzureLongRunningTask.AzureCommon.Persistance;
using Envoc.AzureLongRunningTask.AzureCommon.Persistance.Blob;
using Envoc.AzureLongRunningTask.AzureCommon.Persistance.Queues;
using Envoc.AzureLongRunningTask.Web.Models;
using Envoc.AzureLongRunningTask.Web.Services;
using SimpleInjector;
using SimpleInjector.Extensions;
using SimpleInjector.Integration.Web;
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

            var lifestyle = new WebRequestLifestyle();
            container.Register(() => new AzureContext(), lifestyle);
            container.RegisterOpenGeneric(typeof(IQueueContext<>), typeof(QueueContext<>), lifestyle);
            container.RegisterOpenGeneric(typeof(IStorageContext<>), typeof(StorageContext<>), lifestyle);
            container.RegisterOpenGeneric(typeof(IList<>), typeof(StaticList<>), lifestyle);
            container.Register<ImageProcessService>();
            container.Register<ImageUploadService>();

            container.Verify();
            DependencyResolver.SetResolver(new SimpleInjectorDependencyResolver(container));
        }
    }
}