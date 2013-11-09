namespace Envoc.Azure.Common.Models.Queues
{
    internal class QueueEntity<T> : IQueueEntity<T>
    {
        public string MessageId { get; internal set; }

        public string Reciept { get; set; }

        public T Value { get; set; }
    }
}