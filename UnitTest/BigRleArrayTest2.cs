using System;
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
            RleArray<char> chars;

            chars = new RleArray<char>();
            Assert.AreEqual(0, chars.Count);

            chars = new RleArray<char>();
            Set(chars, "abc");
            Assert.AreEqual(3, chars.Count);
            Assert.AreEqual('a', chars[0].Value);
            Assert.AreEqual('b', chars[1].Value);
            Assert.AreEqual('c', chars[2].Value);
        }

        [TestMethod]
        public void GetSet()
        {
            RleArray<char> chars;

            // get
            chars = new RleArray<char>();
            Set(chars, "abc");
            Assert.AreEqual(3, chars.Count);
            Assert.AreEqual('a', chars[0].Value);
            Assert.AreEqual('b', chars[1].Value);
            Assert.AreEqual('c', chars[2].Value);

            // set - out of bounds
            chars = new RleArray<char>();
            Set(chars, "abc");

            {
                // set - same value
                chars = new RleArray<char>();
                Set(chars, "aabbbcc");
                chars[2] = new BigRleArrayRangeItem<char>('b');
                Assert.AreEqual("a a b b b c c", chars.ToString());
                Assert.AreEqual("[2|a] [3|b] [2|c]", chars.ToRleString());

                // set - new value on left boundary
                chars = new RleArray<char>();
                Set(chars, "aabbbcc");
                chars[2] = new BigRleArrayRangeItem<char>('X');
                Assert.AreEqual("a a X b b c c", chars.ToString());
                Assert.AreEqual("[2|a] [1|X] [2|b] [2|c]", chars.ToRleString());

                // set - same value as the previous node on left boundary
                chars = new RleArray<char>();
                Set(chars, "aabbbcc");
                chars[2] = new BigRleArrayRangeItem<char>('a');
                Assert.AreEqual("a a a b b c c", chars.ToString());
                Assert.AreEqual("[3|a] [2|b] [2|c]", chars.ToRleString());

                // set - new value in the middle of a node
                chars = new RleArray<char>();
                Set(chars, "aabbbcc");
                chars[3] = new BigRleArrayRangeItem<char>('a');
                Assert.AreEqual("a a b a b c c", chars.ToString());
                Assert.AreEqual("[2|a] [1|b] [1|a] [1|b] [2|c]", chars.ToRleString());

                // set - new value on right boundary
                chars = new RleArray<char>();
                Set(chars, "aabbbcc");
                chars[4] = new BigRleArrayRangeItem<char>('X');
                Assert.AreEqual("a a b b X c c", chars.ToString());
                Assert.AreEqual("[2|a] [2|b] [1|X] [2|c]", chars.ToRleString());

                // set - same value as the next node on right boundary
                chars = new RleArray<char>();
                Set(chars, "aabbbcc");
                chars[4] = new BigRleArrayRangeItem<char>('c');
                Assert.AreEqual("a a b b c c c", chars.ToString());
                Assert.AreEqual("[2|a] [2|b] [3|c]", chars.ToRleString());
            }

            {
                // set - combines with previous node and the node disappears
                chars = new RleArray<char>();
                Set(chars, "aabcc");
                chars[2] = new BigRleArrayRangeItem<char>('a');
                Assert.AreEqual("a a a c c", chars.ToString());
                Assert.AreEqual("[3|a] [2|c]", chars.ToRleString());

                // set - combines with next node and the node disappears
                chars = new RleArray<char>();
                Set(chars, "aabcc");
                chars[2] = new BigRleArrayRangeItem<char>('c');
                Assert.AreEqual("a a c c c", chars.ToString());
                Assert.AreEqual("[2|a] [3|c]", chars.ToRleString());

                // set - combines with next and previous node, and the node disappears
                chars = new RleArray<char>();
                Set(chars, "aabaa");
                chars[2] = new BigRleArrayRangeItem<char>('a');
                Assert.AreEqual("a a a a a", chars.ToString());
                Assert.AreEqual("[5|a]", chars.ToRleString());

                // set - combines with previous node and the node disappears
                chars = new RleArray<char>();
                Set(chars, "aabcc");
                chars[2] = new BigRleArrayRangeItem<char>('B');
                Assert.AreEqual("a a B c c", chars.ToString());
                Assert.AreEqual("[2|a] [1|B] [2|c]", chars.ToRleString());
            }
        }

        [TestMethod]
        public void Insert()
        {
            RleArray<char> chars = new RleArray<char>();

            Set(chars, "aabb");
            chars.Insert(2, new BigRleArrayRangeItem<char>('a'));
            Assert.AreEqual(5, chars.Count);
            Assert.AreEqual('a', chars[0].Value);
            Assert.AreEqual('a', chars[1].Value);
            Assert.AreEqual('a', chars[2].Value);
            Assert.AreEqual('b', chars[3].Value);
            Assert.AreEqual('b', chars[4].Value);

            Set(chars, "aabb");
            chars.Insert(2, new BigRleArrayRangeItem<char>('b'));
            Assert.AreEqual(5, chars.Count);
            Assert.AreEqual('a', chars[0].Value);
            Assert.AreEqual('a', chars[1].Value);
            Assert.AreEqual('b', chars[2].Value);
            Assert.AreEqual('b', chars[3].Value);
            Assert.AreEqual('b', chars[4].Value);

            Set(chars, "aabb");
            chars.Insert(2, new BigRleArrayRangeItem<char>('c'));
            Assert.AreEqual(5, chars.Count);
            Assert.AreEqual("a a c b b", chars.ToString());

            Set(chars, "aabb");
            chars.Insert(1, new BigRleArrayRangeItem<char>('a'));
            Assert.AreEqual(5, chars.Count);
            Assert.AreEqual('a', chars[0].Value);
            Assert.AreEqual('a', chars[1].Value);
            Assert.AreEqual('a', chars[2].Value);
            Assert.AreEqual('b', chars[3].Value);
            Assert.AreEqual('b', chars[4].Value);

            Set(chars, "aabb");
            chars.Insert(1, new BigRleArrayRangeItem<char>('c'));
            Assert.AreEqual(5, chars.Count);
            Assert.AreEqual('a', chars[0].Value);
            Assert.AreEqual('c', chars[1].Value);
            Assert.AreEqual('a', chars[2].Value);
            Assert.AreEqual('b', chars[3].Value);
            Assert.AreEqual('b', chars[4].Value);
        }

        [TestMethod]
        public void Remove()
        {
            RleArray<char> chars = new RleArray<char>();

            {
                // In a node
                Set(chars, "aa");
                chars.RemoveAt(0);
                Assert.AreEqual(1, chars.Count);
                Assert.AreEqual("[1|a]", chars.ToRleString());
                chars.RemoveAt(0);
                Assert.AreEqual(0, chars.Count);
                Assert.AreEqual("", chars.ToRleString());

                // Removing a node
                Set(chars, "aaabc");
                chars.RemoveAt(3);
                Assert.AreEqual(4, chars.Count);
                Assert.AreEqual("[3|a] [1|c]", chars.ToRleString());

                // Combining nodes
                Set(chars, "aabaa");
                chars.RemoveAt(2);
                Assert.AreEqual(4, chars.Count);
                Assert.AreEqual("[4|a]", chars.ToRleString());
            }
        }

        #region Utilities
        static void Set(RleArray<char> ary, IEnumerable<char> values)
        {
            ary.Clear();
            foreach (char value in values)
            {
                ary.Add(new BigRleArrayRangeItem<char>(value));
            }
        }
        #endregion
    }
}