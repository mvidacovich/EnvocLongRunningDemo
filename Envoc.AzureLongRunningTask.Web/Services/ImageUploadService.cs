using Envoc.Azure.Common.Persistance.Blob;
using Envoc.AzureLongRunningTask.Common.Models;
using Envoc.AzureLongRunningTask.Web.Models;
using Envoc.Common.Data;
using System;
using System.Linq;

namespace Envoc.AzureLongRunningTask.Web.Services
{
    public class ImageUploadService
    {
        private readonly IRepository<ProcessRequest> repository;
        private readonly IStorageContext<FileBlob> storageContext;

        public ImageUploadService(IRepository<ProcessRequest> repository, IStorageContext<FileBlob> storageContext)
        {
            this.repository = repository;
            this.storageContext = storageContext;
        }

        public ProcessResult Process(BlockUpload blockUpload)
        {
            var request = repository.Entities.FirstOrDefault(x => x.UserId == blockUpload.UserId && x.UploadId == blockUpload.UploadId);
            if (request == null)
            {
                var requestId = Guid.NewGuid();

                //ISSUE: May be URL unsafe, escape / handle
                var filePath = string.Format("{0}/{1}", requestId, blockUpload.FileName);
                request = new ProcessRequest
                {
                    RequestId = requestId,
                    FilePath = filePath,
                    FinishedUploading = false,
                    UploadId = blockUpload.UploadId,
                    UserId = blockUpload.UserId,
                    LastBlock = -1
                };
                //ISSUE: Really should be unit of work.
                request = repository.Add(request);
            }

            if (request.LastBlock >= blockUpload.BlockIndex)
            {
                throw new ArgumentException("Block index invalid, upload is out of order");
            }

            //ISSUE: Concurrency issue could cause shennanigans if something messes up on client
            request.LastBlock = blockUpload.BlockIndex;

            var file = new FileBlob
            {
                Name = request.FilePath,
                Stream = blockUpload.InputStream
            };
            
            if (blockUpload.BlockIndex + 1 >= blockUpload.TotalChunks)
            {
                request.FinishedUploading = true;
            }

            storageContext.StoreChunk(file, blockUpload.BlockIndex, request.FinishedUploading);
            
            return new ProcessResult
            {
                Completed = request.FinishedUploading,
                FilePath = request.FilePath,
                RequestId = request.RequestId
            };
        }
    }
}