using System;
using System.Collections.Generic;
using System.Linq;
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
            return new int[] { reader.ReadInt32() };
        }

        public byte[] Serialize(int[] data)
        {
            var output = new byte[4];
            var memStream = new MemoryStream(output);
            var writer = new BinaryWriter(memStream);
            writer.Write(data[0]);
            writer.Close();
            memStream.Dispose();
            return output;
        }
    }

    [TestClass]
    public class DiskPinableContentDataStoreTest
    {
        [TestMethod]
        public void SetTest()
        {
            var serializer = new TestSerializer();
            var disk = new DiskPinableContentDataStore<int[]>(serializer);
            var test_data = new int[] { 100, 200,300,400 };
            foreach(var item in test_data)
            {
                var data = new PinableContainer<int[]>(new int[] { item });
                disk.Set(data);
                Assert.AreEqual(null, data.Content);

                var pinned = disk.Get(data);
                pinned.Content[0] = item + 1;
                pinned.Dispose();

                pinned = disk.Get(data);
                Assert.AreEqual(item + 1,pinned.Content[0]);
                pinned.Dispose();
            }
        }

        [TestMethod]
        public void GetTest()
        {
            var serializer = new TestSerializer();
            var disk = new DiskPinableContentDataStore<int[]>(serializer);
            var test_data = new int[] { 100, 200, 300, 400 };
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
