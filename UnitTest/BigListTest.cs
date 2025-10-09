/*
 *  Copy from
 *  https://github.com/timdetering/Wintellect.PowerCollections
 *  Fooproject modify
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using FooProject.Collection;
using FooProject.Collection.DataStore;
using Microsoft.VisualStudio.TestPlatform.Utilities;

namespace UnitTest
{
    [TestClass]
    public sealed class ListTest
    {
        class TestDataStore<T> : IPinableContainerStore<T>
        {
            public Action<IPinableContainer<T>> AssertType;

            public Action<IPinableContainer<T>,T,long,long> AssertUpdate;

            public void OnAssertType(IPinableContainer<T> pinableContainer)
            {
                if (AssertType != null)
                    AssertType(pinableContainer);
                else
                    throw new NotImplementedException("AssertTypeが実装されてません");
            }

            public void OnAssertUpdate(IPinableContainer<T> pinableContainer, T newcontent, long oldstart, long oldcount, long newstart, long newcount)
            {
                Assert.IsTrue(newstart >= oldstart);
                Assert.IsTrue(newstart + newcount <= oldstart + oldcount);
                if (AssertUpdate != null)
                    AssertUpdate(pinableContainer,newcontent,oldcount,newcount);
                else
                    throw new NotImplementedException("AssertUpdateが実装されてません");
            }

            public IPinnedContent<T> Get(IPinableContainer<T> pinableContainer)
            {
                IPinnedContent<T> result;
                if (TryGet(pinableContainer, out result))
                    return result;
                else
                    throw new ArgumentException();
            }

            public bool TryGet(IPinableContainer<T> pinableContainer, out IPinnedContent<T> result)
            {
                OnAssertType(pinableContainer);
                result = new PinnedContent<T>(pinableContainer, this);
                return true;
            }

            public void Set(IPinableContainer<T> pinableContainer)
            {
                OnAssertType(pinableContainer);
                return;
            }

            public IPinableContainer<T> Update(IPinableContainer<T> pinableContainer, T newcontent, long oldstart, long oldcount, long newstart, long newcount)
            {
                OnAssertUpdate(pinableContainer,newcontent, oldstart, oldcount,newstart,newcount);
                return this.CreatePinableContainer(newcontent);
            }

            public IPinableContainer<T> CreatePinableContainer(T content)
            {
                return new PinableContainer<T>(content);
            }

            public void Commit()
            {
            }
        }

        class TestLeastFetchStore<T> : IStateStore<T>
        {
            public bool Result { get; private set; }
            public ILeastFetch<T> LeastFetch { get; private set; }

            public TestLeastFetchStore()
            {
                Result = false;
                LeastFetch = null;
            }

            public void ResetState()
            {
                this.Result = true;
                LeastFetch = null;
            }

            public void SetState(Node<T> current, long totalLeftCountInList)
            {
                this.LeastFetch = new LeastFetch<T>(current, totalLeftCountInList);
            }
        }

        class TestCustomConverter<T> : DefaultCustomConverter<T>
        {
            public TestCustomConverter() : base() { Result = false; }
            public bool Result { get; private set; }
            public override IComposableList<T> CreateList(long init_capacity, long maxcapacity, IEnumerable<T> collection = null)
            {
                var list = new ReadOnlyComposableList<T>(collection);
                this.Result = true;
                return list;
            }
        }

        [TestMethod]
        public void LeastFetchStoreTest()
        {
            var testConverter = new TestLeastFetchStore<char>();
            var buf = new FooProject.Collection.BigList<char>();
            buf.LeastFetchStore = testConverter;
            buf.Add('t');
            Assert.AreEqual(true, testConverter.Result);
        }

        [TestMethod]
        public void CustomBuilderTest()
        {
            var testConverter = new TestCustomConverter<char>();
            testConverter.DataStore = new MemoryPinableContentDataStore<IComposableList<char>>();
            var buf = new FooProject.Collection.BigList<char>();
            buf.CustomBuilder = testConverter;
            buf.Add('t');
            Assert.AreEqual(true, testConverter.Result);
        }

        [TestMethod]
        public void BlockSizeTest()
        {
            var buf = new FooProject.Collection.BigList<char>();
            buf.BlockSize = 100;
            buf.Add('t');
        }

        [TestMethod]
        public void GetEnumratorTest()
        {
            var buf = new FooProject.Collection.BigList<char>();
            for(int i = 0; i < 26; i++)
                buf.Add('-');
            foreach (char c in buf)
                Assert.AreEqual('-',c);
        }

        [TestMethod]
        public void GetRangeEnumerable()
        {
            BigList<int> list1, list2, list3;
            IEnumerable<int> list4, list5;
            list1 = new BigList<int>();

            list2 = list1.GetRange(4, 0);  // 0 length range permitted anywhere.
            Assert.AreEqual(0, list2.Count);

            list3 = new BigList<int>(new int[] { 1, 2, 3, 4, 5 });
            list4 = list3.GetRange(2, 3);
            InterfaceTests.TestEnumerableElements(list4, new int[] { 3, 4, 5 });
            list5 = list3.GetRange(0, 3);
            InterfaceTests.TestEnumerableElements(list5, new int[] { 1, 2, 3 });
        }

        [TestMethod]
        public void GetRange()
        {
            BigList<int> list1, list2, list3, list4, list5;
            list1 = new BigList<int>();

            list2 = list1.GetRange(4, 0);  // 0 length range permitted anywhere.
            Assert.AreEqual(0, list2.Count);

            list3 = new BigList<int>(new int[] { 1, 2, 3, 4, 5 });
            list4 = list3.GetRange(2, 3);
            InterfaceTests.TestEnumerableElements(list4, new int[] { 3, 4, 5 });
            list5 = list3.GetRange(0, 3);
            InterfaceTests.TestEnumerableElements(list5, new int[] { 1, 2, 3 });
            list3[3] = 7;
            list4[1] = 2;
            list5[2] = 9;
            InterfaceTests.TestEnumerableElements(list4, new int[] { 3, 2, 5 });
            InterfaceTests.TestEnumerableElements(list5, new int[] { 1, 2, 9 });

            list1 = CreateList(0, 132);
            list2 = list1.GetRange(27, 53);
            for (int i = 0; i < 53; ++i)
                Assert.AreEqual(27 + i, list2[i]);
            int y = 27;
            foreach (int x in list2)
                Assert.AreEqual(y++, x);

            list3 = list2.GetRange(4, 27);
            for (int i = 0; i < 27; ++i)
                Assert.AreEqual(31 + i, list3[i]);
            y = 31;
            foreach (int x in list3)
                Assert.AreEqual(y++, x);
        }

        [TestMethod]
        public void GetRangeExceptions()
        {
            BigList<int> list1 = CreateList(0, 100);

            try
            {
                list1.GetRange(3, 98);
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
                Assert.AreEqual("count", ((ArgumentOutOfRangeException)e).ParamName);
            }

            try
            {
                list1.GetRange(-1, 1);
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
                Assert.AreEqual("index", ((ArgumentOutOfRangeException)e).ParamName);
            }

            try
            {
                list1.GetRange(0, int.MaxValue);
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
                Assert.AreEqual("count", ((ArgumentOutOfRangeException)e).ParamName);
            }

            try
            {
                list1.GetRange(1, int.MinValue);
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
                Assert.AreEqual("count", ((ArgumentOutOfRangeException)e).ParamName);
            }

            try
            {
                list1.GetRange(45, int.MinValue);
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
                Assert.AreEqual("count", ((ArgumentOutOfRangeException)e).ParamName);
            }

            try
            {
                list1.GetRange(0, 101);
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
                Assert.AreEqual("count", ((ArgumentOutOfRangeException)e).ParamName);
            }

            try
            {
                list1.GetRange(100, 1);
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
                Assert.AreEqual("index", ((ArgumentOutOfRangeException)e).ParamName);
            }

            try
            {
                list1.GetRange(int.MinValue, 1);
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
                Assert.AreEqual("index", ((ArgumentOutOfRangeException)e).ParamName);
            }

            try
            {
                list1.GetRange(int.MaxValue, 1);
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
                Assert.AreEqual("index", ((ArgumentOutOfRangeException)e).ParamName);
            }

        }

        [TestMethod]
        public void Indexer()
        {
            BigList<int> list1, list2, list3;
            int i;

            list1 = new BigList<int>();
            for (i = 0; i < 100; ++i)
                list1.Add(i);
            for (i = 99; i >= 0; --i)
                Assert.AreEqual(i, list1[i]);

            list2 = new BigList<int>();
            list2.AddRange(list1);
            for (i = 44; i < 88; ++i)
                list1[i] = i * 2;
            for (i = 99; i >= 0; --i)
            {
                Assert.AreEqual(i, list2[i]);
                list2[i] = 99 * i;
            }
            for (i = 44; i < 88; ++i)
                Assert.AreEqual(i * 2, list1[i]);


            list1 = new BigList<int>();
            list2 = new BigList<int>();
            i = 0;
            while (i < 55)
                list1.Add(i++);
            while (i < 100)
                list2.Add(i++);
            list3 = new BigList<int>();
            list3.AddRange(list1);
            list3.AddRange(list2);
            for (i = 0; i < 100; ++i)
                list3[i] = i * 2;
            for (i = 0; i < list1.Count; ++i)
                Assert.AreEqual(i, list1[i]);
            for (i = 0; i < list2.Count; ++i)
                Assert.AreEqual(i + 55, list2[i]);

            list1.Clear();
            i = 0;
            while (i < 100)
                list1.Add(i++);
            list1.AddRange(CreateList(100, 400));
            for (i = 100; i < 200; ++i)
                list1[i] = -1;
            list2 = list1.GetRange(33, 200);
            for (i = 0; i < list2.Count; ++i)
            {
                if (i < 67 || i >= 167)
                    Assert.AreEqual(i + 33, list2[i]);
                else
                    Assert.AreEqual(-1, list2[i]);
            }

            for (i = 22; i < 169; ++i)
                list1[i] = 187 * i;
            for (i = 0; i < list2.Count; ++i)
            {
                if (i < 67 || i >= 167)
                    Assert.AreEqual(i + 33, list2[i]);
                else
                    Assert.AreEqual(-1, list2[i]);
            }
            for (i = 168; i >= 22; --i)
                Assert.AreEqual(187 * i, list1[i]);

            list1.Clear();
            list1.Add(1);
            list1.Add(2);
            list1.Add(3);
            Assert.AreEqual(1, list1[0]);
            Assert.AreEqual(2, list1[1]);
            Assert.AreEqual(3, list1[2]);
            list2 = new BigList<int>();
            list2.AddRange(list1);
            list1[1] = 4;
            list2[0] = 11;
            Assert.AreEqual(11, list2[0]);
            Assert.AreEqual(2, list2[1]);
            Assert.AreEqual(3, list2[2]);
            Assert.AreEqual(1, list1[0]);
            Assert.AreEqual(4, list1[1]);
            Assert.AreEqual(3, list1[2]);

            list2.RemoveRange(0,list2.Count);
            list2.Add(0);
            Assert.AreEqual(0, list2[0]);
        }

        // Try Create an enumerable.
        [TestMethod]
        public void CreateFromEnumerable()
        {
            const int SIZE = 8000;
            int[] items = new int[SIZE];
            BigList<int> biglist1;
            int i;

            for (i = 0; i < SIZE; ++i)
                items[i] = i + 1;
            biglist1 = new BigList<int>(items);

            for (i = 1; i <= SIZE; ++i)
            {
                Assert.AreEqual(i, biglist1[i - 1]);
            }

            i = 1;
            foreach (int x in biglist1)
                Assert.AreEqual(i++, x);

            BigList<int> biglist2 = new BigList<int>(biglist1);

            for (i = 1; i <= SIZE; ++i)
            {
                Assert.AreEqual(i, biglist1[i - 1]);
            }

            i = 1;
            foreach (int x in biglist2)
                Assert.AreEqual(i++, x);
        }


        [TestMethod]
        public void CreateFromEnumerable2()
        {
            int[] array = new int[0];
            BigList<int> biglist1 = new BigList<int>(array);
            Assert.AreEqual(0, biglist1.Count);
        }

        [TestMethod]
        public void IndexerExceptions()
        {
            BigList<int> list1;
            int x;

            list1 = new BigList<int>();
            try
            {
                list1[0] = 1;
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }

            try
            {
                x = list1[0];
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }

            list1 = new BigList<int>(new int[] { 1, 2, 3 });

            list1 = new BigList<int>();
            try
            {
                list1[-1] = 1;
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }

            try
            {
                x = list1[-1];
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }

            list1 = new BigList<int>();
            try
            {
                list1[3] = 1;
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }

            try
            {
                x = list1[3];
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }

            list1 = new BigList<int>();
            try
            {
                list1[int.MaxValue] = 1;
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }

            try
            {
                x = list1[int.MaxValue];
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }

            list1 = new BigList<int>();
            try
            {
                list1[int.MinValue] = 1;
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }

            try
            {
                x = list1[int.MinValue];
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
        }

        [TestMethod]
        public void AppendAll()
        {
            const int SIZE = 8000;
            BigList<int> biglist1 = new BigList<int>();

            int i = 1, j = 0;
            while (i <= SIZE)
            {
                int[] array = new int[j];
                for (int x = 0; x < j; ++x)
                    array[x] = i + x;
                biglist1.AddRange(array);
                /*
                if (i % 13 <= 2)
                    biglist1.Clone();
                */
                i += j;
                j += 1;
                if (j == 20)
                    j = 0;
            }
            int size = i - 1;

            Assert.AreEqual(size, biglist1.Count);

            for (i = 1; i <= size; ++i)
            {
                Assert.AreEqual(i, biglist1[i - 1]);
            }

            i = 1;
            foreach (int x in biglist1)
                Assert.AreEqual(i++, x);

            BigList<int> biglist2 = new BigList<int>(biglist1);

            for (i = 1; i <= SIZE; ++i)
            {
                Assert.AreEqual(i, biglist1[i - 1]);
            }

            i = 1;
            foreach (int x in biglist2)
                Assert.AreEqual(i++, x);
        }

        [TestMethod]
        public void PrependAll()
        {
            const int SIZE = 8000;
            BigList<int> biglist1 = new BigList<int>();

            int i = 1, j = 0;
            while (i <= SIZE)
            {
                int[] array = new int[j];
                for (int x = 0; x < j; ++x)
                    array[j - x - 1] = i + x;
                biglist1.AddRangeToFront(array);
                /*
                if (i % 13 <= 2)
                    biglist1.Clone();
                */
                i += j;
                j += 1;
                if (j == 20)
                    j = 0;
            }
            int size = i - 1;

            Assert.AreEqual(size, biglist1.Count);

            for (i = 1; i <= size; ++i)
            {
                Assert.AreEqual(i, biglist1[size - i]);
            }

            i = size;
            foreach (int x in biglist1)
                Assert.AreEqual(i--, x);

            BigList<int> biglist2 = new BigList<int>(biglist1);

            for (i = 1; i <= size; ++i)
            {
                Assert.AreEqual(i, biglist1[size - i]);
            }

            i = size;
            foreach (int x in biglist2)
                Assert.AreEqual(i--, x);
        }

        [TestMethod]
        public void AppendBigList()
        {
            const int SIZE = 8000;
            BigList<int> biglist1 = new BigList<int>();

            int i = 1, j = 0;
            while (i <= SIZE)
            {
                int[] array = new int[j];
                for (int x = 0; x < j; ++x)
                    array[x] = i + x;
                BigList<int> biglistOther = new BigList<int>(array);
                biglist1.AddRange(biglistOther);
                /*
                if (i % 13 <= 2)
                    biglist1.Clone();
                */
                i += j;
                j += 1;
                if (j == 20)
                    j = 0;
            }
            int size = i - 1;

            Assert.AreEqual(size, biglist1.Count);

            for (i = 1; i <= size; ++i)
            {
                Assert.AreEqual(i, biglist1[i - 1]);
            }

            i = 1;
            foreach (int x in biglist1)
                Assert.AreEqual(i++, x);

            BigList<int> biglist2 = new BigList<int>(biglist1);

            for (i = 1; i <= size; ++i)
            {
                Assert.AreEqual(i, biglist1[i - 1]);
            }

            i = 1;
            foreach (int x in biglist2)
                Assert.AreEqual(i++, x);
        }

        [TestMethod]
        public void AppendBigList2()
        {
            const int SIZE = 8000;
            BigList<int> biglist1 = new BigList<int>();

            biglist1.Add(1);
            biglist1.Add(2);
            int i = 3, j = 11;
            while (i <= SIZE)
            {
                int[] array = new int[j];
                BigList<int> biglistOther = new BigList<int>();
                for (int x = 0; x < j; ++x)
                    biglistOther.AddToFront(i + (j - x - 1));
                /*
                if (j % 7 == 0)
                    biglistOther.Clone();
                */
                biglist1.AddRange(biglistOther);
                /*
                if (i % 13 <= 2)
                    biglist1.Clone();
                */
                i += j;
                j += 1;
                if (j == 20)
                    j = 0;
            }
            int size = i - 1;

            Assert.AreEqual(size, biglist1.Count);

            for (i = 1; i <= size; ++i)
            {
                Assert.AreEqual(i, biglist1[i - 1]);
            }

            i = 1;
            foreach (int x in biglist1)
                Assert.AreEqual(i++, x);

            BigList<int> biglist2 = new BigList<int>(biglist1);

            for (i = 1; i <= size; ++i)
            {
                Assert.AreEqual(i, biglist1[i - 1]);
            }

            i = 1;
            foreach (int x in biglist2)
                Assert.AreEqual(i++, x);
        }

        [TestMethod]
        public void PrependBigList()
        {
            const int SIZE = 8000;
            BigList<int> biglist1 = new BigList<int>();

            int i = 1, j = 0;
            while (i <= SIZE)
            {
                int[] array = new int[j];
                for (int x = 0; x < j; ++x)
                    array[j - x - 1] = i + x;
                BigList<int> biglistOther = new BigList<int>(array);
                biglist1.AddRangeToFront(biglistOther);
                /*
                if (i % 13 <= 2)
                    biglist1.Clone();
                */
                i += j;
                j += 1;
                if (j == 20)
                    j = 0;
            }
            int size = i - 1;

            Assert.AreEqual(size, biglist1.Count);

            for (i = 1; i <= size; ++i)
            {
                Assert.AreEqual(i, biglist1[size - i]);
            }

            i = size;
            foreach (int x in biglist1)
                Assert.AreEqual(i--, x);

            BigList<int> biglist2 = new BigList<int>(biglist1);

            for (i = 1; i <= SIZE; ++i)
            {
                Assert.AreEqual(i, biglist1[size - i]);
            }

            i = size;
            foreach (int x in biglist2)
                Assert.AreEqual(i--, x);
        }

        [TestMethod]
        public void CountTest()
        {
            BigList<int> list1, list2, list3, list4, list5, list6, list7, list8;

            list1 = new BigList<int>();
            list2 = new BigList<int>(new int[0]);
            list3 = new BigList<int>();
            list3.AddRange(list2);
            list3.AddRange(list1);
            Assert.AreEqual(0, list1.Count);
            Assert.AreEqual(0, list2.Count);
            Assert.AreEqual(0, list3.Count);
            list4 = new BigList<int>(new int[2145]);
            Assert.AreEqual(2145, list4.Count);
            list5 = list4.GetRange(1003, 423);
            Assert.AreEqual(423, list5.Count);
            list6 = list4.GetRange(1, 5);
            Assert.AreEqual(5, list6.Count);
            list7 = new BigList<int>();
            list7.AddRange(list5);
            list7.AddRange(list6);
            Assert.AreEqual(428, list7.Count);
            list8 = list7.GetRange(77, 0);
            Assert.AreEqual(0, list8.Count);
            list6.Clear();
            Assert.AreEqual(0, list6.Count);
        }

            [TestMethod]
        public void AppendItem()
        {
            const int SIZE = 8000;
            BigList<int> biglist1 = new BigList<int>();
            int i;

            for (i = 1; i <= SIZE; ++i)
            {
                biglist1.Add(i);
            }

            for (i = 1; i <= SIZE; ++i)
            {
                Assert.AreEqual(i, biglist1[i - 1]);
            }

            i = 1;
            foreach (int x in biglist1)
                Assert.AreEqual(i++, x);

        }

        [TestMethod]
        public void AppendItemPinableContainerTest()
        {
            const int SIZE = 8000;

            var testStore = new TestDataStore<IComposableList<int>>();
            testStore.AssertType = (pin) => {
                Assert.IsTrue(pin is PinableContainer<IComposableList<int>>);
                Assert.IsTrue(pin.Content is FixedList<int>);
            };
            testStore.AssertUpdate = (pin, newcontent, oldcount, newcount) => {
                Assert.IsTrue(pin.Content.Count == oldcount);
                Assert.IsTrue(newcontent.Count == newcount);
            };
            var customConverter = new DefaultCustomConverter<int>();
            customConverter.DataStore = testStore;
            BigList<int> biglist1 = new BigList<int>();
            biglist1.CustomBuilder = customConverter;
            biglist1.LeastFetchStore = customConverter;

            int i;
            for (i = 1; i <= SIZE; ++i)
            {
                var collection = new FixedList<int>();
                collection.Add(i);
                biglist1.Add(testStore.CreatePinableContainer(collection));
            }

            for (i = 1; i <= SIZE; ++i)
            {
                Assert.AreEqual(i, biglist1[i - 1]);
            }

            i = 1;
            foreach (int x in biglist1)
                Assert.AreEqual(i++, x);

        }

        [TestMethod]
        public void PrependItemTest()
        {
            const int SIZE = 8000;
            BigList<int> biglist1 = new BigList<int>();
            int i;

            for (i = 1; i <= SIZE; ++i)
            {
                biglist1.AddToFront(i);
            }

            for (i = 1; i <= SIZE; ++i)
            {
                Assert.AreEqual(i, biglist1[SIZE - i]);
            }

            i = SIZE;
            foreach (int x in biglist1)
                Assert.AreEqual(i--, x);
        }

        [TestMethod]
        public void PrependItemPinableContainerTest()
        {
            const int SIZE = 8000;
            var testStore = new TestDataStore<IComposableList<int>>();
            testStore.AssertType = (pin) => {
                Assert.IsTrue(pin is PinableContainer<IComposableList<int>>);
                Assert.IsTrue(pin.Content is FixedList<int>);
            };
            testStore.AssertUpdate = (pin, newcontent, oldcount, newcount) => {
                Assert.IsTrue(pin.Content.Count == oldcount);
                Assert.IsTrue(newcontent.Count == newcount);
            };
            var customConverter = new DefaultCustomConverter<int>();
            customConverter.DataStore = testStore;
            BigList<int> biglist1 = new BigList<int>();
            biglist1.CustomBuilder = customConverter;
            biglist1.LeastFetchStore = customConverter;
            int i;

            for (i = 1; i <= SIZE; ++i)
            {
                var collection = new FixedList<int>();
                collection.Add(i);
                biglist1.AddToFront(testStore.CreatePinableContainer(collection));
            }

            for (i = 1; i <= SIZE; ++i)
            {
                Assert.AreEqual(i, biglist1[SIZE - i]);
            }

            i = SIZE;
            foreach (int x in biglist1)
                Assert.AreEqual(i--, x);
        }

        [TestMethod]
        public void ContainsTest()
        {
            var buf = new FooProject.Collection.BigList<char>();
            buf.AddRange("0123456789");
            var result = buf.Contains('0');
            Assert.AreEqual(true, result);
            result = buf.Contains('a');
            Assert.AreEqual(false, result);
        }

        private void CheckArray<T>(T[] actual, T[] expected)
        {
            Assert.AreEqual(expected.Length, actual.Length);
            for (int i = 0; i < actual.Length; ++i)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        [TestMethod]
        public void ConcatLeaf()
        {
            BigList<int> list1, list2, list3;
            list1 = CreateList(0, 5);
            list2 = CreateList(5, 7);
            list3 = new BigList<int>();
            list3.AddRange(list1);
            list3.AddRange(list2);
            list1[3] = -1;
            list2[4] = -1;
            for (int i = 0; i < list3.Count; ++i)
                Assert.AreEqual(i, list3[i]);
        }

        [TestMethod]
        public void PrependLeaf()
        {
            BigList<int> list1, list2, list3;

            list1 = new BigList<int>();
            for (int i = 2; i < 50; ++i)
                list1.Add(i);
            list1.AddToFront(1);
            list1.AddToFront(0);
            list3 = new BigList<int>();
            list3.AddRange(list1);
            list2 = CreateList(0, 2);
            list1.AddRangeToFront(list2);
            list1[17] = -1;
            for (int i = 0; i < 50; ++i)
                Assert.AreEqual(i, list3[i]);
        }

        [TestMethod]
        public void CopyTo2()
        {
            string[] array1 = { "foo", "bar", "baz", "smell", "the", "glove" };
            BigList<string> list1 = new BigList<string>(new string[] { "hello", "Sailor" });
            list1.CopyTo(array1, 3);
            CheckArray<string>(array1, new string[] { "foo", "bar", "baz", "hello", "Sailor", "glove" });

            BigList<string> list2 = new BigList<string>();
            list2.CopyTo(array1, 1);
            CheckArray<string>(array1, new string[] { "foo", "bar", "baz", "hello", "Sailor", "glove" });

            BigList<string> list3 = new BigList<string>(new string[] { "a1", "a2", "a3", "a4" });
            list3.CopyTo(array1, 2);
            CheckArray<string>(array1, new string[] { "foo", "bar", "a1", "a2", "a3", "a4" });

            BigList<string> list4 = new BigList<string>(new string[] { "b1", "b2", "b3", "b4", "b5", "b6" });
            list4.CopyTo(array1, 0);
            CheckArray<string>(array1, new string[] { "b1", "b2", "b3", "b4", "b5", "b6" });

            list1.CopyTo(array1, 4);
            CheckArray<string>(array1, new string[] { "b1", "b2", "b3", "b4", "hello", "Sailor" });

            BigList<string> list5 = new BigList<string>();
            string[] array2 = new string[0];
            list5.CopyTo(array2, 0);
            CheckArray<string>(array2, new string[] { });
        }


        [TestMethod]
        public void IndexOf2Test()
        {
            BigList<int> list = new BigList<int>(new int[] { 4, 8, 1, 1, 4, 9, 7, 11, 4, 9, 1, 7, 19, 1, 7 });
            int index;

            index = list.IndexOf(1);
            Assert.AreEqual(2, index);

            index = list.IndexOf(4);
            Assert.AreEqual(0, index);

            index = list.IndexOf(9);
            Assert.AreEqual(5, index);

            index = list.IndexOf(12);
            Assert.AreEqual(-1, index);

            list = new BigList<int>();
            index = list.IndexOf(1);
            Assert.AreEqual(-1, index);
        }

        [TestMethod]
        public void InsertItemTest()
        {
            BigList<int> list1, list2, list3;

            list1 = new BigList<int>();
            list1.AddRange(new int[] { 1, 2, 3, 4, 5, 6, 7 ,8 });
            list1.Insert(1, 2);
            InterfaceTests.TestEnumerableElements<int>(list1, new int[] { 1, 2, 2, 3, 4, 5, 6, 7, 8 });

            list1 = new BigList<int>();
            list1.Insert(0, 34);
            Assert.AreEqual(1, list1.Count);
            Assert.AreEqual(34, list1[0]);
            list1.Insert(1, 78);
            list1.Insert(0, 11);
            list1.Insert(1, 13);
            InterfaceTests.TestEnumerableElements<int>(list1, new int[] { 11, 13, 34, 78 });

            list2 = CreateList(0, 100);
            int j = 300;
            for (int i = 0; i < list2.Count; i += 3)
                list2.Insert(i, j++);

            int k = 0;
            j = 300;
            for (int i = 0; i < list2.Count; ++i)
            {
                if (i % 3 == 0)
                {
                    Assert.AreEqual(j++, list2[i]);
                }
                else
                {
                    Assert.AreEqual(k++, list2[i]);
                }
            }

            list3 = new BigList<int>();
            for (int i = 0; i < 32; ++i)
                list3.Add(i);

            list3.Insert(24, 101);
            list3.Insert(16, 100);
            list3.Insert(8, 102);
            InterfaceTests.TestEnumerableElements<int>(list3, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 102, 8, 9, 10, 11, 12, 13, 14, 15, 100, 16, 17, 18, 19, 20, 21, 22, 23, 101, 24, 25, 26, 27, 28, 29, 30, 31 });
        }

        [TestMethod]
        public void InsertPinableContainerTest()
        {
            var testStore = new TestDataStore<IComposableList<int>>();
            testStore.AssertType = (pin) => {
                Assert.IsTrue(pin is PinableContainer<IComposableList<int>>);
                Assert.IsTrue(pin.Content is FixedList<int>);
            };
            testStore.AssertUpdate = (pin, newcontent, oldcount, newcount) => {
                Assert.IsTrue(pin.Content.Count == oldcount);
                Assert.IsTrue(newcontent.Count == newcount);
            };
            var customConverter = new DefaultCustomConverter<int>();
            customConverter.DataStore = testStore;
            var list2 = CreateList(0, 20, customConverter, customConverter);
            var collection = new int[] { -10, -9, -8, -7, -6, -5, -4, -3, -2, -1 };
            var e1 = new FixedList<int>(collection.Length,collection.Length);
            e1.AddRange(collection,collection.Length);
            list2.Insert(0, testStore.CreatePinableContainer(e1));
            list2.Insert(17, testStore.CreatePinableContainer(e1));
            InterfaceTests.TestEnumerableElements<int>(list2, new int[] { -10, -9, -8, -7, -6, -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, -10, -9, -8, -7, -6, -5, -4, -3, -2, -1, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 });
        }

        [TestMethod]
        public void InsertExceptions()
        {
            BigList<int> list1, list2;
            list1 = CreateList(0, 10);
            list2 = CreateList(4, 5);

            try
            {
                list1.Insert(-1, 5);
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }

            try
            {
                list1.Insert(11, 5);
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }

            try
            {
                var collection = new FixedList<int>();
                collection.Add(5);
                list1.Insert(-1, new PinableContainer<IComposableList<int>>(collection));
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }

            try
            {
                var collection = new FixedList<int>();
                collection.Add(5);
                list1.Insert(11, new PinableContainer<IComposableList<int>>(collection));
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }

            try
            {
                list1.InsertRange(-1, list2);
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }

            try
            {
                list1.InsertRange(11, list2);
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }

            try
            {
                list1.InsertRange(-1, new int[] { 3, 4, 5 });
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }

            try
            {
                list1.InsertRange(11, new int[] { 3, 4, 5 });
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
        }

        [TestMethod]
        public void InsertListTest()
        {
            BigList<int> list2;
            IEnumerable<int> e1;
            list2 = CreateList(0, 20);
            e1 = new int[] { -10, -9, -8, -7, -6, -5, -4, -3, -2, -1 };
            list2.InsertRange(0, e1);
            list2.InsertRange(17, e1);
            InterfaceTests.TestEnumerableElements<int>(list2, new int[] { -10, -9, -8, -7, -6, -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, -10, -9, -8, -7, -6, -5, -4, -3, -2, -1, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 });

            list2 = CreateList(0, 20);
            e1 = new int[] { -10, -9 };
            list2.InsertRange(0, e1);
            list2.InsertRange(17, e1);
            InterfaceTests.TestEnumerableElements<int>(list2, new int[] { -10, -9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, -10, -9, 15, 16, 17, 18, 19 });

            list2 = CreateList(0, 20);
            e1 = new int[] { -10, -9, -8, -7, -6, -5, -4, -3, -2, -1 };
            list2.InsertRange(1, e1);
            InterfaceTests.TestEnumerableElements<int>(list2, new int[] { 0, -10, -9, -8, -7, -6, -5, -4, -3, -2, -1, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, });

            list2 = new BigList<int>();
            list2.AddRange(new int[] { 1, 2, 3, 4, 5 });
            list2.InsertRange(2, new int[] { 9, 8 });
            InterfaceTests.TestEnumerableElements<int>(list2, new int[] { 1, 2, 9, 8, 3, 4, 5 });

            list2 = new BigList<int>();
            list2.Add(1);
            list2.Add(2);
            list2.InsertRange(1, new int[] { 6, 5, 4 });
            list2.InsertRange(2, new int[] { 9, 8 });
            InterfaceTests.TestEnumerableElements<int>(list2, new int[] { 1, 6, 9, 8, 5, 4, 2 });
        }

        [TestMethod]
        public void RemoveAtTest()
        {
            BigList<int> list1 = new BigList<int>();
            for (int i = 0; i < 100; ++i)
                list1.Add(i);

            for (int i = 0; i < 50; ++i)
                list1.RemoveAt(50);

            list1.RemoveAt(0);

            for (int i = 1; i < list1.Count; i += 2)
                list1.RemoveAt(i);

            InterfaceTests.TestEnumerableElements<int>(list1, new int[] { 1, 3, 4, 6, 7, 9, 10, 12, 13, 15, 16, 18, 19, 21, 22, 24, 25, 27, 28, 30, 31, 33, 34, 36, 37, 39, 40, 42, 43, 45, 46, 48, 49 });

            list1 = CreateList(0, 100);

            for (int i = 0; i < 50; ++i)
                list1.RemoveAt(50);

            list1.RemoveAt(0);

            for (int i = 1; i < list1.Count; i += 2)
                list1.RemoveAt(i);

            InterfaceTests.TestEnumerableElements<int>(list1, new int[] { 1, 3, 4, 6, 7, 9, 10, 12, 13, 15, 16, 18, 19, 21, 22, 24, 25, 27, 28, 30, 31, 33, 34, 36, 37, 39, 40, 42, 43, 45, 46, 48, 49 });
        }

        [TestMethod]
        public void RemoveAtTest2()
        {
            var buf = new FooProject.Collection.BigList<char>();
            buf.AddRange("0123456789");
            var result = buf.Remove('0');
            var output = String.Concat<char>(buf);
            Assert.AreEqual("123456789", output);
            Assert.IsTrue(result);
            result = buf.Remove('x');
            Assert.AreEqual("123456789", output);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void RemoveRangeTest()
        {
            BigList<int> list1 = new BigList<int>();
            for (int i = 0; i < 200; ++i)
                list1.Add(i);

            list1.RemoveRange(0, 5);
            list1.RemoveRange(194, 1);
            list1.RemoveRange(50, 0);
            list1.RemoveRange(30, 25);
            list1.RemoveRange(120, 37);

            InterfaceTests.TestEnumerableElements<int>(list1, new int[] { 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31,
                32, 33, 34, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95,
                96, 97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124,
                125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 187, 188, 189,
                190, 191, 192, 193, 194, 195, 196, 197, 198 });

            list1 = CreateList(0, 200);

            list1.RemoveRange(0, 5);
            list1.RemoveRange(194, 1);
            list1.RemoveRange(50, 0);
            list1.RemoveRange(30, 25);
            list1.RemoveRange(120, 37);

            InterfaceTests.TestEnumerableElements<int>(list1, new int[] { 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31,
                32, 33, 34, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95,
                96, 97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124,
                125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 187, 188, 189,
                190, 191, 192, 193, 194, 195, 196, 197, 198 });
        }

        [TestMethod]
        public void RemoveRangeExceptions()
        {
            BigList<int> list1 = CreateList(0, 100);

            try
            {
                list1.RemoveRange(3, 98);
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
                Assert.AreEqual("count", ((ArgumentOutOfRangeException)e).ParamName);
            }

            try
            {
                list1.RemoveRange(-1, 1);
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
                Assert.AreEqual("index", ((ArgumentOutOfRangeException)e).ParamName);
            }

            try
            {
                list1.RemoveRange(0, int.MaxValue);
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
                Assert.AreEqual("count", ((ArgumentOutOfRangeException)e).ParamName);
            }

            try
            {
                list1.RemoveRange(1, int.MinValue);
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
                Assert.AreEqual("count", ((ArgumentOutOfRangeException)e).ParamName);
            }

            try
            {
                list1.RemoveRange(45, int.MinValue);
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
                Assert.AreEqual("count", ((ArgumentOutOfRangeException)e).ParamName);
            }

            try
            {
                list1.RemoveRange(0, 101);
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
                Assert.AreEqual("count", ((ArgumentOutOfRangeException)e).ParamName);
            }

            try
            {
                list1.RemoveRange(100, 1);
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
                Assert.AreEqual("index", ((ArgumentOutOfRangeException)e).ParamName);
            }

            try
            {
                list1.RemoveRange(int.MinValue, 1);
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
                Assert.AreEqual("index", ((ArgumentOutOfRangeException)e).ParamName);
            }

            try
            {
                list1.RemoveRange(int.MaxValue, 1);
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
                Assert.AreEqual("index", ((ArgumentOutOfRangeException)e).ParamName);
            }
        }

        [TestMethod]
        public void AddToSelf()
        {
            BigList<int> list1 = new BigList<int>();

            for (int i = 0; i < 20; ++i)
                list1.Add(i);

            list1.AddRange(list1);
            Assert.AreEqual(40, list1.Count);
            for (int i = 0; i < 40; ++i)
                Assert.AreEqual(i % 20, list1[i]);

            list1.Clear();
            for (int i = 0; i < 20; ++i)
                list1.Add(i);

            list1.AddRangeToFront(list1);
            Assert.AreEqual(40, list1.Count);
            for (int i = 0; i < 40; ++i)
                Assert.AreEqual(i % 20, list1[i]);


            list1.Clear();
            for (int i = 0; i < 20; ++i)
                list1.Add(i);

            list1.InsertRange(7, list1);
            Assert.AreEqual(40, list1.Count);
            for (int i = 0; i < 40; ++i)
            {
                if (i < 7)
                    Assert.AreEqual(i, list1[i]);
                else if (i >= 7 && i < 27)
                    Assert.AreEqual(i - 7, list1[i]);
                else if (i >= 27)
                    Assert.AreEqual(i - 20, list1[i]);
            }
        }

        [TestMethod]
        public void GenericIListInterface()
        {
            BigList<int> list = new BigList<int>();
            int[] array = new int[0];
            //InterfaceTests.TestReadWriteListGeneric<int>((IList<int>)list, array);

            list = CreateList(0, 5);
            array = new int[5];
            for (int i = 0; i < array.Length; ++i)
                array[i] = i;
            InterfaceTests.TestReadWriteListGeneric<int>((IList<int>)list, array);

            list = CreateList(0, 300);
            array = new int[300];
            for (int i = 0; i < array.Length; ++i)
                array[i] = i;
            InterfaceTests.TestReadWriteListGeneric<int>((IList<int>)list, array);
        }

        BigList<int> CreateList(int start, int length, ICustomBuilder<int> builder = null, IStateStore<int> stateStore = null)
        {
            if (length < 24)
            {
                int[] array = new int[length];
                for (int i = 0; i < length; ++i)
                    array[i] = i + start;
                var collection = new BigList<int>();
                if (builder != null)
                    collection.CustomBuilder = builder;
                if (stateStore != null)
                    collection.LeastFetchStore = stateStore;
                collection.AddRange(array);
                return collection;
            }
            else
            {
                int split = length / 5 * 2;
                var collection = CreateList(start, split);
                var other_list = CreateList(start + split, length - split);
                collection.AddRange(other_list);
                return collection;
            }
        }

        [TestMethod]
        public void RemoveAt2Test()
        {
            BigList<int> list1 = new BigList<int>();
            for (int i = 0; i < 100; ++i)
                list1.Add(i);

            for (int i = 0; i < 50; ++i)
                list1.RemoveAt(50);

            list1.RemoveAt(0);

            for (int i = 1; i < list1.Count; i += 2)
                list1.RemoveAt(i);

            InterfaceTests.TestEnumerableElements<int>(list1, new int[] { 1, 3, 4, 6, 7, 9, 10, 12, 13, 15, 16, 18, 19, 21, 22, 24, 25, 27, 28, 30, 31, 33, 34, 36, 37, 39, 40, 42, 43, 45, 46, 48, 49 });

            list1 = CreateList(0, 100);

            for (int i = 0; i < 50; ++i)
                list1.RemoveAt(50);

            list1.RemoveAt(0);

            for (int i = 1; i < list1.Count; i += 2)
                list1.RemoveAt(i);

            InterfaceTests.TestEnumerableElements<int>(list1, new int[] { 1, 3, 4, 6, 7, 9, 10, 12, 13, 15, 16, 18, 19, 21, 22, 24, 25, 27, 28, 30, 31, 33, 34, 36, 37, 39, 40, 42, 43, 45, 46, 48, 49 });
        }

        [TestMethod]
        public void AsReadOnly()
        {
            BigList<int> list1 = CreateList(0, 400);
            int[] elements = new int[400];
            ReadOnlyBigList<int> list2 = list1.AsReadOnly();

            for (int i = 0; i < 400; ++i)
                elements[i] = i;

            InterfaceTests.TestReadOnlyListGeneric<int>(list2, elements, null);

            list1.Add(27);
            list1.AddToFront(98);
            list1[17] = 9;

            elements = new int[402];
            list2 = list1.AsReadOnly();

            for (int i = 0; i < 401; ++i)
                elements[i] = i - 1;

            elements[0] = 98;
            elements[401] = 27;
            elements[17] = 9;

            InterfaceTests.TestReadOnlyListGeneric<int>(list2, elements, null);

            list1 = new BigList<int>();
            list2 = list1.AsReadOnly();
            InterfaceTests.TestReadOnlyListGeneric<int>(list2, new int[0], null);
            list1.Add(4);
            InterfaceTests.TestReadOnlyListGeneric<int>(list2, new int[] { 4 }, null);
        }

        [TestMethod]
        public void TooLarge()
        {
            const int MAXSIZE = 10000;
            BigList<int> listMaxSize = new BigList<int>();
            listMaxSize.MaxCapacity = MAXSIZE;
            for (int i = 0; i < MAXSIZE; i++)
                listMaxSize.Add(6);
            Assert.AreEqual(MAXSIZE, listMaxSize.Count);

            try
            {
                listMaxSize.Add(3);
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }

            try
            {
                listMaxSize.AddToFront(3);
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }

            try
            {
                listMaxSize.Insert(123456, 3);
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }

            listMaxSize = new BigList<int>();
            listMaxSize.MaxCapacity = MAXSIZE;
            for (int i = 0; i < MAXSIZE - 15; i++)
                listMaxSize.Add(6);
            Assert.AreEqual(MAXSIZE - 15, listMaxSize.Count);

            try
            {
                listMaxSize.AddRange(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 });
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }

            try
            {
                listMaxSize.AddRangeToFront(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 });
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }

            try
            {
                listMaxSize.AddRange(new BigList<int>(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 }));
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }

            try
            {
                listMaxSize.AddRangeToFront(new BigList<int>(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 }));
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }

            try
            {
                var collection = new FixedList<int>();
                collection.AddRange(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 });
                listMaxSize.Add(new PinableContainer<IComposableList<int>>(collection));
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }

            try
            {
                var collection = new FixedList<int>();
                collection.AddRange(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 });
                listMaxSize.AddToFront(listMaxSize.CustomBuilder.DataStore.CreatePinableContainer(collection));
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }

            try
            {
                listMaxSize.InsertRange(1, new BigList<int>(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 }));
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }

            try
            {
                listMaxSize.InsertRange(14, new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 });
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }

            try
            {
                var collection = new FixedList<int>();
                collection.AddRange(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 });
                listMaxSize.Insert(1, listMaxSize.CustomBuilder.DataStore.CreatePinableContainer(collection));
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }

            try
            {
                var collection = new FixedList<int>();
                collection.AddRange(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 });
                listMaxSize.Insert(14, listMaxSize.CustomBuilder.DataStore.CreatePinableContainer(collection));
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }

            try
            {
                listMaxSize.AddRange(listMaxSize);
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }
        }

    }
}
