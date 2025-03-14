using FooProject.Collection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace UnitTest
{
    [TestClass]
    public class CustomConverterTest
    {
        [TestMethod]
        public void TotalTest()
        {
            BigList<int> list = new BigList<int>();
            list.CustomConverter = new CustomConverter();
            for(int i = 0; i < 8; i++)
            {
                list.Add(3);
            }
            list.Insert(0, 3);
            list.Insert(list.Count, 3);

            var expected = new int[] { 3, 6, 6, 6, 6, 6, 6, 6, 6, 9, };
            for(int i=0; i < 10; i++)
            {
                var n = list[i];
                Assert.AreEqual(expected[i], list.CustomConverter.ConvertBack(n));
            }
        }
    }

    interface ICustomeNode
    {
        int TotalSumCount { get; }
    }

    class CustomConcatNode<T> : ConcatNode<T>,ICustomeNode
    {
        public CustomConcatNode(ConcatNode<T> node) : base(node)
        {
        }
        public CustomConcatNode(Node<T> left, Node<T> right) : base(left,right)
        {
        }

        public int TotalSumCount { get; private set; }

        protected override Node<T> NewNodeInPlace(Node<T> newLeft, Node<T> newRight)
        {
            var customNodeLeft = (ICustomeNode)newLeft;
            var customNodeRight = (ICustomeNode)newRight;
            TotalSumCount = customNodeLeft.TotalSumCount + customNodeRight.TotalSumCount;
            return base.NewNodeInPlace(newLeft, newRight);
        }
    }

    class CustomLeafNode<T> : LeafNode<T>, ICustomeNode
    {
        public CustomLeafNode() : base()
        {
            TotalSumCount = 0;
        }

        public CustomLeafNode(T item) : base(item)
        {
            TotalSumCount = Convert.ToInt32(item);
        }

        public CustomLeafNode(int count, FixedList<T> items) : base(count, items)
        {
            foreach (var item in items)
                TotalSumCount += Convert.ToInt32(item);
        }

        public int TotalSumCount { get; private set; }
    }

    class CustomConverter : ICustomConverter<int>
    {
        int absoluteIndex = 0;

        public ILeastFetch<int> LeastFetch { get; private set; }

        public int Convert(int item)
        {
            return item;
        }

        public int ConvertBack(int item)
        {
            return item + absoluteIndex;
        }

        public FixedList<int> Convert(FixedList<int> items)
        {
            return items;
        }

        public FixedList<int> ConvertBack(FixedList<int> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                items[i] += absoluteIndex;
            }
            return items;
        }

        public ConcatNode<int> CreateConcatNode(ConcatNode<int> node)
        {
            return new CustomConcatNode<int>(node);
        }

        public ConcatNode<int> CreateConcatNode(Node<int> left, Node<int> right)
        {
            return new CustomConcatNode<int>(left,right);
        }

        public LeafNode<int> CreateLeafNode()
        {
            return new CustomLeafNode<int>();
        }

        public LeafNode<int> CreateLeafNode(int item)
        {
            return new CustomLeafNode<int>(item);
        }

        public LeafNode<int> CreateLeafNode(int count, FixedList<int> items)
        {
            return new CustomLeafNode<int>(count,items);
        }

        public void NodeWalk(Node<int> current, NodeWalkDirection dir)
        {
            var customNode = (ICustomeNode)current;
            if (dir == NodeWalkDirection.Right)
            {
                absoluteIndex += customNode.TotalSumCount;
            }
        }

        public void SetState(Node<int> current, int totalLeftCountInList)
        {
            this.LeastFetch = new LeastFetch<int>(current, totalLeftCountInList);
        }

        public void ResetState()
        {
            this.LeastFetch = null;
        }
    }
}
