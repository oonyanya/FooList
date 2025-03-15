using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection
{
    public interface IRange
    {
        int Index { get; set; }
        int Length { get; set; }
    }
    public class BigRangeList<T> : BigList<T> where T : IRange
    {
        public BigRangeList() :base()
        {
            this.CustomConverter = new CustomConverter<T>();
        }

    }

    internal interface ICustomeNode
    {
        int TotalSumCount { get; }
    }

    internal class CustomConcatNode<T> : ConcatNode<T>, ICustomeNode where T : IRange
    {
        public CustomConcatNode(ConcatNode<T> node) : base(node)
        {
        }
        public CustomConcatNode(Node<T> left, Node<T> right) : base(left, right)
        {
            var customNodeLeft = (ICustomeNode)left;
            var customNodeRight = (ICustomeNode)right;
            TotalSumCount = customNodeLeft.TotalSumCount + customNodeRight.TotalSumCount;
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

    internal class CustomLeafNode<T> : LeafNode<T>, ICustomeNode where T: IRange
    {
        public CustomLeafNode() : base()
        {
            TotalSumCount = 0;
        }

        public CustomLeafNode(T item) : base(item)
        {
            TotalSumCount = item.Length;
        }

        public CustomLeafNode(int count, FixedList<T> items) : base(count, items)
        {
            NotifyUpdate();
        }

        public override void NotifyUpdate()
        {
            TotalSumCount = 0;
            foreach (var item in items)
                TotalSumCount += item.Length;
        }

        public int TotalSumCount { get; private set; }
    }

    internal class CustomLeastFetch<T> : ILeastFetch<T> where T : IRange
    {
        public Node<T> Node { get; set; }

        public int TotalLeftCount { get; set; }

        public int absoluteIndex { get; set; }

        public CustomLeastFetch()
        {
        }
    }

    internal class CustomConverter<T> : ICustomConverter<T> where T : IRange
    {
        public ILeastFetch<T> LeastFetch { get { return _leastFetch; } }

        CustomLeastFetch<T> _leastFetch = null;

        public T Convert(T item)
        {
            return item;
        }

        public T ConvertBack(T item)
        {
            var customLeastFetch = (CustomLeastFetch<T>)LeastFetch;
            item.Index += customLeastFetch.absoluteIndex;
            return item;
        }

        public FixedList<T> Convert(FixedList<T> items)
        {
            return items;
        }

        public FixedList<T> ConvertBack(FixedList<T> items)
        {
            return items;
        }

        public ConcatNode<T> CreateConcatNode(ConcatNode<T> node)
        {
            return new CustomConcatNode<T>(node);
        }

        public ConcatNode<T> CreateConcatNode(Node<T> left, Node<T> right)
        {
            return new CustomConcatNode<T>(left, right);
        }

        public LeafNode<T> CreateLeafNode()
        {
            return new CustomLeafNode<T>();
        }

        public LeafNode<T> CreateLeafNode(T item)
        {
            return new CustomLeafNode<T>(item);
        }

        public LeafNode<T> CreateLeafNode(int count, FixedList<T> items)
        {
            return new CustomLeafNode<T>(count, items);
        }

        public void NodeWalk(Node<T> current, NodeWalkDirection dir)
        {
            if(this._leastFetch == null)
            {
                this._leastFetch = new CustomLeastFetch<T>();
            }

            var customNode = (ICustomeNode)current.Left;
            if (dir == NodeWalkDirection.Right)
            {
                var customLeastFetch = (CustomLeastFetch<T>)LeastFetch;
                customLeastFetch.absoluteIndex += customNode.TotalSumCount;
            }
        }

        public void SetState(Node<T> current, int totalLeftCountInList)
        {
            this._leastFetch.Node = current;
            this._leastFetch.TotalLeftCount = totalLeftCountInList;
        }

        public void ResetState()
        {
            this._leastFetch = null;
        }
    }
}
