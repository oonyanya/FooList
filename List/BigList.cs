//******************************
// Written by Peter Golde
// Copyright (c) 2004-2007, Wintellect
//
// Use and restribution of this code is subject to the license agreement 
// contained in the file "License.txt" accompanying this file.
//******************************
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.VisualBasic;

namespace FooProject.Collection
{
    /// <summary>
    /// List for huge items.
    /// </summary>
    /// <remarks>
    /// BigList is for only single thread.
    /// </remarks>
    /// <typeparam name="T">The item type of the collection.</typeparam>
    public class BigList<T> : IList<T>, IReadOnlyList<T>,IReadOnlyCollection<T>
    {
        const uint MAXITEMS = int.MaxValue - 1;    // maximum number of items in a BigList.
        // The fibonacci numbers. Used in the rebalancing algorithm. Final MaxValue makes sure we don't go off the end.
        internal static readonly int[] FIBONACCI = {1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144, 233, 377, 610, 987, 1597, 2584,
            4181, 6765, 10946, 17711, 28657, 46368, 75025, 121393, 196418, 317811, 514229, 832040,
            1346269, 2178309, 3524578, 5702887, 9227465, 14930352, 24157817, 39088169, 63245986,
            102334155, 165580141, 267914296, 433494437, 701408733, 1134903170, 1836311903, int.MaxValue};
        internal const int MAXFIB = 44;  // maximum index in the above, not counting the final MaxValue.
        // default block size
#if DEBUG
        internal static int MAXLEAF = 8;
#else
        internal static int MAXLEAF = 392;
#endif
        internal const int BALANCEFACTOR = 6;      // how far the root must be in depth from fully balanced to invoke the rebalance operation (min 3).
        Node<T> root;
        LeafNodeEnumrator<T> leafNodeEnumrator = new LeafNodeEnumrator<T>();

        public BigList()
        {
            root = null;
        }

        public BigList(IEnumerable<T> items) : this()
        {
            AddRange(items);
        }

        /// <summary>
        /// get or set block size
        /// </summary>
        /// <remarks>
        /// It represent block size in each leaf. If you change value, all data is deleted.
        /// </remarks>
        public int BlockSize
        {
            get
            {
                return MAXLEAF;
            }
            set
            {
                MAXLEAF = value;
                Clear();
            }
        }

        struct LeastFetch
        {
            public Node<T> Node;
            public int TotalLeftCount = 0;
            public LeastFetch(Node<T> node,int totalLeft)
            {
                Node= node;
                TotalLeftCount = totalLeft;
            }
        }
        LeastFetch? leastFetch;

        public T this[int index]
        {
            get
            {
                // This could just be a simple call to GetAt on the root.
                // It is recoded as an interative algorithm for performance.

                if (root == null || index < 0 || index >= root.Count)
                    throw new ArgumentOutOfRangeException("index");

                int relativeIndex;
                LeafNode<T> curLeaf = (LeafNode<T>)IndexOfNode(index, out relativeIndex);
                return curLeaf.items[relativeIndex];
            }
            set
            {
                // This could just be a simple call to SetAtInPlace on the root.
                // It is recoded as an interative algorithm for performance.

                if (root == null || index < 0 || index >= root.Count)
                    throw new ArgumentOutOfRangeException("index");

                int relativeIndex;
                LeafNode<T> curLeaf = (LeafNode<T>)IndexOfNode(index,out relativeIndex);
                curLeaf.items[relativeIndex] = value;
            }
        }

        private Node<T> IndexOfNode(int index,out int relativeIndex)
        {
            if(leastFetch != null)
            {
                relativeIndex = index - leastFetch.Value.TotalLeftCount;
                if (relativeIndex >= 0 && relativeIndex < leastFetch.Value.Node.Count)
                    return leastFetch.Value.Node;
            }

            Node<T> current = root;
            relativeIndex = index;
            leastFetch = null;
            int totalLeftCount = 0;

            while (current != null)
            {
                if(current.Left != null)
                {
                    int leftCount = current.Left.Count;
                    if (relativeIndex < leftCount)
                    {
                        current = current.Left;
                    }
                    else
                    {
                        current = current.Right;
                        relativeIndex -= leftCount;
                        totalLeftCount += leftCount;
                    }

                }else if (current.Right != null)
                {
                    throw new InvalidOperationException("Left node is null but right node is not null.somthing wrong.");
                }
                else
                {
                    break;
                }
            }
            leastFetch = new LeastFetch(current,totalLeftCount);

            return current;
        }

        private void ResetFetchCache()
        {
            //このメソッドが呼び出された時点で何かしらの操作がされているのでキャッシュはいったんリセットする
            leastFetch = null;
        }

        public int Count
        {
            get
            {
                if (root == null)
                    return 0;
                else
                    return root.Count;

            }
        }

        public bool IsReadOnly { get { return false; } }

        private void CheckBalance()
        {
            if (root != null &&
                (root.Depth > BALANCEFACTOR && !(root.Depth - BALANCEFACTOR <= MAXFIB && Count >= FIBONACCI[root.Depth - BALANCEFACTOR])))
            {
                Rebalance();
            }
        }

        /// <summary>
        /// Rebalance the current tree. Once rebalanced, the depth of the current tree is no more than
        /// two levels from fully balanced, where fully balanced is defined as having Fibonacci(N+2) or more items
        /// in a tree of depth N.
        /// </summary>
        /// <remarks>The rebalancing algorithm is from "Ropes: an Alternative to Strings", by 
        /// Boehm, Atkinson, and Plass, in SOFTWARE--PRACTICE AND EXPERIENCE, VOL. 25(12), 1315–1330 (DECEMBER 1995).
        /// https://www.cs.tufts.edu/comp/150FP/archive/hans-boehm/ropes.pdf
        /// </remarks>
        internal void Rebalance()
        {
            Node<T>[] rebalanceArray;
            int slots;

            // The basic rebalancing algorithm is add nodes to a rabalance array, where a node at index K in the 
            // rebalance array has Fibonacci(K+1) to Fibonacci(K+2) items, and the entire list has the nodes
            // from largest to smallest concatenated.

            if (root == null)
                return;
            if (root.Depth <= 1 || (root.Depth - 2 <= MAXFIB && Count >= FIBONACCI[root.Depth - 2]))
                return;      // already sufficiently balanced.

            // How many slots does the rebalance array need?
            for (slots = 0; slots <= MAXFIB; ++slots)
                if (root.Count < FIBONACCI[slots])
                    break;
            rebalanceArray = new Node<T>[slots];

            // Add all the nodes to the rebalance array.
            AddNodeToRebalanceArray(rebalanceArray, root, false);

            // Concatinate all the node in the rebalance array.
            Node<T> result = null;
            for (int slot = 0; slot < slots; ++slot)
            {
                Node<T> n = rebalanceArray[slot];
                if (n != null)
                {
                    if (result == null)
                        result = n;
                    else
                        result = result.PrependInPlace(n, null, null);
                }
            }

            // And we're done. Check that it worked!
            root = result;
            Debug.Assert(root.Depth <= 1 || (root.Depth - 2 <= MAXFIB && Count >= FIBONACCI[root.Depth - 2]));
        }

        /// <summary>
        /// Part of the rebalancing algorithm. Adds a node to the rebalance array. If it is already balanced, add it directly, otherwise
        /// add its children.
        /// </summary>
        /// <param name="rebalanceArray">Rebalance array to insert into.</param>
        /// <param name="node">Node to add.</param>
        /// <param name="shared">If true, mark the node as shared before adding, because one
        /// of its parents was shared.</param>
        private void AddNodeToRebalanceArray(Node<T>[] rebalanceArray, Node<T> node, bool shared)
        {
            if (node.IsBalanced())
            {
                AddBalancedNodeToRebalanceArray(rebalanceArray, node);
            }
            else
            {
                ConcatNode<T> n = (ConcatNode<T>)node;          // leaf nodes are always balanced.
                AddNodeToRebalanceArray(rebalanceArray, n.Left, shared);
                AddNodeToRebalanceArray(rebalanceArray, n.Right, shared);
            }
        }

        /// <summary>
        /// Part of the rebalancing algorithm. Adds a balanced node to the rebalance array. 
        /// </summary>
        /// <param name="rebalanceArray">Rebalance array to insert into.</param>
        /// <param name="balancedNode">Node to add.</param>
        private static void AddBalancedNodeToRebalanceArray(Node<T>[] rebalanceArray, Node<T> balancedNode)
        {
            int slot;
            int count;
            Node<T> accum = null;
            Debug.Assert(balancedNode.IsBalanced());

            count = balancedNode.Count;
            slot = 0;
            while (count >= FIBONACCI[slot + 1])
            {
                Node<T> n = rebalanceArray[slot];
                if (n != null)
                {
                    rebalanceArray[slot] = null;
                    if (accum == null)
                        accum = n;
                    else
                        accum = accum.PrependInPlace(n, null, null);
                }
                ++slot;
            }

            // slot is the location where balancedNode originally ended up, but possibly
            // not the final resting place.
            if (accum != null)
                balancedNode = balancedNode.PrependInPlace(accum, null, null);
            for (; ; )
            {
                Node<T> n = rebalanceArray[slot];
                if (n != null)
                {
                    rebalanceArray[slot] = null;
                    balancedNode = balancedNode.PrependInPlace(n,null, null);
                }

                if (balancedNode.Count < FIBONACCI[slot + 1])
                {
                    rebalanceArray[slot] = balancedNode;
                    break;
                }
                ++slot;
            }

#if DEBUG
            // The above operations should ensure that everything in the rebalance array is now almost balanced.
            for (int i = 0; i < rebalanceArray.Length; ++i)
            {
                if (rebalanceArray[i] != null)
                    Debug.Assert(rebalanceArray[i].IsAlmostBalanced());
            }
#endif //DEBUG
        }

        static internal LeafNode<T> GetMostLeftNode(Node<T> node)
        {
            var current = node;
            while (true)
            {
                if (current.Left == null && current.Right == null)
                {
                    return (LeafNode<T>)current;
                }
                else if (current.Left != null)
                {
                    current = current.Left;
                }
                else
                {
                    current = current.Right;
                }
            }
        }

        static internal LeafNode<T> GetMostRightNode(Node<T> node)
        {
            var current = node;
            while (true)
            {
                if (current.Left == null && current.Right == null)
                {
                    return (LeafNode<T>)current;
                }
                else if (current.Right != null)
                {
                    current = current.Right;
                }
                else
                {
                    current = current.Left;
                }
            }
        }

        public void AddToFront(T item)
        {
            if ((uint)Count + 1 > MAXITEMS)
                throw new InvalidOperationException("too large");

            if (root == null)
            {
                var newLeaf = new LeafNode<T>(item);
                root = newLeaf;
                leafNodeEnumrator.AddLast(newLeaf);

            }
            else
            {
                Node<T> newRoot = root.PrependInPlace(item, leafNodeEnumrator);
                if (newRoot != root)
                {
                    root = newRoot;
                    CheckBalance();
                }
            }
            ResetFetchCache();
        }

        public void Add(T item)
        {
            if ((uint)Count + 1 > MAXITEMS)
                throw new InvalidOperationException("too large");

            if (root == null)
            {
                var newLeaf = new LeafNode<T>(item);
                root = newLeaf;
                leafNodeEnumrator.AddLast(newLeaf);
            }
            else
            {
                Node<T> newRoot = root.AppendInPlace(item, leafNodeEnumrator);
                if (newRoot != root)
                {
                    root = newRoot;
                    CheckBalance();
                }
            }
            ResetFetchCache();
        }

        private static LeafNode<T> LeafFromEnumerator(IEnumerator<T> enumerator,int collection_count)
        {
            int i = 0;
            FixedList<T> items = null;

            while (i < MAXLEAF && enumerator.MoveNext())
            {
                if (i == 0)
                {
                    if(collection_count < MAXLEAF)
                        items = new FixedList<T>(collection_count, MAXLEAF);
                    else
                        items = new FixedList<T>(MAXLEAF, MAXLEAF);
                }

                if (items != null)
                {
                    items.Add(enumerator.Current);
                    i++;
                }
            }

            if (items != null)
                return new LeafNode<T>(i, items);
            else
                return null;
        }

        private static Node<T> NodeFromEnumerable(IEnumerable<T> collection, LeafNodeEnumrator<T> leafNodeEnumrator)
        {
            Node<T> node = null;
            LeafNode<T> leaf;
            IEnumerator<T> enumerator = collection.GetEnumerator();

            int collection_count;
            if(collection.TryGetNonEnumeratedCount(out collection_count) == false)
            {
                collection_count = collection.Count();
            }

            while ((leaf = LeafFromEnumerator(enumerator, collection_count)) != null)
            {
                leafNodeEnumrator.AddLast(leaf);
                if (node == null)
                {
                    node = leaf;
                }
                else
                {
                    if ((uint)(node.Count) + (uint)(leaf.Count) > MAXITEMS)
                        throw new InvalidOperationException("too large");

                    //このメソッドでもリンクドリストに追加されるがこのメソッドで追加すると後の処理が面倒になる
                    node = node.AppendInPlace(leaf, null, null);
                }
            }

            return node;
        }

        public void AddRange(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            var tempLeafNodeEnumrator = new LeafNodeEnumrator<T>();
            Node<T> node = NodeFromEnumerable(collection, tempLeafNodeEnumrator);
            if (node == null)
                return;
            else if (root == null)
            {
                root = node;
                leafNodeEnumrator = tempLeafNodeEnumrator;
                CheckBalance();
            }
            else
            {
                if ((uint)Count + (uint)node.Count > MAXITEMS)
                    throw new InvalidOperationException("too large");

                Node<T> newRoot = root.AppendInPlace(node, leafNodeEnumrator,tempLeafNodeEnumrator);
                if (newRoot != root)
                {
                    root = newRoot;
                    CheckBalance();
                }
            }
            ResetFetchCache();
        }

        public void AddRangeToFront(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            var tempLeafNodeEnumrator = new LeafNodeEnumrator<T>();
            Node<T> node = NodeFromEnumerable(collection, tempLeafNodeEnumrator);
            if (node == null)
                return;
            else if (root == null)
            {
                root = node;
                leafNodeEnumrator = tempLeafNodeEnumrator;
                CheckBalance();
            }
            else
            {
                if ((uint)Count + (uint)node.Count > MAXITEMS)
                    throw new InvalidOperationException("too large");

                Node<T> newRoot = root.PrependInPlace(node, leafNodeEnumrator, tempLeafNodeEnumrator);
                if (newRoot != root)
                {
                    root = newRoot;
                    CheckBalance();
                }
            }
            ResetFetchCache();
        }

        public void Clear()
        {
            this.root = null;
            this.leafNodeEnumrator.Clear();
            ResetFetchCache();
        }

        public bool Contains(T item)
        {
            return (IndexOf(item) >= 0);
        }

        public IEnumerable<T> GetRangeEnumerable(int index, int count)
        {
            if (count == 0)
                yield break;

            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException("index");
            if (count < 0 || count > Count - index)
                throw new ArgumentOutOfRangeException("count");

            int relativeIndex;
            var node = (LeafNode<T>)IndexOfNode(index, out relativeIndex);
            var items = node.items.Skip(relativeIndex).ToArray();
            if (count > items.Length)
            {
                foreach (var item in items)
                    yield return item;
            }
            else
            {
                foreach (var item in items.Take(count))
                    yield return item;
                yield break;
            }

            int leftCount = count - items.Length;
            LeafNode<T> current = node.Next;
            while (leftCount > 0 && current != null)
            {
                var currentItems = current.items;
                if (leftCount > currentItems.Count)
                {
                    foreach (var item in currentItems)
                        yield return item;
                }
                else if (leftCount > 0)
                {
                    foreach (var item in currentItems.Take(leftCount))
                        yield return item;
                }
                leftCount -= currentItems.Count;
                current = current.Next;
            }
        }

        public BigList<T> GetRange(int index, int count)
        {
            if (count == 0)
                return new BigList<T>();

            var newList = new BigList<T>();
            newList.AddRange(GetRangeEnumerable(index, count));

            return newList;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            int count = this.Count;

            if (count == 0)
                return;

            if (array == null)
                throw new ArgumentNullException("array");
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException("arrayIndex must not be negative");
            if (arrayIndex >= array.Length || count > array.Length - arrayIndex)
                throw new ArgumentException("array too small");

            int index = arrayIndex, i = 0;
            foreach (T item in this)
            {
                if (i >= count)
                    break;

                array[index] = item;
                ++index;
                ++i;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            if ((uint)Count + 1 > MAXITEMS)
                throw new InvalidOperationException("too large");

            //どうせMAXITEMSまでしか保持できないので、インデクサーで取得しても問題はない
            foreach (var node in leafNodeEnumrator)
            {
                foreach(T item in node.items)
                {
                    yield return item;
                }
            }
        }

        public int IndexOf(T item)
        {
            int index = 0;
            foreach (T x in this)
            {
                if (EqualityComparer<T>.Default.Equals(x, item))
                {
                    return index;
                }
                ++index;
            }

            // didn't find any item that matches.
            return -1;
        }

        public void Insert(int index, T item)
        {
            if ((uint)Count + 1 > MAXITEMS)
                throw new InvalidOperationException("too large");

            if (index <= 0 || index >= Count)
            {
                if (index == 0)
                    AddToFront(item);
                else if (index == Count)
                    Add(item);
                else
                    throw new ArgumentOutOfRangeException("index");
            }
            else
            {
                if (root == null)
                {
                    var newLeafNode = new LeafNode<T>(item);
                    root = newLeafNode;
                    leafNodeEnumrator.AddLast(newLeafNode);
                }
                else
                {
                    Node<T> newRoot = root.InsertInPlace(index, item, leafNodeEnumrator);
                    if (newRoot != root)
                    {
                        root = newRoot;
                        CheckBalance();
                    }
                }
            }
            ResetFetchCache();
        }

        public void InsertRange(int index, IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            if (index <= 0 || index >= Count)
            {
                if (index == 0)
                    AddRangeToFront(collection);
                else if (index == Count)
                    AddRange(collection);
                else
                    throw new ArgumentOutOfRangeException("index");
            }
            else
            {
                var tempLeafNodeEnumrator = new LeafNodeEnumrator<T>();
                Node<T> node = NodeFromEnumerable(collection, tempLeafNodeEnumrator);
                if (node == null)
                    return;
                else if (root == null)
                    root = node;
                else
                {
                    if ((uint)Count + (uint)node.Count > MAXITEMS)
                        throw new InvalidOperationException("too large");

                    Node<T> newRoot = root.InsertInPlace(index, node, leafNodeEnumrator, tempLeafNodeEnumrator);
                    if (newRoot != root)
                    {
                        root = newRoot;
                        CheckBalance();
                    }
                }
            }
            ResetFetchCache();
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void RemoveAt(int index)
        {
            RemoveRange(index, 1);
        }

        public void RemoveRange(int index, int count)
        {
            if (count == 0)
                return;              // nothing to do.
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException("index");
            if (count < 0 || count > Count - index)
                throw new ArgumentOutOfRangeException("count");

            Node<T> newRoot = root.RemoveRangeInPlace(index, index + count - 1, leafNodeEnumrator);
            if (newRoot != root)
            {
                root = newRoot;
                CheckBalance();
            }
            ResetFetchCache();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
