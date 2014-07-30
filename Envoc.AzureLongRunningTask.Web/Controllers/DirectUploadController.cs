using Envoc.AzureLongRunningTask.Web.Models;
using Envoc.AzureLongRunningTask.Web.Services;
using System;
using System.Web;
using System.Web.Mvc;

namespace Envoc.AzureLongRunningTask.Web.Controllers
{
    [Authorize]
    public class DirectUploadController : Controller
    {
        private readonly ImageUploadService imageUploadService;
        private readonly ImageProcessService processService;

        public DirectUploadController(ImageUploadService imageUploadService, ImageProcessService processService)
        {
            this.imageUploadService = imageUploadService;
            this.processService = processService;
        }

        [HttpPost]
        public string UploadFile(HttpPostedFileBase file, string id, string name, int? chunk, int? chunks)
        {
            if (chunks.HasValue && chunks >= 2048 || chunk.HasValue && chunk >= 2047)
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

            // Send block to azure
            var result = imageUploadService.Process(blockUpload);

            if (result.Completed)
            {
                // Add job to azure queue
                processService.CreateNewJobFor(result.RelatedRequest, Url.Action("FinishUpload", "Callback", null, "http"));
            }

            return result.RelatedRequest.RequestId.ToString();
        }
    }
}