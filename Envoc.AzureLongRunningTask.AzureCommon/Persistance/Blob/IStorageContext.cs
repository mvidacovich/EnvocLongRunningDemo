﻿using System;

namespace Envoc.AzureLongRunningTask.AzureCommon.Persistance.Blob
{
    public interface IStorageContext<T> where T : IFileBlob
    {
        void Store(T entity);
        void StoreChunk(T entity, int blockIndex, bool finalize);
        T GetBlob(string name);
        string GetPublicReadUrl(string name, TimeSpan length);
        void CommitChunks(string name, string[] chunks);
    }
}
