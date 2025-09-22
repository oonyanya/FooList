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
        BigList<byte> CreateListAndLoad(int maxvalue,out ReadonlyContentStoreBase<IComposableList<byte>> datastore)
        {
            var memoryStream = new MemoryStream();
            for (int i = 0; i < maxvalue; i++)
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
            int loopCount = (maxvalue + 1) / loadLen;
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
            var list = CreateListAndLoad(byte.MaxValue,out dataStore);

            Assert.AreEqual(byte.MaxValue, list.Count);

            for (int i = 0; i < byte.MaxValue; i++)
            {
                Assert.AreEqual(i, list[i]);
            }
        }

    }
}
