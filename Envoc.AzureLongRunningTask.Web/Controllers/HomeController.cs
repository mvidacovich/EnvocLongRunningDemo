using Envoc.AzureLongRunningTask.Common.Models;
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

            var request = imageUploadService.Process(blockUpload);

            if (request.Completed)
            {
                processService.CreateNewJobFor(new ProcessImageJob
                {
                    FilePath = request.FilePath,
                    PostbackUrl = Url.Action("FinishUpload"),
                    RequestId = request.RequestId
                });
            }

            return request.RequestId.ToString();
        }

        [HttpPost]
        public ActionResult FinishUpload()
        {
            throw new NotImplementedException();
        }
    }
}
