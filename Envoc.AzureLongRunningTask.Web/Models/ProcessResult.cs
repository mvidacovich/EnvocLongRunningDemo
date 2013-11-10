using System;

namespace Envoc.AzureLongRunningTask.Web.Models
{
    public class ProcessResult
    {
        public Guid RequestId { get; set; }
        public bool Completed { get; set; }
        public string FilePath { get; set; }
    }
}