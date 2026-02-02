using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Text;
using FooProject.Collection;
using FooProject.Collection.DataStore;
using Microsoft.VisualStudio.TestPlatform.Utilities;

namespace UnitTest
{
    [TestClass]
    public sealed class ImmutableListTest
    {

        class MixedCustomConverter<T> : DefaultCustomConverter<T>
        {
            public override IComposableList<T> CreateList(long init_capacity, long maxcapacity, IEnumerable<T> collection = null)
            {
                var list = new ReadOnlyComposableList<T>(collection);
                return list;
            }
        }

        [TestMethod]
        public void RemoveAtTest()
        {
            var customBuilder = new MixedCustomConverter<char>();
            customBuilder.DataStore = new MemoryPinableContentDataStore<IComposableList<char>>();
            var init_collection = new ReadOnlyComposableList<char>("this is a pen");
            var buf = new FooProject.Collection.BigList<char>(init_collection, customBuilder, customBuilder);
            buf.RemoveAt(5);
            Assert.AreEqual("this s a pen", new string(buf.ToArray()));
        }

        [TestMethod]
        public void RemoveRangeTest()
        {
            var customBuilder = new MixedCustomConverter<char>();
            customBuilder.DataStore = new MemoryPinableContentDataStore<IComposableList<char>>();
            var init_collection = new ReadOnlyComposableList<char>("this is a pen");
            var buf = new FooProject.Collection.BigList<char>(init_collection, customBuilder, customBuilder);
            buf.RemoveRange(5, 2);
            Assert.AreEqual("this  a pen", new string(buf.ToArray()));
        }

        [TestMethod]
        public void InsertTest()
        {
            var customBuilder = new MixedCustomConverter<char>();
            customBuilder.DataStore = new MemoryPinableContentDataStore<IComposableList<char>>();
            var init_collection = new ReadOnlyComposableList<char>("this  a pen");
            var buf = new FooProject.Collection.BigList<char>(init_collection, customBuilder, customBuilder);
            buf.CustomBuilder = customBuilder;
            buf.Insert(5, 'i');
            Assert.AreEqual("this i a pen", new string(buf.ToArray()));
        }

        [TestMethod]
        public void InsertRangeTest()
        {
            var customBuilder = new MixedCustomConverter<char>();
            customBuilder.DataStore = new MemoryPinableContentDataStore<IComposableList<char>>();
            var init_collection = new ReadOnlyComposableList<char>("this  a pen");
            var buf = new FooProject.Collection.BigList<char>(init_collection, customBuilder, customBuilder);
            buf.CustomBuilder = customBuilder;
            buf.InsertRange(5, "is");
            Assert.AreEqual("this is a pen", new string(buf.ToArray()));
        }

        [TestMethod]
        public void SetTest()
        {
            var customBuilder = new MixedCustomConverter<char>();
            customBuilder.DataStore = new MemoryPinableContentDataStore<IComposableList<char>>();
            var init_collection = new ReadOnlyComposableList<char>("this xs a pen");
            var buf = new FooProject.Collection.BigList<char>(init_collection, customBuilder, customBuilder);
            buf.CustomBuilder = customBuilder;
            buf[5] = 'i';
            Assert.AreEqual("this is a pen", new string(buf.ToArray()));

        }

        [TestMethod]
        public void AddTest()
        {
            var customBuilder = new MixedCustomConverter<char>();
            customBuilder.DataStore = new MemoryPinableContentDataStore<IComposableList<char>>();
            var buf = new FooProject.Collection.BigList<char>("this is a", customBuilder, customBuilder);
            buf.CustomBuilder = customBuilder;
            buf.Add(' ');
            Assert.AreEqual("this is a ", new string(buf.ToArray()));
        }

        [TestMethod]
        public void AddRangeTest()
        {
            var customBuilder = new MixedCustomConverter<char>();
            customBuilder.DataStore = new MemoryPinableContentDataStore<IComposableList<char>>();
            var init_collection = new ReadOnlyComposableList<char>("this is a");
            var buf = new FooProject.Collection.BigList<char>(init_collection, customBuilder, customBuilder);
            buf.CustomBuilder = customBuilder;
            buf.AddRange(" pen");
            Assert.AreEqual("this is a pen", new string(buf.ToArray()));
        }

        [TestMethod]
        public void AddFrontTest()
        {
            var customBuilder = new MixedCustomConverter<char>();
            customBuilder.DataStore = new MemoryPinableContentDataStore<IComposableList<char>>();
            var init_collection = new ReadOnlyComposableList<char>("this is a");
            var buf = new FooProject.Collection.BigList<char>(init_collection, customBuilder, customBuilder);
            buf.CustomBuilder = customBuilder;
            buf.AddToFront(' ');
            Assert.AreEqual(" this is a", new string(buf.ToArray()));
        }

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

        class ReadOnlyTestIndexTableDataSerializer : ISerializeData<IComposableList<TestIndexTableData>>
        {
            public IComposableList<TestIndexTableData> DeSerialize(byte[] inputData)
            {
                var memStream = new MemoryStream(inputData);
                var reader = new BinaryReader(memStream);
                var arrayCount = reader.ReadInt32();
                var array = new List<TestIndexTableData>();
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
                return new ReadOnlyComposableList<TestIndexTableData>(array);
            }

            public byte[] Serialize(IComposableList<TestIndexTableData> data)
            {
                ReadOnlyComposableList<TestIndexTableData> list = (ReadOnlyComposableList<TestIndexTableData>)data;
                //内部配列の確保に時間がかかるので、書き込むメンバー数×バイト数の2倍程度をひとまず確保しておく
                var memStream = new MemoryStream(data.Count * 5 * 8 * 2);
                var writer = new BinaryWriter(memStream, Encoding.Unicode);
                //面倒なのでlongにキャストできるところはlongで書き出す
                writer.Write(list.Count);
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
        private (BigList<TestIndexTableData>, List<TestIndexTableData>, IPinableContainerStore<IComposableList<TestIndexTableData>>) CreateList(int test_size, Stream backingStream)
        {

            BigList<TestIndexTableData> buf = new BigList<TestIndexTableData>();
            var serializer = new ReadOnlyTestIndexTableDataSerializer();
            var str = new List<TestIndexTableData>();
            IPinableContainerStore<IComposableList<TestIndexTableData>> dataStore = new DiskPinableContentDataStore<IComposableList<TestIndexTableData>>(serializer, backingStream, CacheParameters.MINCACHESIZE);
            var customBuilder = new MixedCustomConverter<TestIndexTableData>();
            customBuilder.DataStore = dataStore;
            buf.CustomBuilder = customBuilder;
            buf.BlockSize = 8;

            for (int i = 0; i < test_size; i++)
            {
                int[] test = new int[3] { i + 0, i + 1, i + 2 };
                var test_pattern = new TestIndexTableData() { start = i, length = 1, Syntax = test };
                buf.Add(test_pattern);
                //念のためコピーしておいたほうがいい
                var test_pattern2 = new TestIndexTableData() { start = i, length = 1, Syntax = test.ToArray() };
                str.Add(test_pattern2);
            }

            Assert.AreEqual(str.Count, buf.LongCount);

            return (buf, str, dataStore);
        }

        [TestMethod]
        public void UpdateDiskBaseElementTest()
        {
            const int TEST_SIZE = 100;
            var memStream = new MemoryStream(TEST_SIZE);
            var (buf, str, dataStore) = CreateList(TEST_SIZE, memStream);

            InterfaceTests.TestEnumerableElements(buf, str);
            InterfaceTests.TestIndexerElements(buf, str);

            for (int i = 0; i < buf.Count; i++)
            {
                var info = buf.GetContainerInfo(i);
                using (var pinnable = buf.CustomBuilder.DataStore.Get(info.PinableContainer))
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

        class ReadOnlyStringBufferSerializer : ISerializeData<IComposableList<char>>
        {
            public IComposableList<char> DeSerialize(byte[] inputData)
            {
                var memStream = new MemoryStream(inputData);
                var reader = new BinaryReader(memStream, System.Text.Encoding.Unicode);
                var arrayCount = reader.ReadInt32();
                var array = new ReadOnlyComposableList<char>(reader.ReadChars(arrayCount));
                return array;
            }

            public byte[] Serialize(IComposableList<char> data)
            {
                ReadOnlyComposableList<char> list = (ReadOnlyComposableList<char>)data;
                var output = new byte[data.Count * 2 + 4 + 4]; //int32のサイズは4byte、charのサイズ2byte
                var memStream = new MemoryStream(output);
                var writer = new BinaryWriter(memStream, System.Text.Encoding.Unicode);
                writer.Write(list.Count);
                writer.Write(list.ToArray());
                writer.Close();
                memStream.Dispose();
                return output;
            }
        }

        [TestMethod]
        public void DiskBaseTest()
        {
            //呼ぶべきメソッドを呼んでない可能性があるので、それを検知する

            const string test_pattern = "this is a pen.this is a pen.this is a pen";
            const int TEST_SIZE = 1000;
            BigList<char> buf = new BigList<char>();
            var serializer = new ReadOnlyStringBufferSerializer();
            var memStream = new MemoryStream(TEST_SIZE);
            var str = new StringBuilder();
            IPinableContainerStore<IComposableList<char>> dataStore = new DiskPinableContentDataStore<IComposableList<char>>(serializer, memStream, CacheParameters.MINCACHESIZE);
            var customBuilder = new MixedCustomConverter<char>();
            customBuilder.DataStore = dataStore;
            buf.CustomBuilder = customBuilder;

            for (int i = 0; i < TEST_SIZE; i++)
            {
                buf.AddRange(test_pattern);
                str.Append(test_pattern);
            }
            InterfaceTests.TestEnumerableElements(buf, str);

            for (int i = 0; i < TEST_SIZE; i++)
            {
                if (i % 10 == 0)
                {
                    buf.RemoveAt(i);
                    str.Remove(i, 1);
                }
            }
            InterfaceTests.TestEnumerableElements(buf, str);

            for (int i = 0; i < TEST_SIZE; i++)
            {
                if (i % 10 == 0)
                {
                    buf.RemoveRange(i, 2);
                    str.Remove(i, 2);
                }
            }
            InterfaceTests.TestEnumerableElements(buf, str);

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

            for (int i = 0; i < TEST_SIZE; i++)
            {
                if (i % 10 == 0)
                {
                    buf.InsertRange(i, "ta");
                    str.Insert(i, "ta");
                }
            }
            InterfaceTests.TestEnumerableElements(buf, str);

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

            for (int i = 0; i < TEST_SIZE; i++)
            {
                if (i % 10 == 0)
                {
                    buf.AddRangeToFront("ta");
                    str.Insert(0, "ta");
                }
            }
            InterfaceTests.TestEnumerableElements(buf, str);
        }
    }
}
