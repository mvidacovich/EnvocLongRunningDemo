using Envoc.Azure.Common.Persistance.Queues;
using Envoc.AzureLongRunningTask.Common.Models;
using Envoc.AzureLongRunningTask.Web.Models;
using Envoc.Common.Data;
using System.Security.Principal;

namespace Envoc.AzureLongRunningTask.Web.Services
{
    public class ImageProcessService
    {
        private readonly IQueueContext<ProcessImageJob> queueContext;

        public ImageProcessService(IQueueContext<ProcessImageJob> queueContext, IRepository<ProcessRequest> processRequest)
        {
            this.queueContext = queueContext;
        }

        public void CreateNewJobFor(IPrincipal user, ProcessImageJob processImageJob)
        {
            // Obviously you want to use cryptographically secure RNG here
            processImageJob.ApiKey = "super secret key, shhh";
            queueContext.Enqueue(processImageJob);
        }
    }
}