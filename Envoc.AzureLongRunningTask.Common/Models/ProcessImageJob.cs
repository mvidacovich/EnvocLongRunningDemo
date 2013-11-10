using System;

namespace Envoc.AzureLongRunningTask.Common.Models
{
    public class ProcessImageJob
    {
        public Guid RequestId { get; set; }
        public string FilePath { get; set; }
        public string ApiKey { get; set; }
        public string PostbackUrl { get; set; }
    }
}