using System;
using FooProject.Collection;
using Microsoft.VisualStudio.TestPlatform.Utilities;

namespace UnitTest
{
    [TestClass]
    public sealed class ListTest
    {
        [TestMethod]
        public void TryGetItemAndAddRangeTest()
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
        }

        [TestMethod]
        public void CountTest()
        {
            var buf = new FooProject.Collection.BigList<char>();
            Assert.AreEqual(0, buf.Count);
            buf.AddRange("0123456789");
            Assert.AreEqual(10, buf.Count);
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
            buf.Insert(4, 'a');
            var output = String.Concat<char>(buf);
            Assert.AreEqual("0123a456789", output);
        }

        [TestMethod]
        public void InserRangetTest()
        {
            var buf = new FooProject.Collection.BigList<char>();
            buf.AddRange("0123456789");
            buf.InsertRange(4, "abcdef");
            var output = String.Concat<char>(buf);
            Assert.AreEqual("0123abcdef456789", output);
        }

        [TestMethod]
        public void RemoveTest()
        {
            var buf = new FooProject.Collection.BigList<char>();
            buf.AddRange("0123456789");
            buf.InsertRange(5, "abcdef");
            buf.RemoveRange(5, 7);
            var output = String.Concat<char>(buf);
            Assert.AreEqual("012346789", output);
        }

    }
}
