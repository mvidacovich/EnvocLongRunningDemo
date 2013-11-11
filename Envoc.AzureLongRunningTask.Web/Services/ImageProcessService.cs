﻿using System;
using System.Linq;
using Envoc.Azure.Common.Persistance.Blob;
using Envoc.Azure.Common.Persistance.Queues;
using Envoc.AzureLongRunningTask.Common.Models;
using Envoc.AzureLongRunningTask.Web.Models;
using Envoc.Common.Data;

namespace Envoc.AzureLongRunningTask.Web.Services
{
    public class ImageProcessService
    {
        private readonly IQueueContext<ProcessImageJob> queueContext;
        private readonly IRepository<ProcessRequest> processRequest;
        private readonly IStorageContext<ResultBlob> resultStorageContext;

        public ImageProcessService(IQueueContext<ProcessImageJob> queueContext, IRepository<ProcessRequest> processRequest, IStorageContext<ResultBlob> resultStorageContext)
        {
            this.queueContext = queueContext;
            this.processRequest = processRequest;
            this.resultStorageContext = resultStorageContext;
        }
        
        public void CreateNewJobFor(ProcessRequest request, string action)
        {
            queueContext.Enqueue(new ProcessImageJob
            {
                 ApiKey = request.ApiKey,
                 FilePath = request.FilePath,
                 PostbackUrl = action,
                 RequestId = request.RequestId
            });
        }

        public ProcessRequest CompleteJob(Guid requestid, string apikey, string resultpath)
        {
            var request = processRequest.Entities.FirstOrDefault(x => x.RequestId == requestid && x.ApiKey == apikey);
            if (request == null)
            {
                return null;
            }

            //ISSUE: assumed change tracking and unit of work. Only works because of in-memory hacky shennanigans
            request.ResultPath = resultpath;
            request.GotResult = true;

            return request;
        }

        public string GetResultUrl(ProcessRequest job)
        {
            return resultStorageContext.GetPublicReadUrl(job.ResultPath, TimeSpan.FromMinutes(15));
        }
    }
}