using System;
using System.IO;
using System.Linq;
using System.Text;
using Envoc.Azure.Common.Persistance;
using Envoc.Azure.Common.Persistance.Blob;
using Envoc.Core.UnitTests.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NCrunch.Framework;
using Tests.Common;

namespace Envoc.Azure.Common.Tests.Integration.Persistance
{
    [ExclusivelyUses(TestResources.AzureStorage)]
    [TestClass]
    public class StorageContextTests
    {
        private class DummyFile : IFileBlob
        {
            public string Name { get; set; }
            public Stream Stream { get; set; }
            public string ContentType{ get { return "d'32da_---"; }}
        }

        private IStorageContext<DummyFile> target;

        [TestInitialize]
        public void Init()
        {
            target = new StorageContext<DummyFile>(new AzureContext());
        }

        [TestClass]
        public class StoreMethod : StorageContextTests
        {
            [TestMethod]
            public void WithNullThrowsException()
            {
                // Act
                Action action = ()=> target.Store(null);

                // Assert
                action.ShouldThrow<ArgumentNullException>(@"Value cannot be null.
Parameter name: entity");
            }

            [TestMethod]
            public void WithNameEmptyThrowsException()
            {
                // Act
                Action action = () => target.Store(new DummyFile());

                // Assert
                action.ShouldThrow<ArgumentNullException>(@"Value cannot be null.
Parameter name: blobName");
            }

            [TestMethod]
            public void WithEmptyStreamThrowsException()
            {
                // Act
                Action action = () => target.Store(new DummyFile
                {
                    Name = "foo"
                });

                // Assert
                action.ShouldThrow<ArgumentNullException>(@"Value cannot be null.
Parameter name: source");
            }

            [TestMethod]
            public void WithValidEntityStores()
            {
                // Act
                target.Store(new DummyFile
                {
                    Name = "foo",
                    Stream = new MemoryStream()
                });

                // Assert
                var result = target.GetBlob("foo");
                result.ShouldNotBeNull();
            }
        }

        [TestClass]
        public class GetBlobMethod : StorageContextTests
        {
            [TestMethod]
            public void WithNullReturnsNull()
            {
                // Act
                var result = target.GetBlob(null);

                // Assert
                result.ShouldBeNull();
            }

            [TestMethod]
            public void WithNameEmptyReturnsNull()
            {
                // Act
                var result = target.GetBlob(string.Empty);

                // Assert
                result.ShouldBeNull();
            }

            [TestMethod]
            public void WithBlobThatDoesNotExistReturnsNull()
            {
                // Act
                var result = target.GetBlob("not a blob");

                // Assert
                result.ShouldBeNull();
            }

            [TestMethod]
            public void WithValidNameReturnsBlob()
            {
                // Arrange
                target.Store(new DummyFile
                {
                    Name = "foo",
                    Stream = new MemoryStream()
                });

                // Act
                var result = target.GetBlob("foo");

                // Assert
                result.ShouldNotBeNull();
            }
        }

        [TestClass]
        public class StoreChunkMethod : StorageContextTests
        {
            [TestMethod]
            public void WithNullThrowsException()
            {
                // Act
                Action action = () => target.StoreChunk(null,0,false);

                // Assert
                action.ShouldThrow<ArgumentNullException>(@"Value cannot be null.
Parameter name: entity");
            }

            [TestMethod]
            public void WithNameEmptyThrowsException()
            {
                // Act
                Action action = () => target.StoreChunk(new DummyFile(), 0, false);

                // Assert
                action.ShouldThrow<ArgumentNullException>(@"Value cannot be null.
Parameter name: blobName");
            }

            [TestMethod]
            public void WithNullStreamThrowsException()
            {
                // Act
                Action action = () => target.StoreChunk(new DummyFile
                {
                    Name = "foo"
                }, 0, false);

                // Assert
                action.ShouldThrow<ArgumentNullException>(@"Value cannot be null.
Parameter name: source");
            }

            [TestMethod]
            public void WithNegativeIndexThrowsException()
            {
                // Act
                Action action = () => target.StoreChunk(new DummyFile
                {
                    Name = "foo",
                    Stream = new MemoryStream()
                }, -1, false);

                // Assert
                action.ShouldThrow<ArgumentException>(@"Cannot be negative
Parameter name: blockIndex");
            }

            [TestMethod]
            public void WithEmptyStreamThrowsException()
            {
                // Act
                Action action = () => target.StoreChunk(new DummyFile
                {
                    Name = "foo",
                    Stream = new MemoryStream()
                }, 0, false);

                // Assert
                action.ShouldThrow<ArgumentException>(@"Stream cannot be empty in block upload
Parameter name: source");
            }

            [TestMethod]
            public void WithValidBlobStores()
            {
                // Arrange
                var blob = new DummyFile
                {
                    Name = "foo",
                    Stream = new MemoryStream(new byte[1])
                };

                // Act
                target.StoreChunk(blob, 0, true);

                // Assert
                target.GetBlob(blob.Name).ShouldNotBeNull();
            }

            [TestMethod]
            public void WithWithValidBlobButNotCommitedDoesNotStore()
            {
                // Arrange
                var valueToStore = "a cool message";
                var bytes = Encoding.UTF8.GetBytes(valueToStore);
                var name = "foo";

                // Act
                for (int i = 0; i < bytes.Length; i++)
                {
                    var blob = new DummyFile
                    {
                        Name = name,
                        Stream = new MemoryStream(new[] { bytes[i] })
                    };

                    target.StoreChunk(blob, i, false);
                }

                // Assert
                var result = target.GetBlob(name);
                result.ShouldNotBeNull();
                var buffer = new byte[bytes.Length];
                result.Stream.Read(buffer, 0, buffer.Length);
                buffer.SequenceEqual(new byte[buffer.Length]).ShouldBe(true);
            }

            [TestMethod]
            public void WithWithValidBlobCommitedDoesStore()
            {
                // Arrange
                var valueToStore = "a cool message";
                var bytes = Encoding.UTF8.GetBytes(valueToStore);
                var name = "foo";

                // Act
                for (int i = 0; i < bytes.Length; i++)
                {
                    var blob = new DummyFile
                    {
                        Name = name,
                        Stream = new MemoryStream(new[] { bytes[i] })
                    };
                    var store = i == bytes.Length - 1;
                    target.StoreChunk(blob, i, store);
                }

                // Assert
                var result = target.GetBlob(name);
                result.ShouldNotBeNull();
                var buffer = new byte[bytes.Length];
                result.Stream.Length.ShouldBe(buffer.Length);
                result.Stream.Read(buffer, 0, buffer.Length);
                buffer.SequenceEqual(bytes).ShouldBe(true);
            }
        }

        [TestClass]
        public class GetPublicReadUrlMethod : StorageContextTests
        {
            [TestMethod]
            public void WithNullNameReturnsNull()
            {
                // Act
                var url = target.GetPublicReadUrl(null, TimeSpan.Zero);

                // Assert
                url.ShouldBeNull();
            }

            [TestMethod]
            public void WithEmptyNameReturnsNull()
            {
                // Act
                var url = target.GetPublicReadUrl(string.Empty, TimeSpan.Zero);

                // Assert
                url.ShouldBeNull();
            }

            [TestMethod]
            public void WithInvalidNameReturnsUrl()
            {
                // Act
                var url = target.GetPublicReadUrl("-", TimeSpan.Zero);

                // Assert
                url.ShouldNotBeNull();
            }

            [TestMethod]
            public void WithNegativeTimespan()
            {
                // Act
                var url = target.GetPublicReadUrl("-", TimeSpan.FromDays(-9000));

                // Assert
                url.ShouldNotBeNull();
            }

            [TestMethod]
            public void WithTimespanTooLongThrowsArgumentException()
            {
                // Act
                Action action = ()=> target.GetPublicReadUrl("-", TimeSpan.FromDays(9000));

                // Assert
                action.ShouldThrow<ArgumentException>("Public read url cannot be live for more than 1 hour.");
            }
        }
    }
}
