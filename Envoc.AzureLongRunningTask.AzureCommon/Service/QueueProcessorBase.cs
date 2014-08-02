using System;
using System.Threading;
using System.Threading.Tasks;
using Envoc.AzureLongRunningTask.AzureCommon.Models.Queues;
using Envoc.AzureLongRunningTask.AzureCommon.Persistance.Queues;

namespace Envoc.AzureLongRunningTask.AzureCommon.Service
{
    public abstract class QueueProcessorBase<T>
    {
        // ReSharper disable StaticFieldInGenericType 
        // desired behaviour same <T> should be synced, different <T> should not
        private readonly static object QueueSync = new object();
        // ReSharper restore StaticFieldInGenericType

        private readonly IQueueContext<T> queueContext;
        private readonly object taskSync = new object();
        private TimeSpan queuePollWait = TimeSpan.FromSeconds(10);
        private Task runningTask;

        protected QueueProcessorBase(IQueueContext<T> queueContext)
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
            lock (taskSync)
            {
                if (runningTask == null)
                {
                    runningTask = Task.Factory.StartNew(() => ProcessLoop(runToken));
                }
                return runningTask;
            }
        }

        private void ProcessLoop(CancellationToken runToken)
        {
            while (!runToken.IsCancellationRequested)
            {
                IQueueEntity<T> job;
                lock (QueueSync)
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

                int finished;
                try
                {
                    finished = Task.WaitAny(taskArray, runToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                finally
                {
                    refreshJobToken.Cancel();
                    processJobToken.Cancel();
                }

                if (finished == 0 && processJobTask.Result)
                {
                    refreshJobTask.Wait();
                    lock (QueueSync)
                    {
                        queueContext.MarkCompleted(job);
                    }
                }
            }

            lock (taskSync)
            {
                runningTask = null;
            }
        }

        protected virtual void RefreshJob(IQueueEntity<T> job, CancellationToken refreshJobToken)
        {
            var offset = queueContext.VisibilityTimeout.TotalMilliseconds*0.3;
            var delay = queueContext.VisibilityTimeout - TimeSpan.FromMilliseconds(offset);

            while (!refreshJobToken.IsCancellationRequested)
            {
                if (refreshJobToken.WaitHandle.WaitOne(delay))
                {
                    return;
                }

                lock (QueueSync)
                {
                    delay = queueContext.Renew(job);
                    delay -= TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 0.3);
                }
            }
        }

        protected abstract bool Process(IQueueEntity<T> job, CancellationToken processJobToken);

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
