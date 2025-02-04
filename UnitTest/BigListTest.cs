using System;
using FooProject.Collection;
using Microsoft.VisualStudio.TestPlatform.Utilities;

namespace UnitTest
{
    [TestClass]
    public sealed class ListTest
    {
        [TestMethod]
        public void AddRangeFrontTest()
        {
            var buf = new FooProject.Collection.BigList<char>();
            buf.AddRangeToFront("xyz");
            buf.AddRangeToFront("lqrstuvw");
            buf.AddRangeToFront("ijklmnop");
            buf.AddRangeToFront("abcdefgh");

            var output = String.Concat<char>(buf);
            Assert.AreEqual("abcdefghijklmnoplqrstuvwxyz", output);
        }

        [TestMethod]
        public void AddRangeTest()
        {
            var buf = new FooProject.Collection.BigList<char>();
            buf.AddRange("abcdefgh");
            buf.AddRange("ijklmnop");
            buf.AddRange("lqrstuvw");
            buf.AddRange("xyz");

            Assert.AreEqual('a', buf[0]);
            Assert.AreEqual('h', buf[7]);
            Assert.AreEqual('i', buf[8]);
            Assert.AreEqual('p', buf[15]);
            Assert.AreEqual('l', buf[16]);
            Assert.AreEqual('w', buf[23]);
            Assert.AreEqual('x', buf[24]);
            Assert.AreEqual('z', buf[26]);

            var output = String.Concat<char>(buf);
            Assert.AreEqual("abcdefghijklmnoplqrstuvwxyz", output);
        }

        [TestMethod]
        public void CountTest()
        {
            var buf = new FooProject.Collection.BigList<char>();
            Assert.AreEqual(0, buf.Count);
            buf.AddRange("abcdefghijklmnoplqrstuvwxyz");
            Assert.AreEqual(27, buf.Count);
        }

        [TestMethod]
        public void AddTest()
        {
            var buf = new FooProject.Collection.BigList<char>();
            buf.Add('0');
            buf.Add('1');
            buf.Add('2');
            buf.Add('3');
            buf.Add('4');
            buf.Add('5');
            buf.Add('6');
            buf.Add('7');
            buf.Add('8');
            buf.Add('9');
            buf.Add('a');
            buf.Add('b');
            buf.Add('c');
            buf.Add('d');
            buf.Add('e');
            buf.Add('f');
            buf.Add('g');
            buf.Add('h');
            buf.Add('i');
            buf.Add('j');
            buf.Add('k');
            var output = String.Concat<char>(buf);
            Assert.AreEqual("0123456789abcdefghijk", output);
        }

        [TestMethod]
        public void AddFrontTest()
        {
            var buf = new FooProject.Collection.BigList<char>();
            buf.Add('9');
            buf.AddToFront('8');
            buf.AddToFront('7');
            buf.AddToFront('6');
            buf.AddToFront('5');
            buf.AddToFront('4');
            buf.AddToFront('3');
            buf.AddToFront('2');
            buf.AddToFront('1');
            buf.AddToFront('0');
            var output = String.Concat<char>(buf);
            Assert.AreEqual("0123456789", output);
        }

        [TestMethod]
        public void ContainsTest()
        {
            var buf = new FooProject.Collection.BigList<char>();
            buf.AddRange("0123456789");
            var result = buf.Contains('0');
            Assert.AreEqual(true, result);
            result = buf.Contains('a');
            Assert.AreEqual(false, result);
        }

        [TestMethod]
        public void CopyToTest()
        {
            var buf = new FooProject.Collection.BigList<char>();
            char[] result = new char[6];
            buf.AddRange("012345");
            buf.CopyTo(result, 0);
            Assert.AreEqual("012345", String.Concat<char>(result));
        }

        [TestMethod]
        public void IndexTest()
        {
            var buf = new FooProject.Collection.BigList<char>();
            buf.AddRange("0123456789");
            var result = buf.IndexOf('0');
            Assert.AreEqual(0, result);
            result = buf.IndexOf('a');
            Assert.AreEqual(-1, result);
        }

        [TestMethod]
        public void InsertTest()
        {
            var buf = new FooProject.Collection.BigList<char>();
            buf.AddRange("0123456789");
            buf.Insert(8, ';');
            buf.Insert(4, 'a');
            buf.Insert(5, 'b');
            buf.Insert(6, 'c');
            buf.Insert(7, 'd');
            buf.Insert(8, 'e');
            buf.Insert(9, 'f');
            buf.Insert(10, 'g');
            var output = String.Concat<char>(buf);
            Assert.AreEqual("0123abcdefg4567;89", output);
        }

        [TestMethod]
        public void InserRangetTest()
        {
            var buf = new FooProject.Collection.BigList<char>();
            buf.AddRange("0123456789");
            buf.InsertRange(8, ";:");
            buf.InsertRange(4, "abcdef");
            buf.InsertRange(10, "gjiklmn");
            var output = String.Concat<char>(buf);
            Assert.AreEqual("0123abcdefgjiklmn4567;:89", output);
        }

        [TestMethod]
        public void RemoveTest()
        {
            var buf = new FooProject.Collection.BigList<char>();
            buf.AddRange("0123456789");
            var result = buf.Remove('0');
            var output = String.Concat<char>(buf);
            Assert.AreEqual("123456789", output);
            Assert.IsTrue(result);
            result = buf.Remove('x');
            Assert.AreEqual("123456789", output);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void RemoveRangeTest()
        {
            var buf = new FooProject.Collection.BigList<char>();
            buf.AddRange("0123456789");
            buf.InsertRange(5, "abcdef");
            buf.RemoveRange(5, 7);
            var output = String.Concat<char>(buf);
            Assert.AreEqual("012346789", output);
            buf.RemoveRange(0, 7);
            output = String.Concat<char>(buf);
            Assert.AreEqual("89", output);
        }

        [TestMethod]
        public void RemoveAtTest()
        {
            var buf = new FooProject.Collection.BigList<char>();
            buf.AddRange("01234567"+"89");
            buf.RemoveAt(7);
            var output = String.Concat<char>(buf);
            Assert.AreEqual("012345689", output);
            buf.RemoveAt(7);
            output = String.Concat<char>(buf);
            Assert.AreEqual("01234569", output);
            buf.RemoveAt(0);
            output = String.Concat<char>(buf);
            Assert.AreEqual("1234569", output);
            buf.RemoveAt(6);
            output = String.Concat<char>(buf);
            Assert.AreEqual("123456", output);
        }
    }
}
