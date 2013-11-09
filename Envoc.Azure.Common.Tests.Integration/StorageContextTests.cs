using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Envoc.Azure.Common.Persistance;
using Envoc.Azure.Common.Persistance.Blob;
using Envoc.Core.UnitTests.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NCrunch.Framework;
using Tests.Common;

namespace Envoc.Azure.Common.Tests.Integration
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
