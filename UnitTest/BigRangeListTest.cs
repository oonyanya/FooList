using FooProject.Collection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace UnitTest
{
    [TestClass]
    public class BigRangeListTest
    {
        private void AssertAreRangeEqual(int[] expected, BigRangeList<MyRange> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var n = list.GetIndexIntoRange(i);
                Assert.AreEqual(expected[i], n.start);
            }
        }

        [TestMethod]
        public void AddTest()
        {
            BigRangeList<MyRange> list = new BigRangeList<MyRange>();
            const int length = 3;
            int index = 0;
            for (int i = 0; i < 8; i++)
            {
                list.Add(new MyRange(index, length));
                index += length;
            }
            list.Insert(0, new MyRange(0, length));
            list.Insert(list.Count, new MyRange(index, length));

            var expected = new int[] { 0, 3, 6, 9, 12, 15, 18, 21, 24, 27, };
            AssertAreRangeEqual(expected, list);
        }

        [TestMethod]
        public void AddRangeTest()
        {
            var list = new BigRangeList<MyRange>();
            var rangeList = new List<MyRange>();
            const int length = 3;
            int index = 0;
            for (int i = 0; i < 8; i++)
            {
                rangeList.Add(new MyRange(index, length));
                index += length;
            }
            list.AddRange(rangeList);
            list.Insert(0, new MyRange(0,length));
            list.Insert(list.Count, new MyRange(index,length));

            var expected = new int[] { 0, 3, 6, 9, 12, 15, 18, 21, 24, 27, };
            AssertAreRangeEqual(expected, list);
        }

        [TestMethod]
        public void InsertTest()
        {
            BigRangeList<MyRange> list = new BigRangeList<MyRange>();
            list.Add(new MyRange(0, 3));
            list.Add(new MyRange(3, 3));
            const int length = 3;
            int index = 3;
            for (int i = 0; i < 8; i++)
            {
                list.Insert(1,new MyRange(index, length));
                index += length;
            }
            var expected = new int[] { 0, 3, 6, 9, 12, 15, 18, 21, 24, 27, };
            AssertAreRangeEqual(expected, list);

            list = new BigRangeList<MyRange>();
            list.Add(new MyRange(0, 3));
            list.Add(new MyRange(3, 3));
            index = 0;
            for (int i = 0; i < 8; i++)
            {
                list.Insert(0, new MyRange(index, length));
                index += length;
            }
            expected = new int[] { 0, 3, 6, 9, 12, 15, 18, 21, 24, 27, };
            AssertAreRangeEqual(expected, list);

            list = new BigRangeList<MyRange>();
            list.Add(new MyRange(0, 3));
            list.Add(new MyRange(3, 3));
            index = 6;
            for (int i = 0; i < 8; i++)
            {
                list.Insert(list.Count, new MyRange(index, length));
                index += length;
            }
            expected = new int[] { 0, 3, 6, 9, 12, 15, 18, 21, 24, 27, };
            AssertAreRangeEqual(expected, list);
        }

        [TestMethod]
        public void InsertRangeTest()
        {
            var rangeList = new List<MyRange>();
            const int length = 3;
            int index = 3;
            for (int i = 0; i < 8; i++)
            {
                rangeList.Add(new MyRange(index, length));
                index += length;
            }

            var list = new BigRangeList<MyRange>();
            list.Add(new MyRange(0, length));
            list.Add(new MyRange(length, length));
            list.InsertRange(1,rangeList);

            var expected = new int[] { 0, 3, 6, 9, 12, 15, 18, 21, 24, 27, };
            AssertAreRangeEqual(expected, list);

            list = new BigRangeList<MyRange>();
            list.Add(new MyRange(0, length));
            list.Add(new MyRange(length, length));
            list.InsertRange(0, rangeList);

            expected = new int[] { 0, 3, 6, 9, 12, 15, 18, 21, 24, 27, };
            AssertAreRangeEqual(expected, list);

            list = new BigRangeList<MyRange>();
            list.Add(new MyRange(0, length));
            list.Add(new MyRange(length, length));
            list.InsertRange(list.Count, rangeList);

            expected = new int[] { 0, 3, 6, 9, 12, 15, 18, 21, 24, 27, };
            AssertAreRangeEqual(expected, list);
        }

        [TestMethod]
        public void RemoveTest()
        {
            var rangeList = new List<MyRange>();
            const int length = 3;
            int index = 0;
            for (int i = 0; i < 10; i++)
            {
                rangeList.Add(new MyRange(index, length));
                index += length;
            }

            var list = new BigRangeList<MyRange>();
            list.AddRange(rangeList);
            list.RemoveAt(0);
            list.RemoveAt(0);
            list.RemoveAt(2);
            list.RemoveAt(3);
            list.RemoveAt(5);

            var expected = new int[] { 0, 3, 6, 9, 12, 15,   };
            AssertAreRangeEqual(expected, list);
        }

        [TestMethod]
        public void RemoveRangeTest()
        {
            var rangeList = new List<MyRange>();
            const int length = 3;
            int index = 0;
            for (int i = 0; i < 10; i++)
            {
                rangeList.Add(new MyRange(index, length));
                index += length;
            }

            var list = new BigRangeList<MyRange>();
            list.AddRange(rangeList);
            list.RemoveRange(0, 2);
            list.RemoveRange(2, 2);
            list.RemoveRange(5, 1);
            var expected = new int[] { 0, 3, 6, 9, 12, 15, };
            AssertAreRangeEqual(expected, list);
        }

        [TestMethod]
        public void GetIndexFromIndexIntoRangeTest()
        {
            var rangeList = new List<MyRange>();
            const int length = 3;
            int index = 0;
            for (int i = 0; i < 10; i++)
            {
                rangeList.Add(new MyRange(index, length));
                index += length;
            }

            var list = new BigRangeList<MyRange>();
            list.AddRange(rangeList);

            var expected = new int[] { 0, 3, 6, 9, 12, 15, 18, 21, 24, 27, };
            foreach(var item in expected)
            {
                var absoluteindex = list.GetIndexFromIndexIntoRange(item);
                var range = list.GetIndexIntoRange(absoluteindex);
                Assert.AreEqual(item, range.start);
            }
        }

        [TestMethod]
        public void IndexerTest()
        {
            var rangeList = new List<MyRange>();
            const int length = 3;
            int index = 0;
            for (int i = 0; i < 10; i++)
            {
                rangeList.Add(new MyRange(index, length));
                index += length;
            }

            var list = new BigRangeList<MyRange>();
            BigRangeList<MyRange>.BlockSize = 8;
            list.AddRange(rangeList);

            Assert.AreEqual(0,list[0].start);
            Assert.AreEqual(3, list[0].length);
            Assert.AreEqual(0, list[8].start);
            Assert.AreEqual(3, list[8].length);

            var newValue = (MyRange)list[0].DeepCopy();
            newValue.length = 4;
            list.RemoveAt(0);
            list.Insert(0,newValue);
            Assert.AreEqual(0, list[0].start);
            Assert.AreEqual(4, list[0].length);

            var expected = new int[] { 0, 4, 7, 10, 13, 16, 19, 22, 25, 28, };
            AssertAreRangeEqual(expected, list);
        }
    }

    class MyRange : IRange
    {
        public int start { get; set; }
        public int length { get; set; }

        public MyRange(int index, int length)
        {
            start = index; 
            this.length = length;
        }
        public MyRange()
        {
        }

        public IRange DeepCopy()
        {
            return new MyRange(start, length);
        }
    }
}
