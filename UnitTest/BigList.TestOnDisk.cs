using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FooProject.Collection;
using FooProject.Collection.DataStore;
using SharedDemoProgram;

namespace UnitTest
{
    [TestClass]
    public class BigListTestOnDisk
    {
        const int TEST_SIZE = 100;

        class TestIndexTableData
        {
            public long start { get; set; }
            public long length { get; set; }

            public int[] Syntax { get; set; }

            public override bool Equals(object? obj)
            {
                var other = (TestIndexTableData)obj;
                return this.start == other.start && this.length == other.length && this.Syntax.SequenceEqual(other.Syntax);
            }
        }

        class TestIndexTableDataSerializer : ISerializeData<IComposableList<TestIndexTableData>>
        {
            public IComposableList<TestIndexTableData> DeSerialize(byte[] inputData)
            {
                var memStream = new MemoryStream(inputData);
                var reader = new BinaryReader(memStream);
                var arrayCount = reader.ReadInt32();
                var maxcapacity = reader.ReadInt32();
                var array = new FixedList<TestIndexTableData>(arrayCount, maxcapacity);
                for (int i = 0; i < arrayCount; i++)
                {
                    var item = new TestIndexTableData();
                    item.start = reader.ReadInt64();
                    item.length = reader.ReadInt64();
                    var syntax_item_count = reader.ReadInt64();
                    if (syntax_item_count > 0)
                    {
                        var syntax_items = new int[syntax_item_count];
                        for (int j = 0; j < syntax_item_count; j++)
                        {
                            var info = reader.ReadInt32();
                            syntax_items[j] = info;
                        }
                        item.Syntax = syntax_items;
                    }
                    else
                    {
                        item.Syntax = null;
                    }
                    array.Add(item);
                }
                return array;
            }

            public byte[] Serialize(IComposableList<TestIndexTableData> data)
            {
                FixedList<TestIndexTableData> list = (FixedList<TestIndexTableData>)data;
                //内部配列の確保に時間がかかるので、書き込むメンバー数×バイト数の2倍程度をひとまず確保しておく
                var memStream = new MemoryStream(data.Count * 5 * 8 * 2);
                var writer = new BinaryWriter(memStream, Encoding.Unicode);
                //面倒なのでlongにキャストできるところはlongで書き出す
                writer.Write(list.Count);
                writer.Write(list.MaxCapacity);
                foreach (var item in list)
                {
                    writer.Write(item.start);
                    writer.Write(item.length);
                    if (item.Syntax == null)
                    {
                        writer.Write(0L);
                    }
                    else
                    {
                        writer.Write((long)item.Syntax.LongLength);
                        foreach (var s in item.Syntax)
                        {
                            writer.Write(s);
                        }
                    }
                }
                writer.Close();
                var result = memStream.ToArray();
                memStream.Dispose();
                return result;
            }
        }

        class StringBufferSerializer : ISerializeData<IComposableList<char>>
        {
            public IComposableList<char> DeSerialize(byte[] inputData)
            {
                var memStream = new MemoryStream(inputData);
                var reader = new BinaryReader(memStream, System.Text.Encoding.Unicode);
                var arrayCount = reader.ReadInt32();
                var maxcapacity = reader.ReadInt32();
                var array = new FixedList<char>(arrayCount, maxcapacity);
                array.AddRange(reader.ReadChars(arrayCount));
                return array;
            }

            public byte[] Serialize(IComposableList<char> data)
            {
                FixedList<char> list = (FixedList<char>)data;
                var output = new byte[list.MaxCapacity * 2 + 4 + 4]; //int32のサイズは4byte、charのサイズ2byte
                var memStream = new MemoryStream(output);
                var writer = new BinaryWriter(memStream, System.Text.Encoding.Unicode);
                writer.Write(list.Count);
                writer.Write(list.MaxCapacity);
                writer.Write(list.ToArray());
                writer.Close();
                memStream.Dispose();
                return output;
            }
        }

        private (BigList<TestIndexTableData>, List<TestIndexTableData>, IPinableContainerStore<IComposableList<TestIndexTableData>>) CreateList(int test_size, Stream backingStream)
        {

            BigList<TestIndexTableData> buf = new BigList<TestIndexTableData>();
            var serializer = new TestIndexTableDataSerializer();
            var str = new List<TestIndexTableData>();
            IPinableContainerStore<IComposableList<TestIndexTableData>> dataStore = new DiskPinableContentDataStore<IComposableList<TestIndexTableData>>(serializer, backingStream, CacheParameters.MINCACHESIZE);
            buf.CustomBuilder.DataStore = dataStore;
            buf.BlockSize = 8;

            for (int i = 0; i < test_size; i++)
            {
                int[] test = new int[3] { i + 0, i + 1, i + 2 };
                var test_pattern = new TestIndexTableData() { start = i ,length = 1, Syntax = test };
                buf.Add(test_pattern);
                //念のためコピーしておいたほうがいい
                var test_pattern2 = new TestIndexTableData() { start = i, length = 1, Syntax = test.ToArray() };
                str.Add(test_pattern2);
            }

            Assert.AreEqual(str.Count, buf.LongCount);

            return (buf, str, dataStore);
        }

        [TestMethod]
        public void UpdateElementTest()
        {
            var memStream = new MemoryStream(TEST_SIZE);
            var (buf, str, dataStore) = CreateList(TEST_SIZE, memStream);

            InterfaceTests.TestEnumerableElements(buf, str);
            InterfaceTests.TestIndexerElements(buf, str);

            for (int i = 0; i < buf.Count; i++)
            {
                var info = buf.GetContainerInfo(i);
                using(var pinnable = buf.CustomBuilder.DataStore.Get(info.PinableContainer))
                {
                    pinnable.Content[(int)info.RelativeIndex].start = i + 1;
                    pinnable.Content[(int)info.RelativeIndex].Syntax = new int[3] { i + 4, i + 5, i + 6 };
                    pinnable.NotifyWriteContent();
                }
                str[i].start = i + 1;
                str[i].Syntax = new int[3] { i + 4, i + 5, i + 6 };
            }

            InterfaceTests.TestEnumerableElements(buf, str);
            InterfaceTests.TestIndexerElements(buf, str);
        }

        private (BigList<char>, StringBuilder, IPinableContainerStore<IComposableList<char>>) CreateList(string test_pattern,int test_size, Stream backingStream)
        {

            BigList<char> buf = new BigList<char>();
            var serializer = new StringBufferSerializer();
            var str = new StringBuilder();
            IPinableContainerStore<IComposableList<char>> dataStore = new DiskPinableContentDataStore<IComposableList<char>>(serializer, backingStream, CacheParameters.MINCACHESIZE);
            buf.CustomBuilder.DataStore = dataStore;
            buf.BlockSize = 8;

            for (int i = 0; i < test_size; i++)
            {
                buf.AddRange(test_pattern);
                str.Append(test_pattern);
            }

            Assert.AreEqual(str.Length, buf.LongCount);

            return (buf, str, dataStore);
        }

        [TestMethod]
        public void RemoveRangeAndInsertRangeTest()
        {
            const string test_pattern = "this is a pen.this is a pen.this is a pen";
            var memStream = new MemoryStream(TEST_SIZE);
            var (buf, str, dataStore) = CreateList(test_pattern, TEST_SIZE, memStream);

            var target = "pen";
            var pattern = "ratking";
            TextSearch ts = new TextSearch(target, false);
            char[] pattern_chars = pattern.ToCharArray();
            long left = 0, right = buf.LongCount;
            while (right != -1)
            {
                while ((right = ts.IndexOf(buf, left, buf.LongCount - 1)) != -1)
                {
                    buf.RemoveRange(right, target.Length);
                    buf.InsertRange(right, pattern_chars);
                    str.Remove((int)right, target.Length);
                    str.Insert((int)right, pattern_chars);
                    left = right + pattern.Length;
                }
            }

            InterfaceTests.TestEnumerableElements(buf, str);
            InterfaceTests.TestIndexerElements(buf, str);

        }

        [TestMethod]
        public void AddRangeTest()
        {
            const string test_pattern = "this is a pen.this is a pen.this is a pen";
            //CreateListのなかでAddRange()を呼び出してる
            var memStream = new MemoryStream(TEST_SIZE);
            var (buf, str, dataStore) = CreateList(test_pattern, TEST_SIZE, memStream);

            InterfaceTests.TestEnumerableElements(buf, str);
            InterfaceTests.TestIndexerElements(buf, str);
        }

        [TestMethod]
        public void RemoveAtTest()
        {
            const string test_pattern = "this is a pen.this is a pen.this is a pen";
            var memStream = new MemoryStream(TEST_SIZE);
            var (buf, str, dataStore) = CreateList(test_pattern, TEST_SIZE, memStream);

            for (int i = 0; i < TEST_SIZE; i++)
            {
                if (i % 10 == 0)
                {
                    buf.RemoveAt(i);
                    str.Remove(i, 1);
                }
            }
            InterfaceTests.TestEnumerableElements(buf, str);
            InterfaceTests.TestIndexerElements(buf, str);
        }


        [TestMethod]
        public void RemoveRangeTest()
        {
            const string test_pattern = "this is a pen.this is a pen.this is a pen";
            var memStream = new MemoryStream(TEST_SIZE);
            var (buf, str, dataStore) = CreateList(test_pattern, TEST_SIZE, memStream);

            for (int i = 0; i < TEST_SIZE; i++)
            {
                if (i % 10 == 0)
                {
                    buf.RemoveRange(i, 2);
                    str.Remove(i, 2);
                }
            }
            InterfaceTests.TestEnumerableElements(buf, str);
            InterfaceTests.TestIndexerElements(buf, str);
        }

        [TestMethod]
        public void InsertTest()
        {
            const string test_pattern = "this is a pen.this is a pen.this is a pen";
            var memStream = new MemoryStream(TEST_SIZE);
            var (buf, str, dataStore) = CreateList(test_pattern, TEST_SIZE, memStream);

            for (int i = 0; i < TEST_SIZE; i++)
            {
                if (i % 10 == 0)
                {
                    buf.Insert(i, 't');
                    str.Insert(i, 't');

                    var list = buf.CustomBuilder.CreateList(buf.BlockSize, buf.BlockSize, "t");
                    var pin = dataStore.CreatePinableContainer(list);
                    buf.Insert(i, pin);
                    str.Insert(i, 't');
                }
            }
            InterfaceTests.TestEnumerableElements(buf, str);
            InterfaceTests.TestIndexerElements(buf, str);
        }

        [TestMethod]
        public void InsertRangeTest()
        {
            const string test_pattern = "this is a pen.this is a pen.this is a pen";
            var memStream = new MemoryStream(TEST_SIZE);
            var (buf, str, dataStore) = CreateList(test_pattern, TEST_SIZE, memStream);

            for (int i = 0; i < TEST_SIZE; i++)
            {
                if (i % 10 == 0)
                {
                    buf.InsertRange(i, "ta");
                    str.Insert(i, "ta");
                }
            }
            InterfaceTests.TestEnumerableElements(buf, str);
            InterfaceTests.TestIndexerElements(buf, str);

        }

        [TestMethod]
        public void AddTest()
        {
            const string test_pattern = "this is a pen.this is a pen.this is a pen";
            var memStream = new MemoryStream(TEST_SIZE);
            var (buf, str, dataStore) = CreateList(test_pattern, TEST_SIZE, memStream);

            for (int i = 0; i < TEST_SIZE; i++)
            {
                if (i % 10 == 0)
                {
                    buf.Add('t');
                    str.Append("t");

                    var list = buf.CustomBuilder.CreateList(buf.BlockSize, buf.BlockSize, "t");
                    var pin = dataStore.CreatePinableContainer(list);
                    buf.Add(pin);
                    str.Append('t');
                }
            }
            InterfaceTests.TestEnumerableElements(buf, str);
            InterfaceTests.TestIndexerElements(buf, str);
        }

        [TestMethod]
        public void AddToFrontTest()
        {
            const string test_pattern = "this is a pen.this is a pen.this is a pen";
            var memStream = new MemoryStream(TEST_SIZE);
            var (buf, str, dataStore) = CreateList(test_pattern, TEST_SIZE, memStream);

            for (int i = 0; i < TEST_SIZE; i++)
            {
                if (i % 10 == 0)
                {
                    buf.AddToFront('t');
                    str.Insert(0, "t");

                    var list = buf.CustomBuilder.CreateList(buf.BlockSize, buf.BlockSize, "t");
                    var pin = dataStore.CreatePinableContainer(list);
                    buf.AddToFront(pin);
                    str.Insert(0, "t");
                }
            }
            InterfaceTests.TestEnumerableElements(buf, str);
            InterfaceTests.TestIndexerElements(buf, str);
        }

        [TestMethod]
        public void AddRangeToFront()
        {
            const string test_pattern = "this is a pen.this is a pen.this is a pen";
            var memStream = new MemoryStream(TEST_SIZE);
            var (buf, str, dataStore) = CreateList(test_pattern, TEST_SIZE, memStream);

            for (int i = 0; i < TEST_SIZE; i++)
            {
                if (i % 10 == 0)
                {
                    buf.AddRangeToFront("ta");
                    str.Insert(0, "ta");
                }
            }
            InterfaceTests.TestEnumerableElements(buf, str);
            InterfaceTests.TestIndexerElements(buf, str);
        }
    }
}
