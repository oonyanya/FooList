using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FooProject.Collection;

namespace UnitTest
{
    [TestClass]
    public class BigList_LeafEnumrator
    {
        [TestMethod]
        public void AddLast()
        {
            var leafNodeEnumrator = new LeafNodeEnumrator<int>();

            var numberList = new List<int>() { 1, 2, 3 };
            foreach (var number in numberList)
                leafNodeEnumrator.AddLast(new LeafNode<int>(number));

            AssertEuqaltyCollection(leafNodeEnumrator, numberList.ToArray());
        }

        [TestMethod]
        public void AddNext()
        {
            var leafNodeEnumrator = new LeafNodeEnumrator<int>();

            var node1 = new LeafNode<int>(1);
            leafNodeEnumrator.AddNext(leafNodeEnumrator.FirstNode, node1);

            var node2 = new LeafNode<int>(4);
            leafNodeEnumrator.AddNext(leafNodeEnumrator.LastNode, node2);

            var node3 = new LeafNode<int>(2);
            leafNodeEnumrator.AddNext(leafNodeEnumrator.FirstNode, node3);

            var node4 = new LeafNode<int>(3);
            leafNodeEnumrator.AddNext(node3, node4);

            var expectNumberList = new List<int>() { 1, 2, 3, 4 };
            AssertEuqaltyCollection(leafNodeEnumrator, expectNumberList.ToArray());

            Assert.AreEqual(4, leafNodeEnumrator.LastNode.items[0]);
        }

        private LeafNodeEnumrator<int> CreateList(int start,int end)
        {
            var leafNodeEnumrator = new LeafNodeEnumrator<int>();
            for (int i = start; i <= end; i++)
            {
                var node = new LeafNode<int>(i);
                leafNodeEnumrator.AddLast(node);
            }
            return leafNodeEnumrator;
        }

        private void AssertEuqaltyCollection(LeafNodeEnumrator<int> leafNodeEnumrator, int[] expect)
        {
            var node = leafNodeEnumrator.FirstNode;
            for (int i = 0; i < expect.Length; i++)
            {
                Assert.AreEqual(expect[i], node.items[0]);
                node = (LeafNode<int>)node.Next;
            }
        }

        [TestMethod]
        public void AddNext2()
        {
            var leafNodeEnumrator = CreateList(0,3);
            var otherLeafNodeEnumrator = CreateList(4,7);
            leafNodeEnumrator.AddNext(leafNodeEnumrator.LastNode, otherLeafNodeEnumrator);
            AssertEuqaltyCollection(leafNodeEnumrator, new int[] { 0, 1, 2, 3, 4, 5, 6, 7 });
            Assert.AreEqual(0, leafNodeEnumrator.FirstNode.items[0]);
            Assert.AreEqual(7, leafNodeEnumrator.LastNode.items[0]);

            leafNodeEnumrator = CreateList(0, 3);
            otherLeafNodeEnumrator = CreateList(4, 7);
            leafNodeEnumrator.AddNext(leafNodeEnumrator.FirstNode, otherLeafNodeEnumrator);
            AssertEuqaltyCollection(leafNodeEnumrator, new int[] { 0, 4, 5, 6, 7, 1, 2, 3 });
            Assert.AreEqual(0, leafNodeEnumrator.FirstNode.items[0]);
            Assert.AreEqual(3, leafNodeEnumrator.LastNode.items[0]);

            leafNodeEnumrator = CreateList(0, 3);
            otherLeafNodeEnumrator = CreateList(4, 7);
            leafNodeEnumrator.AddNext((LeafNode<int>)leafNodeEnumrator.FirstNode.Next, otherLeafNodeEnumrator);
            AssertEuqaltyCollection(leafNodeEnumrator, new int[] { 0, 1, 4, 5, 6, 7, 2, 3 });
            Assert.AreEqual(0, leafNodeEnumrator.FirstNode.items[0]);
            Assert.AreEqual(3, leafNodeEnumrator.LastNode.items[0]);
        }

        [TestMethod]
        public void AddBefore()
        {
            var leafNodeEnumrator = new LeafNodeEnumrator<int>();

            var node1 = new LeafNode<int>(4);
            leafNodeEnumrator.AddBefore(leafNodeEnumrator.FirstNode, node1);

            var node2 = new LeafNode<int>(1);
            leafNodeEnumrator.AddBefore(leafNodeEnumrator.LastNode, node2);

            var node3 = new LeafNode<int>(3);
            leafNodeEnumrator.AddBefore(node1, node3);

            var node4 = new LeafNode<int>(2);
            leafNodeEnumrator.AddBefore(node3, node4);

            var expectNumberList = new List<int>() { 1, 2, 3, 4 };
            AssertEuqaltyCollection(leafNodeEnumrator, expectNumberList.ToArray());

            Assert.AreEqual(4, leafNodeEnumrator.LastNode.items[0]);
        }

        [TestMethod]
        public void AddBefore2()
        {
            var leafNodeEnumrator = CreateList(0, 3);
            var otherLeafNodeEnumrator = CreateList(4, 7);
            leafNodeEnumrator.AddBefore(leafNodeEnumrator.LastNode, otherLeafNodeEnumrator);
            AssertEuqaltyCollection(leafNodeEnumrator, new int[] { 0, 1, 2, 4, 5, 6, 7, 3});
            Assert.AreEqual(0, leafNodeEnumrator.FirstNode.items[0]);
            Assert.AreEqual(3, leafNodeEnumrator.LastNode.items[0]);

            leafNodeEnumrator = CreateList(0, 3);
            otherLeafNodeEnumrator = CreateList(4, 7);
            leafNodeEnumrator.AddBefore(leafNodeEnumrator.FirstNode, otherLeafNodeEnumrator);
            AssertEuqaltyCollection(leafNodeEnumrator, new int[] { 4, 5, 6, 7, 0, 1, 2, 3 });
            Assert.AreEqual(4, leafNodeEnumrator.FirstNode.items[0]);
            Assert.AreEqual(3, leafNodeEnumrator.LastNode.items[0]);

            leafNodeEnumrator = CreateList(0, 3);
            otherLeafNodeEnumrator = CreateList(4, 7);
            leafNodeEnumrator.AddBefore((LeafNode<int>)leafNodeEnumrator.FirstNode.Next, otherLeafNodeEnumrator);
            AssertEuqaltyCollection(leafNodeEnumrator, new int[] { 0, 4, 5, 6, 7, 1, 2, 3 });
            Assert.AreEqual(0, leafNodeEnumrator.FirstNode.items[0]);
            Assert.AreEqual(3, leafNodeEnumrator.LastNode.items[0]);

        }

        [TestMethod]
        public void Remove()
        {
            var leafNodeEnumrator = new LeafNodeEnumrator<int>();

            var node1 = new LeafNode<int>(1);
            leafNodeEnumrator.AddLast(node1);
            var node2 = new LeafNode<int>(2);
            leafNodeEnumrator.AddLast(node2);
            var node3 = new LeafNode<int>(3);
            leafNodeEnumrator.AddLast(node3);
            var node4 = new LeafNode<int>(4);
            leafNodeEnumrator.AddLast(node4);

            leafNodeEnumrator.Remove(node2);
            Assert.IsNull(node2.Next);
            Assert.IsNull(node2.Previous);

            leafNodeEnumrator.Remove(node1);
            Assert.IsNull(node1.Next);
            Assert.IsNull(node1.Previous);

            leafNodeEnumrator.Remove(node4);
            Assert.IsNull(node4.Next);
            Assert.IsNull(node4.Previous);

            var firstNode = leafNodeEnumrator.FirstNode;
            Assert.AreEqual(3, firstNode.items[0]);
            Assert.IsNull(firstNode.Next);
            Assert.IsNull(firstNode.Previous);
        }

        [TestMethod]
        public void ReplaceTest()
        {
            var leafNodeEnumrator = new LeafNodeEnumrator<int>();

            var node1 = new LeafNode<int>(1);
            leafNodeEnumrator.AddLast(node1);
            var node2 = new LeafNode<int>(2);
            leafNodeEnumrator.AddLast(node2);
            var node3 = new LeafNode<int>(3);
            leafNodeEnumrator.AddLast(node3);
            var node4 = new LeafNode<int>(4);
            leafNodeEnumrator.AddLast(node4);

            var newNode = new LeafNode<int>(5);
            leafNodeEnumrator.Replace(node2,newNode);
            Assert.IsNotNull(newNode.Next);
            Assert.IsNotNull(newNode.Previous);
            Assert.IsNull(node2.Next);
            Assert.IsNull(node2.Previous);

            newNode = new LeafNode<int>(5);
            leafNodeEnumrator.Replace(node1, newNode);
            Assert.IsNotNull(newNode.Next);
            Assert.IsNull(newNode.Previous);
            Assert.IsNull(node1.Next);
            Assert.IsNull(node1.Previous);

            newNode = new LeafNode<int>(5);
            leafNodeEnumrator.Replace(node4, newNode);
            Assert.IsNotNull(newNode.Previous);
            Assert.IsNull(newNode.Next);
            Assert.IsNull(node4.Next);
            Assert.IsNull(node4.Previous);

            newNode = new LeafNode<int>(5);
            leafNodeEnumrator.Replace(node3, newNode);
            Assert.IsNotNull(newNode.Previous);
            Assert.IsNotNull(newNode.Next);
            Assert.IsNull(node3.Next);
            Assert.IsNull(node3.Previous);

            var node = leafNodeEnumrator.FirstNode;
            while (node != null)
            {
                Assert.AreEqual(5, node.items[0]);
                node = (LeafNode<int>)node.Next;
            }
        }
    }
}
