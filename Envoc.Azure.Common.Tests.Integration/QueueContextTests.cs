using System;
using System.Threading;
using Envoc.Azure.Common.Models.Queues;
using Envoc.Azure.Common.Persistance;
using Envoc.Azure.Common.Persistance.Queues;
using Envoc.Core.UnitTests.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NCrunch.Framework;
using Tests.Common;

namespace Envoc.Azure.Common.Tests.Integration
{
    [ExclusivelyUses(TestResources.AzureQueues)]
    [TestClass]
    public class QueueContextTests
    {
        private class DummyItem
        {
            public object Data { get; set; }
        }

        private IQueueContext<DummyItem> target;

        [TestInitialize]
        public void Init()
        {
            target = new QueueContext<DummyItem>(new AzureContext());
        }

        [TestCleanup]
        public void Cleanup()
        {
            // don't use against production unless you like losing data.
            target.Clear();
        }

        [TestClass]
        public class Constructor : QueueContextTests
        {
            private class Bad_Name
            {
            }

            private class Ab
            {
            }

            private class Badaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa
            {
            }


            private class Goodaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa
            {
            }

            [TestMethod]
            public void WithValidTypeReturnsInstance()
            {
                // Act
                var result = new QueueContext<DummyItem>(new AzureContext());

                // Assert
                result.Count().ShouldBe(0);
            }

            [TestMethod]
            public void WithLongNameReturnsInstance()
            {
                // Act
                var result = new QueueContext<Goodaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa>(new AzureContext());

                // Assert
                result.Count().ShouldBe(0);
            }

            [TestMethod]
            public void WithInvalidCharactersInTypeThrowsException()
            {
                // Act
                Action action = ()=> new QueueContext<Bad_Name>(new AzureContext());

                // Assert
                action.ShouldThrow<ArgumentException>();
            }

            [TestMethod]
            public void WithNameTooShortThrowsException()
            {
                // Act
                Action action = () => new QueueContext<Ab>(new AzureContext());

                // Assert
                action.ShouldThrow<ArgumentException>();
            }

            [TestMethod]
            public void WithNameTooLongThrowsException()
            {
                // Act
                Action action = () => new QueueContext<Badaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa>(new AzureContext());

                // Assert
                action.ShouldThrow<ArgumentException>();
            }
        }

        [TestClass]
        public class DequeueMethod : QueueContextTests
        {
            [TestMethod]
            public void WhenEmptyReturnsNothing()
            {
                // Act
                var result = target.Dequeue();

                // Assert
                result.ShouldBeNull();
            }

            [TestMethod]
            public void WhenItemEnqueuedReturnsItemButDoesNotClear()
            {
                // Arrange
                target.Enqueue(new DummyItem
                {
                    Data = ConsoleColor.Red
                });

                // Act
                var result = target.Dequeue();

                // Assert
                result.ShouldNotBeNull();
                result.Value.Data.ShouldBe((Int64)ConsoleColor.Red);
                target.Count().ShouldBe(1);
            }
        }

        [TestClass]
        public class Visibility : QueueContextTests
        {
            [TestMethod]
            public void LettingItemReappear()
            {
                // Arrange
                target.Enqueue(new DummyItem());
                target.VisibilityTimeout = TimeSpan.FromSeconds(1);
                var count = target.Count();

                // Act
                var resulta = target.Dequeue();
                var resultb = target.Dequeue();
                Thread.Sleep(target.VisibilityTimeout + TimeSpan.FromMilliseconds(10));
                var resultc = target.Dequeue();

                // Assert
                count.ShouldBe(1);
                resulta.ShouldNotBeNull();
                resultb.ShouldBeNull();
                resultc.ShouldNotBeNull();
            }

            [TestMethod]
            public void LettingMarkingItemCompleteDoesNotReappear()
            {
                // Arrange
                target.Enqueue(new DummyItem());
                target.VisibilityTimeout = TimeSpan.FromSeconds(1);
                var count = target.Count();

                // Act
                var resulta = target.Dequeue();
                var resultb = target.Dequeue();
                target.MarkCompleted(resulta);
                Thread.Sleep(target.VisibilityTimeout + TimeSpan.FromMilliseconds(10));
                var resultc = target.Dequeue();

                // Assert
                count.ShouldBe(1);
                resulta.ShouldNotBeNull();
                resultb.ShouldBeNull();
                resultc.ShouldBeNull();
            }

            [TestMethod]
            public void RenewingItemDoesNotReappear()
            {
                // Arrange
                target.Enqueue(new DummyItem());
                target.VisibilityTimeout = TimeSpan.FromSeconds(1);
                var count = target.Count();

                // Act
                var resulta = target.Dequeue();
                var resultb = target.Dequeue();
                Thread.Sleep(target.VisibilityTimeout - TimeSpan.FromMilliseconds(500));
                target.Renew(resulta);
                Thread.Sleep(target.VisibilityTimeout - TimeSpan.FromMilliseconds(400));
                var resultc = target.Dequeue();
                Thread.Sleep(target.VisibilityTimeout - TimeSpan.FromMilliseconds(400));
                var resultd = target.Dequeue();

                // Assert
                count.ShouldBe(1);
                resulta.ShouldNotBeNull();
                resultb.ShouldBeNull();
                resultc.ShouldBeNull();
                resultd.ShouldNotBeNull();
            }
        }

        [TestClass]
        public class MarkCompletedMethod : QueueContextTests
        {
            private class FakeQueueItem : IQueueEntity<DummyItem>
            {
                public string MessageId { get; set; }
                public string Reciept { get; set; }
                public DummyItem Value { get; set; }
            }

            [TestMethod]
            public void WithNullThrowsException()
            {
                // Act
                Action action = ()=> target.MarkCompleted(null);

                // Assert
                action.ShouldThrow<ArgumentNullException>(@"Value cannot be null.
Parameter name: entity");
            }

            [TestMethod]
            public void WithEmptyMessageThrowsException()
            {
                // Act
                Action action = () => target.MarkCompleted(new FakeQueueItem());

                // Assert
                action.ShouldThrow<ArgumentNullException>(@"Value cannot be null.
Parameter name: messageId");
            }

            [TestMethod]
            public void WithMessageThatHasInvalidMessageThrowsException()
            {
                // Arrange
                target.Enqueue(new DummyItem());
                target.VisibilityTimeout = TimeSpan.FromSeconds(1);
                var message = target.Dequeue();

                // Act
                Action action = () => target.MarkCompleted(new FakeQueueItem
                {
                    MessageId = message.MessageId + "asd",
                    Reciept = message.Reciept
                });

                // Assert
                action.ShouldThrow<ArgumentException>("Invalid messageId or Reciept");
            }

            [TestMethod]
            public void WithMessageThatHasInvalidRecieptThrowsException()
            {
                // Arrange
                target.Enqueue(new DummyItem());
                target.VisibilityTimeout = TimeSpan.FromSeconds(1);
                var message = target.Dequeue();

                // Act
                Action action = () => target.MarkCompleted(new FakeQueueItem
                {
                    MessageId = message.MessageId,
                    Reciept = message.Reciept + "asd"
                });

                // Assert
                action.ShouldThrow<ArgumentException>("Invalid messageId or Reciept");
            }

            [TestMethod]
            public void WithMessageThatHasTimedOutThrowsException()
            {
                // Arrange
                target.Enqueue(new DummyItem());
                target.VisibilityTimeout = TimeSpan.FromSeconds(1);
                var message = target.Dequeue();
                Thread.Sleep(target.VisibilityTimeout + TimeSpan.FromMilliseconds(10));
                var messageb = target.Dequeue();
                messageb.ShouldNotBeNull();

                // Act
                Action action = ()=> target.MarkCompleted(message);

                // Assert
                action.ShouldThrow<ArgumentException>("Invalid messageId or Reciept");
            }
        }

        [TestClass]
        public class RenewMethod : QueueContextTests
        {
            private class FakeQueueItem : IQueueEntity<DummyItem>
            {
                public string MessageId { get; set; }
                public string Reciept { get; set; }
                public DummyItem Value { get; set; }
            }

            [TestMethod]
            public void WithNullThrowsException()
            {
                // Act
                Action action = () => target.Renew(null);

                // Assert
                action.ShouldThrow<ArgumentNullException>(@"Value cannot be null.
Parameter name: entity");
            }

            [TestMethod]
            public void WithEmptyMessageThrowsException()
            {
                // Act
                Action action = () => target.Renew(new FakeQueueItem());

                // Assert
                action.ShouldThrow<ArgumentNullException>(@"Value cannot be null.
Parameter name: messageId");
            }

            [TestMethod]
            public void WithMessageThatHasInvalidMessageThrowsException()
            {
                // Arrange
                target.Enqueue(new DummyItem());
                target.VisibilityTimeout = TimeSpan.FromSeconds(1);
                var message = target.Dequeue();

                // Act
                Action action = () => target.Renew(new FakeQueueItem
                {
                    MessageId = message.MessageId + "asd",
                    Reciept = message.Reciept
                });

                // Assert
                action.ShouldThrow<ArgumentException>("Invalid messageId or Reciept");
            }

            [TestMethod]
            public void WithMessageThatHasInvalidRecieptThrowsException()
            {
                // Arrange
                target.Enqueue(new DummyItem());
                target.VisibilityTimeout = TimeSpan.FromSeconds(1);
                var message = target.Dequeue();

                // Act
                Action action = () => target.Renew(new FakeQueueItem
                {
                    MessageId = message.MessageId,
                    Reciept = message.Reciept + "asd"
                });

                // Assert
                action.ShouldThrow<ArgumentException>("Invalid messageId or Reciept");
            }

            [TestMethod]
            public void WithMessageThatHasTimedOutThrowsException()
            {
                // Arrange
                target.Enqueue(new DummyItem());
                target.VisibilityTimeout = TimeSpan.FromSeconds(1);
                var message = target.Dequeue();
                Thread.Sleep(target.VisibilityTimeout + TimeSpan.FromMilliseconds(10));
                var messageb = target.Dequeue();
                messageb.ShouldNotBeNull();

                // Act
                Action action = () => target.Renew(message);

                // Assert
                action.ShouldThrow<ArgumentException>("Invalid messageId or Reciept");
            }
        }

        [TestClass]
        public class VisibilityTimeoutProperty : QueueContextTests
        {
            [TestMethod]
            public void WhenSetToLessThanOneSecondDefaultsToOneSecond()
            {
                // Arrange
                target.Enqueue(new DummyItem());

                // Act
                target.VisibilityTimeout = TimeSpan.Zero;
                var result = target.VisibilityTimeout;
                target.Dequeue();

                // Assert
                result.ShouldBe(TimeSpan.FromSeconds(1));
                target.Count().ShouldBe(1);
            }

            [TestMethod]
            public void WhenSetToMoreThanTwoHoursDefaultsToTwoHours()
            {
                // Arrange
                target.Enqueue(new DummyItem());

                // Act
                target.VisibilityTimeout = TimeSpan.FromHours(3);
                var result = target.VisibilityTimeout;
                target.Dequeue();

                // Assert
                result.ShouldBe(TimeSpan.FromHours(2));
                target.Count().ShouldBe(1);
            }

            [TestMethod]
            public void WhenSetToTwoSecondsReturrnsTwoSeconds()
            {
                // Arrange
                target.Enqueue(new DummyItem());
                var expected = TimeSpan.FromSeconds(2);

                // Act
                target.VisibilityTimeout = expected;
                var result = target.VisibilityTimeout;
                target.Dequeue();

                // Assert
                result.ShouldBe(expected);
                target.Count().ShouldBe(1);
            }
        }

        [TestClass]
        public class EnqueueMethod : QueueContextTests
        {
            [TestMethod]
            public void WhenNullThrowsException()
            {
                // Act
                Action action = ()=>target.Enqueue(null);

                // Assert
                action.ShouldThrow<ArgumentNullException>(@"Value cannot be null.
Parameter name: message");
                target.Count().ShouldBe(0);
            }

            [TestMethod]
            public void WhenGivenBlankItemEnqueues()
            {
                // Act
                target.Enqueue(new DummyItem());

                // Assert
                target.Count().ShouldBe(1);
            }

            [TestMethod]
            public void WhenGivenItemUnderSizeLimitEnqueues()
            {
                // Act
                target.Enqueue(new DummyItem
                {
                    Data = " ".PadRight(48*1024-11)
                });

                // Assert
                target.Count().ShouldBe(1);
            }

            [TestMethod]
            public void WhenGivenItemOverSizeLimitThrowsException()
            {
                // Act
                Action action = ()=> target.Enqueue(new DummyItem
                {
                    Data = " ".PadRight(48 * 1024 - 10)
                });

                // Assert
                action.ShouldThrow<ArgumentException>("Messages cannot be larger than 65536 bytes.");
                target.Count().ShouldBe(0);
            }
        }
    }
}