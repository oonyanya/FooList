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
        [TestMethod]
        public void SetTest()
        {
            var serializer = new TestSerializer();
            var test_data = new int[] { 100, 200,300,400,500,600,700,800,900,1000,1100,1200,1300,1400,1500,1600,1700,1800,1900,2000 };

            //ページサイズが32KBなので、初めはページサイズに収まる奴でテストし、次はページサイズからあふれる奴でテストする
            //1回目：4096 Byte + ヘッダーサイズ、２回目：32768 Byte + ヘッダーサイズ
            int[] repeatLengths = new int[] { 1024, 8192 };

            foreach (var repeatLength in repeatLengths)
            {
                List<PinableContainer<int[]>> containers = new List<PinableContainer<int[]>>();
                var disk = new DiskPinableContentDataStore<int[]>(serializer, 2);
                foreach (var item in test_data)
                {
                    var data = new PinableContainer<int[]>(Enumerable.Repeat(item, repeatLength).ToArray());
                    disk.Set(data);

                    var pinned = disk.Get(data);
                    pinned.Content[0] = item + 1;
                    pinned.Dispose();

                    pinned = disk.Get(data);
                    Assert.AreEqual(item + 1, pinned.Content[0]);
                    pinned.Dispose();

                    containers.Add(data);
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

                    var pinned = disk.Get(data);
                    Assert.AreEqual(item, pinned.Content[0]);
                    pinned.Dispose();

                    containers.Add(data);
                }

                disk.Dispose();
            }
        }

        [TestMethod]
        public void GetTest()
        {
            var serializer = new TestSerializer();
            var disk = new DiskPinableContentDataStore<int[]>(serializer, 2);
            var test_data = new int[] { 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1100, 1200, 1300, 1400, 1500, 1600, 1700, 1800, 1900, 2000 };
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
