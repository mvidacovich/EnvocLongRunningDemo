using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Envoc.AzureLongRunningTask.AzureCommon.Persistance;
using Envoc.AzureLongRunningTask.Web.Models;
using Envoc.AzureLongRunningTask.Web.Services;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Envoc.AzureLongRunningTask.Web.Controllers
{
    [Authorize]
    public class CorsUploadController : Controller
    {
        private readonly AzureContext storageCredentials;
        private readonly ImageUploadService uploadService;

        public CorsUploadController(AzureContext storageCredentials, ImageUploadService uploadService)
        {
            this.storageCredentials = storageCredentials;
            this.uploadService = uploadService;
        }

        [HttpGet]
        public string UploadUrl(string id, string name, long size)
        {
            var uploadChunk = new BlockUpload
            {
                UploadId = id,
                FileName = name,
                UserId = User.Identity.Name
            };

            // ISSUE: In production a user's internet goes down and incomplete files need to be cleaned up
            var path = uploadService.GetUploadPath(uploadChunk);
            return GetUrl(path);
        }

        private string GetUrl(string name)
        {
            var blobClient = storageCredentials.Account.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("FileBlob");
            var blob = container.GetBlockBlobReference(name);

            // ISSUE: 1 minute window may be too large or small - depending on use case
            var url = blob.Uri.AbsoluteUri + blob.GetSharedAccessSignature(new SharedAccessBlobPolicy
            {
                Permissions = SharedAccessBlobPermissions.Write,
                SharedAccessExpiryTime = DateTime.UtcNow.AddMinutes(1),
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-1)
            });

            return url;
        }
    }
}