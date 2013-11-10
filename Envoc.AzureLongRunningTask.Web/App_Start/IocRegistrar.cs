using Envoc.Azure.Common.Persistance;
using Envoc.Azure.Common.Persistance.Blob;
using Envoc.Azure.Common.Persistance.Queues;
using Envoc.AzureLongRunningTask.Web.Services;
using Envoc.Common.Data;
using SimpleInjector;
using SimpleInjector.Extensions;
using SimpleInjector.Integration.Web.Mvc;
using System.Reflection;
using System.Web.Mvc;

namespace Envoc.AzureLongRunningTask.Web.App_Start
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
            container.RegisterOpenGeneric(typeof(IRepository<>), typeof(InMemoryRepository<>));
            container.Register<ImageProcessService>();
            container.Register<ImageUploadService>();

            container.Verify();
            DependencyResolver.SetResolver(new SimpleInjectorDependencyResolver(container));
        }
    }
}