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
        [TestMethod]
        public void AddTest()
        {
            BigRangeList<MyRange> list = new BigRangeList<MyRange>();
            for (int i = 0; i < 8; i++)
            {
                list.Add(new MyRange(3));
            }
            list.Insert(0, new MyRange(3));
            list.Insert(list.Count, new MyRange(3));

            var expected = new int[] { 3, 6, 6, 6, 6, 6, 6, 6, 6, 30, };
            for (int i = 0; i < 10; i++)
            {
                var n = list.CustomConverter.ConvertBack(list[i]);
                Assert.AreEqual(expected[i], n.Index);
            }
        }

        [TestMethod]
        public void AddRangeTest()
        {
            var list = new BigRangeList<MyRange>();
            var rangeList = new List<MyRange>();
            for (int i = 0; i < 8; i++)
            {
                rangeList.Add(new MyRange(3));
            }
            list.AddRange(rangeList);
            list.Insert(0, new MyRange(3));
            list.Insert(list.Count, new MyRange(3));

            var expected = new int[] { 3, 6, 6, 6, 6, 6, 6, 6, 6, 30, };
            for (int i = 0; i < 10; i++)
            {
                var n = list.CustomConverter.ConvertBack(list[i]);
                Assert.AreEqual(expected[i], n.Index);
            }
        }
    }

    class MyRange : IRange
    {
        public int Index { get; set; }
        public int Length { get; set; }

        public MyRange(int length)
        {
            Index = 0; 
            Length = length;
        }
    }
}
