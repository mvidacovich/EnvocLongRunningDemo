using System;
using Envoc.Common.Data;

namespace Envoc.AzureLongRunningTask.Web.Models
{
    public class ProcessRequest : IIdentifiable
    {
        public int Id { get; set; }

        public Guid RequestId { get; set; }
        public string UserId { get; set; }

        public string UploadId { get; set; }

        public string FilePath { get; set; }
        public string ApiKey { get; set; }
        public bool FinishedUploading { get; set; }

        public int LastBlock { get; set; }
    }
}