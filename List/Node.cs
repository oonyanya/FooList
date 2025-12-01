/*
 *  Copy from
 *  https://github.com/timdetering/Wintellect.PowerCollections
 *  Fooproject modify
 */
//#define MODIFY_NODE_BY_NORECURSIVE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using FooProject.Collection.DataStore;

namespace FooProject.Collection
{
    public class NodeUtil
    {

    }
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

        public abstract Node<T> SetAtInPlace(long index, T item, LeafNodeEnumrator<T> leafNodeEnumrator, BigListArgs<T> args);

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
        public IPinableContainer<IComposableList<T>> container;

        public LeafNode<T> Next { get; set; }

        public LeafNode<T> Previous { get; set; }

        public LeafNode() : base()
        {
            this.Previous = null;
            this.Next = null;
            this.NodeCount = 1;
        }

        public LeafNode(long count, IPinableContainer<IComposableList<T>> pinableContent) : this()
        {
            this.container = pinableContent;
            Count = count;
        }

        public override Node<T> SetAtInPlace(long index, T item, LeafNodeEnumrator<T> leafNodeEnumrator, BigListArgs<T> args)
        {
            bool requireSetNotInPlace = false;
            using(var pinnedContent = args.CustomBuilder.DataStore.Get(this.container))
            {
                if (pinnedContent.Content.QueryUpdate((int)index, item))
                {
                    var items = pinnedContent.Content;
                    items[(int)index] = item;
                }
                else
                {
                    requireSetNotInPlace = true;
                }
            }
            if (requireSetNotInPlace)
            {
                var node = this.RemoveRangeInPlace(index, index, leafNodeEnumrator, args);
                return node.InsertInPlace(index,item,leafNodeEnumrator,args);
            }
            NotifyUpdate(index, 1, args);
            return this;
        }

        public override Node<T> AppendInPlace(T item, LeafNodeEnumrator<T> leafNodeEnumrator,BigListArgs<T> args)
        {
            bool result = false;
            using (var pinnedContent = args.CustomBuilder.DataStore.Get(this.container))
            {
                result = pinnedContent.Content.QueryAddRange(null,1);
            }

            if (result)
            {
                using (var pinnedContent = args.CustomBuilder.DataStore.Get(this.container))
                {
                    var items = pinnedContent.Content;
                    checked
                    {
                        items.Insert((int)Count, item);
                    }
                }
                NotifyUpdate(Count, 1, args);
                Count += 1;
                return this;
            }
            else
            {
                var newLeafNode = args.CustomBuilder.CreateLeafNode(item, args.BlockSize);
                newLeafNode.NotifyUpdate(0, 1, args);
                leafNodeEnumrator.AddNext(this,newLeafNode);
                return args.CustomBuilder.CreateConcatNode(this, newLeafNode);
            }
        }
        private bool MergeBeforeLeafInPlace(Node<T> other, BigListArgs<T> args)
        {
            LeafNode<T> otherLeaf = (other as LeafNode<T>);
            long newCount;
            if (otherLeaf != null)
            {
                bool result;
                using (var pinnedContent = args.CustomBuilder.DataStore.Get(this.container))
                using (var otherLeafPinnedCotent = args.CustomBuilder.DataStore.Get(otherLeaf.container))
                {
                    var items = pinnedContent.Content;
                    var otherLeafItems = otherLeafPinnedCotent.Content;
                    newCount = otherLeaf.Count + this.Count;
                    result = items.QueryInsertRange(0, null,  otherLeafItems.Count);
                }

                if (result)
                {
                    using (var pinnedContent = args.CustomBuilder.DataStore.Get(this.container))
                    using (var otherLeafPinnedCotent = args.CustomBuilder.DataStore.Get(otherLeaf.container))
                    {
                        var items = pinnedContent.Content;
                        var otherLeafItems = otherLeafPinnedCotent.Content;
                        checked
                        {
                            items.InsertRange(0, otherLeafItems, (int)otherLeaf.Count);
                        }
                        otherLeafPinnedCotent.RemoveContent();
                    }
                    NotifyUpdate(0, otherLeaf.Count, args);
                    Count = newCount;
                    return true;
                }
            }
            return false;
        }
        private bool MergeLeafInPlace(Node<T> other,BigListArgs<T> args)
        {
            LeafNode<T> otherLeaf = (other as LeafNode<T>);
            long newCount;
            if (otherLeaf != null)
            {
                bool result;
                using (var pinnedContent = args.CustomBuilder.DataStore.Get(this.container))
                using (var otherLeafPinnedCotent = args.CustomBuilder.DataStore.Get(otherLeaf.container))
                {
                    var items = pinnedContent.Content;
                    var otherLeafItems = otherLeafPinnedCotent.Content;
                    newCount = otherLeaf.Count + this.Count;
                    result = items.QueryAddRange(null, otherLeafItems.Count);
                }
                if (result)
                {
                    long itemsCount;
                    using (var pinnedContent = args.CustomBuilder.DataStore.Get(this.container))
                    using (var otherLeafPinnedCotent = args.CustomBuilder.DataStore.Get(otherLeaf.container))
                    {
                        var items = pinnedContent.Content;
                        itemsCount = items.Count;
                        var otherLeafItems = otherLeafPinnedCotent.Content;
                        checked
                        {
                            items.AddRange(otherLeafItems, (int)otherLeaf.Count);
                        }
                        otherLeafPinnedCotent.RemoveContent();
                    }
                    NotifyUpdate(itemsCount, otherLeaf.Count, args);
                    Count = newCount;
                    return true;
                }
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
            if(leafNodeEnumrator != null)
            {
                if (nodeBelongLeafNodeEnumrator != null)
                {
                    leafNodeEnumrator.AddNext(BigList<T>.GetMostRightNode(this), nodeBelongLeafNodeEnumrator);
                }
                else
                {
                    leafNodeEnumrator.AddNext(BigList<T>.GetMostRightNode(this), (LeafNode<T>)node);
                }
            }
            return args.CustomBuilder.CreateConcatNode(this, node);
        }

        public override Node<T> InsertInPlace(long index, T item, LeafNodeEnumrator<T> leafNodeEnumrator,BigListArgs<T> args)
        {
            bool result = false;
            using (var pinnedContent = args.CustomBuilder.DataStore.Get(this.container))
            {
                var items = pinnedContent.Content;
                result = items.QueryInsertRange((int)index, null, 1);
            }

            if (result)
            {
                using (var pinnedContent = args.CustomBuilder.DataStore.Get(this.container))
                {
                    var items = pinnedContent.Content;
                    items.Insert((int)index, item);
                }
                NotifyUpdate(index, 1, args);
                Count += 1;
                return this;
            }
            else
            {
                if (index == Count)
                {
                    var newLeafNode = args.CustomBuilder.CreateLeafNode(item,args.BlockSize);
                    newLeafNode.NotifyUpdate(0, 1, args);
                    leafNodeEnumrator.AddNext(this, newLeafNode);
                    // Inserting at count is just an appending operation.
                    return args.CustomBuilder.CreateConcatNode(this, newLeafNode);
                }
                else if (index == 0)
                {
                    var newLeafNode = args.CustomBuilder.CreateLeafNode(item, args.BlockSize);
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
                    IComposableList<T> items;
                    using (var pinnedContent = args.CustomBuilder.DataStore.Get(this.container))
                    {
                        items = pinnedContent.Content;
                        pinnedContent.RemoveContent();
                    }
                    IComposableList<T> leftItems = args.CustomBuilder.CreateList(leftItemCount, args.BlockSize, items.GetRange(0, splitLength));
                    if(leftItems.QueryAddRange(null, 1))
                    {
                        leftItems.Add(item);
                        var leftContainer = args.CustomBuilder.DataStore.Update(this.container, leftItems, 0, items.Count, 0, leftItemCount);
                        LeafNode<T> leftNode = args.CustomBuilder.CreateLeafNode(index + 1, leftContainer);
                        leftNode.NotifyUpdate(0, leftItems.Count, args);
                        leafNodeEnumrator.Replace(this, leftNode);

                        int rightItemCount = items.Count - (int)index;
                        IComposableList<T> rightItems = args.CustomBuilder.CreateList(rightItemCount, args.BlockSize, items.GetRange(splitLength, rightItemCount));
                        var rightContainer = args.CustomBuilder.DataStore.Update(this.container, rightItems, 0, items.Count, index, rightItemCount);
                        LeafNode<T> rightNode = args.CustomBuilder.CreateLeafNode(Count - index, rightContainer);
                        rightNode.NotifyUpdate(0, rightItems.Count, args);
                        leafNodeEnumrator.AddNext(leftNode, rightNode);

                        return args.CustomBuilder.CreateConcatNode(leftNode, rightNode);
                    }
                    else
                    {
                        var leftContainer = args.CustomBuilder.DataStore.Update(this.container, leftItems, 0, items.Count, 0, leftItemCount);
                        LeafNode<T> leftLeafNode = args.CustomBuilder.CreateLeafNode(leftItems.Count, leftContainer);
                        leftLeafNode.NotifyUpdate(0, leftItems.Count, args);
                        Node<T> leftNode = leftLeafNode;
                        leafNodeEnumrator.Replace(this, leftLeafNode);

                        int rightItemCount = items.Count - (int)index;
                        IComposableList<T> rightItems = args.CustomBuilder.CreateList(rightItemCount, args.BlockSize, items.GetRange(splitLength, rightItemCount));
                        var rightContainer = args.CustomBuilder.DataStore.Update(this.container, rightItems, 0, items.Count, index, rightItemCount);
                        LeafNode<T> rightNode = args.CustomBuilder.CreateLeafNode(Count - index, rightContainer);
                        rightNode.NotifyUpdate(0, rightItems.Count, args);

                        var newNode = args.CustomBuilder.CreateLeafNode(item, args.BlockSize);
                        newNode.NotifyUpdate(0,1,args);
                        leftNode = leftNode.AppendInPlace(newNode, leafNodeEnumrator, null, args);

                        leftNode = leftNode.AppendInPlace(rightNode, leafNodeEnumrator, null, args);

                        return leftNode;
                    }
                }
            }
        }

        public override Node<T> InsertInPlace(long index, Node<T> node, LeafNodeEnumrator<T> leafNodeEnumrator, LeafNodeEnumrator<T> nodeBelongLeafNodeEnumrator,BigListArgs<T> args)
        {
            LeafNode<T> otherLeaf = (node as LeafNode<T>);
            long newCount = this.Count;　//マージされる側のノードは少なくとも何かが存在するはず
            bool result = false;
            if (otherLeaf != null)
            {
                using (var pinnedContent = args.CustomBuilder.DataStore.Get(this.container))
                using (var otherLeafPinnedCotent = args.CustomBuilder.DataStore.Get(otherLeaf.container))
                {
                    var items = pinnedContent.Content;
                    var otherLeafItems = otherLeafPinnedCotent.Content;
                    newCount = otherLeaf.Count + this.Count;
                    result = items.QueryInsertRange((int)index, null, otherLeafItems.Count);
                }
            }
            if (result)
            {
                using (var pinnedContent = args.CustomBuilder.DataStore.Get(this.container))
                using (var otherLeafPinnedCotent = args.CustomBuilder.DataStore.Get(otherLeaf.container))
                {
                    var items = pinnedContent.Content;
                    var otherLeafItems = otherLeafPinnedCotent.Content;

                    nodeBelongLeafNodeEnumrator.Remove(otherLeaf);
                    // Combine the two leaf nodes into one.
                    items.InsertRange((int)index, otherLeafItems);

                    otherLeafPinnedCotent.RemoveContent();
                }
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

                IComposableList<T> items;
                using (var pinnedContent = args.CustomBuilder.DataStore.Get(this.container))
                {
                    items = pinnedContent.Content;
                    pinnedContent.RemoveContent();
                }

                IComposableList<T> leftItems = args.CustomBuilder.CreateList(leftItemCount, args.BlockSize, items.GetRange(0, splitLength));
                var leftContainer = args.CustomBuilder.DataStore.Update(this.container, leftItems, 0, items.Count, 0, leftItemCount);
                var leftLeafNode = args.CustomBuilder.CreateLeafNode(index, leftContainer);
                leftLeafNode.NotifyUpdate(0, leftItems.Count, args);
                Node<T> leftNode = leftLeafNode;
                leafNodeEnumrator.Replace(this, leftLeafNode);

                int rightItemCount;
                checked
                {
                    rightItemCount = items.Count - (int)index;
                }
                IComposableList<T> rightItems = args.CustomBuilder.CreateList(rightItemCount, args.BlockSize, items.GetRange(splitLength, rightItemCount));
                var rightContainer = args.CustomBuilder.DataStore.Update(this.container, rightItems, 0, items.Count, index, rightItemCount);
                var rightLeafNode = args.CustomBuilder.CreateLeafNode(Count - index, rightContainer);
                rightLeafNode.NotifyUpdate(0, rightItems.Count, args);
                Node<T> rightNode = rightLeafNode;

                leftNode = leftNode.AppendInPlace(node, leafNodeEnumrator, nodeBelongLeafNodeEnumrator, args);

                leftNode = leftNode.AppendInPlace(rightNode, leafNodeEnumrator, null, args);
                return leftNode;
            }
        }

        public override Node<T> PrependInPlace(T item, LeafNodeEnumrator<T> leafNodeEnumrator,BigListArgs<T> args)
        {
            bool result = false;
            using (var pinnedContent = args.CustomBuilder.DataStore.Get(this.container))
            {
                var items = pinnedContent.Content;
                result = items.QueryInsertRange(0, null, 1);
            }
            // Add into the current leaf, if possible.
            if (result)
            {
                using (var pinnedContent = args.CustomBuilder.DataStore.Get(this.container))
                {
                    var items = pinnedContent.Content;

                    items.Insert(0, item);
                    NotifyUpdate(0, 1, args);
                    Count += 1;
                }
                return this;
            }
            else
            {
                var newLeafNode = args.CustomBuilder.CreateLeafNode(item, args.BlockSize);
                long newLeafNodeItemsCount;
                using (var pinnedContent = args.CustomBuilder.DataStore.Get(newLeafNode.container))
                {
                    var items = pinnedContent.Content;
                    newLeafNodeItemsCount = items.Count;
                }
                newLeafNode.NotifyUpdate(0, newLeafNodeItemsCount, args);
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
                using (var pinnedContent = args.CustomBuilder.DataStore.Get(this.container))
                {
                    pinnedContent.RemoveContent();
                }
                leafNodeEnumrator.Remove(this);
                return null;     // removing entire node.
            }

            if (first < 0)
                first = 0;
            if (last >= Count)
                last = Count - 1;
            long newCount = first + (Count - last - 1);      // number of items remaining.
            long removeLength = last - first + 1;
            bool result = false;

            using (var pinnedContent = args.CustomBuilder.DataStore.Get(this.container))
            {
                result = pinnedContent.Content.QueryRemoveRange((int)first, (int)removeLength);
            }

            if (result)
            {
                checked
                {
                    using (var pinnedContent = args.CustomBuilder.DataStore.Get(this.container))
                    {
                        var items = pinnedContent.Content;
                        items.RemoveRange((int)first, (int)removeLength);
                    }
                }
                NotifyUpdate(first, -removeLength, args);
                Count = newCount;
                return this;
            }
            else
            {
                int leftItemCount;
                checked
                {
                    leftItemCount = (int)first;
                }

                IComposableList<T> items;
                using (var pinnedContent = args.CustomBuilder.DataStore.Get(this.container))
                {
                    items = pinnedContent.Content;
                    pinnedContent.RemoveContent();
                }

                IComposableList<T> leftItems = args.CustomBuilder.CreateList(leftItemCount, args.BlockSize, items.GetRange(0, leftItemCount));
                var leftContainer = args.CustomBuilder.DataStore.Update(this.container, leftItems, 0, items.Count, 0, leftItemCount);
                var leftLeafNode = args.CustomBuilder.CreateLeafNode(leftItemCount, leftContainer);
                leftLeafNode.NotifyUpdate(0, leftItems.Count, args);
                Node<T> leftNode = leftLeafNode;

                int rightItemCount;
                int rightIndex;
                checked
                {
                    rightItemCount = (int)(this.Count - last - 1);
                    rightIndex = (int)last + 1;
                }
                IComposableList<T> rightItems = args.CustomBuilder.CreateList(rightItemCount, args.BlockSize, items.GetRange(rightIndex, rightItemCount));
                var rightContainer = args.CustomBuilder.DataStore.Update(this.container, rightItems, 0, items.Count, rightIndex, rightItemCount);
                var rightLeafNode = args.CustomBuilder.CreateLeafNode(rightItemCount, rightContainer);
                rightLeafNode.NotifyUpdate(0, rightItems.Count, args);
                Node<T> rightNode = rightLeafNode;

                if (leftItemCount == 0 && rightItemCount == 0)
                {
                    return null;
                }
                else if (leftItemCount == 0)
                {
                    leafNodeEnumrator.Replace(this, rightLeafNode);
                    return rightLeafNode;
                }
                else if (rightItemCount == 0)
                {
                    leafNodeEnumrator.Replace(this, leftLeafNode);
                    return leftLeafNode;
                }
                else
                {
                    leafNodeEnumrator.Replace(this, leftLeafNode);
                    leftNode = leftNode.AppendInPlace(rightNode, leafNodeEnumrator, null, args);
                    return leftNode;
                }
            }
        }

    }

    public class ConcatNode<T> : Node<T>
    {

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
            this.OnNewNode(Left, Right);
        }

        protected virtual void OnNewNode(Node<T> newLeft, Node<T> newRight)
        {
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
            NodeCount = Left.NodeCount + Right.NodeCount;
            if (Left.Depth > Right.Depth)
                Depth = (short)(Left.Depth + 1);
            else
                Depth = (short)(Right.Depth + 1);
            this.OnNewNode(Left, Right);
            return this;
        }

        public override Node<T> SetAtInPlace(long index, T item,LeafNodeEnumrator<T> leafNodeEnumrator, BigListArgs<T> args)
        {
#if MODIFY_NODE_BY_NORECURSIVE
            Stack<NodeWalkInfo> traklist = new Stack<NodeWalkInfo>();
            Node<T> current = this;
            long currentIndex = index;

            while (current != null && current.Left != null && current.Right != null)
            {
                long leftCount = current.Left.Count;
                if (currentIndex < leftCount)
                {
                    traklist.Push(new NodeWalkInfo(current, NodeWalkDirection.Left));
                    current = current.Left;
                }
                else
                {
                    traklist.Push(new NodeWalkInfo(current, NodeWalkDirection.Right));
                    current = current.Right;
                    currentIndex -= leftCount;
                }
            }

            System.Diagnostics.Debug.Assert(currentIndex >= 0);
            Node<T> resultNode = current.SetAtInPlace(currentIndex,item, leafNodeEnumrator, args);

            NodeWalkInfo poped = null;

            while (traklist.Count > 0)
            {
                poped = traklist.Pop();
                var concatNodeCurrent = (ConcatNode<T>)poped.node;
                if (poped.direction == NodeWalkDirection.Left)
                {
                    concatNodeCurrent.Left = resultNode;
                    concatNodeCurrent.NewNodeInPlace(resultNode, concatNodeCurrent.Right);
                    resultNode = concatNodeCurrent;
                }
                else
                {
                    concatNodeCurrent.Right = resultNode;
                    concatNodeCurrent.NewNodeInPlace(concatNodeCurrent.Left, resultNode);
                    resultNode = concatNodeCurrent;
                }
            }
            if (poped != null)
            {
                return poped.node;
            }
            else
            {
                throw new Exception("something wrong");
            }
#else
            long leftCount = Left.Count;

            if (index < leftCount)
            {
                var newLeft = Left.SetAtInPlace(index, item, leafNodeEnumrator, args);
                return NewNodeInPlace(newLeft, Right);
            }
            else
            {
                var newRight = Right.SetAtInPlace(index - leftCount, item, leafNodeEnumrator, args);
                return NewNodeInPlace(Left, newRight);
            }
#endif
        }

        public override Node<T> PrependInPlace(Node<T> node, LeafNodeEnumrator<T> leafNodeEnumrator, LeafNodeEnumrator<T> nodeBelongLeafNodeEnumrator,BigListArgs<T> args)
        {
            var LeftLeafNode = Left as LeafNode<T>;
            var newLeafNode = node as LeafNode<T>;
            if (LeftLeafNode != null && newLeafNode != null)
            {
                bool result = false;
                using(var LeftPinnedContent = args.CustomBuilder.DataStore.Get(LeftLeafNode.container))
                {
                    result = LeftPinnedContent.Content.QueryInsertRange(0, null, (int)newLeafNode.Count);
                }
                //Leftはリーフノードであることが確定してるのでスタックに積む必要がない
                if (result)
                    return NewNodeInPlace(Left.PrependInPlace(node, leafNodeEnumrator, nodeBelongLeafNodeEnumrator, args), Right);
            }
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
            var RightLeafNode = Right as LeafNode<T>;
            var newLeafNode = node as LeafNode<T>;
            if (RightLeafNode != null && newLeafNode != null)
            {
                bool result = false;
                using (var RightPinnedContent = args.CustomBuilder.DataStore.Get(RightLeafNode.container))
                {
                    result = RightPinnedContent.Content.QueryAddRange(null, (int)newLeafNode.Count);
                }
                //Rightはリーフノードであることが確定してるのでスタックに積む必要がない
                if (result)
                    return NewNodeInPlace(Left, Right.AppendInPlace(node, leafNodeEnumrator, nodeBelongLeafNodeEnumrator, args));
            }
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
            LeafNode<T> rightLeaf = Right as LeafNode<T>;
            bool result = false;
            if (rightLeaf != null)
            {
                using (var pinnedContent = args.CustomBuilder.DataStore.Get(rightLeaf.container))
                {
                    var rightLeafItems = pinnedContent.Content;
                    result = rightLeafItems.QueryAddRange(null, 1);
                }
            }

            if (rightLeaf != null && result)
            {
                using (var pinnedContent = args.CustomBuilder.DataStore.Get(rightLeaf.container))
                {
                    var rightLeafItems = pinnedContent.Content;
                    rightLeafItems.Add(item);
                }
                rightLeaf.Count += 1;
                System.Diagnostics.Debug.Assert(rightLeaf.Count > 0);
                rightLeaf.NotifyUpdate(rightLeaf.Count - 1, 1, args);
                this.Count += 1;
                return this;
            }
            else
            {
                var newLeafNode = args.CustomBuilder.CreateLeafNode(item, args.BlockSize);
                newLeafNode.NotifyUpdate(0, newLeafNode.Count, args);
                leafNodeEnumrator.AddNext(BigList<T>.GetMostRightNode(this), newLeafNode);
                return args.CustomBuilder.CreateConcatNode(this, newLeafNode);
            }
        }

        public override Node<T> InsertInPlace(long index, T item, LeafNodeEnumrator<T> leafNodeEnumrator,BigListArgs<T> args)
        {
#if MODIFY_NODE_BY_NORECURSIVE
            Stack<NodeWalkInfo> traklist = new Stack<NodeWalkInfo>(BigList<T>.MAXFIB);
            Node<T> current = this;
            long currentIndex = index;

            while (current != null && current.Left != null && current.Right != null)
            {
                long leftCount = current.Left.Count;
                if (currentIndex <= leftCount)
                {
                    traklist.Push(new NodeWalkInfo(current, NodeWalkDirection.Left));
                    current = current.Left;
                }
                else
                {
                    traklist.Push(new NodeWalkInfo(current, NodeWalkDirection.Right));
                    current = current.Right;
                    currentIndex -= leftCount;
                }
            }

            System.Diagnostics.Debug.Assert(currentIndex >= 0);
            Node<T> resultNode = current.InsertInPlace(currentIndex, item, leafNodeEnumrator, args);

            NodeWalkInfo poped = null;

            while (traklist.Count > 0)
            {
                poped = traklist.Pop();
                var concatNodeCurrent = (ConcatNode<T>)poped.node;
                if (poped.direction == NodeWalkDirection.Left)
                {
                    concatNodeCurrent.Left = resultNode;
                    concatNodeCurrent.NewNodeInPlace(resultNode, concatNodeCurrent.Right);
                    resultNode = concatNodeCurrent;
                }
                else
                {
                    concatNodeCurrent.Right = resultNode;
                    concatNodeCurrent.NewNodeInPlace(concatNodeCurrent.Left, resultNode);
                    resultNode = concatNodeCurrent;
                }
            }
            if (poped != null)
            {
                return poped.node;
            }
            else
            {
                throw new Exception("something wrong");
            }
#else
            long leftCount = Left.Count;
            if (index <= leftCount)
                return NewNodeInPlace(Left.InsertInPlace(index, item, leafNodeEnumrator, args), Right);
            else
                return NewNodeInPlace(Left, Right.InsertInPlace(index - leftCount, item, leafNodeEnumrator,args));
#endif
        }

        public override Node<T> InsertInPlace(long index, Node<T> node, LeafNodeEnumrator<T> leafNodeEnumrator, LeafNodeEnumrator<T> nodeBelongLeafNodeEnumrator,BigListArgs<T> args)
        {
#if MODIFY_NODE_BY_NORECURSIVE
            Stack<NodeWalkInfo> traklist = new Stack<NodeWalkInfo>(BigList<T>.MAXFIB);
            Node<T> current = this;
            long currentIndex = index;

            while (current != null && current.Left != null && current.Right != null)
            {
                long leftCount = current.Left.Count;
                if (currentIndex <= leftCount)
                {
                    traklist.Push(new NodeWalkInfo(current, NodeWalkDirection.Left));
                    current = current.Left;
                }
                else
                {
                    traklist.Push(new NodeWalkInfo(current, NodeWalkDirection.Right));
                    current = current.Right;
                    currentIndex -= leftCount;
                }
            }

            System.Diagnostics.Debug.Assert(currentIndex >= 0);
            Node<T> resultNode = current.InsertInPlace(currentIndex, node, leafNodeEnumrator, nodeBelongLeafNodeEnumrator, args);

            NodeWalkInfo poped = null;

            while (traklist.Count > 0)
            {
                poped = traklist.Pop();
                var concatNodeCurrent = (ConcatNode<T>)poped.node;
                if (poped.direction == NodeWalkDirection.Left)
                {
                    concatNodeCurrent.Left = resultNode;
                    concatNodeCurrent.NewNodeInPlace(resultNode, concatNodeCurrent.Right);
                    resultNode = concatNodeCurrent;
                }
                else
                {
                    concatNodeCurrent.Right = resultNode;
                    concatNodeCurrent.NewNodeInPlace(concatNodeCurrent.Left, resultNode);
                    resultNode = concatNodeCurrent;
                }
            }
            if (poped != null)
            {
                return poped.node;
            }
            else
            {
                throw new Exception("something wrong");
            }
#else
            long leftCount = Left.Count;
            if (index < leftCount)
                return NewNodeInPlace(Left.InsertInPlace(index, node, leafNodeEnumrator,nodeBelongLeafNodeEnumrator,args), Right);
            else
                return NewNodeInPlace(Left, Right.InsertInPlace(index - leftCount, node, leafNodeEnumrator, nodeBelongLeafNodeEnumrator, args));
#endif
        }

        public override Node<T> PrependInPlace(T item, LeafNodeEnumrator<T> leafNodeEnumrator,BigListArgs<T> args)
        {
            LeafNode<T> leftLeaf = Left as LeafNode<T>;
            bool result = false;
            if (leftLeaf != null)
            {
                using (var pinnedContent = args.CustomBuilder.DataStore.Get(leftLeaf.container))
                {
                    var leftLeafItems = pinnedContent.Content;
                    result = leftLeafItems.QueryAddRange(null, 1);
                }
            }

            if (result && leftLeaf != null)
            {
                using (var pinnedContent = args.CustomBuilder.DataStore.Get(leftLeaf.container))
                {
                    var leftLeafItems = pinnedContent.Content;
                    // Prepend the item to the left leaf. This keeps repeated prepends from creating
                    // single item nodes.
                    leftLeafItems.Insert(0, item);
                }
                leftLeaf.Count += 1;
                leftLeaf.NotifyUpdate(0, 1, args);
                this.Count += 1;
                return this;
            }
            else
            {
                var newLeaf = args.CustomBuilder.CreateLeafNode(item, args.BlockSize);
                newLeaf.NotifyUpdate(0, 1, args);
                leafNodeEnumrator.AddBefore(BigList<T>.GetMostLeftNode(this), newLeaf);
                return args.CustomBuilder.CreateConcatNode(newLeaf, this);
            }
        }

        public override Node<T> RemoveRangeInPlace(long first, long last, LeafNodeEnumrator<T> leafNodeEnumrator,BigListArgs<T> args)
        {
#if MODIFY_NODE_BY_NORECURSIVE
            Debug.Assert(first < Count);
            Debug.Assert(last >= 0);

            Stack<NodeWalkInfo> stack = new Stack<NodeWalkInfo>(BigList<T>.MAXFIB);
            Stack<Node<T>>  ret_vals = new Stack<Node<T>>(BigList<T>.MAXFIB);

            stack.Push(new NodeWalkInfo(this, NodeWalkDirection.None, first, last, 0));

            while (stack.Count > 0)
            {
                var current_info = stack.Pop();
                Node<T> current = current_info.node;
                if (current.Left == null && current.Right == null)
                {
                    var retval = current.RemoveRangeInPlace(current_info.start, current_info.end, leafNodeEnumrator, args);
                    ret_vals.Push(retval);
                    continue;
                }

                int state = current_info.state;
                long start = current_info.start, end = current_info.end;
                long leftCount;
                if (state == 0)
                {
                    leftCount = current.Left.Count;
                    state++;
                    if (start < leftCount)
                    {
                        stack.Push(new NodeWalkInfo(current, NodeWalkDirection.Left, start, end, state, leftCount));
                        stack.Push(new NodeWalkInfo(current.Left, NodeWalkDirection.Left, start, end, 0));
                        continue;
                    }
                    else
                    {
                        ret_vals.Push(current.Left);
                    }
                }

                if (state == 1)
                {
                    leftCount = current_info.left_count == NodeWalkInfo.NOT_SETTED ? current.Left.Count : current_info.left_count;
                    state++;
                    if (end >= leftCount)
                    {
                        stack.Push(new NodeWalkInfo(current, NodeWalkDirection.Right, start, end, state, leftCount));
                        stack.Push(new NodeWalkInfo(current.Right, NodeWalkDirection.Right, start - leftCount, end - leftCount, 0));
                        continue;
                    }
                    else
                    {
                        ret_vals.Push(current.Right);
                    }
                }

                if (state == 2)
                {
                    var current_concat = current as ConcatNode<T>;
                    System.Diagnostics.Debug.Assert(current_concat != null);
                    var right = ret_vals.Pop();
                    var left = ret_vals.Pop();
                    var ret = current_concat.NewNodeInPlace(left, right);
                    ret_vals.Push(ret);
                    state = 0;
                }
            }
            return this;
#else
            Debug.Assert(first < Count);
            Debug.Assert(last >= 0);

            // TODO：まとめで削除できるケースがあるが、リンクドリストから削除するのが面倒なので再帰呼び出しで消す
            /*
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
#endif
        }

    }
}
