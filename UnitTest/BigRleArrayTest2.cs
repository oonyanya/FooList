using System;
using System.Collections;
using System.Collections.Generic;
using FooProject.Collection;
using Sgry.Azuki;

using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
using TestClassAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute;
using TestMethodAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute;

namespace Sgry.Azuki.Test
{
    [TestClass]
    public class RleArrayTest
    {
        [TestMethod]
        public void InitDispose()
        {
            BigRleArrayCollection<char> chars;

            chars = new BigRleArrayCollection<char>();
            Assert.AreEqual(0, chars.TotalCount);

            chars = new BigRleArrayCollection<char>();
            Set(chars, "abc");
            Assert.AreEqual(3, chars.TotalCount);
            Assert.AreEqual('a', chars[0].Value);
            Assert.AreEqual('b', chars[1].Value);
            Assert.AreEqual('c', chars[2].Value);
        }

        [TestMethod]
        public void GetSet()
        {
            BigRleArrayCollection<char> chars;

            // get
            chars = new BigRleArrayCollection<char>();
            Set(chars, "abc");
            Assert.AreEqual(3, chars.TotalCount);
            Assert.AreEqual('a', chars[0].Value);
            Assert.AreEqual('b', chars[1].Value);
            Assert.AreEqual('c', chars[2].Value);

            // set - out of bounds
            chars = new BigRleArrayCollection<char>();
            Set(chars, "abc");

            {
                // set - same value
                chars = new BigRleArrayCollection<char>();
                Set(chars, "aabbbcc");
                chars[2] = new BigRleArrayRangeItem<char>('b');
                Assert.AreEqual("a a b b b c c", chars.ToString());
                Assert.AreEqual("[2|a] [3|b] [2|c]", chars.ToRleString());

                // set - new value on left boundary
                chars = new BigRleArrayCollection<char>();
                Set(chars, "aabbbcc");
                chars[2] = new BigRleArrayRangeItem<char>('X');
                Assert.AreEqual("a a X b b c c", chars.ToString());
                Assert.AreEqual("[2|a] [1|X] [2|b] [2|c]", chars.ToRleString());

                // set - same value as the previous node on left boundary
                chars = new BigRleArrayCollection<char>();
                Set(chars, "aabbbcc");
                chars[2] = new BigRleArrayRangeItem<char>('a');
                Assert.AreEqual("a a a b b c c", chars.ToString());
                Assert.AreEqual("[3|a] [2|b] [2|c]", chars.ToRleString());

                // set - new value in the middle of a node
                chars = new BigRleArrayCollection<char>();
                Set(chars, "aabbbcc");
                chars[3] = new BigRleArrayRangeItem<char>('a');
                Assert.AreEqual("a a b a b c c", chars.ToString());
                Assert.AreEqual("[2|a] [1|b] [1|a] [1|b] [2|c]", chars.ToRleString());

                // set - new value on right boundary
                chars = new BigRleArrayCollection<char>();
                Set(chars, "aabbbcc");
                chars[4] = new BigRleArrayRangeItem<char>('X');
                Assert.AreEqual("a a b b X c c", chars.ToString());
                Assert.AreEqual("[2|a] [2|b] [1|X] [2|c]", chars.ToRleString());

                // set - same value as the next node on right boundary
                chars = new BigRleArrayCollection<char>();
                Set(chars, "aabbbcc");
                chars[4] = new BigRleArrayRangeItem<char>('c');
                Assert.AreEqual("a a b b c c c", chars.ToString());
                Assert.AreEqual("[2|a] [2|b] [3|c]", chars.ToRleString());
            }

            {
                // set - combines with previous node and the node disappears
                chars = new BigRleArrayCollection<char>();
                Set(chars, "aabcc");
                chars[2] = new BigRleArrayRangeItem<char>('a');
                Assert.AreEqual("a a a c c", chars.ToString());
                Assert.AreEqual("[3|a] [2|c]", chars.ToRleString());

                // set - combines with next node and the node disappears
                chars = new BigRleArrayCollection<char>();
                Set(chars, "aabcc");
                chars[2] = new BigRleArrayRangeItem<char>('c');
                Assert.AreEqual("a a c c c", chars.ToString());
                Assert.AreEqual("[2|a] [3|c]", chars.ToRleString());

                // set - combines with next and previous node, and the node disappears
                chars = new BigRleArrayCollection<char>();
                Set(chars, "aabaa");
                chars[2] = new BigRleArrayRangeItem<char>('a');
                Assert.AreEqual("a a a a a", chars.ToString());
                Assert.AreEqual("[5|a]", chars.ToRleString());

                // set - combines with previous node and the node disappears
                chars = new BigRleArrayCollection<char>();
                Set(chars, "aabcc");
                chars[2] = new BigRleArrayRangeItem<char>('B');
                Assert.AreEqual("a a B c c", chars.ToString());
                Assert.AreEqual("[2|a] [1|B] [2|c]", chars.ToRleString());
            }
        }

        [TestMethod]
        public void Update()
        {
            var list = new BigRleArrayCollection<char>();
            list.AddRange('a', 2);
            list.AddRange('b', 3);
            list.UpdateRange(2, '0', 2);
            Assert.AreEqual("a a 0 0 b", list.ToString());

            list = new BigRleArrayCollection<char>();
            list.AddRange('a', 2);
            list.AddRange('b', 3);
            list.UpdateRange(3, '0', 1);
            Assert.AreEqual("a a b 0 b", list.ToString());

            list = new BigRleArrayCollection<char>();
            list.AddRange('a', 2);
            list.AddRange('b', 3);
            list.UpdateRange(3, '0', 2);
            Assert.AreEqual("a a b 0 0", list.ToString());

            list = new BigRleArrayCollection<char>();
            list.AddRange('a', 2);
            list.AddRange('b', 3);
            list.AddRange('c', 3);
            list.UpdateRange(1, '0', 5);
            Assert.AreEqual("a 0 0 0 0 0 c c", list.ToString());

            list = new BigRleArrayCollection<char>();
            list.AddRange('a', 2);
            list.AddRange('b', 3);
            list.AddRange('c', 3);
            list.UpdateRange(1, 'a', 5);
            Assert.AreEqual("a a a a a a c c", list.ToString());
        }

        [TestMethod]
        public void Insert()
        {
            BigRleArrayCollection<char> chars = new BigRleArrayCollection<char>();

            Set(chars, "aabb");
            chars.InsertRange(2, new BigRleArrayRangeItem<char>('a'));
            Assert.AreEqual(5, chars.TotalCount);
            Assert.AreEqual('a', chars[0].Value);
            Assert.AreEqual('a', chars[1].Value);
            Assert.AreEqual('a', chars[2].Value);
            Assert.AreEqual('b', chars[3].Value);
            Assert.AreEqual('b', chars[4].Value);

            Set(chars, "aabb");
            chars.InsertRange(2, new BigRleArrayRangeItem<char>('b'));
            Assert.AreEqual(5, chars.TotalCount);
            Assert.AreEqual('a', chars[0].Value);
            Assert.AreEqual('a', chars[1].Value);
            Assert.AreEqual('b', chars[2].Value);
            Assert.AreEqual('b', chars[3].Value);
            Assert.AreEqual('b', chars[4].Value);

            Set(chars, "aabb");
            chars.InsertRange(2, new BigRleArrayRangeItem<char>('c'));
            Assert.AreEqual(5, chars.TotalCount);
            Assert.AreEqual("a a c b b", chars.ToString());

            Set(chars, "aabb");
            chars.InsertRange(1, new BigRleArrayRangeItem<char>('a'));
            Assert.AreEqual(5, chars.TotalCount);
            Assert.AreEqual('a', chars[0].Value);
            Assert.AreEqual('a', chars[1].Value);
            Assert.AreEqual('a', chars[2].Value);
            Assert.AreEqual('b', chars[3].Value);
            Assert.AreEqual('b', chars[4].Value);

            Set(chars, "aabb");
            chars.InsertRange(1, new BigRleArrayRangeItem<char>('c'));
            Assert.AreEqual(5, chars.TotalCount);
            Assert.AreEqual('a', chars[0].Value);
            Assert.AreEqual('c', chars[1].Value);
            Assert.AreEqual('a', chars[2].Value);
            Assert.AreEqual('b', chars[3].Value);
            Assert.AreEqual('b', chars[4].Value);
        }

        [TestMethod]
        public void Remove()
        {
            BigRleArrayCollection<char> list = new BigRleArrayCollection<char>();

            {
                // In a node
                Set(list, "aa");
                list.RemoveAt(0);
                Assert.AreEqual(1, list.TotalCount);
                Assert.AreEqual("[1|a]", list.ToRleString());
                list.RemoveAt(0);
                Assert.AreEqual(0, list.TotalCount);
                Assert.AreEqual("", list.ToRleString());

                // Removing a node
                Set(list, "aaabc");
                list.RemoveAt(3);
                Assert.AreEqual(4, list.TotalCount);
                Assert.AreEqual("[3|a] [1|c]", list.ToRleString());

                // Combining nodes
                Set(list, "aabaa");
                list.RemoveAt(2);
                Assert.AreEqual(4, list.TotalCount);
                Assert.AreEqual("[4|a]", list.ToRleString());
            }

            {
                list = new BigRleArrayCollection<char>();
                list.AddRange('a', 2);
                list.AddRange('b', 3);
                list.RemoveRange(0, 3);
                Assert.AreEqual("b b", list.ToString());

                list = new BigRleArrayCollection<char>();
                list.AddRange('a', 2);
                list.AddRange('b', 3);
                list.RemoveRange(1, 4);
                Assert.AreEqual("a", list.ToString());

                list = new BigRleArrayCollection<char>();
                list.AddRange('a', 2);
                list.AddRange('b', 3);
                list.AddRange('c', 2);
                list.RemoveRange(1, 5);
                Assert.AreEqual("a c", list.ToString());

                list = new BigRleArrayCollection<char>();
                list.AddRange('a', 2);
                list.AddRange('b', 3);
                list.RemoveRange(0, 5);
                Assert.AreEqual(0, list.Count);

            }
        }

        #region Utilities
        static void Set(BigRleArrayCollection<char> ary, IEnumerable<char> values)
        {
            ary.Clear();
            foreach (char value in values)
            {
                ary.AddRange(new BigRleArrayRangeItem<char>(value));
            }
        }
        #endregion
    }
}