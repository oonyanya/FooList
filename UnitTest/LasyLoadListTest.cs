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
            var list = new FixedList<byte>(array.Length, array.Length);
            list.AddRange(array.Take(read_bytes));
            return list;
        }

        public override IComposableList<byte> OnRead(long index, int count)
        {
            byte[] array = new byte[count];
            stream.Position = index;
            stream.Read(array, 0, count);
            var list = new FixedList<byte>(array.Length, array.Length);
            list.AddRange(array);
            return list;
        }
    }

    [TestClass]
    public class LasyLoadListTest
    {
        BigList<byte> CreateList(out ReadonlyContentStoreBase<IComposableList<byte>> datastore,out long loadedCount)
        {
            var memoryStream = new MemoryStream();
            for (int i = 0; i < byte.MaxValue; i++)
            {
                memoryStream.WriteByte((byte)i);
            }
            memoryStream.Position = 0;
            var lazyLoadStore = new ReadOnlyByteDataStore(memoryStream);
            var customConverter = new DefaultCustomConverter<byte>();
            customConverter.DataStore = lazyLoadStore;
            BigList<byte> biglist1 = new BigList<byte>();
            biglist1.CustomBuilder = customConverter;
            biglist1.LeastFetchStore = customConverter;
            datastore = lazyLoadStore;
            // byte.MaxValueを8で割ると32になる
            for (int i = 0; i < 32; i++)
            {
                biglist1.Add(lazyLoadStore.Load(8));
            }
            loadedCount = byte.MaxValue;
            return biglist1;
        }

        [TestMethod]
        public void Load()
        {
            ReadonlyContentStoreBase<IComposableList<byte>> dataStore;
            long loadedCount;
            var list = CreateList(out dataStore,out loadedCount);

            Assert.AreEqual(loadedCount, list.Count);

            for (int i = 0; i < byte.MaxValue; i++)
            {
                Assert.AreEqual(i, list[i]);
            }
        }
    }
}
