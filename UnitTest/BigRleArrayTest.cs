using System;
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
        class CharRleArray : IRleArrayRange<char>
        {
            public char Value { get; set; }
            public long start { get; set; }
            public long length { get; set; }

            public CharRleArray()
            {
            }

            public CharRleArray(char v, long index,long length)
            {
                this.Value = v;
                this.start = index;
                this.length = length;
            }

            public CharRleArray(char v,long length)
            {
                this.Value = v;
                this.length = length;
            }

            public IRange DeepCopy()
            {
                var new_item = new CharRleArray();
                new_item.start = start;
                new_item.Value  = Value;
                new_item.length = length;
                return new_item;
            }

            public override bool Equals(object? obj)
            {
                var other = obj as CharRleArray;
                if (other == null) return false;
                if(other.Value == this.Value && other.length == this.length && other.start == this.start) return true;
                return false;
            }
        }

        [TestMethod]
        public void GetTest()
        {
            var list = new BigRleArray<CharRleArray, char>();
            list.AddRange('a');
            list.AddRange('a');
            list.AddRange('a');
            list.AddRange('b');
            list.AddRange('b');

            var item = list.Get(0);
            Assert.AreEqual('a', item.Value);

            var item2 = list.Get(1);
            Assert.AreEqual('a', item2.Value);

            var item3 = list.Get(4);
            Assert.AreEqual('b', item3.Value);
        }

        [TestMethod]
        public void AddTest()
        {
            var list = new BigRleArray<CharRleArray, char>();
            list.AddRange('a');
            list.AddRange('a');
            list.AddRange('a');
            list.AddRange('b',2);

            var expected_list = new CharRleArray[] { new CharRleArray('a', 0, 3), new CharRleArray('b', 3, 2) };
            InterfaceTests.TestEnumerableElements<CharRleArray>(list, expected_list);
        }

        [TestMethod]
        public void UpdateTest()
        {
            var list = new BigRleArray<CharRleArray, char>();
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
            InterfaceTests.TestEnumerableElements<CharRleArray>(list, expected_list);

            list.InsertRange(6, 'd');
            expected_list = new CharRleArray[] { new CharRleArray('a', 0, 4), new CharRleArray('c', 4, 1), new CharRleArray('b', 5, 1), new CharRleArray('d', 6, 1), new CharRleArray('b', 7, 3) };
            InterfaceTests.TestEnumerableElements<CharRleArray>(list, expected_list);
        }

        [TestMethod]
        public void RemoveTest()
        {
            var list = new BigRleArray<CharRleArray, char>();
            list.AddRange('a');
            list.AddRange('a');
            list.AddRange('a');
            list.AddRange('b');
            list.AddRange('b');

            list.RemoveRange(1);

            var expected_list = new CharRleArray[] { new CharRleArray('a', 0, 2), new CharRleArray('b', 2, 2) };
            InterfaceTests.TestEnumerableElements<CharRleArray>(list, expected_list);

            list = new BigRleArray<CharRleArray, char>();
            list.AddRange('a', 2);
            list.AddRange('b', 3);
            list.RemoveRange(0,3);
            expected_list = new CharRleArray[] { new CharRleArray('b', 0, 2) };
            InterfaceTests.TestEnumerableElements<CharRleArray>(list, expected_list);

            list = new BigRleArray<CharRleArray, char>();
            list.AddRange('a', 2);
            list.AddRange('b', 3);
            list.RemoveRange(1, 4);
            expected_list = new CharRleArray[] { new CharRleArray('a', 0, 1) };
            InterfaceTests.TestEnumerableElements<CharRleArray>(list, expected_list);

            list = new BigRleArray<CharRleArray, char>();
            list.AddRange('a', 2);
            list.AddRange('b', 3);
            list.AddRange('c', 2);
            list.RemoveRange(1, 5);
            expected_list = new CharRleArray[] { new CharRleArray('a', 0, 1), new CharRleArray('c', 1, 1) };
            InterfaceTests.TestEnumerableElements<CharRleArray>(list, expected_list);

            list = new BigRleArray<CharRleArray, char>();
            list.AddRange('a',2);
            list.AddRange('b',3);
            list.RemoveRange(0, 5);
            Assert.AreEqual(0,list.Count);
        }
    }
}
