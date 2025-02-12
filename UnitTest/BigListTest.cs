using System;
using FooProject.Collection;
using Microsoft.VisualStudio.TestPlatform.Utilities;

namespace UnitTest
{
    [TestClass]
    public sealed class ListTest
    {
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
        public void GetAtTest()
        {
            var buf = new FooProject.Collection.BigList<char>();
            buf.AddRange("abcdefghijklmnoplqrstuvwxyz");

            Assert.AreEqual('a', buf[0]);
            Assert.AreEqual('h', buf[7]);
            Assert.AreEqual('i', buf[8]);
            Assert.AreEqual('p', buf[15]);
            Assert.AreEqual('l', buf[16]);
            Assert.AreEqual('w', buf[23]);
            Assert.AreEqual('x', buf[24]);
            Assert.AreEqual('z', buf[26]);

            buf.AddRange("-");
            Assert.AreEqual('-', buf[27]);
        }

        [TestMethod]
        public void SetAtTest()
        {
            var buf = new FooProject.Collection.BigList<char>();
            buf.AddRange("abcdefghijklmnoplqrstuvwxyz");

            buf[0] = '-';
            Assert.AreEqual('-', buf[0]);

            buf[7] = '-';
            Assert.AreEqual('-', buf[7]);

            buf[8] = '-';
            Assert.AreEqual('-', buf[8]);

            buf[15] = '-';
            Assert.AreEqual('-', buf[15]);

            buf[16] = '-';
            Assert.AreEqual('-', buf[16]);

            buf[23] = '-';
            Assert.AreEqual('-', buf[23]);

            buf[24] = '-';
            Assert.AreEqual('-', buf[24]);

            buf[26] = '-';
            Assert.AreEqual('-', buf[26]);

            buf.AddRange(";");
            buf[27] = '-';
            Assert.AreEqual('-', buf[27]);
        }

        [TestMethod]
        public void AddRangeFrontTest()
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
        }

        [TestMethod]
        public void AddRangeTest()
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
        }

        [TestMethod]
        public void CountTest()
        {
            var buf = new FooProject.Collection.BigList<char>();
            Assert.AreEqual(0, buf.Count);
            buf.AddRange("abcdefghijklmnoplqrstuvwxyz");
            Assert.AreEqual(27, buf.Count);
        }

        [TestMethod]
        public void AddTest()
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
        public void AddFrontTest()
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
        public void ContainsTest()
        {
            var buf = new FooProject.Collection.BigList<char>();
            buf.AddRange("0123456789");
            var result = buf.Contains('0');
            Assert.AreEqual(true, result);
            result = buf.Contains('a');
            Assert.AreEqual(false, result);
        }

        [TestMethod]
        public void CopyToTest()
        {
            var buf = new FooProject.Collection.BigList<char>();
            char[] result = new char[6];
            buf.AddRange("012345");
            buf.CopyTo(result, 0);
            Assert.AreEqual("012345", String.Concat<char>(result));
        }

        [TestMethod]
        public void IndexTest()
        {
            var buf = new FooProject.Collection.BigList<char>();
            buf.AddRange("0123456789");
            var result = buf.IndexOf('0');
            Assert.AreEqual(0, result);
            result = buf.IndexOf('a');
            Assert.AreEqual(-1, result);
        }

        [TestMethod]
        public void InsertTest()
        {
            BigList<int> list1, list2, list3;

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
        public void InserRangetTest()
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
        public void RemoveTest()
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

        BigList<int> CreateList(int start, int length)
        {
            if (length < 24)
            {
                int[] array = new int[length];
                for (int i = 0; i < length; ++i)
                    array[i] = i + start;
                var collection = new BigList<int>();
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
    }
}
