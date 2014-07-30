using System.IO;

namespace Envoc.AzureLongRunningTask.Web.Models
{
    public class BlockUpload
    {
        public string[] Chunks { get; set; }
        public string UploadId { get; set; }
        public string FileName { get; set; }
        public int BlockIndex { get; set; }
        public int TotalChunks { get; set; }
        public Stream InputStream { get; set; }
        public string UserId { get; set; }
    }
}