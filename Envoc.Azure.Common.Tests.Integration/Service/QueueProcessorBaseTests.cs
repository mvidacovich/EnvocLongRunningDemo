using Envoc.Azure.Common.Persistance;
using Envoc.Azure.Common.Persistance.Queues;
using Envoc.Core.UnitTests.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NCrunch.Framework;
using System;
using System.Diagnostics;
using System.Threading;
using Tests.Common;

namespace Envoc.Azure.Common.Tests.Integration.Service
{
    [ExclusivelyUses(TestResources.AzureQueues + "2")]
    [TestClass]
    public class QueueProcessorBaseTests
    {
        private IQueueContext<FakeTask> queue;
        private FakeTaskProcessor target;

        [TestInitialize]
        public void Init()
        {
            queue = new QueueContext<FakeTask>(new AzureContext())
            {
                VisibilityTimeout = TimeSpan.FromSeconds(1)
            };
            target = new FakeTaskProcessor(queue);
        }

        [TestCleanup]
        public void Cleanup()
        {
             queue.Clear();
        }

        [TestClass]
        public class ProcessMethod : QueueProcessorBaseTests
        {
            [TestMethod]
            public void WithNoJobsSpinsForever()
            {
                // Act
                var tokenSource = new CancellationTokenSource();
                var promise = target.Run(tokenSource.Token);

                // Assert
                var stopwatch = Stopwatch.StartNew();
                tokenSource.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(1));
                tokenSource.Cancel();
                promise.Wait();
                promise.IsCompleted.ShouldBe(true);
                stopwatch.ElapsedMilliseconds.ShouldBeLessThan(3500);
            }

            [TestMethod]
            public void WithOneLongJobDoesNotComplete()
            {
                // Act
                queue.Enqueue(new FakeTask
                {
                    Duration = TimeSpan.FromHours(1)
                });
                var tokenSource = new CancellationTokenSource();
                var promise = target.Run(tokenSource.Token);

                // Assert
                var stopwatch = Stopwatch.StartNew();
                tokenSource.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(2));
                tokenSource.Cancel();
                promise.Wait();
                promise.IsCompleted.ShouldBe(true);
                stopwatch.ElapsedMilliseconds.ShouldBeLessThan(2500);
                queue.Count(true).ShouldBe(1);
            }

            [TestMethod]
            public void WithSeveralShortJobsCompletes()
            {
                // Act
                for (int i = 0; i < 10; i++)
                {
                    queue.Enqueue(new FakeTask
                    {
                        Duration = TimeSpan.FromMilliseconds(1)
                    });
                }
                var tokenSource = new CancellationTokenSource();
                var promise = target.Run(tokenSource.Token);

                // Assert
                var stopwatch = Stopwatch.StartNew();
                tokenSource.Token.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(1000));
                tokenSource.Cancel();
                promise.Wait();
                promise.IsCompleted.ShouldBe(true);
                stopwatch.ElapsedMilliseconds.ShouldBeLessThan(1500);
                queue.Count(true).ShouldBe(0);
            }
        }
    }
}
