using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using FooProject.Collection;
using FooProject.Collection.DataStore;
using Microsoft.VisualStudio.TestPlatform.Utilities;

namespace UnitTest
{
    class TestSerializer : ISerializeData<int[]>
    {
        public int[] DeSerialize(byte[] inputData)
        {
            var memStream = new MemoryStream(inputData);
            var reader = new BinaryReader(memStream);
            var arrayCount = reader.ReadInt32();
            var array = new int[arrayCount];
            for (int i = 0; i < arrayCount; i++)
            {
                array[i] = reader.ReadInt32();
            }
            return array;
        }

        public byte[] Serialize(int[] data)
        {
            var output = new byte[data.Length * 4 + 4]; //int32のサイズは4byte
            var memStream = new MemoryStream(output);
            var writer = new BinaryWriter(memStream);
            writer.Write(data.Length);
            for(int i = 0; i < data.Length; i++)
            {
                writer.Write(data[i]);
            }
            writer.Close();
            memStream.Dispose();
            return output;
        }
    }

    [TestClass]
    public class DiskPinableContentDataStoreTest
    {
        const int CACHE_SIZE = 4;
        [TestMethod]
        public void ConstructorTest()
        {
            const int TEST_SIZE = 50;
            var serializer = new TestSerializer();
            var disk = new DiskPinableContentDataStore<int[]>(serializer, null, CACHE_SIZE);
            var test_data = Enumerable.Range(1, TEST_SIZE).Select((i) => { return i * 100; }).ToArray();
            List<PinableContainer<int[]>> containers = new List<PinableContainer<int[]>>();
            foreach (var item in test_data)
            {
                var data = new PinableContainer<int[]>(new int[] { item });
                disk.Set(data);
                containers.Add(data);
            }

            int i = 0;
            foreach (var data in containers)
            {
                var pinned = disk.Get(data);
                Assert.AreEqual(test_data[i], pinned.Content[0]);
                pinned.Dispose();
                i++;
            }

            disk.Dispose();
        }


        [TestMethod]
        public void CloneTest()
        {
            const int TEST_SIZE = 50;
            var serializer = new TestSerializer();
            var disk = new DiskPinableContentDataStore<int[]>(serializer, null, CACHE_SIZE);
            var test_data = Enumerable.Range(1, TEST_SIZE).Select((i) => { return i * 100; }).ToArray();
            List<PinableContainer<int[]>> containers = new List<PinableContainer<int[]>>();
            foreach (var item in test_data)
            {
                var data = new PinableContainer<int[]>(new int[] { item });
                disk.Set(data);
                containers.Add(data);
            }

            disk.Commit();

            foreach (var data in containers)
            {
                var newpinned = (PinableContainer<int[]>)disk.Clone(data, null);
                Assert.AreEqual(data.Content, null);
                Assert.AreEqual(data.ID, newpinned.ID);
                Assert.AreEqual(data.IsRemoved, newpinned.IsRemoved);
            }

            disk.Dispose();
        }

        [TestMethod]
        public void SetTest()
        {
            var serializer = new TestSerializer();
            var test_data = Enumerable.Range(1, 20).Select((i) => { return i * 100; }).ToArray();

            //ページサイズが32KBなので、初めはページサイズに収まる奴でテストし、次はページサイズからあふれる奴でテストする
            //1回目：4096 Byte + ヘッダーサイズ、２回目：32768 Byte + ヘッダーサイズ
            int[] repeatLengths = new int[] { 1024, 8192 };

            foreach (var repeatLength in repeatLengths)
            {
                List<PinableContainer<int[]>> containers = new List<PinableContainer<int[]>>();
                int disposedCount = 0;
                var disk = new DiskPinableContentDataStore<int[]>(serializer, CACHE_SIZE);
                disk.Disposeing += (o) =>
                {
                    disposedCount++;
                };

                foreach (var item in test_data)
                {
                    var data = new PinableContainer<int[]>(Enumerable.Repeat(item, repeatLength).ToArray());
                    disk.Set(data);

                    var pinned = disk.Get(data);
                    pinned.Content[0] = item + 1;
                    pinned.Dispose();

                    containers.Add(data);
                }

                int i = 0;
                foreach (var data in containers)
                {
                    var pinned = disk.Get(data);
                    Assert.AreEqual(test_data[i] + 1, pinned.Content[0]);
                    pinned.Dispose();
                    i++;
                }

                foreach (var data in containers)
                {
                    var pinned = disk.Get(data);
                    pinned.RemoveContent();
                    pinned.Dispose();
                }

                containers.Clear();

                foreach (var item in test_data)
                {
                    var data = new PinableContainer<int[]>(Enumerable.Repeat(item, repeatLength / 2).ToArray());
                    disk.Set(data);

                    containers.Add(data);
                }

                i = 0;
                foreach (var data in containers)
                {
                    var pinned = disk.Get(data);
                    Assert.AreEqual(test_data[i], pinned.Content[0]);
                    pinned.Dispose();
                    i++;
                }

                Assert.IsTrue(disposedCount > 0);

                disk.Dispose();
            }
        }

        [TestMethod]
        public void ForEachAvailableContentTest()
        {
            var serializer = new TestSerializer();
            var disk = new DiskPinableContentDataStore<int[]>(serializer, CACHE_SIZE);
            var test_data = Enumerable.Range(1, 20).Select((i) => { return i * 100; }).ToArray();
            List<PinableContainer<int[]>> containers = new List<PinableContainer<int[]>>();

            foreach (var item in test_data)
            {
                var data = new PinableContainer<int[]>(new int[] { item });
                disk.Set(data);
                containers.Add(data);
            }

            Assert.AreEqual(true, disk.ForEachAvailableContent().Any());

            disk.Commit();

            Assert.AreEqual(0, disk.ForEachAvailableContent().Count());

            disk.Dispose();
        }

        [TestMethod]
        public void CommitTest()
        {
            var serializer = new TestSerializer();
            int disposedCount = 0;
            var disk = new DiskPinableContentDataStore<int[]>(serializer, CACHE_SIZE);
            disk.Disposeing += (o) =>
            {
                disposedCount++;
            };
            var test_data = Enumerable.Range(1, 20).Select((i) => { return i * 100; }).ToArray();
            List<PinableContainer<int[]>> containers = new List<PinableContainer<int[]>>();
            foreach (var item in test_data)
            {
                var data = new PinableContainer<int[]>(new int[] { item });
                disk.Set(data);
                containers.Add(data);
            }

            Assert.IsTrue(disposedCount > 0);

            disposedCount = 0;

            disk.Commit();

            Assert.IsTrue(disposedCount > 0);

            int i = 0;
            foreach (var data in containers)
            {
                var pinned = disk.Get(data);
                Assert.AreEqual(test_data[i], pinned.Content[0]);
                pinned.Dispose();
                i++;
            }
        }

        [TestMethod]
        public void GetTest()
        {
            var serializer = new TestSerializer();
            var disk = new DiskPinableContentDataStore<int[]>(serializer, CACHE_SIZE);
            var test_data = Enumerable.Range(1, 20).Select((i) => { return i * 100; }).ToArray();
            List<PinableContainer<int[]>> containers = new List<PinableContainer<int[]>>();
            foreach (var item in test_data)
            {
                var data = new PinableContainer<int[]>(new int[] { item });
                disk.Set(data);
                containers.Add(data);
            }

            int i = 0;
            foreach (var data in containers)
            {
                var pinned = disk.Get(data);
                Assert.AreEqual(test_data[i], pinned.Content[0]);
                pinned.Dispose();
                i++;
            }

        }

    }
}
