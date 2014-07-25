using System;
using Envoc.AzureLongRunningTask.AzureCommon.Models.Queues;

namespace Envoc.AzureLongRunningTask.AzureCommon.Persistance.Queues
{
    public interface IQueueContext<T>
    {
        TimeSpan VisibilityTimeout { get; set; }
        IQueueEntity<T> Dequeue();
        void Enqueue(T message);
        TimeSpan Renew(IQueueEntity<T> entity);
        void MarkCompleted(IQueueEntity<T> entity);
        int Count(bool forceRefresh);

        /// <summary>
        /// Use with caution, WILL delete everything.
        /// </summary>
        void Clear();
    }
}
