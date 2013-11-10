using Envoc.Azure.Common.Models.Queues;
using Envoc.Azure.Common.Persistance.Queues;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Envoc.Azure.Common.Service
{
    public class QueueProcessorBase<T>
    {
        private readonly object queueSync = new object();
        private readonly IQueueContext<T> queueContext;
        private TimeSpan queuePollWait = TimeSpan.FromSeconds(10);

        public QueueProcessorBase(IQueueContext<T> queueContext)
        {
            this.queueContext = queueContext;
        }

        public TimeSpan QueuePollWait
        {
            get { return queuePollWait; }
            set { queuePollWait = GetPollTimeInbounds(value); }
        }

        public Task Run(CancellationToken runToken)
        {
            return Task.Factory.StartNew(()=>ProcessLoop(runToken));
        }

        private void ProcessLoop(CancellationToken runToken)
        {
            while (!runToken.IsCancellationRequested)
            {
                IQueueEntity<T> job;
                lock (queueSync)
                {
                    job = queueContext.Dequeue();
                }

                if (job == null)
                {
                    runToken.WaitHandle.WaitOne(QueuePollWait);
                    continue;
                }

                // ReSharper disable ImplicitlyCapturedClosure - We are running to completion or bust inside scope, so no need to fear.
                var refreshJobToken = new CancellationTokenSource();
                var refreshJobTask = Task.Factory.StartNew(() => RefreshJob(job, refreshJobToken.Token), refreshJobToken.Token);

                var processJobToken = new CancellationTokenSource();
                var processJobTask = Task.Factory.StartNew(() => Process(job, processJobToken.Token), processJobToken.Token);
                // ReSharper restore ImplicitlyCapturedClosure

                var taskArray = new [] { processJobTask, refreshJobTask };

                var finished = Task.WaitAny(taskArray, runToken);
                refreshJobToken.Cancel();
                processJobToken.Cancel();

                if (finished == 0 && processJobTask.Result)
                {
                    lock (queueSync)
                    {
                        queueContext.MarkCompleted(job);
                    }
                }
            }
        }

        protected virtual void RefreshJob(IQueueEntity<T> job, CancellationToken refreshJobToken)
        {
            var offset = queueContext.VisibilityTimeout.TotalMilliseconds*0.3;
            var delay = queueContext.VisibilityTimeout - TimeSpan.FromMilliseconds(offset);

            while (!refreshJobToken.IsCancellationRequested)
            {
                if (!refreshJobToken.WaitHandle.WaitOne(delay))
                {
                    return;
                }

                lock (queueSync)
                {
                    delay = queueContext.Renew(job);
                    delay -= TimeSpan.FromMilliseconds(delay.TotalMilliseconds*0.3);
                }
            }
        }

        protected virtual bool Process(IQueueEntity<T> job, CancellationToken processJobToken)
        {
            return false;
        }

        private static TimeSpan GetPollTimeInbounds(TimeSpan value)
        {
            if (value < TimeSpan.Zero)
            {
                return TimeSpan.Zero;
            }

            return value;
        }
    }
}
