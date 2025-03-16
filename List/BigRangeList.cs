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
            this.CustomConverter = new CustomConverter<T>();
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

            CustomConverter<T> myCustomConverter = (CustomConverter<T>)CustomConverter;
            relativeIndex = index;
            var node = WalkNode((current, leftCount) => {
                if (relativeIndex < leftCount)
                {
                    return NodeWalkDirection.Left;
                }
                else
                {
                    relativeIndex -= leftCount;
                    var customNode = (ICustomeNode)current.Left;
                    myCustomConverter.customLeastFetch.absoluteIndex += customNode.TotalSumCount;
                    return NodeWalkDirection.Right;
                }
            });

            leafNode = (LeafNode<T>)CustomConverter.LeastFetch.Node;
            return CustomConverter.ConvertBack(leafNode.items[relativeIndex]);
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

    internal class CustomLeastFetch<T> : ILeastFetch<T> where T : IRange
    {
        public Node<T> Node { get; set; }

        public int TotalLeftCount { get; set; }

        public int absoluteIndex { get; set; }

        public CustomLeastFetch()
        {
        }
    }

    internal class CustomConverter<T> : ICustomConverter<T> where T : IRange, new()
    {
        public ILeastFetch<T> LeastFetch { get { return customLeastFetch; } }

        public CustomLeastFetch<T> customLeastFetch { get; set; }

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

        public void SetState(Node<T> current, int totalLeftCountInList)
        {
            if (current == null)
            {
                this.customLeastFetch = new CustomLeastFetch<T>();
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
