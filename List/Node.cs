/*
 *  Copy from
 *  https://github.com/timdetering/Wintellect.PowerCollections
 *  Fooproject modify
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public long Depth {  get; set; }

        public long NodeCount { get; set; }

        // TODO
        public long Count { get; set; }

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
            return (Depth <= BigList<T>.MAXFIB && NodeCount >= BigList<T>.FIBONACCI[Depth]);
        }

        public bool IsAlmostBalanced()
        {
            return (Depth == 0 || (Depth - 1 <= BigList<T>.MAXFIB && NodeCount >= BigList<T>.FIBONACCI[Depth - 1]));
        }

        public abstract Node<T> SetAtInPlace(long index, T item,BigListArgs<T> args);

        public abstract Node<T> PrependInPlace(Node<T> node, LeafNodeEnumrator<T> leafNodeEnumrator, LeafNodeEnumrator<T> nodeBelongLeafNodeEnumrator,BigListArgs<T> args);

        public abstract Node<T> PrependInPlace(T item, LeafNodeEnumrator<T> leafNodeEnumrator,BigListArgs<T> args);

        public abstract Node<T> AppendInPlace(T item, LeafNodeEnumrator<T> leafNodeEnumrator,BigListArgs<T> args);

        public abstract Node<T> AppendInPlace(Node<T> node, LeafNodeEnumrator<T> leafNodeEnumrator, LeafNodeEnumrator<T> nodeBelongLeafNodeEnumrator,BigListArgs<T> args);

        public abstract Node<T> RemoveRangeInPlace(long first, long last, LeafNodeEnumrator<T> leafNodeEnumrator,BigListArgs<T> args);

        public abstract Node<T> InsertInPlace(long index, T item, LeafNodeEnumrator<T> leafNodeEnumrator,BigListArgs<T> args);

        public abstract Node<T> InsertInPlace(long index, Node<T> node, LeafNodeEnumrator<T> leafNodeEnumrator, LeafNodeEnumrator<T> nodeBelongLeafNodeEnumrator,BigListArgs<T> args);
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
            this.NodeCount = 1;
        }

        [Obsolete]
        public LeafNode(T item) : this()
        {
            this.items = new FixedList<T>(BigList<T>.MAXLEAF);
            this.items.Add(item);
            Count = 1;
        }

        public LeafNode(long count, FixedList<T> items) : this()
        {
            this.items = items;
            Count = count;
        }

        public override Node<T> SetAtInPlace(long index, T item,BigListArgs<T> args)
        {
            items[(int)index] = item;
            NotifyUpdate(index, 1, args);
            return this;
        }

        public override Node<T> AppendInPlace(T item, LeafNodeEnumrator<T> leafNodeEnumrator,BigListArgs<T> args)
        {
            if (Count < BigList<T>.MAXLEAF)
            {
                checked
                {
                    items.Insert((int)Count, item);
                }
                NotifyUpdate(Count, 1, args);
                Count += 1;
                return this;
            }
            else
            {
                var newLeafNode = args.CustomBuilder.CreateLeafNode(item);
                newLeafNode.NotifyUpdate(0, 1, args);
                leafNodeEnumrator.AddNext(this,newLeafNode);
                return args.CustomBuilder.CreateConcatNode(this, newLeafNode);
            }
        }
        private bool MergeBeforeLeafInPlace(Node<T> other, BigListArgs<T> args)
        {
            LeafNode<T> otherLeaf = (other as LeafNode<T>);
            long newCount;
            if (otherLeaf != null && (newCount = otherLeaf.Count + this.Count) <= BigList<T>.MAXLEAF)
            {
                checked
                {
                    items.InsertRange(0, otherLeaf.items, (int)otherLeaf.Count);
                }
                NotifyUpdate(0, otherLeaf.Count, args);
                Count = newCount;
                return true;
            }
            return false;
        }
        private bool MergeLeafInPlace(Node<T> other,BigListArgs<T> args)
        {
            LeafNode<T> otherLeaf = (other as LeafNode<T>);
            long newCount;
            if (otherLeaf != null && (newCount = otherLeaf.Count + this.Count) <= BigList<T>.MAXLEAF)
            {
                checked
                {
                    items.AddRange(otherLeaf.items, (int)otherLeaf.Count);
                }
                NotifyUpdate(items.Count, otherLeaf.Count, args);
                Count = newCount;
                return true;
            }
            return false;
        }

        // lengthがマイナスな場合削除されることを表す
        public virtual void NotifyUpdate(long index, long length, BigListArgs<T> args)
        {
        }

        public override Node<T> PrependInPlace(Node<T> node, LeafNodeEnumrator<T> leafNodeEnumrator, LeafNodeEnumrator<T> nodeBelongLeafNodeEnumrator,BigListArgs<T> args)
        {
            if (nodeBelongLeafNodeEnumrator != null)
            {
                if (MergeBeforeLeafInPlace(node,args))
                {
                    nodeBelongLeafNodeEnumrator.Remove((LeafNode<T>)node);
                    return this;
                }
            }

            if (leafNodeEnumrator != null)
            {
                if (nodeBelongLeafNodeEnumrator == null)
                {
                    LeafNode<T> leafNode = (LeafNode<T>)node;
                    leafNodeEnumrator.AddBefore(BigList<T>.GetMostLeftNode(this), leafNode);
                }
                else
                {
                    leafNodeEnumrator.AddBefore(BigList<T>.GetMostLeftNode(this), nodeBelongLeafNodeEnumrator);
                }
            }
            return args.CustomBuilder.CreateConcatNode(node, this);

        }

        public override Node<T> AppendInPlace(Node<T> node, LeafNodeEnumrator<T> leafNodeEnumrator, LeafNodeEnumrator<T> nodeBelongLeafNodeEnumrator,BigListArgs<T> args)
        {
            if (nodeBelongLeafNodeEnumrator != null)
            {
                if (MergeLeafInPlace(node, args))
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
                return customConverter.CreateConcatNode(this, otherConcat.Right);
            }
             */
if (leafNodeEnumrator != null && nodeBelongLeafNodeEnumrator != null)
            {
                leafNodeEnumrator.AddNext(BigList<T>.GetMostRightNode(this), nodeBelongLeafNodeEnumrator);
            }
            return args.CustomBuilder.CreateConcatNode(this, node);
        }

        public override Node<T> InsertInPlace(long index, T item, LeafNodeEnumrator<T> leafNodeEnumrator,BigListArgs<T> args)
        {
            if (Count < BigList<T>.MAXLEAF)
            {
                items.Insert((int)index, item);
                NotifyUpdate(index, 1, args);
                Count += 1;
                return this;
            }
            else
            {
                if (index == Count)
                {
                    var newLeafNode = args.CustomBuilder.CreateLeafNode(item);
                    newLeafNode.NotifyUpdate(0, 1, args);
                    leafNodeEnumrator.AddNext(this, newLeafNode);
                    // Inserting at count is just an appending operation.
                    return args.CustomBuilder.CreateConcatNode(this, newLeafNode);
                }
                else if (index == 0)
                {
                    var newLeafNode = args.CustomBuilder.CreateLeafNode(item);
                    newLeafNode.NotifyUpdate(0, 1, args);
                    leafNodeEnumrator.AddBefore(this, newLeafNode);
                    // Inserting at 0 is just a prepending operation.
                    return args.CustomBuilder.CreateConcatNode(newLeafNode, this);
                }
                else
                {
                    // Split into two nodes, and put the new item at the end of the first.
                    int leftItemCount;
                    int splitLength;
                    checked
                    {
                        leftItemCount = (int)index + 1;
                        splitLength = (int)index;
                    }
                    FixedList<T> leftItems = args.CustomBuilder.CreateList(leftItemCount, BigList<T>.MAXLEAF);
                    leftItems.AddRange(items.GetRange(0, splitLength),splitLength);
                    leftItems.Add(item);
                    LeafNode<T> leftNode = args.CustomBuilder.CreateLeafNode(index + 1, leftItems);
                    leftNode.NotifyUpdate(0, leftItems.Count, args);
                    leafNodeEnumrator.Replace(this, leftNode);

                    int rightItemCount = items.Count - (int)index;
                    FixedList<T> rightItems = args.CustomBuilder.CreateList(rightItemCount,BigList<T>.MAXLEAF);
                    rightItems.AddRange(items.GetRange(splitLength, rightItemCount), rightItemCount);
                    LeafNode<T> rightNode = args.CustomBuilder.CreateLeafNode(Count - index, rightItems);
                    rightNode.NotifyUpdate(0, rightItems.Count, args);
                    leafNodeEnumrator.AddNext(leftNode, rightNode);

                    return args.CustomBuilder.CreateConcatNode(leftNode, rightNode);
                }
            }
        }

        public override Node<T> InsertInPlace(long index, Node<T> node, LeafNodeEnumrator<T> leafNodeEnumrator, LeafNodeEnumrator<T> nodeBelongLeafNodeEnumrator,BigListArgs<T> args)
        {
            LeafNode<T> otherLeaf = (node as LeafNode<T>);
            long newCount;
            if (otherLeaf != null && (newCount = otherLeaf.Count + this.Count) <= BigList<T>.MAXLEAF)
            {
                nodeBelongLeafNodeEnumrator.Remove(otherLeaf);
                // Combine the two leaf nodes into one.
                items.InsertRange((int)index, otherLeaf.items);
                NotifyUpdate(index, otherLeaf.Count, args);
                Count = newCount;
                return this;
            }
            else if (index == 0)
            {
                // Inserting at 0 is a prepend.
                return PrependInPlace(node,leafNodeEnumrator, nodeBelongLeafNodeEnumrator,args);
            }
            else if (index == Count)
            {
                // Inserting at count is an append.
                return AppendInPlace(node, leafNodeEnumrator, nodeBelongLeafNodeEnumrator, args);
            }
            else
            {
                // Split existing node into two nodes at the insertion point, then concat all three nodes together.
                int leftItemCount;
                int splitLength;
                checked
                {
                    leftItemCount = (int)index;
                    splitLength = (int)index;
                }
                FixedList<T> leftItems = args.CustomBuilder.CreateList(leftItemCount,BigList<T>.MAXLEAF);
                leftItems.AddRange(items.GetRange(0, splitLength),splitLength);
                var leftLeafNode = args.CustomBuilder.CreateLeafNode(index, leftItems);
                leftLeafNode.NotifyUpdate(0, leftItems.Count, args);
                Node<T> leftNode = leftLeafNode;
                leafNodeEnumrator.Replace(this, leftLeafNode);

                int rightItemCount;
                checked
                {
                    rightItemCount = items.Count - (int)index;
                }
                FixedList<T> rightItems = args.CustomBuilder.CreateList(rightItemCount, BigList<T>.MAXLEAF);
                rightItems.AddRange(items.GetRange(splitLength, rightItemCount), rightItemCount);
                var rightLeafNode = args.CustomBuilder.CreateLeafNode(Count - index, rightItems);
                rightLeafNode.NotifyUpdate(0, rightItems.Count, args);
                Node<T> rightNode = rightLeafNode;

                leftNode = leftNode.AppendInPlace(node, leafNodeEnumrator, nodeBelongLeafNodeEnumrator, args);

                leafNodeEnumrator.AddNext(BigList<T>.GetMostRightNode(leftNode), rightLeafNode);
                leftNode = leftNode.AppendInPlace(rightNode, null, null, args);
                return leftNode;
            }
        }

        public override Node<T> PrependInPlace(T item, LeafNodeEnumrator<T> leafNodeEnumrator,BigListArgs<T> args)
        {
            // Add into the current leaf, if possible.
            if (Count < BigList<T>.MAXLEAF)
            {
                items.Insert(0, item);
                NotifyUpdate(0, 1, args);
                Count += 1;

                return this;
            }
            else
            {
                var newLeafNode = args.CustomBuilder.CreateLeafNode(item);
                newLeafNode.NotifyUpdate(0,newLeafNode.items.Count, args);
                leafNodeEnumrator.AddBefore(this, newLeafNode);
                return args.CustomBuilder.CreateConcatNode(newLeafNode, this);
            }
        }

        public override Node<T> RemoveRangeInPlace(long first, long last, LeafNodeEnumrator<T> leafNodeEnumrator,BigListArgs<T> args)
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
            long newCount = first + (Count - last - 1);      // number of items remaining.
            long removeLength = last - first + 1;
            checked
            {
                items.RemoveRange((int)first, (int)removeLength);
            }
            NotifyUpdate(first, -removeLength, args);
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
            this.NodeCount  = node.Left.NodeCount + node.Right.NodeCount;
        }

        public ConcatNode(Node<T> left, Node<T> right) : base(left, right)
        {
            Debug.Assert(left != null && right != null);
            this.Left = left;
            this.Right = right;
            this.Count = left.Count + right.Count;
            this.NodeCount = left.NodeCount + right.NodeCount;
            if (left.Depth > right.Depth)
                this.Depth = (short)(left.Depth + 1);
            else
                this.Depth = (short)(right.Depth + 1);
        }

        protected virtual Node<T> NewNodeInPlace(Node<T> newLeft, Node<T> newRight)
        {
            if (newLeft == null)
                return newRight;
            else if (newRight == null)
                return newLeft;

            Left = newLeft;
            Right = newRight;
            Count = Left.Count + Right.Count;
            NodeCount = Left.NodeCount + Right.NodeCount;
            if (Left.Depth > Right.Depth)
                Depth = (short)(Left.Depth + 1);
            else
                Depth = (short)(Right.Depth + 1);
            return this;
        }

        public override Node<T> SetAtInPlace(long index, T item,BigListArgs<T> args)
        {
            long leftCount = Left.Count;

            if (index < leftCount)
            {
                var newLeft = Left.SetAtInPlace(index, item, args);
                return NewNodeInPlace(newLeft, Right);
            }
            else
            {
                var newRight = Right.SetAtInPlace(index - leftCount, item, args);
                return NewNodeInPlace(Left, newRight);
            }
        }

        public override Node<T> PrependInPlace(Node<T> node, LeafNodeEnumrator<T> leafNodeEnumrator, LeafNodeEnumrator<T> nodeBelongLeafNodeEnumrator,BigListArgs<T> args)
        {
            if (Left.Count + node.Count <= BigList<T>.MAXLEAF && Left is LeafNode<T> && node is LeafNode<T>)
                return NewNodeInPlace(Left.PrependInPlace(node, leafNodeEnumrator, nodeBelongLeafNodeEnumrator, args), Right);
            if (leafNodeEnumrator != null)
            {
                var rightLeafNode = node as LeafNode<T>;
                if (nodeBelongLeafNodeEnumrator != null)
                    leafNodeEnumrator.AddBefore(BigList<T>.GetMostLeftNode(this), nodeBelongLeafNodeEnumrator);
                else if (rightLeafNode != null)
                    leafNodeEnumrator.AddBefore(BigList<T>.GetMostLeftNode(this), rightLeafNode);
            }
            return args.CustomBuilder.CreateConcatNode(node, this);
        }

        public override Node<T> AppendInPlace(Node<T> node, LeafNodeEnumrator<T> leafNodeEnumrator, LeafNodeEnumrator<T> nodeBelongLeafNodeEnumrator,BigListArgs<T> args)
        {
            if (Right.Count + node.Count <= BigList<T>.MAXLEAF && Right is LeafNode<T> && node is LeafNode<T>)
                return NewNodeInPlace(Left, Right.AppendInPlace(node, leafNodeEnumrator, nodeBelongLeafNodeEnumrator, args));
            if (leafNodeEnumrator != null)
            {
                var rightLeafNode = node as LeafNode<T>;
                if (nodeBelongLeafNodeEnumrator != null)
                    leafNodeEnumrator.AddNext(BigList<T>.GetMostRightNode(this), nodeBelongLeafNodeEnumrator);
                else if(rightLeafNode != null)
                    leafNodeEnumrator.AddNext(BigList<T>.GetMostRightNode(this), rightLeafNode);
            }
            return args.CustomBuilder.CreateConcatNode(this, node);
        }

        public override Node<T> AppendInPlace(T item, LeafNodeEnumrator<T> leafNodeEnumrator,BigListArgs<T> args)
        {
            LeafNode<T> rightLeaf;
            if (Right.Count < BigList<T>.MAXLEAF && (rightLeaf = Right as LeafNode<T>) != null)
            {
                rightLeaf.items.Add(item);
                rightLeaf.Count += 1;
                rightLeaf.NotifyUpdate(rightLeaf.Count, 1, args);
                this.Count += 1;
                return this;
            }
            else
            {
                var newLeafNode = args.CustomBuilder.CreateLeafNode(item);
                newLeafNode.NotifyUpdate(0, newLeafNode.Count, args);
                leafNodeEnumrator.AddNext(BigList<T>.GetMostRightNode(this), newLeafNode);
                return args.CustomBuilder.CreateConcatNode(this, newLeafNode);
            }
        }

        public override Node<T> InsertInPlace(long index, T item, LeafNodeEnumrator<T> leafNodeEnumrator,BigListArgs<T> args)
        {
            long leftCount = Left.Count;
            if (index <= leftCount)
                return NewNodeInPlace(Left.InsertInPlace(index, item, leafNodeEnumrator, args), Right);
            else
                return NewNodeInPlace(Left, Right.InsertInPlace(index - leftCount, item, leafNodeEnumrator,args));
        }

        public override Node<T> InsertInPlace(long index, Node<T> node, LeafNodeEnumrator<T> leafNodeEnumrator, LeafNodeEnumrator<T> nodeBelongLeafNodeEnumrator,BigListArgs<T> args)
        {
            long leftCount = Left.Count;
            if (index < leftCount)
                return NewNodeInPlace(Left.InsertInPlace(index, node, leafNodeEnumrator,nodeBelongLeafNodeEnumrator,args), Right);
            else
                return NewNodeInPlace(Left, Right.InsertInPlace(index - leftCount, node, leafNodeEnumrator, nodeBelongLeafNodeEnumrator, args));
        }

        public override Node<T> PrependInPlace(T item, LeafNodeEnumrator<T> leafNodeEnumrator,BigListArgs<T> args)
        {
            LeafNode<T> leftLeaf;
            if (Left.Count < BigList<T>.MAXLEAF && (leftLeaf = Left as LeafNode<T>) != null)
            {
                // Prepend the item to the left leaf. This keeps repeated prepends from creating
                // single item nodes.
                leftLeaf.items.Insert(0, item);
                leftLeaf.Count += 1;
                leftLeaf.NotifyUpdate(0, 1, args);
                this.Count += 1;
                return this;
            }
            else
            {
                var newLeaf = args.CustomBuilder.CreateLeafNode(item);
                newLeaf.NotifyUpdate(0, 1, args);
                leafNodeEnumrator.AddBefore(BigList<T>.GetMostLeftNode(this), newLeaf);
                return args.CustomBuilder.CreateConcatNode(newLeaf, this);
            }
        }

        public override Node<T> RemoveRangeInPlace(long first, long last, LeafNodeEnumrator<T> leafNodeEnumrator,BigListArgs<T> args)
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

            long leftCount = Left.Count;
            Node<T> newLeft = Left, newRight = Right;

            // Is part of the left being removed?
            if (first < leftCount)
                newLeft = Left.RemoveRangeInPlace(first, last, leafNodeEnumrator, args);
            // Is part of the right being remove?
            if (last >= leftCount)
                newRight = Right.RemoveRangeInPlace(first - leftCount, last - leftCount, leafNodeEnumrator, args);

            return NewNodeInPlace(newLeft, newRight);
        }

    }
}
