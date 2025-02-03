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
    public class BigList<T> : IList<T>
    {
        const uint MAXITEMS = int.MaxValue - 1;    // maximum number of items in a BigList.
        // The fibonacci numbers. Used in the rebalancing algorithm. Final MaxValue makes sure we don't go off the end.
        internal static readonly int[] FIBONACCI = {1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144, 233, 377, 610, 987, 1597, 2584,
            4181, 6765, 10946, 17711, 28657, 46368, 75025, 121393, 196418, 317811, 514229, 832040,
            1346269, 2178309, 3524578, 5702887, 9227465, 14930352, 24157817, 39088169, 63245986,
            102334155, 165580141, 267914296, 433494437, 701408733, 1134903170, 1836311903, int.MaxValue};
        internal const int MAXFIB = 44;  // maximum index in the above, not counting the final MaxValue.
        internal static int MAXLEAF = 8;
        internal const int BALANCEFACTOR = 6;      // how far the root must be in depth from fully balanced to invoke the rebalance operation (min 3).
        Node<T> root;
        LeafNodeEnumrator<T> leafNodeEnumrator = new LeafNodeEnumrator<T>();

        public T this[int index]
        {
            get
            {
                // This could just be a simple call to GetAt on the root.
                // It is recoded as an interative algorithm for performance.

                if (root == null || index < 0 || index >= root.Count)
                    throw new ArgumentOutOfRangeException("index");

                Node<T> current = root;
                ConcatNode<T> curConcat = current as ConcatNode<T>;

                while (curConcat != null)
                {
                    int leftCount = curConcat.Left.Count;
                    if (index < leftCount)
                        current = curConcat.Left;
                    else
                    {
                        current = curConcat.Right;
                        index -= leftCount;
                    }

                    curConcat = current as ConcatNode<T>;
                }

                LeafNode<T> curLeaf = (LeafNode<T>)current;
                return curLeaf.items[index];
            }
            set
            {
                // This could just be a simple call to SetAtInPlace on the root.
                // It is recoded as an interative algorithm for performance.

                if (root == null || index < 0 || index >= root.Count)
                    throw new ArgumentOutOfRangeException("index");

                Node<T> current = root;
                ConcatNode<T> curConcat = current as ConcatNode<T>;

                while (curConcat != null)
                {
                    int leftCount = curConcat.Left.Count;
                    if (index < leftCount)
                    {
                        current = curConcat.Left;
                    }
                    else
                    {
                        current = curConcat.Right;
                        index -= leftCount;
                    }

                    curConcat = current as ConcatNode<T>;
                }

                LeafNode<T> curLeaf = (LeafNode<T>)current;
                curLeaf.items[index] = value;
            }
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
                        result = result.PrependInPlace(n, leafNodeEnumrator);
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
                        accum = accum.PrependInPlace(n, null);
                }
                ++slot;
            }

            // slot is the location where balancedNode originally ended up, but possibly
            // not the final resting place.
            if (accum != null)
                balancedNode = balancedNode.PrependInPlace(accum, null);
            for (; ; )
            {
                Node<T> n = rebalanceArray[slot];
                if (n != null)
                {
                    rebalanceArray[slot] = null;
                    balancedNode = balancedNode.PrependInPlace(n,null);
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


        public void AddToFront(T item)
        {
            if ((uint)Count + 1 > MAXITEMS)
                throw new InvalidOperationException("too large");

            if (root == null)
                root = new LeafNode<T>(item);
            else
            {
                Node<T> newRoot = root.PrependInPlace(item, leafNodeEnumrator);
                if (newRoot != root)
                {
                    root = newRoot;
                    CheckBalance();
                }
            }
        }

        public void Add(T item)
        {
            if ((uint)Count + 1 > MAXITEMS)
                throw new InvalidOperationException("too large");

            if (root == null)
                root = new LeafNode<T>(item);
            else
            {
                Node<T> newRoot = root.AppendInPlace(item, leafNodeEnumrator);
                if (newRoot != root)
                {
                    root = newRoot;
                    CheckBalance();
                }
            }
        }

        private static LeafNode<T> LeafFromEnumerator(IEnumerator<T> enumerator)
        {
            int i = 0;
            List<T> items = null;

            while (i < MAXLEAF && enumerator.MoveNext())
            {
                if (i == 0)
                    items = new List<T>(MAXLEAF);

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

            while ((leaf = LeafFromEnumerator(enumerator)) != null)
            {
                if (node == null)
                    node = leaf;
                else
                {
                    if ((uint)(node.Count) + (uint)(leaf.Count) > MAXITEMS)
                        throw new InvalidOperationException("too large");

                    node = node.AppendInPlace(leaf, leafNodeEnumrator);
                }
            }

            return node;
        }

        public void AddRange(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            Node<T> node = NodeFromEnumerable(collection, leafNodeEnumrator);
            if (node == null)
                return;
            else if (root == null)
            {
                root = node;
                CheckBalance();
            }
            else
            {
                if ((uint)Count + (uint)node.Count > MAXITEMS)
                    throw new InvalidOperationException("too large");

                Node<T> newRoot = root.AppendInPlace(node, leafNodeEnumrator);
                if (newRoot != root)
                {
                    root = newRoot;
                    CheckBalance();
                }
            }
        }

        public void AddRangeToFront(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            Node<T> node = NodeFromEnumerable(collection, leafNodeEnumrator);
            if (node == null)
                return;
            else if (root == null)
            {
                root = node;
                CheckBalance();
            }
            else
            {
                if ((uint)Count + (uint)node.Count > MAXITEMS)
                    throw new InvalidOperationException("too large");

                Node<T> newRoot = root.PrependInPlace(node, leafNodeEnumrator);
                if (newRoot != root)
                {
                    root = newRoot;
                    CheckBalance();
                }
            }
        }

        public void Clear()
        {
            this.root = null;
            this.leafNodeEnumrator.Clear();
        }

        public bool Contains(T item)
        {
            return (IndexOf(item) >= 0);
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
            /*
             まだ何も追加してない
            foreach(var node in leafNodeEnumrator)
            {
                foreach (T item in node.items)
                    yield return item;
            }
            */
            int start = 0, maxItems = int.MaxValue - 1;
            if (root != null && maxItems > 0)
            {
                ConcatNode<T>[] stack = new ConcatNode<T>[root.Depth];
                bool[] leftStack = new bool[root.Depth];
                int stackPtr = 0, startIndex = 0;
                Node<T> current = root;
                LeafNode<T> currentLeaf;
                ConcatNode<T> currentConcat;

                if (start != 0)
                {
                    // Set current to the node containing start, and set startIndex to
                    // the index within that node.
                    if (start < 0 || start >= root.Count)
                        throw new ArgumentOutOfRangeException("start");

                    currentConcat = current as ConcatNode<T>;
                    startIndex = start;
                    while (currentConcat != null)
                    {
                        stack[stackPtr] = currentConcat;

                        int leftCount = currentConcat.Left.Count;
                        if (startIndex < leftCount)
                        {
                            leftStack[stackPtr] = true;
                            current = currentConcat.Left;
                        }
                        else
                        {
                            leftStack[stackPtr] = false;
                            current = currentConcat.Right;
                            startIndex -= leftCount;
                        }

                        ++stackPtr;
                        currentConcat = current as ConcatNode<T>;
                    }
                }

                for (; ; )
                {
                    // If not already at a leaf, walk to the left to find a leaf node.
                    while ((currentConcat = current as ConcatNode<T>) != null)
                    {
                        stack[stackPtr] = currentConcat;
                        leftStack[stackPtr] = true;
                        ++stackPtr;
                        current = currentConcat.Left;
                    }

                    // Iterate the leaf.
                    currentLeaf = (LeafNode<T>)current;

                    int limit = currentLeaf.Count;
                    if (limit > startIndex + maxItems)
                        limit = startIndex + maxItems;

                    for (int i = startIndex; i < limit; ++i)
                    {
                        yield return currentLeaf.items[i];
                    }

                    // Update the number of items to interate.
                    maxItems -= limit - startIndex;
                    if (maxItems <= 0)
                        yield break;    // Done!

                    // From now on, start enumerating at 0.
                    startIndex = 0;

                    // Go back up the stack until we find a place to the right
                    // we didn't just come from.
                    for (; ; )
                    {
                        ConcatNode<T> parent;
                        if (stackPtr == 0)
                            yield break;        // iteration is complete.

                        parent = stack[--stackPtr];
                        if (leftStack[stackPtr])
                        {
                            leftStack[stackPtr] = false;
                            ++stackPtr;
                            current = parent.Right;
                            break;
                        }

                        current = parent;
                        // And keep going up...
                    }

                    // current is now a new node we need to visit. Loop around to get it.
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
                    root = new LeafNode<T>(item);
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
                Node<T> node = NodeFromEnumerable(collection, leafNodeEnumrator);
                if (node == null)
                    return;
                else if (root == null)
                    root = node;
                else
                {
                    if ((uint)Count + (uint)node.Count > MAXITEMS)
                        throw new InvalidOperationException("too large");

                    Node<T> newRoot = root.InsertInPlace(index, node, leafNodeEnumrator);
                    if (newRoot != root)
                    {
                        root = newRoot;
                        CheckBalance();
                    }
                }
            }
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
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
