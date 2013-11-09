using System.IO;

namespace Envoc.Azure.Common.Persistance.Blob
{
    public interface IFileBlob
    {
        string Name { set; get; }
        Stream Stream { set; get; }
        string ContentType { get; }
    }
}