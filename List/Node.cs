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

        public Node<T> Next { get; set; }

        public Node<T> Previous {  get; set; }

        public int Depth {  get; set; }

        // TODO
        public int Count { get; set; }

        public Node() 
        {
            this.Left = null;
            this.Right = null;
            this.Previous = null;
            this.Next = null;
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

        public abstract T GetAt(int index, LeafNodeEnumrator<T> leafNodeEnumrator);

        public abstract Node<T> SetAtInPlace(int index, T item, LeafNodeEnumrator<T> leafNodeEnumrator);

        public abstract Node<T> Subrange(int first, int last, LeafNodeEnumrator<T> leafNodeEnumrator);

        public Node<T> PrependInPlace(Node<T> node,LeafNodeEnumrator<T> leafNodeEnumrator)
        {
            return node.AppendInPlace(this,leafNodeEnumrator);
        }
        public abstract Node<T> PrependInPlace(T item, LeafNodeEnumrator<T> leafNodeEnumrator);

        public abstract Node<T> AppendInPlace(T item, LeafNodeEnumrator<T> leafNodeEnumrator);

        public abstract Node<T> AppendInPlace(Node<T> node, LeafNodeEnumrator<T> leafNodeEnumrator);

        public abstract Node<T> RemoveRangeInPlace(int first, int last, LeafNodeEnumrator<T> leafNodeEnumrator);

        public abstract Node<T> InsertInPlace(int index, T item, LeafNodeEnumrator<T> leafNodeEnumrator);

        public abstract Node<T> InsertInPlace(int index, Node<T> node, LeafNodeEnumrator<T> leafNodeEnumrator);
    }
    public class LeafNode<T> : Node<T>
    {
        public FixedList<T> items;

        public LeafNode(T item) : base()
        {
            this.items = new FixedList<T>(BigList<T>.MAXLEAF);
            this.items.Add(item);
            Count = 1;
        }

        public LeafNode(int count, FixedList<T> items)
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
                return new ConcatNode<T>(this, new LeafNode<T>(item));
            }
        }
        public override Node<T> AppendInPlace(Node<T> node, LeafNodeEnumrator<T> leafNodeEnumrator)
        {
            return new ConcatNode<T>(this, node);
        }

        public override T GetAt(int index, LeafNodeEnumrator<T> leafNodeEnumrator)
        {
            return items[index];
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
                    // Inserting at count is just an appending operation.
                    return new ConcatNode<T>(this, new LeafNode<T>(item));
                }
                else if (index == 0)
                {
                    // Inserting at 0 is just a prepending operation.
                    return new ConcatNode<T>(new LeafNode<T>(item), this);
                }
                else
                {
                    // Split into two nodes, and put the new item at the end of the first.
                    FixedList<T> leftItems = new FixedList<T>(BigList<T>.MAXLEAF);
                    leftItems.AddRange(items.Take(index));
                    leftItems.Add(item);
                    Node<T> leftNode = new LeafNode<T>(index + 1, leftItems);

                    FixedList<T> rightItems = new FixedList<T>(BigList<T>.MAXLEAF);
                    rightItems.AddRange(items.Skip(index));
                    Node<T> rightNode = new LeafNode<T>(Count - index, rightItems);

                    return new ConcatNode<T>(leftNode, rightNode);
                }
            }
        }

        public override Node<T> InsertInPlace(int index, Node<T> node, LeafNodeEnumrator<T> leafNodeEnumrator)
        {
            LeafNode<T> otherLeaf = (node as LeafNode<T>);
            int newCount;
            if (otherLeaf != null && (newCount = otherLeaf.Count + this.Count) <= BigList<T>.MAXLEAF)
            {
                // Combine the two leaf nodes into one.
                items.InsertRange(index, otherLeaf.items);
                Count = newCount;
                return this;
            }
            else if (index == 0)
            {
                // Inserting at 0 is a prepend.
                return PrependInPlace(node,leafNodeEnumrator);
            }
            else if (index == Count)
            {
                // Inserting at count is an append.
                return AppendInPlace(node, leafNodeEnumrator);
            }
            else
            {
                // Split existing node into two nodes at the insertion point, then concat all three nodes together.

                FixedList<T> leftItems = new FixedList<T>(BigList<T>.MAXLEAF);
                leftItems.AddRange(items.Take(index));
                Node<T> leftNode = new LeafNode<T>(index, leftItems);

                FixedList<T> rightItems = new FixedList<T>(BigList<T>.MAXLEAF);
                rightItems.AddRange(items.Skip(index));
                Node<T> rightNode = new LeafNode<T>(Count - index, rightItems);

                leftNode = leftNode.AppendInPlace(node, leafNodeEnumrator);
                leftNode = leftNode.AppendInPlace(rightNode, leafNodeEnumrator);
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
                return new ConcatNode<T>(new LeafNode<T>(item), this);
            }
        }

        public override Node<T> RemoveRangeInPlace(int first, int last, LeafNodeEnumrator<T> leafNodeEnumrator)
        {
            Debug.Assert(first <= last);
            Debug.Assert(last >= 0);

            if (first <= 0 && last >= Count - 1)
            {
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

        public override Node<T> SetAtInPlace(int index, T item, LeafNodeEnumrator<T> leafNodeEnumrator)
        {
            items[index] = item;
            return this;
        }

        public override Node<T> Subrange(int first, int last, LeafNodeEnumrator<T> leafNodeEnumrator)
        {
            Debug.Assert(first <= last);
            Debug.Assert(last >= 0);
            if (first < 0)
                first = 0;
            if (last >= Count)
                last = Count - 1;
            int n = last - first + 1;
            FixedList<T> newItems = new FixedList<T>(BigList<T>.MAXLEAF);
            newItems.AddRange(items.Skip(first).Take(n));
            return new LeafNode<T>(n, newItems);
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

        public override Node<T> AppendInPlace(Node<T> node, LeafNodeEnumrator<T> leafNodeEnumrator)
        {
            if (Right.Count + node.Count <= BigList<T>.MAXLEAF && Right is LeafNode<T> && node is LeafNode<T>)
                return NewNodeInPlace(Left, Right.AppendInPlace(node, leafNodeEnumrator));
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
                return new ConcatNode<T>(this, new LeafNode<T>(item));
        }

        public override T GetAt(int index, LeafNodeEnumrator<T> leafNodeEnumrator)
        {
            int leftCount = Left.Count;
            if (index < leftCount)
                return Left.GetAt(index, leafNodeEnumrator);
            else
                return Right.GetAt(index - leftCount, leafNodeEnumrator);
        }

        public override Node<T> InsertInPlace(int index, T item, LeafNodeEnumrator<T> leafNodeEnumrator)
        {
            int leftCount = Left.Count;
            if (index <= leftCount)
                return NewNodeInPlace(Left.InsertInPlace(index, item, leafNodeEnumrator), Right);
            else
                return NewNodeInPlace(Left, Right.InsertInPlace(index - leftCount, item, leafNodeEnumrator));
        }

        public override Node<T> InsertInPlace(int index, Node<T> node, LeafNodeEnumrator<T> leafNodeEnumrator)
        {
            int leftCount = Left.Count;
            if (index < leftCount)
                return NewNodeInPlace(Left.InsertInPlace(index, node, leafNodeEnumrator), Right);
            else
                return NewNodeInPlace(Left, Right.InsertInPlace(index - leftCount, node, leafNodeEnumrator));
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
                return new ConcatNode<T>(newLeaf, this);
            }
        }

        public override Node<T> RemoveRangeInPlace(int first, int last, LeafNodeEnumrator<T> leafNodeEnumrator)
        {
            Debug.Assert(first < Count);
            Debug.Assert(last >= 0);

            if (first <= 0 && last >= Count - 1)
            {
                return null;
            }

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

        public override Node<T> SetAtInPlace(int index, T item, LeafNodeEnumrator<T> leafNodeEnumrator)
        {
            int leftCount = Left.Count;

            if (index < leftCount)
            {
                return Left.SetAtInPlace(index, item, leafNodeEnumrator);
            }
            else
            {
                return Right.SetAtInPlace(index - leftCount, item, leafNodeEnumrator);
            }
        }

        public override Node<T> Subrange(int first, int last, LeafNodeEnumrator<T> leafNodeEnumrator)
        {
            Debug.Assert(first < Count);
            Debug.Assert(last >= 0);

            if (first <= 0 && last >= Count - 1)
            {
                return new ConcatNode<T>(this);
            }

            int leftCount = Left.Count;
            Node<T> leftPart = null, rightPart = null;

            // Is part of the left included?
            if (first < leftCount)
                leftPart = Left.Subrange(first, last, leafNodeEnumrator);
            // Is part of the right included?
            if (last >= leftCount)
                rightPart = Right.Subrange(first - leftCount, last - leftCount, leafNodeEnumrator);

            Debug.Assert(leftPart != null || rightPart != null);

            // Combine the left parts and the right parts.
            if (leftPart == null)
                return rightPart;
            else if (rightPart == null)
                return leftPart;
            else
                return new ConcatNode<T>(leftPart, rightPart);
        }
    }
}
