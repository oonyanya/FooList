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
    // 注意：テスト用に作ったので、BOMは全く考慮してない
    class ReadOnlyCharDataStore : ReadonlyContentStoreBase<IComposableList<char>>
    {
        MemoryStream stream;
        Encoding _encoding;
        Decoder _decoder;
        long _leastLoadPostion;
        public ReadOnlyCharDataStore(MemoryStream stream,Encoding enc) : base(8)
        {
            this.stream = stream;
            _encoding = enc;
            _decoder = enc.GetDecoder();
            _leastLoadPostion = 0;
        }

        public override IComposableList<char> OnLoad(int count, out long index, out int read_bytes)
        {
            int byte_array_len = _encoding.GetMaxByteCount(count);
            byte[] byte_array = new byte[byte_array_len];

            stream.Position = _leastLoadPostion;

            index = _leastLoadPostion;
            stream.Read(byte_array, 0, byte_array.Length);

            var char_array = new char[count];

            int acutal_bytes, actual_chars;
            bool completed;
            _decoder.Convert(byte_array, 0, byte_array_len, char_array, 0, char_array.Length, false, out  acutal_bytes, out actual_chars, out completed);

            _leastLoadPostion += acutal_bytes;
            read_bytes = acutal_bytes;

            return new ReadOnlyComposableList<char>(char_array);
        }

        public override IComposableList<char> OnRead(long index, int bytes)
        {
            byte[] array = new byte[bytes];
            stream.Position = index;
            stream.Read(array, 0, bytes);
            var str = _encoding.GetString(array, 0, bytes);
            var list = new ReadOnlyComposableList<char>(str);
            return list;
        }
    }

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
        const int loadLen = 8;
        BigList<char> CreateListAndLoad(string str, out ReadonlyContentStoreBase<IComposableList<char>> datastore,Action<BigList<char>> fn = null)
        {
            var memoryStream = new MemoryStream();
            memoryStream.Write(Encoding.UTF8.GetBytes(str));
            memoryStream.Position = 0;
            var lazyLoadStore = new ReadOnlyCharDataStore(memoryStream, Encoding.UTF8);
            var customConverter = new DefaultCustomConverter<char>();
            customConverter.DataStore = lazyLoadStore;
            BigList<char> biglist1 = new BigList<char>();
            biglist1.CustomBuilder = customConverter;
            biglist1.LeastFetchStore = customConverter;
            datastore = lazyLoadStore;

            int leftLen = str.Length;
            while (leftLen - loadLen > 0)
            {
                var pinableContainer = lazyLoadStore.Load(loadLen);
                biglist1.Add(pinableContainer);
                leftLen -= loadLen;
                if (fn != null)
                    fn(biglist1);
            }
            if (leftLen > 0)
            {
                var pinableContainer = lazyLoadStore.Load(leftLen);
                biglist1.Add(pinableContainer);
                if (fn != null)
                    fn(biglist1);
            }

            return biglist1;
        }

        [TestMethod]
        public void LoadStringWithAddAndInsertAndRemoveTest()
        {
            ReadonlyContentStoreBase<IComposableList<char>> dataStore;
            var str = "日本国民は、正当に選挙された国会における代表者を通じて行動し、われらとわれらの子孫のために、諸国民との協和による成果と、わが国全土にわたって自由のもたらす恵沢を確保し、政府の行為によって再び戦争の惨禍が起ることのないやうにすることを決意し、ここに主権が国民に存することを宣言し、この憲法を確定する。";
            var str_builder = new StringBuilder(str);
            var list = CreateListAndLoad(str, out dataStore, (biglist) =>
            {
                switch ((biglist.Count / loadLen) % 5)
                {
                    case 1:
                        str_builder.Insert(biglist.Count, "test");
                        biglist.AddRange("test");
                        break;
                    case 2:
                        str_builder.Insert(biglist.Count - loadLen, "test");
                        biglist.InsertRange(biglist.Count - loadLen, "test");
                        break;
                    case 3:
                        str_builder.Insert(biglist.Count - loadLen / 2, "test");
                        biglist.InsertRange(biglist.Count - loadLen / 2, "test");
                        break;
                    case 4:
                        str_builder.Remove(biglist.Count - loadLen, 2);
                        biglist.RemoveRange(biglist.Count - loadLen ,2);
                        break;
                }
            });

            Assert.AreEqual(str_builder.Length, list.Count);

            for (int i = 0; i < str.Length; i++)
            {
                Assert.AreEqual(str_builder[i], list[i]);
            }
        }

        [TestMethod]
        public void LoadStringTest()
        {
            ReadonlyContentStoreBase<IComposableList<char>> dataStore;
            var str = "日本国民は、正当に選挙された国会における代表者を通じて行動し、われらとわれらの子孫のために、諸国民との協和による成果と、わが国全土にわたって自由のもたらす恵沢を確保し、政府の行為によって再び戦争の惨禍が起ることのないやうにすることを決意し、ここに主権が国民に存することを宣言し、この憲法を確定する。";
            var list = CreateListAndLoad(str, out dataStore);

            Assert.AreEqual(str.Length, list.Count);

            for (int i = 0; i < str.Length; i++)
            {
                Assert.AreEqual(str[i], list[i]);
            }
        }

        [TestMethod]
        public void AddRangeStringTest()
        {
            ReadonlyContentStoreBase<IComposableList<char>> dataStore;
            var str = "日本国民は、正当に選挙された国会における代表者を通じて行動し、われらとわれらの子孫のために、諸国民との協和による成果と、わが国全土にわたって自由のもたらす恵沢を確保し、政府の行為によって再び戦争の惨禍が起ることのないやうにすることを決意し、ここに主権が国民に存することを宣言し、この憲法を確定する。";
            var str_builder = new StringBuilder(str);
            var list = CreateListAndLoad(str, out dataStore);

            str_builder.Append("neko");
            list.AddRange("neko");

            Assert.AreEqual(str_builder.Length, list.Count);

            for (int i = 0; i < str_builder.Length; i++)
            {
                Assert.AreEqual(str_builder[i], list[i]);
            }
        }

        [TestMethod]
        public void AddRangeToFrontStringTest()
        {
            ReadonlyContentStoreBase<IComposableList<char>> dataStore;
            var str = "日本国民は、正当に選挙された国会における代表者を通じて行動し、われらとわれらの子孫のために、諸国民との協和による成果と、わが国全土にわたって自由のもたらす恵沢を確保し、政府の行為によって再び戦争の惨禍が起ることのないやうにすることを決意し、ここに主権が国民に存することを宣言し、この憲法を確定する。";
            var str_builder = new StringBuilder(str);
            var list = CreateListAndLoad(str, out dataStore);

            str_builder.Insert(0,"neko");
            list.AddRangeToFront("neko");

            Assert.AreEqual(str_builder.Length, list.Count);

            for (int i = 0; i < str_builder.Length; i++)
            {
                Assert.AreEqual(str_builder[i], list[i]);
            }
        }

        [TestMethod]
        public void InsertRangeStringTest()
        {
            ReadonlyContentStoreBase<IComposableList<char>> dataStore;
            var str = "日本国民は、正当に選挙された国会における代表者を通じて行動し、われらとわれらの子孫のために、諸国民との協和による成果と、わが国全土にわたって自由のもたらす恵沢を確保し、政府の行為によって再び戦争の惨禍が起ることのないやうにすることを決意し、ここに主権が国民に存することを宣言し、この憲法を確定する。";
            var str_builder = new StringBuilder(str);
            var list = CreateListAndLoad(str, out dataStore);

            str_builder.Insert(0, "neko");
            list.InsertRange(0, "neko");

            str_builder.Insert(4, "neko");
            list.InsertRange(4, "neko");

            str_builder.Insert(31, "neko");
            list.InsertRange(31, "neko");

            str_builder.Insert(str.Length - 1 + 12, "neko");
            list.InsertRange(str.Length - 1 + 12, "neko");

            Assert.AreEqual(str_builder.Length, list.Count);

            for (int i = 0; i < str_builder.Length; i++)
            {
                Assert.AreEqual(str_builder[i], list[i]);
            }
        }

        [TestMethod]
        public void RemoveRangeStringTest()
        {
            ReadonlyContentStoreBase<IComposableList<char>> dataStore;
            var str = "日本国民は、正当に選挙された国会における代表者を通じて行動し、われらとわれらの子孫のために、諸国民との協和による成果と、わが国全土にわたって自由のもたらす恵沢を確保し、政府の行為によって再び戦争の惨禍が起ることのないやうにすることを決意し、ここに主権が国民に存することを宣言し、この憲法を確定する。";
            var str_builder = new StringBuilder(str);
            var list = CreateListAndLoad(str, out dataStore);

            str_builder.Remove(0,2);
            list.RemoveRange(0,2);

            str_builder.Remove(14,2);
            list.RemoveRange(14,2);

            str_builder.Remove(18,2);
            list.RemoveRange(18,2);

            str_builder.Remove(str.Length - 1 - 8, 2);
            list.RemoveRange(str.Length - 1 - 8, 2);

            Assert.AreEqual(str_builder.Length, list.Count);

            for (int i = 0; i < str_builder.Length; i++)
            {
                Assert.AreEqual(str_builder[i], list[i]);
            }
        }

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

            expected.Insert(12, 0);
            list.Insert(12, 0);

            expected.Insert(255, 0);
            list.Insert(255, 0);

            Assert.AreEqual(expected.Count, list.Count);

            for(int i = 0; i < expected.Count;i++)
            {
                Assert.AreEqual(expected[i],list[i]);
            }
        }

        [TestMethod]
        public void RemoveTest()
        {
            ReadonlyContentStoreBase<IComposableList<byte>> dataStore;
            var expected = new List<int>(Enumerable.Range(0, byte.MaxValue));
            var list = CreateListAndLoad(expected, out dataStore);

            expected.RemoveAt(0);
            list.RemoveAt(0);

            expected.RemoveAt(14);
            list.RemoveAt(14);

            expected.RemoveAt(18);
            list.RemoveAt(18);

            expected.RemoveAt(251);
            list.RemoveAt(251);

            Assert.AreEqual(expected.Count, list.Count);

            for (int i = 0; i < expected.Count; i++)
            {
                Assert.AreEqual(expected[i], list[i]);
            }
        }
    }
}
