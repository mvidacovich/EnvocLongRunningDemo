using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Envoc.Azure.Common.Persistance.Blob
{
    public class StorageContext<T> : IStorageContext<T> where T : IFileBlob, new()
    {
        // ReSharper disable StaticFieldInGenericType 
        private static bool loaded;
        // ReSharper restore StaticFieldInGenericType

        private readonly CloudBlobContainer container;

        public StorageContext(AzureContext context)
        {
            var storage = context.Account.CreateCloudBlobClient();
            var containerName = typeof(T).Name.ToLower();
            container = storage.GetContainerReference(containerName);
        }

        public void Store(T entity)
        {
            CreateIfNotExist();
            ValidateEntity(entity);

            // No page blobs for now (only block). See cheat sheet for more features
            var blob = container.GetBlockBlobReference(entity.Name);
            blob.Properties.ContentType = entity.ContentType;
            blob.UploadFromStream(entity.Stream);
            blob.SetProperties();
        }

        public void StoreChunk(T entity, int blockIndex, bool finalize)
        {
            CreateIfNotExist();
            ValidateEntity(entity);
            if (blockIndex < 0)
            {
                throw new ArgumentException("Cannot be negative", "blockIndex");
            }
            if (entity.Stream.Length <= 0)
            {
                throw new ArgumentException("Stream cannot be empty in block upload", "source");
            }
            var blob = container.GetBlockBlobReference(entity.Name);
            var blockId = Convert.ToBase64String(BitConverter.GetBytes(blockIndex));
            blob.Properties.ContentType = entity.ContentType;
            blob.PutBlock(blockId, entity.Stream, null);

            if (finalize)
            {
                var blockIds = new List<string>();
                for (int i = 0; i < blockIndex+1; i++)
                {
                    blockIds.Add(Convert.ToBase64String(BitConverter.GetBytes(i)));
                }
                blob.PutBlockList(blockIds);
                blob.SetProperties();
            }
        }

        public T GetBlob(string name)
        {
            CreateIfNotExist();
            if (string.IsNullOrEmpty(name))
            {
                return default(T);
            }
            var blob = container.GetBlockBlobReference(name);
            if (blob == null)
            {
                return default(T);
            }

            Stream stream;
            try
            {
                blob.FetchAttributes();
                stream = blob.OpenRead();
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == 404)
                {
                    return default(T);
                }
                throw;
            }

            var result = new T
            {
                Stream = stream,
                Name = name
            };

            return result;
        }

        public string GetPublicReadUrl(string name, TimeSpan length)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            // See cheat sheet, azure limits the access time of non revokable SAS
            if (length > TimeSpan.FromHours(1))
            {
                throw new ArgumentException("Public read url cannot be live for more than 1 hour.");
            }

            var blob = container.GetBlockBlobReference(name);
            if (blob == null)
            {
                return null;
            }

            var sas = blob.GetSharedAccessSignature(new SharedAccessBlobPolicy
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessExpiryTime = DateTime.UtcNow + length
            });

            return blob.Uri.AbsoluteUri + sas;
        }

        private static void ValidateEntity(T entity)
        {
            if (ReferenceEquals(entity,null))
            {
                throw new ArgumentNullException("entity");
            }

            if (ReferenceEquals(entity.Name, null))
            {
                throw new ArgumentNullException("blobName");
            }

            if (ReferenceEquals(entity.Stream, null))
            {
                throw new ArgumentNullException("source");
            }
        }

        private void CreateIfNotExist()
        {
            // TODO: check connection to ensure a different connection was not made
            if (!loaded)
            {
                loaded = true;
                container.CreateIfNotExists(BlobContainerPublicAccessType.Off);
            }
        }
    }
}