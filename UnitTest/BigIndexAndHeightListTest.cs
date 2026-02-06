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
    public class BigIndexAndHeightTest
    {
        private void AssertAreRangeEqual(int[] expected, double[] expected2, BigIndexAndHeightList<MyRangeWithHeight> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var n = list.GetWithConvertAbsolteIndex(i);
                Assert.AreEqual(expected[i], n.start);
                Assert.AreEqual(expected2[i], n.sumHeight);
            }
        }

        [TestMethod]
        public void AddTest()
        {
            BigIndexAndHeightList<MyRangeWithHeight> list = new BigIndexAndHeightList<MyRangeWithHeight>();
            const int length = 3;
            const double height = 3.0;
            int index = 0;
            double sumHeight = 0;
            for (int i = 0; i < 8; i++)
            {
                list.Add(new MyRangeWithHeight(index, length, sumHeight, height));
                index += length;
                sumHeight += height;
            }
            list.Insert(0, new MyRangeWithHeight(0, length, 0, height));
            list.Insert(list.Count, new MyRangeWithHeight(index, length, sumHeight, height));

            var expected = new int[] { 0, 3, 6, 9, 12, 15, 18, 21, 24, 27, };
            var expected2 = new double[] { 0.0, 3.0, 6.0, 9.0, 12.0, 15.0, 18.0, 21.0, 24.0, 27.0, };
            AssertAreRangeEqual(expected, expected2, list);
        }

        [TestMethod]
        public void AddRangeTest()
        {
            var list = new BigIndexAndHeightList<MyRangeWithHeight>();
            var rangeList = new List<MyRangeWithHeight>();
            const int length = 3;
            const double height = 3.0;
            int index = 0;
            double sumHeight = 0;
            for (int i = 0; i < 8; i++)
            {
                rangeList.Add(new MyRangeWithHeight(index, length, sumHeight, height));
                index += length;
                sumHeight += height;
            }
            list.AddRange(rangeList);
            list.Insert(0, new MyRangeWithHeight(0, length, 0, height));
            list.Insert(list.Count, new MyRangeWithHeight(index, length, sumHeight, height));

            var expected = new int[] { 0, 3, 6, 9, 12, 15, 18, 21, 24, 27, };
            var expected2 = new double[] { 0.0, 3.0, 6.0, 9.0, 12.0, 15.0, 18.0, 21.0, 24.0, 27.0, };
            AssertAreRangeEqual(expected, expected2, list);
        }

        [TestMethod]
        public void InsertTest()
        {
            BigIndexAndHeightList<MyRangeWithHeight> list = new BigIndexAndHeightList<MyRangeWithHeight>();
            list.Add(new MyRangeWithHeight(0, 3, 0.0, 3.0));
            list.Add(new MyRangeWithHeight(3, 3, 3.0, 3.0));
            const int length = 3;
            const double height = 3.0;
            int index = 0;
            double sumHeight = 0;
            for (int i = 0; i < 8; i++)
            {
                list.Add(new MyRangeWithHeight(index, length, sumHeight, height));
                index += length;
                sumHeight += height;
            }

            var expected = new int[] { 0, 3, 6, 9, 12, 15, 18, 21, 24, 27, };
            var expected2 = new double[] { 0.0, 3.0, 6.0, 9.0, 12.0, 15.0, 18.0, 21.0, 24.0, 27.0, };
            AssertAreRangeEqual(expected, expected2, list);

            list = new BigIndexAndHeightList<MyRangeWithHeight>();
            list.Add(new MyRangeWithHeight(0, 3, 0.0, 3.0));
            list.Add(new MyRangeWithHeight(3, 3, 3.0, 3.0));
            index = 0;
            sumHeight = 0;
            for (int i = 0; i < 8; i++)
            {
                list.Add(new MyRangeWithHeight(index, length, sumHeight, height));
                index += length;
                sumHeight += height;
            }

            expected = new int[] { 0, 3, 6, 9, 12, 15, 18, 21, 24, 27, };
            expected2 = new double[] { 0.0, 3.0, 6.0, 9.0, 12.0, 15.0, 18.0, 21.0, 24.0, 27.0, };
            AssertAreRangeEqual(expected, expected2, list);

            list = new BigIndexAndHeightList<MyRangeWithHeight>();
            list.Add(new MyRangeWithHeight(0, 3, 0.0, 3.0));
            list.Add(new MyRangeWithHeight(3, 3, 3.0, 3.0));
            index = 6;
            sumHeight = 6.0;
            for (int i = 0; i < 8; i++)
            {
                list.Insert(list.Count, new MyRangeWithHeight(index, length, sumHeight, height));
                index += length;
                sumHeight += height;
            }

            expected = new int[] { 0, 3, 6, 9, 12, 15, 18, 21, 24, 27, };
            expected2 = new double[] { 0.0, 3.0, 6.0, 9.0, 12.0, 15.0, 18.0, 21.0, 24.0, 27.0, };
            AssertAreRangeEqual(expected, expected2, list);
        }

        [TestMethod]
        public void InsertRangeTest()
        {
            var rangeList = new List<MyRangeWithHeight>();
            const int length = 3;
            const double height = 3.0;
            int index = 0;
            double sumHeight = 0;
            for (int i = 0; i < 8; i++)
            {
                rangeList.Add(new MyRangeWithHeight(index, length, sumHeight, height));
                index += length;
                sumHeight += height;
            }

            var list = new BigIndexAndHeightList<MyRangeWithHeight>();
            list.Add(new MyRangeWithHeight(0, 3, 0.0, 3.0));
            list.Add(new MyRangeWithHeight(3, 3, 3.0, 3.0));
            list.InsertRange(1, rangeList);

            var expected = new int[] { 0, 3, 6, 9, 12, 15, 18, 21, 24, 27, };
            var expected2 = new double[] { 0.0, 3.0, 6.0, 9.0, 12.0, 15.0, 18.0, 21.0, 24.0, 27.0, };
            AssertAreRangeEqual(expected, expected2, list);

            list = new BigIndexAndHeightList<MyRangeWithHeight>();
            list.Add(new MyRangeWithHeight(0, length, 0, height));
            list.Add(new MyRangeWithHeight(length, length, height, height));
            list.InsertRange(0, rangeList);

            expected = new int[] { 0, 3, 6, 9, 12, 15, 18, 21, 24, 27, };
            expected2 = new double[] { 0.0, 3.0, 6.0, 9.0, 12.0, 15.0, 18.0, 21.0, 24.0, 27.0, };
            AssertAreRangeEqual(expected, expected2, list);

            list = new BigIndexAndHeightList<MyRangeWithHeight>();
            list.Add(new MyRangeWithHeight(0, length, 0, height));
            list.Add(new MyRangeWithHeight(length, length, height, height));
            list.InsertRange(list.Count, rangeList);

            expected = new int[] { 0, 3, 6, 9, 12, 15, 18, 21, 24, 27, };
            expected2 = new double[] { 0.0, 3.0, 6.0, 9.0, 12.0, 15.0, 18.0, 21.0, 24.0, 27.0, };
            AssertAreRangeEqual(expected, expected2, list);
        }

        [TestMethod]
        public void RemoveTest()
        {
            var rangeList = new List<MyRangeWithHeight>();
            const int length = 3;
            const double height = 3.0;
            int index = 0;
            double sumHeight = 0;
            for (int i = 0; i < 10; i++)
            {
                rangeList.Add(new MyRangeWithHeight(index, length, sumHeight, height));
                index += length;
                sumHeight += height;
            }

            var list = new BigIndexAndHeightList<MyRangeWithHeight>();
            list.AddRange(rangeList);
            list.RemoveAt(0);
            list.RemoveAt(0);
            list.RemoveAt(2);
            list.RemoveAt(3);
            list.RemoveAt(5);

            var expected = new int[] { 0, 3, 6, 9, 12, 15, };
            var expected2 = new double[] { 0.0, 3.0, 6.0, 9.0, 12.0, 15.0 };
            AssertAreRangeEqual(expected, expected2, list);
        }

        [TestMethod]
        public void RemoveRangeTest()
        {
            var rangeList = new List<MyRangeWithHeight>();
            const int length = 3;
            const double height = 3.0;
            int index = 0;
            double sumHeight = 0;
            for (int i = 0; i < 10; i++)
            {
                rangeList.Add(new MyRangeWithHeight(index, length, sumHeight, height));
                index += length;
                sumHeight += height;
            }

            var list = new BigIndexAndHeightList<MyRangeWithHeight>();
            list.AddRange(rangeList);
            list.RemoveRange(0, 2);
            list.RemoveRange(2, 2);
            list.RemoveRange(5, 1);

            var expected = new int[] { 0, 3, 6, 9, 12, 15, };
            var expected2 = new double[] { 0.0, 3.0, 6.0, 9.0, 12.0, 15.0 };
            AssertAreRangeEqual(expected, expected2, list);
        }

        [TestMethod]
        public void GetIndexFromIndexIntoRangeTest()
        {
            var rangeList = new List<MyRangeWithHeight>();
            const int length = 3;
            const double height = 4.0;
            int index = 0;
            double sumHeight = 0;
            for (int i = 0; i < 10; i++)
            {
                rangeList.Add(new MyRangeWithHeight(index, length, sumHeight, height));
                index += length;
                sumHeight += height;
            }

            var list = new BigIndexAndHeightList<MyRangeWithHeight>();
            list.AddRange(rangeList);

            var expected = new long[] { 0, 3, 6, 9, 12, 15, 18, 21, 24, 27, };
            var expected_sumheight = new double[] { 0, 4, 8, 12, 16, 20, 24, 28, 32, 36, };
            for (int i = 0; i < expected.Length; i++)
            {
                var item = expected[i];
                double sumHeightFromBox;
                long sumIndexFromBox;
                var boxIndex = list.GetIndexFromAbsoluteIndexIntoRange(item + 1, out sumIndexFromBox, out sumHeightFromBox);
                var range = list.GetWithConvertAbsolteIndex(boxIndex);
                Assert.AreEqual(item, sumIndexFromBox);
                Assert.AreEqual(expected_sumheight[i], sumHeightFromBox);
                Assert.AreEqual(item, range.start);
            }
        }

        [TestMethod]
        public void GetIndexFromSumHeightTest()
        {
            var rangeList = new List<MyRangeWithHeight>();
            const int length = 3;
            const double height = 4.0;
            int index = 0;
            double sumHeight = 0;
            for (int i = 0; i < 10; i++)
            {
                rangeList.Add(new MyRangeWithHeight(index, length, sumHeight, height));
                index += length;
                sumHeight += height;
            }

            var list = new BigIndexAndHeightList<MyRangeWithHeight>();
            list.AddRange(rangeList);

            var expected = new long[] { 0, 3, 6, 9, 12, 15, 18, 21, 24, 27, };
            var expected_sumheight = new double[] { 0, 4, 8, 12, 16, 20, 24, 28, 32, 36, };
            for (int i = 0; i < expected.Length; i++)
            {
                double sumHeightFromBox = 0;
                var absoluteindex = list.GetIndexFromAbsoluteSumHeight(expected_sumheight[i] + 1.0, out sumHeightFromBox);
                var range = list.GetWithConvertAbsolteIndex(absoluteindex);
                Assert.AreEqual(expected_sumheight[i], sumHeightFromBox);
                Assert.AreEqual(expected_sumheight[i], range.sumHeight);
                Assert.AreEqual(expected[i], range.start);
            }
        }

        [TestMethod]
        public void IndexerTest()
        {
            var rangeList = new List<MyRangeWithHeight>();
            const int length = 3;
            const double height = 3.0;
            int index = 0;
            double sumHeight = 0;
            for (int i = 0; i < 10; i++)
            {
                rangeList.Add(new MyRangeWithHeight(index, length, sumHeight, height));
                index += length;
                sumHeight += height;
            }

            var list = new BigIndexAndHeightList<MyRangeWithHeight>();
            list.BlockSize = 8;
            list.AddRange(rangeList);

            Assert.AreEqual(0, list[0].start);
            Assert.AreEqual(3, list[0].length);
            Assert.AreEqual(0.0, list[0].sumHeight);
            Assert.AreEqual(3.0, list[0].Height);
            Assert.AreEqual(0, list[8].start);
            Assert.AreEqual(3, list[8].length);
            Assert.AreEqual(0.0, list[8].sumHeight);
            Assert.AreEqual(3.0, list[8].Height);

            var newValue = (MyRangeWithHeight)list[0].DeepCopy();
            newValue.length = 4;
            newValue.Height = 4.0;
            list[0] = newValue;
            Assert.AreEqual(0, list[0].start);
            Assert.AreEqual(4, list[0].length);
            Assert.AreEqual(0.0, list[0].sumHeight);
            Assert.AreEqual(4.0, list[0].Height);

            var expected = new int[] { 0, 4, 7, 10, 13, 16, 19, 22, 25, 28, };
            var expected2 = new double[] { 0.0, 4.0, 7.0, 10.0, 13.0, 16.0, 19.0, 22.0, 25.0, 28.0, };
            AssertAreRangeEqual(expected, expected2, list);
        }
    }

    class MyRangeWithHeight : IRangeWithHeight
    {
        public long start { get; set; }
        public long length { get; set; }
        public double sumHeight { get; set; }
        public double Height { get; set; }

        public MyRangeWithHeight(long index, long length, double sumHeight, double height)
        {
            start = index;
            this.length = length;
            this.sumHeight = sumHeight;
            this.Height = height;
        }
        public MyRangeWithHeight()
        {
        }

        public IRange DeepCopy()
        {
            return new MyRangeWithHeight(start, length, sumHeight, Height);
        }
    }
}
