using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using FooProject.Collection;
using FooProject.Collection.DataStore;
using Microsoft.VisualStudio.TestPlatform.Utilities;

namespace UnitTest
{
    class ReadOnlyByteDataStore : ReadonlyContentStoreBase<IComposableList<byte>>
    {
        MemoryStream stream;
        public ReadOnlyByteDataStore(MemoryStream stream) : base(8)
        {
            this.stream = stream;
        }

        public override IComposableList<byte> OnLoad(int count, out long index, out int read_bytes)
        {
            byte[] array = new byte[count];
            index = stream.Position;
            read_bytes = stream.Read(array, 0, count);
            var list = new ReadOnlyComposableList<byte>(array.Take(read_bytes));
            return list;
        }

        public override IComposableList<byte> OnRead(long index, int count)
        {
            byte[] array = new byte[count];
            stream.Position = index;
            stream.Read(array, 0, count);
            var list = new ReadOnlyComposableList<byte>(array);
            return list;
        }
    }

    [TestClass]
    public class LasyLoadListTest
    {
        BigList<byte> CreateListAndLoad(IEnumerable<int> collection,out ReadonlyContentStoreBase<IComposableList<byte>> datastore)
        {
            var memoryStream = new MemoryStream();
            int collection_count = collection.Count();
            //面倒なのでオーバーフロー対策のために256のあまりを突っ込んでる
            foreach(var i in collection)
            {
                memoryStream.WriteByte((byte)(i % byte.MaxValue));

            }
            memoryStream.Position = 0;
            var lazyLoadStore = new ReadOnlyByteDataStore(memoryStream);
            var customConverter = new DefaultCustomConverter<byte>();
            customConverter.DataStore = lazyLoadStore;
            BigList<byte> biglist1 = new BigList<byte>();
            biglist1.CustomBuilder = customConverter;
            biglist1.LeastFetchStore = customConverter;
            datastore = lazyLoadStore;
 
            const int loadLen = 8;
            int loopCount = (collection_count + 1) / loadLen;
            for (int i = 0; i < loopCount; i++)
            {
                biglist1.Add(lazyLoadStore.Load(loadLen));
            }
            return biglist1;
        }

        [TestMethod]
        public void Load()
        {
            ReadonlyContentStoreBase<IComposableList<byte>> dataStore;
            var list = CreateListAndLoad(Enumerable.Range(0,byte.MaxValue),out dataStore);

            Assert.AreEqual(byte.MaxValue, list.Count);

            for (int i = 0; i < byte.MaxValue; i++)
            {
                Assert.AreEqual(i, list[i]);
            }
        }

        [TestMethod]
        public void Add()
        {
            ReadonlyContentStoreBase<IComposableList<byte>> dataStore;
            var list = CreateListAndLoad(Enumerable.Range(0, byte.MaxValue), out dataStore);

            list.Add(0);
            list.Add(1);

            Assert.AreEqual(byte.MaxValue + 2, list.Count);

            for (int i = 0; i < byte.MaxValue; i++)
            {
                Assert.AreEqual(i, list[i]);
            }
            Assert.AreEqual(0, list[byte.MaxValue]);
            Assert.AreEqual(1, list[byte.MaxValue + 1]);
        }

        [TestMethod]
        public void AddToFront()
        {
            ReadonlyContentStoreBase<IComposableList<byte>> dataStore;
            var list = CreateListAndLoad(Enumerable.Range(0, byte.MaxValue), out dataStore);

            list.AddToFront(0);
            list.AddToFront(1);

            Assert.AreEqual(byte.MaxValue + 2, list.Count);

            for (int i = 2, j = 0; i < byte.MaxValue + 2; i++, j++)
            {
                Assert.AreEqual(j, list[i]);
            }
        }

        [TestMethod]
        public void InsertTest()
        {
            ReadonlyContentStoreBase<IComposableList<byte>> dataStore;
            var expected = new List<int>(Enumerable.Range(0, byte.MaxValue));
            var list = CreateListAndLoad(expected, out dataStore);

            expected.Insert(0, 0);
            list.Insert(0, 0);

            expected.Insert(4, 0);
            list.Insert(4, 0);

            expected.Insert(255, 0);
            list.Insert(255, 0);

            Assert.AreEqual(expected.Count, list.Count);

            for(int i = 0; i < expected.Count;i++)
            {
                Assert.AreEqual(expected[i],list[i]);
            }
        }

    }
}
