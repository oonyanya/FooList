// file: RleArray.cs
// brief: Array of items which compresses contents with RLE compression method.
//=========================================================
//#define RLE_ARRAY_ENABLE_SANITY_CHECK
using System;
using System.Collections;
using System.Collections.Generic;
using FooProject.Collection;
using ConditionalAttribute = System.Diagnostics.ConditionalAttribute;
using Debug = System.Diagnostics.Debug;
using StringBuilder = System.Text.StringBuilder;

namespace Sgry.Azuki
{
    public class RleArray<J> : RleArrayBase<BigRleArrayRangeItem<J>>
    {
        protected override BigRleArrayRangeItem<J> CreateItem(BigRleArrayRangeItem<J> value, long start = -1, long length = -1)
        {
            var actual_start = 1L;
            var actual_length = 1L;
            if (start != -1)
                actual_start = start;
            if (length != -1)
                actual_start = length;
            return new BigRleArrayRangeItem<J>(value.Value, actual_start, actual_length);
        }

        /// <summary>
        /// Returns a string that represents this object.
        /// </summary>
        public override string ToString()
        {
            StringBuilder str = new StringBuilder();
            if (0 < Count)
            {
                str.Append(this[0].Value.ToString());
                for (int i = 1; i < Count; i++)
                {
                    str.Append(" " + this[i].Value.ToString());
                }
            }
            return str.ToString();
        }

        public string ToRleString()
        {
            System.Text.StringBuilder buf;
            int count;

            if (Count == 0)
                return String.Empty;

            buf = new System.Text.StringBuilder();
            count = this._Nodes.Count;
            buf.Append(ToNodeString(this._Nodes.Get(0)));
            for (int i = 1; i < count; i++)
                buf.Append(" " + ToNodeString(this._Nodes.Get(i)));
            return buf.ToString();
        }

        private string ToNodeString(BigRleArrayRangeItem<J> node)
        {
            return String.Format("[{0}|{1}]", node.length, node.Value);
        }
    }

    /// <summary>
    /// Array of items which compresses contents with RLE compression method.
    /// </summary>
    public class RleArrayBase<T> : IEnumerable<T> where T : class, IRleArrayRangeItem
    {
        internal BigRangeList<T> _Nodes;
        int _TotalCount = 0;

        #region Init / Dispose
        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public RleArrayBase()
            : this(32)
        { }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public RleArrayBase(int initCapacity)
        {
            _Nodes = new BigRangeList<T>();
            __check_sanity__();
        }
        #endregion

        #region like list

        /// <summary>
        /// アイテムを作成する
        /// </summary>
        /// <param name="value">値</param>
        /// <param name="start">開始インデックス。特に指定してない場合は-1</param>
        /// <param name="length">長さ。特に指定してない場合は-1</param>
        /// <returns>IRleArrayRangeを継承したクラスを返す。nullを返してはならない</returns>
        /// <exception cref="NotImplementedException"></exception>
        protected virtual T CreateItem(T value, long start = -1, long length = -1)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Insert specified amount of specific value.
        /// </summary>
        /// <exception cref='ArgumentOutOfRangeException'>
        ///   Parameter <paramref name="index"/> is out of valid range.
        /// </exception>
        public void Insert(long index, T value, int count)
        {
            T insertedNode;

            Insert(index, value, out insertedNode);
            insertedNode.length += count - 1;
            _TotalCount += count - 1;

            __check_sanity__();
        }

        /// <summary>
        /// Insert the specified value at specified index.
        /// </summary>
        /// <exception cref='ArgumentOutOfRangeException'>
        ///   Parameter <paramref name="index"/> is out of valid range.
        /// </exception>
        public void Insert(long index, T value)
        {
            if (index < 0 || _TotalCount < index)
                throw new ArgumentOutOfRangeException("index");

            T insertedNode;
            Insert(index, value, out insertedNode);
        }

        void Insert(long index, T value, out T insertedNode)
        {
            Debug.Assert(0 <= index && index <= _TotalCount,
                            "index is out of valid range. (index:" + index + ", Count:" + Count + ")");

            long nodeIndex;
            long indexInNode;
            T prevNode;
            T theNode;

            // find the node which contains the insertion point
            FindNodeIndex(index, out nodeIndex, out indexInNode);
            if (nodeIndex < 0 || indexInNode < 0)
            {
                // there is no nodes following.
                Debug.Assert(index == _TotalCount);
                if (0 < _Nodes.Count)
                {
                    prevNode = _Nodes[_Nodes.Count - 1];
                    if (value.EqualsValue(prevNode))
                    {
                        insertedNode = prevNode;
                        prevNode.length++;
                        _TotalCount++;
                    }
                    else
                    {
                        insertedNode = this.CreateItem(value, 1);
                        _Nodes.Add(insertedNode);
                        _TotalCount++;
                    }
                }
                else
                {
                    // Simply add a new node since the array was empty.
                    insertedNode = this.CreateItem(value, 1);
                    _Nodes.Add(insertedNode);
                    _TotalCount++;
                }

                __check_sanity__();
                return;
            }

            // get the node at one previous position
            theNode = _Nodes.Get(nodeIndex);
            prevNode = null;
            if (0 < nodeIndex)
            {
                prevNode = _Nodes.Get(nodeIndex - 1);
            }

            // Insert the value
            if (indexInNode == 0)
            {
                // Take care of both left and right boundaries
                if (value.EqualsValue(theNode))
                {
                    insertedNode = theNode;
                    theNode.length++;
                    _TotalCount++;
                }
                else if (prevNode != null && value.EqualsValue(prevNode))
                {
                    insertedNode = prevNode;
                    prevNode.length++;
                    _TotalCount++;
                }
                else
                {
                    insertedNode = this.CreateItem(value, 1);
                    _Nodes.Insert(nodeIndex, insertedNode);
                    _TotalCount++;
                }
            }
            else
            {
                // Take care of both right boundary
                if (value.EqualsValue(theNode))
                {
                    insertedNode = theNode;
                    theNode.length++;
                    _TotalCount++;
                }
                else
                {
                    var orgNodeLen = theNode.length;

                    // split the node
                    theNode.length = indexInNode;
                    _Nodes.Insert(nodeIndex + 1, this.CreateItem(theNode, orgNodeLen - indexInNode));

                    // insert a new node holding the new value
                    insertedNode = this.CreateItem(value, 1);
                    _Nodes.Insert(nodeIndex + 1, insertedNode);

                    _TotalCount++;
                }
            }

            __check_sanity__();
        }

        /// <summary>
        /// Removes an item at specified index.
        /// </summary>
        /// <param name='index'>Index of the item to be removed.</param>
        /// <exception cref='ArgumentOutOfRangeException'>
        ///   <paramref name="index"/> is out of valid range.
        /// </exception>
        public void RemoveAt(long index)
        {
            if (index < 0 || _TotalCount <= index)
                throw new ArgumentOutOfRangeException("index",
                                                        "Specified index is out of valid range. ("
                                                        + "index:" + index + ", this.Count:" + Count + ")");

            long nodeIndex;
            long indexInNode;

            // find the node which contains the item at specified index
            FindNodeIndex(index, out nodeIndex, out indexInNode);

            // if the node contains only the item to be removed,
            // remove the node itself.
            if (_Nodes.Get(nodeIndex).length <= 1)
            {
                _Nodes.RemoveAt(nodeIndex);
                if (0 <= nodeIndex - 1 && nodeIndex < _Nodes.Count
                    && _Nodes.Get(nodeIndex - 1).EqualsValue(_Nodes.Get(nodeIndex)))
                {
                    // combine this node, the previous node, and the next node
                    _Nodes.Get(nodeIndex - 1).length += _Nodes.Get(nodeIndex).length;
                    _Nodes.RemoveAt(nodeIndex);
                }
            }
            else
            {
                _Nodes.Get(nodeIndex).length--;
            }
            _TotalCount--;

            __check_sanity__();
        }

        /// <summary>
        /// Gets or sets an item at the specified index.
        /// </summary>
        /// <exception cref="System.ArgumentOutOfRangeException"/>
        public T this[int index]
        {
            get
            {
                if (index < 0 || _TotalCount <= index)
                    throw new ArgumentOutOfRangeException();

                var nodeIndex = FindNodeIndex(index);
                Debug.Assert(0 <= nodeIndex,
                                "no node for the specified index was found!"
                                + " (index:" + index + ", Count:" + Count + ")");

                return _Nodes.Get(nodeIndex);
            }
            set
            {
                if (index < 0 || _TotalCount <= index)
                    throw new ArgumentOutOfRangeException();

                long nodeIndex;
                long indexInNode;
                T thisNode, prevNode, nextNode;

                // find the node at the index
                FindNodeIndex(index, out nodeIndex, out indexInNode);
                Debug.Assert(0 <= nodeIndex);

                // if the value at the index was already same as
                // the specified new value, do nothing
                thisNode = _Nodes.Get(nodeIndex);
                if (thisNode.EqualsValue(value))
                {
                    return;
                }

                if (thisNode.length == 1)
                {
                    Debug.Assert(indexInNode == 0);
                    prevNode = (0 <= nodeIndex - 1) ? _Nodes.Get(nodeIndex - 1) : null;
                    nextNode = (nodeIndex + 1 < _Nodes.Count) ? _Nodes.Get(nodeIndex + 1) : null;

                    if (prevNode != null
                        && prevNode.EqualsValue(value))
                    {
                        // combine this node and the previous node if they have same value
                        prevNode.length++; // 1 == thisNode.Length
                        _Nodes.RemoveAt(nodeIndex);

                        // combine this node and the next node if they have same value
                        if (nextNode != null
                            && nextNode.EqualsValue(value))
                        {
                            prevNode.length += nextNode.length;
                            _Nodes.RemoveAt(nodeIndex);
                        }
                    }
                    else if (nextNode != null
                        && nextNode.EqualsValue(value))
                    {
                        // combine this node and the next node
                        _Nodes.Set(nodeIndex,value);
                        _Nodes.Get(nodeIndex).length += nextNode.length;
                        _Nodes.RemoveAt(nodeIndex + 1);
                    }
                    else
                    {
                        // change the value of this node
                        _Nodes.Set(nodeIndex, value);
                    }
                }
                else if (indexInNode == 0) // at left boundary
                {
                    prevNode = (0 <= nodeIndex - 1) ? _Nodes.Get(nodeIndex - 1) : null;
                    if (prevNode != null
                        && prevNode.EqualsValue(value))
                    {
                        // combine this node and the previous node
                        prevNode.length++;
                    }
                    else
                    {
                        // insert a new node which holds the new value
                        _Nodes.Insert(nodeIndex, this.CreateItem(value, 1));
                    }

                    // decrement the length of this node,
                    // or delete this node if it no longer holds data
                    thisNode.length--;
                }
                else if (indexInNode == thisNode.length - 1) // at right boundary
                {
                    nextNode = (nodeIndex + 1 < _Nodes.Count) ? _Nodes.Get(nodeIndex + 1) : null;
                    if (nextNode != null
                        && nextNode.EqualsValue(value))
                    {
                        // combine this node and the next node
                        nextNode.length++;
                    }
                    else
                    {
                        // insert a new node which holds the new value
                        _Nodes.Insert(nodeIndex + 1, this.CreateItem(value, 1));
                    }

                    // decrement the length of this node,
                    // or delete this node if it no longer holds data
                    thisNode.length--;
                }
                else
                {
                    //-- split this node and insert a new node holding the new value --
                    var followingNodeLength = thisNode.length - indexInNode - 1;
                    Debug.Assert(0 < indexInNode);
                    Debug.Assert(0 < followingNodeLength);

                    // chop this node off at the specified index
                    thisNode.length = indexInNode;

                    // insert a new node which holds the new value
                    _Nodes.Insert(nodeIndex + 1, this.CreateItem(value, 1));

                    // insert a new node which holds the same value as this node
                    _Nodes.Insert(nodeIndex + 2, this.CreateItem(thisNode, followingNodeLength));
                }

                __check_sanity__();
            }
        }
        #endregion

        #region ICollection<T> + alpha
        /// <summary>
        /// Add the specified value.
        /// </summary>
        /// <param name='value'>The value to add.</param>
        public void Add(T value)
        {
            Insert(_TotalCount, value);
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            _Nodes.Clear();
            _TotalCount = 0;
            __check_sanity__();
        }

        /// <summary>
        /// Copies the items of this object to an array, starting at the specified index.
        /// </summary>
        /// <param name="array">
        ///   An array the items will be copied to.
        /// </param>
        /// <param name="arrayIndex">
        ///   The index in <paramref name="array"/> at which copying begins.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="array"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="arrayIndex"/> is less than zero.
        /// </exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException("arrayIndex");

            for (int i = 0; i < Count; i++)
            {
                array[arrayIndex + i] = this[i];
            }
        }

        /// <summary>
        /// Gets total number of items stored in this array.
        /// </summary>
        public int Count
        {
            get { return _TotalCount; }
        }

        /// <summary>
        /// 全ての範囲の長さを合計した値を返す
        /// </summary>
        public long TotalRangeCount
        {
            get
            {
                return this._Nodes.TotalRangeCount;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is read only.
        /// </summary>
        /// <value>
        ///   <c>false</c> always since this class can not be read-only.
        /// </value>
        public bool IsReadOnly
        {
            get { return false; }
        }

        #endregion

        #region Behavior as an object
        /// <summary>
        /// Retrieves an enumerator.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Utilities
        /// <summary>
        /// 対応するアイテムのインデックスを取得する
        /// </summary>
        /// <param name="absolute_index">絶対インデックス</param>
        /// <param name="nearest_index">一番近くのアイテムのインデックス</param>
        /// <returns>インデックス</returns>
        public long TryIndexOfNearst(long absolute_index, out long nearest_index, out long indexInNode)
        {
            var index = 0L;
            nearest_index = 0L;

            if (absolute_index == 0)
            {
                if (this._Nodes.Count > 0)
                {
                    indexInNode = 0;
                }
                else
                {
                    nearest_index = -1;
                    indexInNode = -1;
                    return -1;
                }
            }
            else if (absolute_index == this.TotalRangeCount)
            {
                index = this.Count - 1;
                indexInNode = this._Nodes[this.Count - 1].length;
            }
            else
            {
                index = _Nodes.GetIndexFromAbsoluteIndexIntoRange(absolute_index, out nearest_index);
                var CustomConverter = (RangeConverter<T>)_Nodes.LeastFetchStore;
                var headIndexInNode = CustomConverter.customLeastFetch.absoluteIndexIntoRange;
                indexInNode = index - headIndexInNode;
            }

            return index;
        }

        long FindNodeIndex(long index)
        {
            long nodeIndex, indexInNode;

            FindNodeIndex(index, out nodeIndex, out indexInNode);

            return nodeIndex;
        }

        void FindNodeIndex(long index, out long nodeIndex, out long indexInNode)
        {
            /*
            Debug.Assert(0 <= index);

            var sum = 0L;

            for (int i = 0; i < _Nodes.Count; i++)
            {
                var node = _Nodes[i];
                if (index < sum + node.length)
                {
                    // target item is in this RLE node
                    nodeIndex = i;
                    indexInNode = index - sum;
                    return;
                }
                sum += node.length;
            }

            nodeIndex = -1;
            indexInNode = -1;
            */
            var nearest_index = 0L;
            nodeIndex = this.TryIndexOfNearst(index, out nearest_index, out indexInNode);

            if (nodeIndex == -1)
            {
                if (nearest_index == -1)
                {
                    nodeIndex = -1;
                    indexInNode = -1;
                    return;
                }
                nodeIndex = nearest_index;
            }
        }

        [Conditional("TEST")]
        [Conditional("RLE_ARRAY_ENABLE_SANITY_CHECK")]
        void __check_sanity__()
        {
            var count = 0L;

            foreach (var item in _Nodes)
            {
                count += item.length;
                Debug.Assert(0 < item.length);
            }

            Debug.Assert(count == _TotalCount);
        }
        #endregion
    }
}
