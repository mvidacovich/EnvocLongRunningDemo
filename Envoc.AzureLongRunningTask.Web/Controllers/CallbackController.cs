using Envoc.AzureLongRunningTask.Web.Services;
using System;
using System.Web.Mvc;

namespace Envoc.AzureLongRunningTask.Web.Controllers
{
    public class CallbackController : Controller
    {
        private readonly ImageProcessService processService;

        public CallbackController(ImageProcessService processService)
        {
            this.processService = processService;
        }

        [HttpPost]
        public ActionResult FinishUpload(string apikey, string resultpath, Guid requestid)
        {
            var job = processService.CompleteJob(requestid, apikey, resultpath);
            if (job == null)
            {
                //ISSUE: this says we accepted the payload, which really isn't true. 
                return new EmptyResult();
            }

            //ISSUE: passing in the user ID is cheap. we could have a custom authorizer. I'm lazy
            NotificationHub.SendNotification(job.UserId, new ClientNotification
            {
                Type = "Completed",
                Content = new
                {
                    job.RequestId,
                    ResultUrl = processService.GetResultUrl(job)
                }
            });
            return new EmptyResult();
        }
    }
}