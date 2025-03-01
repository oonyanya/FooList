using FooProject.Collection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest
{
    [TestClass]
    public class FixedListTest
    {
        [TestMethod]
        public void AddTest()
        {
            var list = new FixedList<char>();
            list.Add('1');
            list.Add('2');
            list.Add('3');
            list.Add('4');
            Assert.AreEqual(4, list.Count);
            Assert.AreEqual("1234", string.Concat(list));
       }
        [TestMethod]
        public void GetAtAndSetATTest()
        {
            var list = new FixedList<char>();
            list.AddRange("1234");
            list[0] = 'a';
            list[1] = 'a';
            list[3] = 'a';
            Assert.AreEqual(4, list.Count);
            Assert.AreEqual("aa3a", string.Concat(list));
        }

        [TestMethod]
        public void AddRageTest()
        {
            var list = new FixedList<char>();
            list.AddRange("123");
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual("123", string.Concat(list));
        }
        [TestMethod]
        public void InsertTest()
        {
            var list = new FixedList<char>();
            list.AddRange("12");
            list.Insert(0, '3');
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual("312", string.Concat(list));

            list.Insert(1, '4');
            Assert.AreEqual(4, list.Count);
            Assert.AreEqual("3412", string.Concat(list));

            list = new FixedList<char>();
            list.AddRange("12");
            list.Insert(2, '3');
            Assert.AreEqual("123", string.Concat(list));
        }

        [TestMethod]
        public void InsertRageTest()
        {
            var list = new FixedList<char>();
            list.AddRange("12");
            list.InsertRange(1, "34".ToArray());
            Assert.AreEqual(4, list.Count);
            Assert.AreEqual("1342", string.Concat(list));

            list = new FixedList<char>();
            list.AddRange("12");
            list.InsertRange(2, "34".ToArray());
            Assert.AreEqual("1234", string.Concat(list));

            list = new FixedList<char>();
            list.AddRange("12");
            list.InsertRange(0, "34".ToArray());
            Assert.AreEqual("3412", string.Concat(list));
        }

        [TestMethod]
        public void RemoveAtTest()
        {
            var list = new FixedList<char>();
            list.AddRange("1234");
            list.RemoveAt(0);
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual("234", string.Concat(list));

            list.RemoveAt(1);
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("24", string.Concat(list));

            list.RemoveAt(0);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("4", string.Concat(list));
        }

        [TestMethod]
        public void RemoveRageTest()
        {
            var list = new FixedList<char>();
            list.AddRange("1234");
            list.RemoveRange(0, 2);
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("34", string.Concat(list));

            list = new FixedList<char>();
            list.AddRange("1234");
            list.RemoveRange(1, 2);
            Assert.AreEqual("14", string.Concat(list));

            list = new FixedList<char>();
            list.AddRange("1234");
            list.RemoveRange(1, 3);
            Assert.AreEqual("1", string.Concat(list));
        }

        [TestMethod]
        public void GrowTest()
        {
            var list = new FixedList<char>(4, 10);
            list.AddRange("1234");
            list.AddRange("1234");
            Assert.IsTrue(list.Count == 8);
            Assert.AreEqual("12341234", string.Concat(list));
            try
            {
                list.AddRange("1234");
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }

            try
            {
                list.AddRange("12");
            }
            catch (Exception e)
            {
                Assert.Fail("should not throw");
            }

            try
            {
                list.InsertRange(0,"1234");
                Assert.Fail("should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidOperationException);
            }

            try
            {
                list.RemoveRange(0, 2);
                list.InsertRange(0, "12");
            }
            catch (Exception e)
            {
                Assert.Fail("should not throw");
            }
        }
    }
}
