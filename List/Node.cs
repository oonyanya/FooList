using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FooProject.Collection
{
    public abstract class Node<T>
    {
        public Node<T> Left {  get; set; }

        public Node<T> Right { get; set; }

        public int Depth {  get; set; }

        // TODO
        public int Count { get; set; }

        public Node() 
        {
            this.Left = null;
            this.Right = null;
        }
        public Node(Node<T> left, Node<T> right) : this()
        {
            this.Left = left;
            this.Right = right;
        }

        public bool IsBalanced()
        {
            return (Depth <= BigList<T>.MAXFIB && Count >= BigList<T>.FIBONACCI[Depth]);
        }

        public bool IsAlmostBalanced()
        {
            return (Depth == 0 || (Depth - 1 <= BigList<T>.MAXFIB && Count >= BigList<T>.FIBONACCI[Depth - 1]));
        }

        public abstract Node<T> PrependInPlace(Node<T> node, LeafNodeEnumrator<T> leafNodeEnumrator, LeafNodeEnumrator<T> nodeBelongLeafNodeEnumrator);

        public abstract Node<T> PrependInPlace(T item, LeafNodeEnumrator<T> leafNodeEnumrator);

        public abstract Node<T> AppendInPlace(T item, LeafNodeEnumrator<T> leafNodeEnumrator);

        public abstract Node<T> AppendInPlace(Node<T> node, LeafNodeEnumrator<T> leafNodeEnumrator, LeafNodeEnumrator<T> nodeBelongLeafNodeEnumrator);

        public abstract Node<T> RemoveRangeInPlace(int first, int last, LeafNodeEnumrator<T> leafNodeEnumrator);

        public abstract Node<T> InsertInPlace(int index, T item, LeafNodeEnumrator<T> leafNodeEnumrator);

        public abstract Node<T> InsertInPlace(int index, Node<T> node, LeafNodeEnumrator<T> leafNodeEnumrator, LeafNodeEnumrator<T> nodeBelongLeafNodeEnumrator);
    }
    public class LeafNode<T> : Node<T>
    {
        public FixedList<T> items;

        public LeafNode<T> Next { get; set; }

        public LeafNode<T> Previous { get; set; }

        public LeafNode() : base()
        {
            this.Previous = null;
            this.Next = null;
        }

        public LeafNode(T item) : this()
        {
            this.items = new FixedList<T>(BigList<T>.MAXLEAF);
            this.items.Add(item);
            Count = 1;
        }

        public LeafNode(int count, FixedList<T> items) : this()
        {
            this.items = items;
            Count = count;
        }

        public override Node<T> AppendInPlace(T item, LeafNodeEnumrator<T> leafNodeEnumrator)
        {
            if (Count < BigList<T>.MAXLEAF)
            {
                items.Insert(Count, item);
                Count += 1;
                return this;
            }
            else
            {
                var newLeafNode = new LeafNode<T>(item);
                leafNodeEnumrator.AddNext(this,newLeafNode);
                return new ConcatNode<T>(this, newLeafNode);
            }
        }
        private bool MergeBeforeLeafInPlace(Node<T> other)
        {
            LeafNode<T> otherLeaf = (other as LeafNode<T>);
            int newCount;
            if (otherLeaf != null && (newCount = otherLeaf.Count + this.Count) <= BigList<T>.MAXLEAF)
            {
                items.InsertRange(0, otherLeaf.items, otherLeaf.Count);
                Count = newCount;
                return true;
            }
            return false;
        }
        private bool MergeLeafInPlace(Node<T> other)
        {
            LeafNode<T> otherLeaf = (other as LeafNode<T>);
            int newCount;
            if (otherLeaf != null && (newCount = otherLeaf.Count + this.Count) <= BigList<T>.MAXLEAF)
            {
                items.AddRange(otherLeaf.items, otherLeaf.Count);
                Count = newCount;
                return true;
            }
            return false;
        }

        public override Node<T> PrependInPlace(Node<T> node, LeafNodeEnumrator<T> leafNodeEnumrator, LeafNodeEnumrator<T> nodeBelongLeafNodeEnumrator)
        {
            if (nodeBelongLeafNodeEnumrator != null)
            {
                if (MergeBeforeLeafInPlace(node))
                {
                    nodeBelongLeafNodeEnumrator.Remove((LeafNode<T>)node);
                    return this;
                }
            }

            if (leafNodeEnumrator != null)
            {
                if (nodeBelongLeafNodeEnumrator == null)
                {
                    leafNodeEnumrator.AddBefore(BigList<T>.GetMostLeftNode(this), (LeafNode<T>)node);
                }
                else
                {
                    leafNodeEnumrator.AddBefore(BigList<T>.GetMostLeftNode(this), nodeBelongLeafNodeEnumrator);
                }
            }
            return new ConcatNode<T>(node, this);

        }

        public override Node<T> AppendInPlace(Node<T> node, LeafNodeEnumrator<T> leafNodeEnumrator, LeafNodeEnumrator<T> nodeBelongLeafNodeEnumrator)
        {
            if (nodeBelongLeafNodeEnumrator != null)
            {
                if (MergeLeafInPlace(node))
                {
                    nodeBelongLeafNodeEnumrator.Remove((LeafNode<T>)node);
                    return this;
                }
            }

            /*
             * 移植元ではマージしていたが、テキストエディタで使うケースだとあまり意味がない
            ConcatNode<T> otherConcat = (node as ConcatNode<T>);
            if (otherConcat != null && MergeLeafInPlace(otherConcat.Left))
            {
                return new ConcatNode<T>(this, otherConcat.Right);
            }
             */
            if (leafNodeEnumrator != null && nodeBelongLeafNodeEnumrator != null)
            {
                leafNodeEnumrator.AddNext(BigList<T>.GetMostRightNode(this), nodeBelongLeafNodeEnumrator);
            }
            return new ConcatNode<T>(this, node);
        }

        public override Node<T> InsertInPlace(int index, T item, LeafNodeEnumrator<T> leafNodeEnumrator)
        {
            if (Count < BigList<T>.MAXLEAF)
            {
                items.Insert(index, item);
                Count += 1;
                return this;
            }
            else
            {
                if (index == Count)
                {
                    var newLeafNode = new LeafNode<T>(item);
                    leafNodeEnumrator.AddNext(this, newLeafNode);
                    // Inserting at count is just an appending operation.
                    return new ConcatNode<T>(this, newLeafNode);
                }
                else if (index == 0)
                {
                    var newLeafNode = new LeafNode<T>(item);
                    leafNodeEnumrator.AddBefore(this, newLeafNode);
                    // Inserting at 0 is just a prepending operation.
                    return new ConcatNode<T>(new LeafNode<T>(item), this);
                }
                else
                {
                    // Split into two nodes, and put the new item at the end of the first.
                    int leftItemCount = index + 1;
                    int splitLength = index;
                    FixedList<T> leftItems = new FixedList<T>(leftItemCount, BigList<T>.MAXLEAF);
                    leftItems.AddRange(items.GetRange(0, splitLength),splitLength);
                    leftItems.Add(item);
                    LeafNode<T> leftNode = new LeafNode<T>(index + 1, leftItems);
                    leafNodeEnumrator.Replace(this, leftNode);

                    int rightItemCount = items.Count - index;
                    FixedList<T> rightItems = new FixedList<T>(rightItemCount,BigList<T>.MAXLEAF);
                    rightItems.AddRange(items.GetRange(splitLength, rightItemCount), rightItemCount);
                    LeafNode<T> rightNode = new LeafNode<T>(Count - index, rightItems);
                    leafNodeEnumrator.AddNext(leftNode, rightNode);

                    return new ConcatNode<T>(leftNode, rightNode);
                }
            }
        }

        public override Node<T> InsertInPlace(int index, Node<T> node, LeafNodeEnumrator<T> leafNodeEnumrator, LeafNodeEnumrator<T> nodeBelongLeafNodeEnumrator)
        {
            LeafNode<T> otherLeaf = (node as LeafNode<T>);
            int newCount;
            if (otherLeaf != null && (newCount = otherLeaf.Count + this.Count) <= BigList<T>.MAXLEAF)
            {
                nodeBelongLeafNodeEnumrator.Remove(otherLeaf);
                // Combine the two leaf nodes into one.
                items.InsertRange(index, otherLeaf.items);
                Count = newCount;
                return this;
            }
            else if (index == 0)
            {
                // Inserting at 0 is a prepend.
                return PrependInPlace(node,leafNodeEnumrator, nodeBelongLeafNodeEnumrator);
            }
            else if (index == Count)
            {
                // Inserting at count is an append.
                return AppendInPlace(node, leafNodeEnumrator, nodeBelongLeafNodeEnumrator);
            }
            else
            {
                // Split existing node into two nodes at the insertion point, then concat all three nodes together.

                int leftItemCount = index;
                int splitLength = index;
                FixedList<T> leftItems = new FixedList<T>(leftItemCount,BigList<T>.MAXLEAF);
                leftItems.AddRange(items.GetRange(0, splitLength),splitLength);
                Node<T> leftNode = new LeafNode<T>(index, leftItems);
                leafNodeEnumrator.Replace(this, (LeafNode<T>)leftNode);

                int rightItemCount = items.Count - index;
                FixedList<T> rightItems = new FixedList<T>(rightItemCount, BigList<T>.MAXLEAF);
                rightItems.AddRange(items.GetRange(splitLength, rightItemCount), rightItemCount);
                Node<T> rightNode = new LeafNode<T>(Count - index, rightItems);

                leftNode = leftNode.AppendInPlace(node, leafNodeEnumrator, nodeBelongLeafNodeEnumrator);

                leafNodeEnumrator.AddNext(BigList<T>.GetMostRightNode(leftNode), (LeafNode<T>)rightNode);
                leftNode = leftNode.AppendInPlace(rightNode, null, null);
                return leftNode;
            }
        }

        public override Node<T> PrependInPlace(T item, LeafNodeEnumrator<T> leafNodeEnumrator)
        {
            // Add into the current leaf, if possible.
            if (Count < BigList<T>.MAXLEAF)
            {
                items.Insert(0, item);
                Count += 1;

                return this;
            }
            else
            {
                var newLeafNode = new LeafNode<T>(item);
                leafNodeEnumrator.AddBefore(this, newLeafNode);
                return new ConcatNode<T>(newLeafNode, this);
            }
        }

        public override Node<T> RemoveRangeInPlace(int first, int last, LeafNodeEnumrator<T> leafNodeEnumrator)
        {
            Debug.Assert(first <= last);
            Debug.Assert(last >= 0);

            if (first <= 0 && last >= Count - 1)
            {
                leafNodeEnumrator.Remove(this);
                return null;     // removing entire node.
            }

            if (first < 0)
                first = 0;
            if (last >= Count)
                last = Count - 1;
            int newCount = first + (Count - last - 1);      // number of items remaining.
            items.RemoveRange(first, last - first + 1);
            Count = newCount;
            return this;
        }

    }

    public class ConcatNode<T> : Node<T>
    {
        public ConcatNode(ConcatNode<T> node) : base() 
        {
            this.Left = node.Left;
            this.Right = node.Right;
            this.Count = node.Count;
            this.Depth = node.Depth;
        }

        public ConcatNode(Node<T> left, Node<T> right) : base(left, right)
        {
            Debug.Assert(left != null && right != null);
            this.Left = left;
            this.Right = right;
            this.Count = left.Count + right.Count;
            if (left.Depth > right.Depth)
                this.Depth = (short)(left.Depth + 1);
            else
                this.Depth = (short)(right.Depth + 1);
        }

        private Node<T> NewNodeInPlace(Node<T> newLeft, Node<T> newRight)
        {
            if (newLeft == null)
                return newRight;
            else if (newRight == null)
                return newLeft;

            Left = newLeft;
            Right = newRight;
            Count = Left.Count + Right.Count;
            if (Left.Depth > Right.Depth)
                Depth = (short)(Left.Depth + 1);
            else
                Depth = (short)(Right.Depth + 1);
            return this;
        }

        public override Node<T> PrependInPlace(Node<T> node, LeafNodeEnumrator<T> leafNodeEnumrator, LeafNodeEnumrator<T> nodeBelongLeafNodeEnumrator)
        {
            if (Left.Count + node.Count <= BigList<T>.MAXLEAF && Left is LeafNode<T> && node is LeafNode<T>)
                return NewNodeInPlace(Left.PrependInPlace(node, leafNodeEnumrator, nodeBelongLeafNodeEnumrator), Right);
            if (leafNodeEnumrator != null)
            {
                var rightLeafNode = node as LeafNode<T>;
                if (nodeBelongLeafNodeEnumrator != null)
                    leafNodeEnumrator.AddBefore(BigList<T>.GetMostLeftNode(this), nodeBelongLeafNodeEnumrator);
                else if (rightLeafNode != null)
                    leafNodeEnumrator.AddBefore(BigList<T>.GetMostLeftNode(this), rightLeafNode);
            }
            return new ConcatNode<T>(node, this);
        }

        public override Node<T> AppendInPlace(Node<T> node, LeafNodeEnumrator<T> leafNodeEnumrator, LeafNodeEnumrator<T> nodeBelongLeafNodeEnumrator)
        {
            if (Right.Count + node.Count <= BigList<T>.MAXLEAF && Right is LeafNode<T> && node is LeafNode<T>)
                return NewNodeInPlace(Left, Right.AppendInPlace(node, leafNodeEnumrator, nodeBelongLeafNodeEnumrator));
            if (leafNodeEnumrator != null)
            {
                var rightLeafNode = node as LeafNode<T>;
                if (nodeBelongLeafNodeEnumrator != null)
                    leafNodeEnumrator.AddNext(BigList<T>.GetMostRightNode(this), nodeBelongLeafNodeEnumrator);
                else if(rightLeafNode != null)
                    leafNodeEnumrator.AddNext(BigList<T>.GetMostRightNode(this), rightLeafNode);
            }
            return new ConcatNode<T>(this, node);
        }

        public override Node<T> AppendInPlace(T item, LeafNodeEnumrator<T> leafNodeEnumrator)
        {
            LeafNode<T> rightLeaf;
            if (Right.Count < BigList<T>.MAXLEAF && (rightLeaf = Right as LeafNode<T>) != null)
            {
                rightLeaf.items.Add(item);
                rightLeaf.Count += 1;
                this.Count += 1;
                return this;
            }
            else
            {
                var newLeafNode = new LeafNode<T>(item);
                leafNodeEnumrator.AddNext(BigList<T>.GetMostRightNode(this), newLeafNode);
                return new ConcatNode<T>(this, newLeafNode);
            }
        }

        public override Node<T> InsertInPlace(int index, T item, LeafNodeEnumrator<T> leafNodeEnumrator)
        {
            int leftCount = Left.Count;
            if (index <= leftCount)
                return NewNodeInPlace(Left.InsertInPlace(index, item, leafNodeEnumrator), Right);
            else
                return NewNodeInPlace(Left, Right.InsertInPlace(index - leftCount, item, leafNodeEnumrator));
        }

        public override Node<T> InsertInPlace(int index, Node<T> node, LeafNodeEnumrator<T> leafNodeEnumrator, LeafNodeEnumrator<T> nodeBelongLeafNodeEnumrator)
        {
            int leftCount = Left.Count;
            if (index < leftCount)
                return NewNodeInPlace(Left.InsertInPlace(index, node, leafNodeEnumrator,nodeBelongLeafNodeEnumrator), Right);
            else
                return NewNodeInPlace(Left, Right.InsertInPlace(index - leftCount, node, leafNodeEnumrator, nodeBelongLeafNodeEnumrator));
        }

        public override Node<T> PrependInPlace(T item, LeafNodeEnumrator<T> leafNodeEnumrator)
        {
            LeafNode<T> leftLeaf;
            if (Left.Count < BigList<T>.MAXLEAF && (leftLeaf = Left as LeafNode<T>) != null)
            {
                // Prepend the item to the left leaf. This keeps repeated prepends from creating
                // single item nodes.
                leftLeaf.items.Insert(0, item);
                leftLeaf.Count += 1;
                this.Count += 1;
                return this;
            }
            else
            {
                var newLeaf = new LeafNode<T>(item);
                leafNodeEnumrator.AddBefore(BigList<T>.GetMostLeftNode(this), newLeaf);
                return new ConcatNode<T>(newLeaf, this);
            }
        }

        public override Node<T> RemoveRangeInPlace(int first, int last, LeafNodeEnumrator<T> leafNodeEnumrator)
        {
            Debug.Assert(first < Count);
            Debug.Assert(last >= 0);

            /*
             * TODO：まとめで削除できるケースがあるが、リンクドリストから削除するのが面倒なので再帰呼び出しで消す
            if (first <= 0 && last >= Count - 1)
            {
                return null;
            }
            */

            int leftCount = Left.Count;
            Node<T> newLeft = Left, newRight = Right;

            // Is part of the left being removed?
            if (first < leftCount)
                newLeft = Left.RemoveRangeInPlace(first, last, leafNodeEnumrator);
            // Is part of the right being remove?
            if (last >= leftCount)
                newRight = Right.RemoveRangeInPlace(first - leftCount, last - leftCount, leafNodeEnumrator);

            return NewNodeInPlace(newLeft, newRight);
        }

    }
}
