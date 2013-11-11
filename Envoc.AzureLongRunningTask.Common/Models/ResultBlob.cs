using System.IO;
using Envoc.Azure.Common.Persistance.Blob;

namespace Envoc.AzureLongRunningTask.Common.Models
{
    public class ResultBlob : IFileBlob
    {
        public string Name { get; set; }
        public Stream Stream { get; set; }

        public string ContentType
        {
            get { return "image/png"; }
        }
    }
}