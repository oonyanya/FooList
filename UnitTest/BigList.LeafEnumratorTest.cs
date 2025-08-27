using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FooProject.Collection;
using FooProject.Collection.DataStore;

namespace UnitTest
{
    [TestClass]
    public class BigList_LeafEnumrator
    {
        private IComposableList<int> GetItems(LeafNode<int> leafNode)
        {
            return leafNode.container.Content;
        }
        private LeafNode<int> CreateLeafNode(int item)
        {
            var items = new FixedList<int>(4);
            items.Add(item);
            var pinabile = new PinableContainer<IComposableList<int>>(items);
            return new LeafNode<int>(1, pinabile);
        }

        [TestMethod]
        public void AddLast()
        {
            var leafNodeEnumrator = new LeafNodeEnumrator<int>();

            var numberList = new List<int>() { 1, 2, 3 };
            foreach (var number in numberList)
                leafNodeEnumrator.AddLast(CreateLeafNode(number));

            AssertEuqaltyCollection(leafNodeEnumrator, numberList.ToArray());
            AssertEuqaltyCollectionBackword(leafNodeEnumrator, numberList.Reverse<int>().ToArray());
        }

        [TestMethod]
        public void AddNext()
        {
            var leafNodeEnumrator = new LeafNodeEnumrator<int>();

            var node1 = CreateLeafNode(1);
            leafNodeEnumrator.AddNext(leafNodeEnumrator.FirstNode, node1);

            var node2 = CreateLeafNode(4);
            leafNodeEnumrator.AddNext(leafNodeEnumrator.LastNode, node2);

            var node3 = CreateLeafNode(2);
            leafNodeEnumrator.AddNext(leafNodeEnumrator.FirstNode, node3);

            var node4 = CreateLeafNode(3);
            leafNodeEnumrator.AddNext(node3, node4);

            var expectNumberList = new List<int>() { 1, 2, 3, 4 };
            AssertEuqaltyCollection(leafNodeEnumrator, expectNumberList.ToArray());
            AssertEuqaltyCollectionBackword(leafNodeEnumrator, expectNumberList.Reverse<int>().ToArray());

            Assert.AreEqual(4, GetItems(leafNodeEnumrator.LastNode)[0]);
        }

        private LeafNodeEnumrator<int> CreateList(int start,int end)
        {
            var leafNodeEnumrator = new LeafNodeEnumrator<int>();
            for (int i = start; i <= end; i++)
            {
                var node = CreateLeafNode(i);
                leafNodeEnumrator.AddLast(node);
            }
            return leafNodeEnumrator;
        }

        private void AssertEuqaltyCollection(LeafNodeEnumrator<int> leafNodeEnumrator, int[] expect)
        {
            var node = leafNodeEnumrator.FirstNode;
            for (int i = 0; i < expect.Length; i++)
            {
                Assert.AreEqual(expect[i], GetItems(node)[0]);
                node = (LeafNode<int>)node.Next;
            }
        }

        private void AssertEuqaltyCollectionBackword(LeafNodeEnumrator<int> leafNodeEnumrator, int[] expect)
        {
            var node = leafNodeEnumrator.LastNode;
            for (int i = 0; i < expect.Length; i++)
            {
                Assert.AreEqual(expect[i], GetItems(node)[0]);
                node = (LeafNode<int>)node.Previous;
            }
        }

        [TestMethod]
        public void AddNext2()
        {
            var leafNodeEnumrator = CreateList(0,3);
            var otherLeafNodeEnumrator = CreateList(4,7);
            leafNodeEnumrator.AddNext(leafNodeEnumrator.LastNode, otherLeafNodeEnumrator);
            var expectList = new int[] { 0, 1, 2, 3, 4, 5, 6, 7 };
            AssertEuqaltyCollection(leafNodeEnumrator, expectList);
            AssertEuqaltyCollectionBackword(leafNodeEnumrator, expectList.Reverse<int>().ToArray());
            Assert.AreEqual(0, GetItems(leafNodeEnumrator.FirstNode)[0]);
            Assert.AreEqual(7, GetItems(leafNodeEnumrator.LastNode)[0]);

            leafNodeEnumrator = CreateList(0, 3);
            otherLeafNodeEnumrator = CreateList(4, 7);
            leafNodeEnumrator.AddNext(leafNodeEnumrator.FirstNode, otherLeafNodeEnumrator);
            expectList = new int[] { 0, 4, 5, 6, 7, 1, 2, 3 };
            AssertEuqaltyCollection(leafNodeEnumrator, expectList);
            AssertEuqaltyCollectionBackword(leafNodeEnumrator, expectList.Reverse<int>().ToArray());
            Assert.AreEqual(0, GetItems(leafNodeEnumrator.FirstNode)[0]);
            Assert.AreEqual(3, GetItems(leafNodeEnumrator.LastNode)[0]);

            leafNodeEnumrator = CreateList(0, 3);
            otherLeafNodeEnumrator = CreateList(4, 7);
            leafNodeEnumrator.AddNext((LeafNode<int>)leafNodeEnumrator.FirstNode.Next, otherLeafNodeEnumrator);
            expectList = new int[] { 0, 1, 4, 5, 6, 7, 2, 3 };
            AssertEuqaltyCollection(leafNodeEnumrator, expectList);
            AssertEuqaltyCollectionBackword(leafNodeEnumrator, expectList.Reverse<int>().ToArray());
            Assert.AreEqual(0, GetItems(leafNodeEnumrator.FirstNode)[0]);
            Assert.AreEqual(3, GetItems(leafNodeEnumrator.LastNode)[0]);
        }

        [TestMethod]
        public void AddBefore()
        {
            var leafNodeEnumrator = new LeafNodeEnumrator<int>();

            var node1 = CreateLeafNode(4);
            leafNodeEnumrator.AddBefore(leafNodeEnumrator.FirstNode, node1);

            var node2 = CreateLeafNode(1);
            leafNodeEnumrator.AddBefore(leafNodeEnumrator.LastNode, node2);

            var node3 = CreateLeafNode(3);
            leafNodeEnumrator.AddBefore(node1, node3);

            var node4 = CreateLeafNode(2);
            leafNodeEnumrator.AddBefore(node3, node4);

            var expectNumberList = new List<int>() { 1, 2, 3, 4 };
            AssertEuqaltyCollection(leafNodeEnumrator, expectNumberList.ToArray());
            AssertEuqaltyCollectionBackword(leafNodeEnumrator, expectNumberList.Reverse<int>().ToArray());

            Assert.AreEqual(4, GetItems(leafNodeEnumrator.LastNode)[0]);
        }

        [TestMethod]
        public void AddBefore2()
        {
            var leafNodeEnumrator = CreateList(0, 3);
            var otherLeafNodeEnumrator = CreateList(4, 7);
            leafNodeEnumrator.AddBefore(leafNodeEnumrator.LastNode, otherLeafNodeEnumrator);
            var expectNumberList = new int[] { 0, 1, 2, 4, 5, 6, 7, 3 };
            AssertEuqaltyCollection(leafNodeEnumrator, expectNumberList);
            AssertEuqaltyCollectionBackword(leafNodeEnumrator, expectNumberList.Reverse<int>().ToArray());
            Assert.AreEqual(0, GetItems(leafNodeEnumrator.FirstNode)[0]);
            Assert.AreEqual(3, GetItems(leafNodeEnumrator.LastNode)[0]);

            leafNodeEnumrator = CreateList(0, 3);
            otherLeafNodeEnumrator = CreateList(4, 7);
            leafNodeEnumrator.AddBefore(leafNodeEnumrator.FirstNode, otherLeafNodeEnumrator);
            expectNumberList = new int[] { 4, 5, 6, 7, 0, 1, 2, 3 };
            AssertEuqaltyCollection(leafNodeEnumrator, expectNumberList);
            AssertEuqaltyCollectionBackword(leafNodeEnumrator, expectNumberList.Reverse<int>().ToArray());
            Assert.AreEqual(4, GetItems(leafNodeEnumrator.FirstNode)[0]);
            Assert.AreEqual(3, GetItems(leafNodeEnumrator.LastNode)[0]);

            leafNodeEnumrator = CreateList(0, 3);
            otherLeafNodeEnumrator = CreateList(4, 7);
            leafNodeEnumrator.AddBefore((LeafNode<int>)leafNodeEnumrator.FirstNode.Next, otherLeafNodeEnumrator);
            expectNumberList = new int[] { 0, 4, 5, 6, 7, 1, 2, 3 };
            AssertEuqaltyCollection(leafNodeEnumrator, expectNumberList);
            AssertEuqaltyCollectionBackword(leafNodeEnumrator, expectNumberList.Reverse<int>().ToArray());
            Assert.AreEqual(0, GetItems(leafNodeEnumrator.FirstNode)[0]);
            Assert.AreEqual(3, GetItems(leafNodeEnumrator.LastNode)[0]);

        }

        [TestMethod]
        public void Remove()
        {
            var leafNodeEnumrator = new LeafNodeEnumrator<int>();

            var node1 = CreateLeafNode(1);
            leafNodeEnumrator.AddLast(node1);
            var node2 = CreateLeafNode(2);
            leafNodeEnumrator.AddLast(node2);
            var node3 = CreateLeafNode(3);
            leafNodeEnumrator.AddLast(node3);
            var node4 = CreateLeafNode(4);
            leafNodeEnumrator.AddLast(node4);

            leafNodeEnumrator.Remove(node2);
            Assert.IsNull(node2.Next);
            Assert.IsNull(node2.Previous);
            var expectNumberList = new int[] { 1, 3, 4 };
            AssertEuqaltyCollection(leafNodeEnumrator, expectNumberList);
            AssertEuqaltyCollectionBackword(leafNodeEnumrator, expectNumberList.Reverse<int>().ToArray());

            leafNodeEnumrator.Remove(node1);
            Assert.IsNull(node1.Next);
            Assert.IsNull(node1.Previous);
            expectNumberList = new int[] { 3, 4 };
            AssertEuqaltyCollection(leafNodeEnumrator, expectNumberList);
            AssertEuqaltyCollectionBackword(leafNodeEnumrator, expectNumberList.Reverse<int>().ToArray());

            leafNodeEnumrator.Remove(node4);
            Assert.IsNull(node4.Next);
            Assert.IsNull(node4.Previous);

            var firstNode = leafNodeEnumrator.FirstNode;
            Assert.AreEqual(3, GetItems(firstNode)[0]);
            Assert.IsNull(firstNode.Next);
            Assert.IsNull(firstNode.Previous);
        }

        [TestMethod]
        public void ReplaceTest()
        {
            var leafNodeEnumrator = new LeafNodeEnumrator<int>();

            var node1 = CreateLeafNode(1);
            leafNodeEnumrator.AddLast(node1);
            var node2 = CreateLeafNode(2);
            leafNodeEnumrator.AddLast(node2);
            var node3 = CreateLeafNode(3);
            leafNodeEnumrator.AddLast(node3);
            var node4 = CreateLeafNode(4);
            leafNodeEnumrator.AddLast(node4);

            var newNode = CreateLeafNode(5);
            leafNodeEnumrator.Replace(node2,newNode);
            Assert.IsNotNull(newNode.Next);
            Assert.IsNotNull(newNode.Previous);
            Assert.IsNull(node2.Next);
            Assert.IsNull(node2.Previous);
            var expectNumberList = new int[] { 1, 5, 3, 4 };
            AssertEuqaltyCollection(leafNodeEnumrator, expectNumberList);
            AssertEuqaltyCollectionBackword(leafNodeEnumrator, expectNumberList.Reverse<int>().ToArray());

            var newNode2 = CreateLeafNode(2);
            leafNodeEnumrator.AddNext(newNode,newNode2);
            expectNumberList = new int[] { 1, 5, 2, 3, 4 };
            AssertEuqaltyCollection(leafNodeEnumrator, expectNumberList);
            AssertEuqaltyCollectionBackword(leafNodeEnumrator, expectNumberList.Reverse<int>().ToArray());

            var newNode3 = CreateLeafNode(-2);
            leafNodeEnumrator.AddBefore(newNode, newNode3);
            expectNumberList = new int[] { 1, -2, 5, 2, 3, 4 };
            AssertEuqaltyCollection(leafNodeEnumrator, expectNumberList);
            AssertEuqaltyCollectionBackword(leafNodeEnumrator, expectNumberList.Reverse<int>().ToArray());

            newNode = CreateLeafNode(5);
            leafNodeEnumrator.Replace(node1, newNode);
            Assert.IsNotNull(newNode.Next);
            Assert.IsNull(newNode.Previous);
            Assert.IsNull(node1.Next);
            Assert.IsNull(node1.Previous);
            expectNumberList = new int[] { 5, -2, 5, 2, 3, 4 };
            AssertEuqaltyCollection(leafNodeEnumrator, expectNumberList);
            AssertEuqaltyCollectionBackword(leafNodeEnumrator, expectNumberList.Reverse<int>().ToArray());

            newNode2 = CreateLeafNode(2);
            leafNodeEnumrator.AddNext(newNode, newNode2);
            expectNumberList = new int[] { 5, 2, -2, 5, 2, 3, 4 };
            AssertEuqaltyCollection(leafNodeEnumrator, expectNumberList);
            AssertEuqaltyCollectionBackword(leafNodeEnumrator, expectNumberList.Reverse<int>().ToArray());

            newNode3 = CreateLeafNode(-2);
            leafNodeEnumrator.AddBefore(newNode, newNode3);
            expectNumberList = new int[] {-2, 5, 2 ,-2, 5, 2, 3, 4 };
            AssertEuqaltyCollection(leafNodeEnumrator, expectNumberList);
            AssertEuqaltyCollectionBackword(leafNodeEnumrator, expectNumberList.Reverse<int>().ToArray());

            newNode = CreateLeafNode(5);
            leafNodeEnumrator.Replace(node4, newNode);
            Assert.IsNotNull(newNode.Previous);
            Assert.IsNull(newNode.Next);
            Assert.IsNull(node4.Next);
            Assert.IsNull(node4.Previous);
            expectNumberList = new int[] { -2, 5, 2, -2, 5, 2, 3, 5 };
            AssertEuqaltyCollection(leafNodeEnumrator, expectNumberList);
            AssertEuqaltyCollectionBackword(leafNodeEnumrator, expectNumberList.Reverse<int>().ToArray());

            newNode2 = CreateLeafNode(2);
            leafNodeEnumrator.AddNext(newNode, newNode2);
            expectNumberList = new int[] { -2, 5, 2, -2, 5, 2, 3, 5, 2 };
            AssertEuqaltyCollection(leafNodeEnumrator, expectNumberList);
            AssertEuqaltyCollectionBackword(leafNodeEnumrator, expectNumberList.Reverse<int>().ToArray());

            newNode3 = CreateLeafNode(-2);
            leafNodeEnumrator.AddBefore(newNode, newNode3);
            expectNumberList = new int[] { -2, 5, 2, -2, 5, 2, 3, -2 ,5, 2 };
            AssertEuqaltyCollection(leafNodeEnumrator, expectNumberList);
            AssertEuqaltyCollectionBackword(leafNodeEnumrator, expectNumberList.Reverse<int>().ToArray());

            newNode = CreateLeafNode(5);
            leafNodeEnumrator.Replace(node3, newNode);
            Assert.IsNotNull(newNode.Previous);
            Assert.IsNotNull(newNode.Next);
            Assert.IsNull(node3.Next);
            Assert.IsNull(node3.Previous);
            expectNumberList = new int[] { -2, 5, 2, -2, 5, 2, 5, -2, 5, 2 };
            AssertEuqaltyCollection(leafNodeEnumrator, expectNumberList);
            AssertEuqaltyCollectionBackword(leafNodeEnumrator, expectNumberList.Reverse<int>().ToArray());

            newNode2 = CreateLeafNode(2);
            leafNodeEnumrator.AddNext(newNode, newNode2);
            expectNumberList = new int[] { -2, 5, 2, -2, 5, 2, 5, 2, -2, 5, 2 };
            AssertEuqaltyCollection(leafNodeEnumrator, expectNumberList);
            AssertEuqaltyCollectionBackword(leafNodeEnumrator, expectNumberList.Reverse<int>().ToArray());

            newNode3 = CreateLeafNode(-2);
            leafNodeEnumrator.AddBefore(newNode, newNode3);
            expectNumberList = new int[] { -2, 5, 2, -2, 5, 2, -2, 5, 2, -2, 5, 2 };
            AssertEuqaltyCollection(leafNodeEnumrator, expectNumberList);
            AssertEuqaltyCollectionBackword(leafNodeEnumrator, expectNumberList.Reverse<int>().ToArray());
        }
    }
}
