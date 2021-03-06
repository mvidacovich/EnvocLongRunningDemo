﻿using System.Collections.Generic;
using System.Linq;
using Envoc.AzureLongRunningTask.AzureCommon.Persistance.Blob;
using Envoc.AzureLongRunningTask.Common.Models;
using Envoc.AzureLongRunningTask.Web.Models;
using System;

namespace Envoc.AzureLongRunningTask.Web.Services
{
    public class ImageUploadService
    {
        private readonly IList<ProcessRequest> repository;
        private readonly IStorageContext<FileBlob> storageContext;

        public ImageUploadService(IList<ProcessRequest> repository, IStorageContext<FileBlob> storageContext)
        {
            this.repository = repository;
            this.storageContext = storageContext;
        }

        public ProcessResult Process(BlockUpload blockUpload)
        {
            var request = GetOrCreateRequest(blockUpload);

            //ISSUE: Concurrency issue could cause shennanigans if something messes up on client / blocks are uploaded out of order
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
                RelatedRequest = request
            };
        }

        public ProcessRequest FinalizeChunks(BlockUpload uploadChunk)
        {
            var request = repository.Single(x => x.UserId == uploadChunk.UserId && x.UploadId == uploadChunk.UploadId && !x.FinishedUploading);
            request.FinishedUploading = true;
            if (uploadChunk.Chunks != null && uploadChunk.Chunks.Any())
            {
                var path = request.FilePath;
                storageContext.CommitChunks(path, uploadChunk.Chunks);
            }
            return request;
        }

        public string GetUploadPath(BlockUpload uploadChunk)
        {
            var request = GetOrCreateRequest(uploadChunk);
            return string.Format("{0}/{1}", request.RequestId, uploadChunk.FileName);
        }

        private ProcessRequest GetOrCreateRequest(BlockUpload blockUpload)
        {
            var request = repository.SingleOrDefault(x => x.UserId == blockUpload.UserId && x.UploadId == blockUpload.UploadId);
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
                    //ISSUE:  Obviously you want to use cryptographically secure RNG here
                    ApiKey = "super secret key, shhh"
                };

                //ISSUE: This would be a database call with transactional safety (assumed EF due to automatic change tracking magic)
                repository.Add(request);
            }
            return request;
        }
    }
}