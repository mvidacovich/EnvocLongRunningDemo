using System.IO;
using Envoc.Azure.Common.Persistance.Blob;

namespace Envoc.AzureLongRunningTask.Common.Models
{
    public class FileBlob : IFileBlob
    {
        public string Name { get; set; }
        public Stream Stream { get; set; }

        public string ContentType
        {
            get { return "application/octet-stream"; }
        }
    }
}