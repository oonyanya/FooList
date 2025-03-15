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
                var range = new MyRange(list[i]);
                var n = list.CustomConverter.ConvertBack(range);
                Assert.AreEqual(expected[i], n.Index);
            }
        }

        [TestMethod]
        public void AddTest()
        {
            BigRangeList<MyRange> list = new BigRangeList<MyRange>();
            for (int i = 0; i < 8; i++)
            {
                list.Add(new MyRange(0,3));
            }
            list.Insert(0, new MyRange(0, 3));
            list.Insert(list.Count, new MyRange(0, 3));

            var expected = new int[] { 0, 3, 6, 9, 12, 15, 18, 21, 24, 27, };
            AssertAreRangeEqual(expected, list);
        }

        [TestMethod]
        public void AddRangeTest()
        {
            var list = new BigRangeList<MyRange>();
            var rangeList = new List<MyRange>();
            for (int i = 0; i < 8; i++)
            {
                rangeList.Add(new MyRange(0,3));
            }
            list.AddRange(rangeList);
            list.Insert(0, new MyRange(0,3));
            list.Insert(list.Count, new MyRange(0,3));

            var expected = new int[] { 0, 3, 6, 9, 12, 15, 18, 21, 24, 27, };
            AssertAreRangeEqual(expected, list);
        }

    }

    class MyRange : IRange
    {
        public int Index { get; set; }
        public int Length { get; set; }

        public MyRange(int index, int length)
        {
            Index = index; 
            Length = length;
        }
        public MyRange(MyRange range)
        {
            Index = range.Index;
            Length = range.Length;
        }
    }
}
