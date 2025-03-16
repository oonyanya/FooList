using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            RangeConverter<T> myCustomConverter = (RangeConverter<T>)CustomConverter;
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
                    myCustomConverter.customLeastFetch.absoluteIndexIntoRange += customNode.TotalRangeCount;
                    return NodeWalkDirection.Right;
                }
            });

            leafNode = (LeafNode<T>)CustomConverter.LeastFetch.Node;
            return CustomConverter.ConvertBack(leafNode.items[relativeIndex]);
        }

        public int GetIndexFromIndexIntoRange(int indexIntoRange)
        {
            RangeConverter<T> myCustomConverter = (RangeConverter<T>)CustomConverter;
            int relativeIndexIntoRange = indexIntoRange;

            var node = WalkNode((current, leftCount) => {
                var rangeLeftNode = (IRangeNode)current.Left;
                int leftTotalSumCount = rangeLeftNode.TotalRangeCount;

                if (relativeIndexIntoRange < leftTotalSumCount)
                {
                    return NodeWalkDirection.Left;
                }
                else
                {
                    relativeIndexIntoRange -= leftTotalSumCount;
                    myCustomConverter.customLeastFetch.TotalLeftCount += leftCount;
                    myCustomConverter.customLeastFetch.absoluteIndexIntoRange += leftTotalSumCount;
                    return NodeWalkDirection.Right;
                }
            });

            int relativeIndex, relativeNearIndex;
            var leafNode = (LeafNode<T>)node;
            relativeIndex = this.IndexOfNearest(leafNode.items, relativeIndexIntoRange, out relativeNearIndex);

            return relativeIndex + myCustomConverter.customLeastFetch.TotalLeftCount;
        }

        int IndexOfNearest(IList<T> collection,int start, out int nearIndex)
        {
            if (start < 0)
                throw new ArgumentOutOfRangeException("indexに負の値を設定することはできません");

            nearIndex = -1;
            if (collection.Count == 0)
                return -1;

            if (start == 0 && collection.Count > 0)
                return 0;

            T line;
            int lineHeadIndex;

            int left = 0, right = collection.Count - 1, mid;
            while (left <= right)
            {
                mid = (left + right) / 2;
                line = collection[mid];
                lineHeadIndex = line.Index;
                if (start >= lineHeadIndex && start < lineHeadIndex + line.Length)
                {
                    return mid;
                }
                if (start < lineHeadIndex)
                {
                    right = mid - 1;
                }
                else
                {
                    left = mid + 1;
                }
            }

            System.Diagnostics.Debug.Assert(left >= 0 || right >= 0);
            nearIndex = left >= 0 ? left : right;
            if (nearIndex > collection.Count - 1)
                nearIndex = right;

            return -1;
        }
    }

    internal interface IRangeNode
    {
        int TotalRangeCount { get; }
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
            TotalRangeCount = customNodeLeft.TotalRangeCount + customNodeRight.TotalRangeCount;
        }

        public int TotalRangeCount { get; private set; }

        protected override Node<T> NewNodeInPlace(Node<T> newLeft, Node<T> newRight)
        {
            var customNodeLeft = (IRangeNode)newLeft;
            var customNodeRight = (IRangeNode)newRight;
            TotalRangeCount = customNodeLeft.TotalRangeCount + customNodeRight.TotalRangeCount;
            return base.NewNodeInPlace(newLeft, newRight);
        }
    }

    internal class RangeLeafNode<T> : LeafNode<T>, IRangeNode where T: IRange
    {
        public RangeLeafNode() : base()
        {
            TotalRangeCount = 0;
        }

        public RangeLeafNode(T item) : base(item)
        {
            TotalRangeCount = item.Length;
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
            TotalRangeCount = totalLength;
        }

        public int TotalRangeCount { get; private set; }
    }

    internal class RangeLeastFetch<T> : ILeastFetch<T> where T : IRange
    {
        public Node<T> Node { get; set; }

        public int TotalLeftCount { get; set; }

        public int absoluteIndexIntoRange { get; set; }

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
            result.Index = item.Index + customLeastFetch.absoluteIndexIntoRange;
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
