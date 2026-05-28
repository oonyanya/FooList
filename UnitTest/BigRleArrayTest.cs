using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FooProject.Collection;

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
