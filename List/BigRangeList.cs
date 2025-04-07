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
        long start { get; set; }
        long length { get; set; }

        IRange DeepCopy();
    }
    public class BigRangeList<T> : BigList<T> where T : IRange
    {
        public BigRangeList() :base()
        {
            var custom = new RangeConverter<T>();
            this.CustomConverter = custom;
            this.CustomBuilder = custom;
        }

        public override T Get(long index)
        {
            var result = GetRawData(index);
            return result;
        }

        public override void Set(long index, T value)
        {
            var args = new BigListArgs<T>(CustomBuilder, CustomConverter);
            Root.SetAtInPlace(index, value, args);
        }

        public T GetIndexIntoRange(long index)
        {
            return CustomConverter.ConvertBack(GetRawData(index));
        }

        public T GetRawData(long index)
        {
            RangeConverter<T> myCustomConverter = (RangeConverter<T>)CustomConverter;
            long relativeIndex;
            LeafNode<T> leafNode;
            if (CustomConverter.LeastFetch != null)
            {
                relativeIndex = index - CustomConverter.LeastFetch.TotalLeftCount;
                if (relativeIndex >= 0 && relativeIndex < CustomConverter.LeastFetch.Node.Count)
                {
                    leafNode = (LeafNode<T>)CustomConverter.LeastFetch.Node;
                    return leafNode.items[(int)relativeIndex];
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
            return leafNode.items[(int)relativeIndex];
        }

        public long GetIndexFromIndexIntoRange(long indexIntoRange)
        {
            RangeConverter<T> myCustomConverter = (RangeConverter<T>)CustomConverter;
            long relativeIndexIntoRange = indexIntoRange;

            var node = WalkNode((current, leftCount) => {
                var rangeLeftNode = (IRangeNode)current.Left;
                long leftTotalSumCount = rangeLeftNode.TotalRangeCount;

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

            long relativeIndex, relativeNearIndex;
            var leafNode = (LeafNode<T>)node;
            relativeIndex = this.IndexOfNearest(leafNode.items, relativeIndexIntoRange, out relativeNearIndex);

            if (relativeIndex == -1)
            {
                myCustomConverter.ResetState();
                return -1;
            }
            return relativeIndex + myCustomConverter.customLeastFetch.TotalLeftCount;
        }

        int IndexOfNearest(IList<T> collection,long start, out long nearIndex)
        {
            if (start < 0)
                throw new ArgumentOutOfRangeException("indexに負の値を設定することはできません");

            nearIndex = -1;
            if (collection.Count == 0)
                return -1;

            if (start == 0 && collection.Count > 0)
                return 0;

            T line;
            long lineHeadIndex;

            int left = 0, right = collection.Count - 1, mid;
            while (left <= right)
            {
                mid = (left + right) / 2;
                line = collection[mid];
                lineHeadIndex = line.start;
                if (start >= lineHeadIndex && start < lineHeadIndex + line.length)
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

        public override IEnumerator<T> GetEnumerator()
        {
            for(int i = 0; i < this.Count; i++)
            {
                yield return GetIndexIntoRange(i);
            }
        }
    }

    internal interface IRangeNode
    {
        long TotalRangeCount { get; }
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

        public long TotalRangeCount { get; private set; }

        protected override Node<T> NewNodeInPlace(Node<T> newLeft, Node<T> newRight)
        {
            var customNodeLeft = (IRangeNode)newLeft;
            var customNodeRight = (IRangeNode)newRight;
            if (customNodeLeft != null)
                TotalRangeCount = customNodeLeft.TotalRangeCount;
            if (customNodeRight != null)
                TotalRangeCount += customNodeRight.TotalRangeCount;
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
            TotalRangeCount = item.length;
        }

        public RangeLeafNode(long count, FixedList<T> items) : base(count, items)
        {
        }

        public override void NotifyUpdate(long startIndex, long count, BigListArgs<T> args)
        {
            var fixedRangeList = (FixedRangeList<T>)this.items;
            TotalRangeCount = fixedRangeList.TotalCount;
        }

        public long TotalRangeCount { get; private set; }
    }

    internal class RangeLeastFetch<T> : ILeastFetch<T> where T : IRange
    {
        public Node<T> Node { get; set; }

        public long TotalLeftCount { get; set; }

        public long absoluteIndexIntoRange { get; set; }

        public RangeLeastFetch()
        {
        }
    }

    internal class RangeConverter<T> : ICustomConverter<T>,ICustomBuilder<T> where T : IRange
    {
        public ILeastFetch<T> LeastFetch { get { return customLeastFetch; } }

        public RangeLeastFetch<T> customLeastFetch { get; set; }

        public T Convert(T item)
        {
            var result = item;
            result.start -= customLeastFetch.absoluteIndexIntoRange;
            return result;
        }

        public T ConvertBack(T item)
        {
            T result = (T)item.DeepCopy();
            result.start = item.start + customLeastFetch.absoluteIndexIntoRange;
            result.length = item.length;
            return result;
        }

        public FixedList<T> CreateList(long init, long max)
        {
            return new FixedRangeList<T>((int)init, (int)max);
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
            var newLeafNode = new RangeLeafNode<T>();
            newLeafNode.items = this.CreateList(4, BigList<T>.MAXLEAF);
            return newLeafNode;
        }

        public LeafNode<T> CreateLeafNode(T item)
        {
            var list = this.CreateList(4, BigList<T>.MAXLEAF);
            list.Add(item);
            return new RangeLeafNode<T>(list.Count, list);
        }

        public LeafNode<T> CreateLeafNode(long count, FixedList<T> items)
        {
            return new RangeLeafNode<T>(count, items);
        }

        public void SetState(Node<T> current, long totalLeftCountInList)
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
