namespace Envoc.Azure.Common.Models.Queues
{
    public interface IQueueEntity<T>
    {
        string MessageId { get; }
        string Reciept { get; set; }

        T Value { get; set; }
    }
}
