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
