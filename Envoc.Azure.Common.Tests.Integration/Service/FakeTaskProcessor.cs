using System;
using System.Threading;
using Envoc.Azure.Common.Models.Queues;
using Envoc.Azure.Common.Persistance.Queues;
using Envoc.Azure.Common.Service;

namespace Envoc.Azure.Common.Tests.Integration.Service
{
    internal class FakeTaskProcessor : QueueProcessorBase<FakeTask>
    {
        public FakeTaskProcessor(IQueueContext<FakeTask> queueContext) 
            : base(queueContext)
        {
        }

        protected override bool Process(IQueueEntity<FakeTask> job, CancellationToken processJobToken)
        {
            if (job.Value.Duration < TimeSpan.Zero)
            {
                return false;
            }
            Thread.Sleep(job.Value.Duration);
            return true;
        }
    }
}