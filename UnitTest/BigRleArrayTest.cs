using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FooProject.Collection;
using static UnitTest.BigRleArrayTest;

namespace UnitTest
{
    [TestClass]
    public class BigRleArrayTest
    {
        public class CharRleArray : BigRleArrayRange<char>
        {
            public CharRleArray()
            {
            }

            public CharRleArray(char v, long index, long length)
            {
                this.Value = v;
                this.start = index;
                this.length = length;
            }

            public CharRleArray(char v, long length)
            {
                this.Value = v;
                this.length = length;
            }
        }

        [TestMethod]
        public void ClearTest()
        {
            var list = new BigRleArray<char>();
            list.AddRange('a');
            list.AddRange('a');
            list.AddRange('a');
            list.AddRange('b');
            list.AddRange('b');

            list.Clear();

            Assert.AreEqual(0, list.Count);
        }

        [TestMethod]
        public void GetValueTest()
        {
            var list = new BigRleArray<char>();
            list.AddRange('a');
            list.AddRange('a');
            list.AddRange('a');
            list.AddRange('b');
            list.AddRange('b');

            var item = list.GetValue(0);
            Assert.AreEqual('a', item);

            var item2 = list.GetValue(1);
            Assert.AreEqual('a', item2);

            var item3 = list.GetValue(4);
            Assert.AreEqual('b', item3);
        }

        [TestMethod]
        public void GetAtAndSetAtTest()
        {
            var list = new BigRleArray<char>();
            list.AddRange('a');
            list.AddRange('a');
            list.AddRange('a');
            list.AddRange('b');
            list.AddRange('b');

            var item = list.GetAt(0);
            item.length += 1;
            list.SetAt(0,item);

            var expected_list = new CharRleArray[] { new CharRleArray('a', 0, 4), new CharRleArray('b', 4, 2)};
            InterfaceTests.TestEnumerableElements(list, expected_list);

            Assert.AreEqual(6, list.TotalRangeCount);
        }

        [TestMethod]
        public void GetRangesAndClampTest()
        {
            var list = new BigRleArray<char>();
            list.AddRange('a');
            list.AddRange('a');
            list.AddRange('a');
            list.AddRange('b');
            list.AddRange('b');
            list.AddRange('c');
            list.AddRange('c');


            var expected_list = new CharRleArray[] { new CharRleArray('a', 1, 2), new CharRleArray('b', 3, 2), new CharRleArray('c', 5, 1) };
            InterfaceTests.TestEnumerableElements(list.GetRangesAndClamp(1,5), expected_list);

            expected_list = new CharRleArray[] { new CharRleArray('a', 1, 2) };
            InterfaceTests.TestEnumerableElements(list.GetRangesAndClamp(1, 2), expected_list);

            expected_list = new CharRleArray[] { new CharRleArray('b', 3, 2) };
            InterfaceTests.TestEnumerableElements(list.GetRangesAndClamp(3, 2), expected_list);

            expected_list = new CharRleArray[] { new CharRleArray('b', 3, 2), new CharRleArray('c', 5, 1) };
            InterfaceTests.TestEnumerableElements(list.GetRangesAndClamp(3, 3), expected_list);

            expected_list = new CharRleArray[] { new CharRleArray('b', 3, 2), new CharRleArray('c', 5, 2) };
            InterfaceTests.TestEnumerableElements(list.GetRangesAndClamp(3, 4), expected_list);

            list = new BigRleArray<char>(2);
            list.AddRange('a', 3);
            list.AddRange('b', 3);
            list.AddRange('c', 3);
            list.AddRange('d', 3);
            list.AddRange('e', 3);
            list.AddRange('f', 3);
            expected_list = new CharRleArray[] {
                new CharRleArray('a', 1, 2), 
                new CharRleArray('b', 3, 3), 
                new CharRleArray('c', 6, 3), 
                new CharRleArray('d', 9, 3), 
                new CharRleArray('e', 12, 1)};
            InterfaceTests.TestEnumerableElements(list.GetRangesAndClamp(1, 12), expected_list);

            expected_list = new CharRleArray[] {
                new CharRleArray('b', 4, 2),
                new CharRleArray('c', 6, 3),
                new CharRleArray('d', 9, 2)};
            InterfaceTests.TestEnumerableElements(list.GetRangesAndClamp(4, 7), expected_list);

            expected_list = new CharRleArray[] {
                new CharRleArray('a', 1, 2),
                new CharRleArray('b', 3, 3),
                new CharRleArray('c', 6, 3),
                new CharRleArray('d', 9, 3),
                new CharRleArray('e', 12, 3),
                new CharRleArray('f', 15, 1),
            };
            InterfaceTests.TestEnumerableElements(list.GetRangesAndClamp(1, 15), expected_list);
        }

        [TestMethod]
        public void AddItemTest()
        {
            var list = new BigRleArray<char>();
            list.Add(new CharRleArray('a', 0, 3));
            list.Add(new CharRleArray('b', 3, 2));

            var expected_list = new CharRleArray[] { new CharRleArray('a', 0, 3), new CharRleArray('b', 3, 2) };
            InterfaceTests.TestEnumerableElements(list, expected_list);

            list = new BigRleArray<char>();
            list.Add(new CharRleArray('a', 0, 0));
            list.Add(new CharRleArray('a', 0, 10));
            expected_list = new CharRleArray[] { new CharRleArray('a', 0, 10) };
        }

        [TestMethod]
        public void AddTest()
        {
            var list = new BigRleArray<char>();
            list.AddRange('a');
            list.AddRange('a');
            list.AddRange('a');
            list.AddRange('b',2);

            var expected_list = new CharRleArray[] { new CharRleArray('a', 0, 3), new CharRleArray('b', 3, 2) };
            InterfaceTests.TestEnumerableElements(list, expected_list);
        }

        [TestMethod]
        public void InsertItemTest()
        {
            var list = new BigRleArray<char>();
            list.AddRange('a');
            list.AddRange('a');
            list.AddRange('a');
            list.AddRange('b');
            list.AddRange('b');
            list.AddRange('b');
            list.AddRange('b');
            list.Insert(new CharRleArray('a', 0, 1));
            list.Insert(new CharRleArray('c', 4, 1));

            var expected_list = new CharRleArray[] { new CharRleArray('a', 0, 4), new CharRleArray('c', 4, 1), new CharRleArray('b', 5, 4) };
            InterfaceTests.TestEnumerableElements(list, expected_list);

            list = new BigRleArray<char>();
            list.Add(new CharRleArray('a', 0, 0));
            list.Insert(new CharRleArray('a', 0, 10));
            expected_list = new CharRleArray[] { new CharRleArray('a', 0, 10) };
        }

        [TestMethod]
        public void InsertRangeTest()
        {
            var list = new BigRleArray<char>();
            list.AddRange('a');
            list.AddRange('a');
            list.AddRange('a');
            list.AddRange('b');
            list.AddRange('b');
            list.AddRange('b');
            list.AddRange('b');
            list.InsertRange(1, 'a');
            list.InsertRange(4, 'c');

            var expected_list = new CharRleArray[] { new CharRleArray('a', 0, 4),  new CharRleArray('c', 4, 1), new CharRleArray('b', 5, 4) };
            InterfaceTests.TestEnumerableElements(list, expected_list);

            list.InsertRange(6, 'd');
            expected_list = new CharRleArray[] { new CharRleArray('a', 0, 4), new CharRleArray('c', 4, 1), new CharRleArray('b', 5, 1), new CharRleArray('d', 6, 1), new CharRleArray('b', 7, 3) };
            InterfaceTests.TestEnumerableElements(list, expected_list);
        }

        [TestMethod]
        public void UpdateRangeTest()
        {
            var list = new BigRleArray<char>();
            list.AddRange('a', 2);
            list.AddRange('b', 3);
            list.UpdateRange(2, '0', 2);
            var expected_list = new CharRleArray[] { new CharRleArray('a', 0, 2), new CharRleArray('0', 2, 2), new CharRleArray('b', 4, 1) };
            InterfaceTests.TestEnumerableElements(list, expected_list);

            list = new BigRleArray<char>();
            list.AddRange('a', 2);
            list.AddRange('b', 3);
            list.UpdateRange(3, '0', 1);
            expected_list = new CharRleArray[] { new CharRleArray('a', 0, 2), new CharRleArray('b', 2, 1), new CharRleArray('0', 3, 1), new CharRleArray('b', 4, 1) };
            InterfaceTests.TestEnumerableElements(list, expected_list);

            list = new BigRleArray<char>();
            list.AddRange('a', 2);
            list.AddRange('b', 3);
            list.UpdateRange(3, '0', 2);
            expected_list = new CharRleArray[] { new CharRleArray('a', 0, 2), new CharRleArray('b', 2, 1), new CharRleArray('0', 3, 2)};
            InterfaceTests.TestEnumerableElements(list, expected_list);

            list = new BigRleArray<char>();
            list.AddRange('a', 2);
            list.AddRange('b', 3);
            list.AddRange('c', 3);
            list.UpdateRange(1, '0', 5);
            expected_list = new CharRleArray[] { new CharRleArray('a', 0, 1), new CharRleArray('0', 1, 1), new CharRleArray('0', 2, 3), new CharRleArray('0', 5, 1), new CharRleArray('c', 6, 2) };
            InterfaceTests.TestEnumerableElements(list, expected_list);

            list = new BigRleArray<char>();
            list.Add(new CharRleArray('a', 0, 0));
            list.Update(0, 0, new CharRleArray('a', 0, 10));
            expected_list = new CharRleArray[] { new CharRleArray('a', 0, 10) };
        }

        [TestMethod]
        public void RemoveTest()
        {
            var list = new BigRleArray<char>();
            list.AddRange('a');
            list.AddRange('a');
            list.AddRange('a');
            list.AddRange('b');
            list.AddRange('b');

            list.RemoveRange(1);

            var expected_list = new CharRleArray[] { new CharRleArray('a', 0, 2), new CharRleArray('b', 2, 2) };
            InterfaceTests.TestEnumerableElements(list, expected_list);

            list = new BigRleArray<char>();
            list.AddRange('a', 2);
            list.AddRange('b', 3);
            list.RemoveRange(0,3);
            expected_list = new CharRleArray[] { new CharRleArray('b', 0, 2) };
            InterfaceTests.TestEnumerableElements(list, expected_list);

            list = new BigRleArray<char>();
            list.AddRange('a', 2);
            list.AddRange('b', 3);
            list.RemoveRange(1, 4);
            expected_list = new CharRleArray[] { new CharRleArray('a', 0, 1) };
            InterfaceTests.TestEnumerableElements(list, expected_list);

            list = new BigRleArray<char>();
            list.AddRange('a', 2);
            list.AddRange('b', 3);
            list.AddRange('c', 2);
            list.RemoveRange(1, 5);
            expected_list = new CharRleArray[] { new CharRleArray('a', 0, 1), new CharRleArray('c', 1, 1) };
            InterfaceTests.TestEnumerableElements(list, expected_list);

            list = new BigRleArray<char>();
            list.AddRange('a',2);
            list.AddRange('b',3);
            list.RemoveRange(0, 5);
            Assert.AreEqual(0,list.Count);
        }
    }
}
