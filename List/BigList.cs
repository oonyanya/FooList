/*
 *  Copy from
 *  https://github.com/timdetering/Wintellect.PowerCollections
 *  Fooproject modify
 */
//#define MODIFY_NODE_BY_RECURSIVE
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.VisualBasic;
using FooProject.Collection.DataStore;

namespace FooProject.Collection
{
    public class BigListArgs<T>
    {
        public ICustomConverter<T> CustomConverter { get; set; }
        public ICustomBuilder<T> CustomBuilder { get; set; }

        public int BlockSize { get; set; }

        public BigListArgs(ICustomBuilder<T> builder, ICustomConverter<T> conv, int blockSize) 
        {
            CustomConverter = conv;
            CustomBuilder = builder;
            BlockSize = blockSize;
        }
    }

    /// <summary>
    /// List for huge items.
    /// </summary>
    /// <remarks>
    /// BigList is for only single thread.
    /// </remarks>
    /// <typeparam name="T">The item type of the collection.</typeparam>
    public class BigList<T> : ReadOnlyList<T>, IList<T>
    {
        internal const long MAXITEMS = int.MaxValue - 1;    // maximum number of items in a BigList.
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
        Node<T> _root;
        LeafNodeEnumrator<T> leafNodeEnumrator = new LeafNodeEnumrator<T>();

        private protected Node<T> Root {  get { return _root; } }

        public BigList()
        {
            _root = null;
            var custom = new DefaultCustomConverter<T>();
            custom.DataStore = new MemoryPinableContentDataStore<FixedList<T>>();
            CustomConverter = custom;
            CustomBuilder = custom;
            MaxCapacity = MAXITEMS;
            BlockSize = MAXLEAF;
        }

        public BigList(IEnumerable<T> items) : this()
        {
            AddRange(items);
        }

        /// <summary>
        /// get or set block size
        /// </summary>
        /// <remarks>
        /// It represent block size in each leaf. 
        /// </remarks>
        public int BlockSize
        {
            get; set;
        }

        /// <summary>
        /// get or set max items
        /// </summary>
        public long MaxCapacity
        {
            get; set;
        }

        public ICustomConverter<T> CustomConverter { get; set; }

        public ICustomBuilder<T> CustomBuilder { get; set; }

        public new T this[int index]
        {
            get
            {
                return Get(index);
            }
            set
            {
                Set(index, value);
            }
        }

        public virtual T Get(long index)
        {
            // This could just be a simple call to GetAt on the root.
            // It is recoded as an interative algorithm for performance.

            if (_root == null || index < 0 || index >= _root.Count)
                throw new ArgumentOutOfRangeException("index");

            long relativeIndex;
            LeafNode<T> curLeaf = (LeafNode<T>)IndexOfNode(index, out relativeIndex);
            checked
            {
                using (var pinnedContent = CustomBuilder.DataStore.Get(curLeaf.container))
                {
                    var items = pinnedContent.Content;
                    return items[(int)relativeIndex];
                }
            }
        }

        public virtual void Set(long index, T value)
        {
            // This could just be a simple call to SetAtInPlace on the root.
            // It is recoded as an interative algorithm for performance.

            if (_root == null || index < 0 || index >= _root.Count)
                throw new ArgumentOutOfRangeException("index");

            long relativeIndex;
            LeafNode<T> curLeaf = (LeafNode<T>)IndexOfNode(index, out relativeIndex);
            checked
            {
                using (var pinnedContent = CustomBuilder.DataStore.Get(curLeaf.container))
                {
                    var items = pinnedContent.Content;
                    items[(int)relativeIndex] = value;
                }
            }
        }

        private Node<T> IndexOfNode(long index,out long resultRelativeIndex)
        {
            long relativeIndex;
            if (CustomConverter.LeastFetch != null)
            {
                relativeIndex = index - CustomConverter.LeastFetch.TotalLeftCount;
                if (relativeIndex >= 0 && relativeIndex < CustomConverter.LeastFetch.Node.Count)
                {
                    resultRelativeIndex = relativeIndex;
                    return CustomConverter.LeastFetch.Node;
                }
            }

            relativeIndex = index;
            var node =  WalkNode((current, leftCount) => {
                if (relativeIndex < leftCount)
                {
                    return NodeWalkDirection.Left;
                }
                else
                {
                    relativeIndex -= leftCount;
                    return NodeWalkDirection.Right;
                }
            });

            resultRelativeIndex = relativeIndex;

            return node;
        }

        protected Node<T> WalkNode(Func<Node<T>,long,NodeWalkDirection> fn)
        {
            Node<T> current = _root;
            CustomConverter.ResetState();
            CustomConverter.SetState(null, 0);
            long totalLeftCount = 0;

            while (current != null)
            {
                if (current.Left != null)
                {
                    long leftCount = current.Left.Count;
                    var direction = fn(current, leftCount);
                    if (direction == NodeWalkDirection.Left)
                    {
                        current = current.Left;
                    }
                    else
                    {
                        totalLeftCount += leftCount;
                        current = current.Right;
                    }
                }
                else if (current.Right != null)
                {
                    throw new InvalidOperationException("Left node is null but right node is not null.somthing wrong.");
                }
                else
                {
                    break;
                }
            }
            CustomConverter.SetState(current, totalLeftCount);
            return current;
        }

        private void ResetFetchCache()
        {
            //このメソッドが呼び出された時点で何かしらの操作がされているのでキャッシュはいったんリセットする
            CustomConverter.ResetState();
        }

        public override int Count
        {
            get
            {
                if (_root == null)
                    return 0;
                else
                    return (int)_root.Count;

            }
        }

        public long LongCount
        {
            get
            {
                if (_root == null)
                    return 0;
                else
                    return _root.Count;

            }
        }

        //ReadOnlyCollectionにキャストしたときはtrueにしたいのでこうする
        public new bool IsReadOnly { get { return false; } }

        private void CheckBalance()
        {
            if (_root != null &&
                (_root.Depth > BALANCEFACTOR && !(_root.Depth - BALANCEFACTOR <= MAXFIB && _root.NodeCount >= FIBONACCI[_root.Depth - BALANCEFACTOR])))
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

            if (_root == null)
                return;
            if (_root.Depth <= 1 || (_root.Depth - 2 <= MAXFIB && _root.NodeCount >= FIBONACCI[_root.Depth - 2]))
                return;      // already sufficiently balanced.

            // How many slots does the rebalance array need?
            for (slots = 0; slots <= MAXFIB; ++slots)
                if (_root.NodeCount < FIBONACCI[slots])
                    break;
            rebalanceArray = new Node<T>[slots];

            // Add all the nodes to the rebalance array.
            AddNodeToRebalanceArray(rebalanceArray, _root, false);

            // Concatinate all the node in the rebalance array.
            Node<T> result = null;
            var args = new BigListArgs<T>(CustomBuilder, CustomConverter, this.BlockSize);
            for (int slot = 0; slot < slots; ++slot)
            {
                Node<T> n = rebalanceArray[slot];
                if (n != null)
                {
                    if (result == null)
                        result = n;
                    else
                        result = result.PrependInPlace(n, null, null, args);
                }
            }

            // And we're done. Check that it worked!
            _root = result;
            Debug.Assert(_root.Depth <= 1 || (_root.Depth - 2 <= MAXFIB && _root.NodeCount >= FIBONACCI[_root.Depth - 2]));
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
#if MODIFY_NODE_BY_RECURSIVE
            Stack<Node<T>> stack = new Stack<Node<T>>(MAXFIB);
            stack.Push(node);
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current.IsBalanced())
                {
                    var args = new BigListArgs<T>(CustomBuilder, CustomConverter, this.BlockSize);
                    AddBalancedNodeToRebalanceArray(rebalanceArray, current, args);
                }
                else
                {
                    ConcatNode<T> n = (ConcatNode<T>)current;          // leaf nodes are always balanced.
                    stack.Push(n.Right);
                    stack.Push(n.Left);
                }
            }
#else
            if (node.IsBalanced())
            {
                var args = new BigListArgs<T>(CustomBuilder, CustomConverter, this.BlockSize);
                AddBalancedNodeToRebalanceArray(rebalanceArray, node, args);
            }
            else
            {
                ConcatNode<T> n = (ConcatNode<T>)node;          // leaf nodes are always balanced.
                AddNodeToRebalanceArray(rebalanceArray, n.Left, shared);
                AddNodeToRebalanceArray(rebalanceArray, n.Right, shared);
            }
#endif
        }

        /// <summary>
        /// Part of the rebalancing algorithm. Adds a balanced node to the rebalance array. 
        /// </summary>
        /// <param name="rebalanceArray">Rebalance array to insert into.</param>
        /// <param name="balancedNode">Node to add.</param>
        private static void AddBalancedNodeToRebalanceArray(Node<T>[] rebalanceArray, Node<T> balancedNode, BigListArgs<T> args)
        {
            int slot;
            long count;
            Node<T> accum = null;
            Debug.Assert(balancedNode.IsBalanced());

            count = balancedNode.NodeCount;
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
                        accum = accum.PrependInPlace(n, null, null, args);
                }
                ++slot;
            }

            // slot is the location where balancedNode originally ended up, but possibly
            // not the final resting place.
            if (accum != null)
                balancedNode = balancedNode.PrependInPlace(accum, null, null, args);
            for (; ; )
            {
                Node<T> n = rebalanceArray[slot];
                if (n != null)
                {
                    rebalanceArray[slot] = null;
                    balancedNode = balancedNode.PrependInPlace(n,null, null, args);
                }

                if (balancedNode.NodeCount < FIBONACCI[slot + 1])
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
            if (LongCount + 1 > MaxCapacity)
                throw new InvalidOperationException("too large");

            if (_root == null)
            {
                var newLeaf = CustomBuilder.CreateLeafNode(item, this.BlockSize);
                _root = newLeaf;
                leafNodeEnumrator.AddLast(newLeaf);

            }
            else
            {
                var args = new BigListArgs<T>(CustomBuilder, CustomConverter, this.BlockSize);
                Node<T> newRoot = _root.PrependInPlace(item, leafNodeEnumrator, args);
                if (newRoot != _root)
                {
                    _root = newRoot;
                    CheckBalance();
                }
            }
            ResetFetchCache();
        }

        public new void Add(T item)
        {
            if (LongCount + 1 > MaxCapacity)
                throw new InvalidOperationException("too large");

            if (_root == null)
            {
                var newLeaf = CustomBuilder.CreateLeafNode(item, this.BlockSize);
                _root = newLeaf;
                leafNodeEnumrator.AddLast(newLeaf);
            }
            else
            {
                var args = new BigListArgs<T>(CustomBuilder, CustomConverter, this.BlockSize);
                Node<T> newRoot = _root.AppendInPlace(item, leafNodeEnumrator, args);
                if (newRoot != _root)
                {
                    _root = newRoot;
                    CheckBalance();
                }
            }
            ResetFetchCache();
        }

        private static LeafNode<T> LeafFromEnumerator(IEnumerator<T> enumerator,int collection_count,BigListArgs<T> args)
        {
            int i = 0;
            FixedList<T> items = null;

            while (i < args.BlockSize && enumerator.MoveNext())
            {
                if (i == 0)
                {
                    if(collection_count < args.BlockSize)
                        items = args.CustomBuilder.CreateList(collection_count, args.BlockSize);
                    else
                        items = args.CustomBuilder.CreateList(args.BlockSize, args.BlockSize);
                }

                if (items != null)
                {
                    items.Add(enumerator.Current);
                    i++;
                }
            }

            if (items != null)
            {
                var leafNode = args.CustomBuilder.CreateLeafNode(i, items);
                leafNode.NotifyUpdate(0, items.Count, args);
                return leafNode;

            }
            else
            {
                return null;
            }
        }

        private Node<T> NodeFromEnumerable(IEnumerable<T> collection, LeafNodeEnumrator<T> leafNodeEnumrator,BigListArgs<T> args)
        {
            Node<T> node = null;
            LeafNode<T> leaf;
            IEnumerator<T> enumerator = collection.GetEnumerator();

            int collection_count;
#if NET6_0_OR_GREATER
            if(collection.TryGetNonEnumeratedCount(out collection_count) == false)
            {
                collection_count = collection.Count();
            }
#else
                collection_count = collection.Count();
#endif

            while ((leaf = LeafFromEnumerator(enumerator, collection_count, args)) != null)
            {
                leafNodeEnumrator.AddLast(leaf);
                if (node == null)
                {
                    node = leaf;
                }
                else
                {
                    if (node.Count + leaf.Count > MaxCapacity)
                        throw new InvalidOperationException("too large");

                    //このメソッドでもリンクドリストに追加されるがこのメソッドで追加すると後の処理が面倒になる
                    node = node.AppendInPlace(leaf, null, null, args);
                }
            }

            return node;
        }

        /// <summary>
        /// Add collections.
        /// </summary>
        /// <param name="collection"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <remarks>The collection's count must be within int.Maxvalue - 1</remarks>
        public void AddRange(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            var tempLeafNodeEnumrator = new LeafNodeEnumrator<T>();
            var args = new BigListArgs<T>(CustomBuilder, CustomConverter, this.BlockSize);
            Node<T> node = NodeFromEnumerable(collection, tempLeafNodeEnumrator, args);
            if (node == null)
                return;
            else if (_root == null)
            {
                _root = node;
                leafNodeEnumrator = tempLeafNodeEnumrator;
                CheckBalance();
            }
            else
            {
                if (LongCount + node.Count > MaxCapacity)
                    throw new InvalidOperationException("too large");

                Node<T> newRoot = _root.AppendInPlace(node, leafNodeEnumrator,tempLeafNodeEnumrator, args);
                if (newRoot != _root)
                {
                    _root = newRoot;
                    CheckBalance();
                }
            }
            ResetFetchCache();
        }

        /// <summary>
        /// Add collections before first element.
        /// </summary>
        /// <param name="collection"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <remarks>The collection's count must be within int.Maxvalue - 1</remarks>
        public void AddRangeToFront(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            var tempLeafNodeEnumrator = new LeafNodeEnumrator<T>();
            var args = new BigListArgs<T>(CustomBuilder, CustomConverter, this.BlockSize);
            Node<T> node = NodeFromEnumerable(collection, tempLeafNodeEnumrator, args);
            if (node == null)
                return;
            else if (_root == null)
            {
                _root = node;
                leafNodeEnumrator = tempLeafNodeEnumrator;
                CheckBalance();
            }
            else
            {
                if (LongCount + node.Count > MaxCapacity)
                    throw new InvalidOperationException("too large");

                Node<T> newRoot = _root.PrependInPlace(node, leafNodeEnumrator, tempLeafNodeEnumrator, args);
                if (newRoot != _root)
                {
                    _root = newRoot;
                    CheckBalance();
                }
            }
            ResetFetchCache();
        }

        public new void Clear()
        {
            this._root = null;
            this.leafNodeEnumrator.Clear();
            ResetFetchCache();
        }

        public IEnumerable<T> GetRangeEnumerable(long index, long count)
        {
            if (count == 0)
                yield break;

            if (index < 0 || index >= LongCount)
                throw new ArgumentOutOfRangeException("index");
            if (count < 0 || count > LongCount - index)
                throw new ArgumentOutOfRangeException("count");

            long relativeIndex;
            var node = (LeafNode<T>)IndexOfNode(index, out relativeIndex);
            long nodeItemsLength;
            using (var pinnedContent = CustomBuilder.DataStore.Get(node.container))
            {
                var nodeItems = pinnedContent.Content;
                var items = nodeItems.Skip((int)relativeIndex).ToArray();
                nodeItemsLength = items.Length;
                if (count > items.Length)
                {
                    foreach (var item in items)
                        yield return item;
                }
                else
                {
                    foreach (var item in items.Take((int)count))
                        yield return item;
                    yield break;
                }
            }

            long leftCount = count - nodeItemsLength;
            LeafNode<T> current = node.Next;
            while (leftCount > 0 && current != null)
            {
                using (var pinnedContent = CustomBuilder.DataStore.Get(current.container))
                {
                    var currentItems = pinnedContent.Content;
                    if (leftCount > currentItems.Count)
                    {
                        foreach (var item in currentItems)
                            yield return item;
                    }
                    else if (leftCount > 0)
                    {
                        foreach (var item in currentItems.Take((int)leftCount))
                            yield return item;
                    }
                    leftCount -= currentItems.Count;
                }
                current = current.Next;
            }
        }

        public BigList<T> GetRange(long index, long count)
        {
            if (count == 0)
                return new BigList<T>();

            var newList = new BigList<T>();
            newList.AddRange(GetRangeEnumerable(index, count));

            return newList;
        }

        public ReadOnlyList<T> AsReadOnly()
        {
            return (ReadOnlyList<T>)this;
        }

        public override IEnumerator<T> GetEnumerator()
        {
            if (LongCount + 1 > MaxCapacity)
                throw new InvalidOperationException("too large");

            //どうせMAXITEMSまでしか保持できないので、インデクサーで取得しても問題はない
            foreach (var node in leafNodeEnumrator)
            {
                using (var pinnedContent = CustomBuilder.DataStore.Get(node.container))
                {
                    var nodeItems = pinnedContent.Content;
                    foreach (T item in nodeItems)
                    {
                        yield return item;
                    }
                }
            }
        }

        public new void Insert(int index, T item)
        {
            //　こうしないとスタックオーバーフローになる
            this.Insert((long)index, item);
        }
        public virtual void Insert(long index, T item)
        {
            if (LongCount + 1 > MaxCapacity)
                throw new InvalidOperationException("too large");

            if (index <= 0 || index >= LongCount)
            {
                if (index == 0)
                    AddToFront(item);
                else if (index == LongCount)
                    Add(item);
                else
                    throw new ArgumentOutOfRangeException("index");
            }
            else
            {
                if (_root == null)
                {
                    var newLeafNode = CustomBuilder.CreateLeafNode(item, this.BlockSize);
                    _root = newLeafNode;
                    leafNodeEnumrator.AddLast(newLeafNode);
                }
                else
                {
                    var args = new BigListArgs<T>(CustomBuilder, CustomConverter, this.BlockSize);
                    Node<T> newRoot = _root.InsertInPlace(index, item, leafNodeEnumrator, args);
                    if (newRoot != _root)
                    {
                        _root = newRoot;
                        CheckBalance();
                    }
                }
            }
            ResetFetchCache();
        }

        public void InsertRange(long index, IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            if (index <= 0 || index >= LongCount)
            {
                if (index == 0)
                    AddRangeToFront(collection);
                else if (index == LongCount)
                    AddRange(collection);
                else
                    throw new ArgumentOutOfRangeException("index");
            }
            else
            {
                var tempLeafNodeEnumrator = new LeafNodeEnumrator<T>();
                var args = new BigListArgs<T>(CustomBuilder, CustomConverter, this.BlockSize);
                Node<T> node = NodeFromEnumerable(collection, tempLeafNodeEnumrator, args);
                if (node == null)
                    return;
                else if (_root == null)
                    _root = node;
                else
                {
                    if (LongCount + node.Count > MaxCapacity)
                        throw new InvalidOperationException("too large");

                    Node<T> newRoot = _root.InsertInPlace(index, node, leafNodeEnumrator, tempLeafNodeEnumrator, args);
                    if (newRoot != _root)
                    {
                        _root = newRoot;
                        CheckBalance();
                    }
                }
            }
            ResetFetchCache();
        }

        public new bool Remove(T item)
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

        public new void RemoveAt(int index)
        {
            RemoveRange(index, 1);
        }

        public void RemoveAt(long index)
        {
            RemoveRange(index, 1);
        }

        public void RemoveRange(long index, long count)
        {
            if (count == 0)
                return;              // nothing to do.
            if (index < 0 || index >= LongCount)
                throw new ArgumentOutOfRangeException("index");
            if (count < 0 || count > LongCount - index)
                throw new ArgumentOutOfRangeException("count");

            var args = new BigListArgs<T>(CustomBuilder, CustomConverter, this.BlockSize);
            Node<T> newRoot = _root.RemoveRangeInPlace(index, index + count - 1, leafNodeEnumrator, args);
            if (newRoot != _root)
            {
                _root = newRoot;
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
