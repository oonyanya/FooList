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
    public class BigRangeList<T> : BigList<T> where T : IRange,new()
    {
        public BigRangeList() :base()
        {
            this.CustomConverter = new RangeConverter<T>();
        }

        public T GetIndexIntoRange(int index)
        {
            int relativeIndex;
            LeafNode<T> leafNode;
            if (CustomConverter.LeastFetch != null)
            {
                relativeIndex = index - CustomConverter.LeastFetch.TotalLeftCount;
                if (relativeIndex >= 0 && relativeIndex < CustomConverter.LeastFetch.Node.Count)
                {
                    leafNode = (LeafNode<T>)CustomConverter.LeastFetch.Node;
                    return CustomConverter.ConvertBack(leafNode.items[relativeIndex]);
                }
            }

            RangeConverter<T> myCustomConverter = (RangeConverter<T>)CustomConverter;
            relativeIndex = index;
            var node = WalkNode((current, leftCount) => {
                if (relativeIndex < leftCount)
                {
                    return NodeWalkDirection.Left;
                }
                else
                {
                    relativeIndex -= leftCount;
                    var customNode = (IRangeNode)current.Left;
                    myCustomConverter.customLeastFetch.absoluteIndex += customNode.TotalSumCount;
                    return NodeWalkDirection.Right;
                }
            });

            leafNode = (LeafNode<T>)CustomConverter.LeastFetch.Node;
            return CustomConverter.ConvertBack(leafNode.items[relativeIndex]);
        }

    }

    internal interface IRangeNode
    {
        int TotalSumCount { get; }
    }

    internal class RangeConcatNode<T> : ConcatNode<T>, IRangeNode where T : IRange
    {
        public RangeConcatNode(ConcatNode<T> node) : base(node)
        {
        }
        public RangeConcatNode(Node<T> left, Node<T> right) : base(left, right)
        {
            var customNodeLeft = (IRangeNode)left;
            var customNodeRight = (IRangeNode)right;
            TotalSumCount = customNodeLeft.TotalSumCount + customNodeRight.TotalSumCount;
        }

        public int TotalSumCount { get; private set; }

        protected override Node<T> NewNodeInPlace(Node<T> newLeft, Node<T> newRight)
        {
            var customNodeLeft = (IRangeNode)newLeft;
            var customNodeRight = (IRangeNode)newRight;
            TotalSumCount = customNodeLeft.TotalSumCount + customNodeRight.TotalSumCount;
            return base.NewNodeInPlace(newLeft, newRight);
        }
    }

    internal class RangeLeafNode<T> : LeafNode<T>, IRangeNode where T: IRange
    {
        public RangeLeafNode() : base()
        {
            TotalSumCount = 0;
        }

        public RangeLeafNode(T item) : base(item)
        {
            TotalSumCount = item.Length;
        }

        public RangeLeafNode(int count, FixedList<T> items) : base(count, items)
        {
            NotifyUpdate();
        }

        public override void NotifyUpdate()
        {
            int index = 0, totalLength = 0;
            for(int i =0; i< items.Count; i++)
            {
                items[i].Index = index;
                var length = items[i].Length;
                totalLength += length;
                index += length;
            }
            TotalSumCount = totalLength;
        }

        public int TotalSumCount { get; private set; }
    }

    internal class RangeLeastFetch<T> : ILeastFetch<T> where T : IRange
    {
        public Node<T> Node { get; set; }

        public int TotalLeftCount { get; set; }

        public int absoluteIndex { get; set; }

        public RangeLeastFetch()
        {
        }
    }

    internal class RangeConverter<T> : ICustomConverter<T> where T : IRange, new()
    {
        public ILeastFetch<T> LeastFetch { get { return customLeastFetch; } }

        public RangeLeastFetch<T> customLeastFetch { get; set; }

        public T Convert(T item)
        {
            return item;
        }

        public T ConvertBack(T item)
        {
            var result = new T();
            result.Index = item.Index + customLeastFetch.absoluteIndex;
            result.Length = item.Length;
            return result;
        }

        public ConcatNode<T> CreateConcatNode(ConcatNode<T> node)
        {
            return new RangeConcatNode<T>(node);
        }

        public ConcatNode<T> CreateConcatNode(Node<T> left, Node<T> right)
        {
            return new RangeConcatNode<T>(left, right);
        }

        public LeafNode<T> CreateLeafNode()
        {
            return new RangeLeafNode<T>();
        }

        public LeafNode<T> CreateLeafNode(T item)
        {
            return new RangeLeafNode<T>(item);
        }

        public LeafNode<T> CreateLeafNode(int count, FixedList<T> items)
        {
            return new RangeLeafNode<T>(count, items);
        }

        public void SetState(Node<T> current, int totalLeftCountInList)
        {
            if (current == null)
            {
                this.customLeastFetch = new RangeLeastFetch<T>();
            }
            else
            {
                this.customLeastFetch.Node = current;
                this.customLeastFetch.TotalLeftCount = totalLeftCountInList;
            }
        }

        public void ResetState()
        {
            this.customLeastFetch = null;
        }
    }
}
