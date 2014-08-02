using System;

namespace Envoc.AzureLongRunningTask.Web.Models
{
    public class ProcessRequest 
    {
        public int Id { get; set; }

        public Guid RequestId { get; set; }
        public string UserId { get; set; }

        public string UploadId { get; set; }

        public string FilePath { get; set; }
        public string ApiKey { get; set; }
        public bool FinishedUploading { get; set; }

        public string ResultPath { get; set; }
        public bool GotResult { get; set; }
    }
}