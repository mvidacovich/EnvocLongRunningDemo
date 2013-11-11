namespace Envoc.AzureLongRunningTask.Web.Models
{
    public class ProcessResult
    {
        public bool Completed { get; set; }
        public ProcessRequest RelatedRequest { get; set; }
    }
}