using Envoc.Azure.Common.Persistance;
using Envoc.Azure.Common.Persistance.Blob;
using Envoc.Azure.Common.Persistance.Queues;
using Envoc.AzureLongRunningTask.Common.Models;
using Microsoft.WindowsAzure.ServiceRuntime;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ImageProcessor
{
    public class WorkerRole : RoleEntryPoint
    {
        private ProcessImageJobService processor;
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private Task task;

        public override void Run()
        {
            // This is a sample worker implementation. Replace with your logic.
            Trace.TraceInformation("ImageProcessor entry point called");
            task = processor.Run(cancellationTokenSource.Token);
            Task.WaitAll(task);
        }

        public override void OnStop()
        {
            cancellationTokenSource.Cancel();
            Task.WaitAll(task);
            base.OnStop();
        }

        public override bool OnStart()
        {
            // Load your favorite production connection string, ect
            var context = new AzureContext();
            processor = new ProcessImageJobService(
                new QueueContext<ProcessImageJob>(context),
                new StorageContext<FileBlob>(context),
                new StorageContext<ResultBlob>(context));

            // It costs money to poll, ~$1 per 10 million requets, just FYI
            processor.QueuePollWait = TimeSpan.FromSeconds(1);
            return base.OnStart();
        }
    }
}
