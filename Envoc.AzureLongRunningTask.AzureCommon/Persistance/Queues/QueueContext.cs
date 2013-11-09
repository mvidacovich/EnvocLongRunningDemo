using System;
using System.Text.RegularExpressions;
using Envoc.Azure.Common.Models.Queues;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

namespace Envoc.Azure.Common.Persistance.Queues
{
    public class QueueContext<T> : IQueueContext<T>
    {
        // ReSharper disable once StaticFieldInGenericType 
        // Expected behaviour, one loaded per generic type, static
        private static bool loaded;
        // ReSharper restore StaticFieldInGenericType
        
        private readonly CloudQueue queue;
        private readonly TimeSpan countCacheDuration = TimeSpan.FromSeconds(15);
        private DateTime lastCountUpdate;
        private TimeSpan visibilityTimeout;

        public QueueContext(AzureContext storageContext)
        {
            var storage = storageContext.Account.CreateCloudQueueClient();
            var queueName = typeof(T).Name.ToLower();

            //Naming restrictions, see cheat sheet
            if (!Regex.IsMatch(queueName, @"^[0-9a-z]+([a-z0-9]\-)*[0-9a-z]+$") ||
                queueName.Length < 3 ||
                queueName.Length > 63)
            {
                throw new ArgumentException("Invalid queue name: " + queueName);
            }

            VisibilityTimeout = TimeSpan.FromSeconds(120); 
            queue = storage.GetQueueReference(queueName);
        }

        public TimeSpan VisibilityTimeout
        {
            get { return visibilityTimeout; }
            set { visibilityTimeout = GetSafeValueVisibilityTimeout(value); }
        }

        public IQueueEntity<T> Dequeue()
        {
            CreateIfNotExist();
            var message = queue.GetMessage(VisibilityTimeout);
            if (message == null)
            {
                return null;
            }

            // TODO: fix hardcoded json deseralization
            // TODO: needs versioning, ect if you change schemas on your message type
            var item = JsonConvert.DeserializeObject<T>(message.AsString);

            var queueItem = new QueueEntity<T>
            {
                MessageId = message.Id,
                Value = item,
                Reciept = message.PopReceipt,
            };
            return queueItem;
        }

        public void Enqueue(T message)
        {
            CreateIfNotExist();

            if (ReferenceEquals(message,null))
            {
                throw new ArgumentNullException("message");
            }

            // TODO: fix hardcoded json seralization
            var item = JsonConvert.SerializeObject(message);
            var cloudQueueMessage = new CloudQueueMessage(item);
            
            queue.AddMessage(cloudQueueMessage);
        }

        public TimeSpan Renew(IQueueEntity<T> entity)
        {
            CreateIfNotExist();
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            // In earlier versions you did not have access to this constructor, so you'd have to use reflection to set the messageid/reciept. Fun times.
            var cloudQueueMessage = new CloudQueueMessage(entity.MessageId, entity.Reciept);
            
            try
            {
                queue.UpdateMessage(cloudQueueMessage, VisibilityTimeout, MessageUpdateFields.Visibility);
                entity.Reciept = cloudQueueMessage.PopReceipt;
                return VisibilityTimeout;
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode == 404 ||
                    ex.RequestInformation.HttpStatusCode == 400)
                {
                    throw new ArgumentException("Invalid messageId or Reciept");
                }
                throw;
            }
        }

        public void MarkCompleted(IQueueEntity<T> entity)
        {
            CreateIfNotExist();
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }
            var cloudQueueMessage = new CloudQueueMessage(entity.MessageId, entity.Reciept);
            try
            {
                queue.DeleteMessage(cloudQueueMessage);
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode == 404 ||
                    ex.RequestInformation.HttpStatusCode ==  400)
                {
                    throw new ArgumentException("Invalid messageId or Reciept");
                }
                throw;
            }
        }

        public void Clear()
        {
            CreateIfNotExist();
            queue.Clear();
        }

        public int Count()
        {
            CreateIfNotExist();
            if (DateTime.UtcNow - lastCountUpdate > countCacheDuration)
            {
                lastCountUpdate = DateTime.UtcNow;
                queue.FetchAttributes();
            }
            return queue.ApproximateMessageCount.GetValueOrDefault();
        }

        private void CreateIfNotExist()
        {
            // TODO: check connection to ensure a different connection was not made
            if (!loaded)
            {
                loaded = true;

                // Notice the retry policy here in optional parameters
                queue.CreateIfNotExists();
            }
        }

        private static TimeSpan GetSafeValueVisibilityTimeout(TimeSpan value)
        {
            if (value < TimeSpan.FromSeconds(1))
            {
                return TimeSpan.FromSeconds(1);
            }

            if (value > TimeSpan.FromHours(2))
            {
                return TimeSpan.FromHours(2);
            }

            return value;
        }
    }
}