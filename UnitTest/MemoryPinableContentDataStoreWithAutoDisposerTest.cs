using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FooProject.Collection.DataStore;

namespace UnitTest
{
    public class TestContent : IDisposable
    {
        public TestContent(int n)
        {
            Disposed = false;
            Value = n;
        }

        public int Value { get; set; }

        public bool Disposed { get; set; }

        public void Dispose()
        {
            Disposed = true;
        }
    }

    [TestClass]
    public class MemoryPinableContentDataStoreWithAutoDisposerTest
    {
        [TestMethod]
        public void SetTest()
        {
            var test_data = new int[] { 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1100, 1200, 1300, 1400, 1500, 1600, 1700, 1800, 1900, 2000 };

            List<PinableContainer<TestContent>> containers = new List<PinableContainer<TestContent>>();
            int disposedCount = 0;
            var disk = new MemoryPinableContentDataStoreWithAutoDisposer<TestContent>(2);
            disk.Dispoing += (o) =>
            {
                disposedCount++;
            };
            foreach (var item in test_data)
            {
                var data = new PinableContainer<TestContent>(new TestContent(item));
                disk.Set(data);

                containers.Add(data);
            }

            Assert.IsTrue(disposedCount > 0);

            disk.Dispose();
        }

        [TestMethod]
        public void ForEachAvailableContentTest()
        {
            var disk = new MemoryPinableContentDataStoreWithAutoDisposer<TestContent>(2);
            var test_data = new int[] { 100, 200, 300, 400 };
            List<PinableContainer<TestContent>> containers = new List<PinableContainer<TestContent>>();

            foreach (var item in test_data)
            {
                var data = new PinableContainer<TestContent>(new TestContent(item));
                disk.Set(data);
                containers.Add(data);
            }

            var expected_data = new int[] { 400, 300 };
            int i = 0;
            foreach (var item in disk.ForEachAvailableContent())
            {
                Assert.AreEqual(expected_data[i], item.Value);
                i++;
            }

            disk.Commit();

            Assert.AreEqual(0, disk.ForEachAvailableContent().Count());

            disk.Dispose();
        }

        [TestMethod]
        public void CommitTest()
        {
            int disposedCount = 0;
            var disk = new MemoryPinableContentDataStoreWithAutoDisposer<TestContent>(10);
            disk.Dispoing += (o) =>
            {
                disposedCount++;
            };
            var test_data = new int[] { 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1100, 1200, 1300, 1400, 1500, 1600, 1700, 1800, 1900, 2000 };
            List<PinableContainer<TestContent>> containers = new List<PinableContainer<TestContent>>();
            foreach (var item in test_data)
            {
                var data = new PinableContainer<TestContent>(new TestContent (item));
                disk.Set(data);
                containers.Add(data);
            }

            disk.Commit();

            Assert.IsTrue(disposedCount > 0);

            disk.Dispose();
        }

        [TestMethod]
        public void GetTest()
        {
            var serializer = new TestSerializer();
            var disk = new MemoryPinableContentDataStoreWithAutoDisposer<TestContent>(2);
            var test_data = new int[] { 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1100, 1200, 1300, 1400, 1500, 1600, 1700, 1800, 1900, 2000 };
            List<PinableContainer<TestContent>> containers = new List<PinableContainer<TestContent>>();
            foreach (var item in test_data)
            {
                var data = new PinableContainer<TestContent>(new TestContent (item));
                disk.Set(data);
                containers.Add(data);
            }

            int i = 0;
            foreach (var data in containers)
            {
                var pinned = disk.Get(data);
                Assert.AreEqual(test_data[i], pinned.Content.Value);
                pinned.Dispose();
                i++;
            }

            disk.Dispose();
        }
    }
}
