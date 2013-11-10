using System;

namespace Envoc.Azure.Common.Persistance.Blob
{
    public interface IStorageContext<T> where T : IFileBlob
    {
        void Store(T entity);
        void StoreChunk(T entity, int blockIndex, bool finalize);
        T GetBlob(string name);
        string GetPublicReadUrl(string name, TimeSpan length);
    }
}
