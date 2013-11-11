using Envoc.AzureLongRunningTask.Common.Models;
using Envoc.AzureLongRunningTask.Web.Connections;
using Envoc.AzureLongRunningTask.Web.Models;
using Envoc.AzureLongRunningTask.Web.Services;
using System;
using System.Web;
using System.Web.Mvc;

namespace Envoc.AzureLongRunningTask.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ImageUploadService imageUploadService;
        private readonly ImageProcessService processService;

        public HomeController(ImageUploadService imageUploadService, ImageProcessService processService)
        {
            this.imageUploadService = imageUploadService;
            this.processService = processService;
        }

        // If you want to actually get here: http://stackoverflow.com/questions/4762538/iis-express-windows-authentication
        [Authorize]
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public string UploadFile(HttpPostedFileBase file, string id, string name, int? chunk, int? chunks)
        {
            if (chunks.HasValue && chunks > 512 || chunk.HasValue && chunk >= 512)
            {
                throw new NotSupportedException("The file is too large.");
            }

            var blockUpload = new BlockUpload
            {
                UploadId = id,
                FileName = name,
                BlockIndex = chunk.GetValueOrDefault(),
                TotalChunks = chunks.GetValueOrDefault(1),
                InputStream = file.InputStream,
                UserId = User.Identity.Name
            };

            var result = imageUploadService.Process(blockUpload);

            if (result.Completed)
            {
                processService.CreateNewJobFor(result.RelatedRequest, Url.Action("FinishUpload", null, null, "http"));
            }

            return result.RelatedRequest.RequestId.ToString();
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
            ReaderNotifications.SendMessage(job.UserId, new ClientNotification
            {
                Type = "Completed",
                Content = new
                {
                    job.UploadId,
                    ResultUrl = processService.GetResultUrl(job)
                }
            });
            return new EmptyResult();
        }
    }
}
