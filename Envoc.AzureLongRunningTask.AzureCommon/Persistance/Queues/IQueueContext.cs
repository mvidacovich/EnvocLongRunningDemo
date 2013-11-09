using System;
using Envoc.Azure.Common.Models.Queues;

namespace Envoc.Azure.Common.Persistance.Queues
{
    public interface IQueueContext<T>
    {
        TimeSpan VisibilityTimeout { get; set; }
        IQueueEntity<T> Dequeue();
        void Enqueue(T message);
        TimeSpan Renew(IQueueEntity<T> entity);
        void MarkCompleted(IQueueEntity<T> entity);
        int Count();

        /// <summary>
        /// Use with caution, WILL delete everything.
        /// </summary>
        void Clear();
    }
}
