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
                var output = new byte[data.Count * 2 + 4 + 4]; //int32のサイズは4byte、charのサイズ2byte
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
