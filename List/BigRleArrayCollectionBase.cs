// Import from Azuki Text Editor https://github.com/kkato233/azuki/blob/main/Azuki/Document/RleArray.cs
//=========================================================
//#define RLE_ARRAY_ENABLE_SANITY_CHECK
using FooProject.Collection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using ConditionalAttribute = System.Diagnostics.ConditionalAttribute;
using Debug = System.Diagnostics.Debug;
using StringBuilder = System.Text.StringBuilder;

namespace FooProject.Collection
{
    /// <summary>
    /// IRleArrayRangeインターフェイス
    /// </summary>
    /// <typeparam name="J"></typeparam>
    public interface IRleArrayRangeItem : IRange
    {
        bool EqualsValue(IRleArrayRangeItem other);
    }

    public class BigRleArrayRangeItem<T> : IRleArrayRangeItem
    {
        public T Value { get; set; }
        public long start { get; set; }
        public long length { get; set; }

        public BigRleArrayRangeItem()
        {
        }

        public BigRleArrayRangeItem(T v, long index, long length)
        {
            this.Value = v;
            this.start = index;
            this.length = length;
            this.OnInit(v, index, length);
        }

        public BigRleArrayRangeItem(T v, long length)
        {
            this.Value = v;
            this.length = length;
            this.OnInit(v, start, length);
        }

        public BigRleArrayRangeItem(T v)
        {
            this.Value = v;
            this.length = 1;
            this.OnInit(v, start, length);
        }
        /// <summary>
        /// 初期化時に呼び出される
        /// </summary>
        /// <param name="v"></param>
        /// <param name="index"></param>
        /// <param name="length"></param>
        public virtual void OnInit(T v, long index, long length)
        {
        }

        public IRange DeepCopy()
        {
            var new_item = new BigRleArrayRangeItem<T>();
            new_item.start = start;
            new_item.Value = Value;
            new_item.length = length;
            return new_item;
        }

        public override bool Equals(object obj)
        {
            var other = obj as BigRleArrayRangeItem<T>;
            if (other == null) return false;
            if (other.Value.Equals(this.Value) && other.length == this.length && other.start == this.start) return true;
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool EqualsValue(IRleArrayRangeItem other)
        {
            BigRleArrayRangeItem<T> other_item = (BigRleArrayRangeItem<T>)other;
            return this.Value.Equals(other_item.Value);
        }
    }

    public class BigRleArrayCollection<J> : BigRleArrayCollectionBase<BigRleArrayRangeItem<J>>
    {
        public BigRleArrayCollection() : base()
        {

        }

        public BigRleArrayCollection(int len) : base(len)
        {

        }

        protected override BigRleArrayRangeItem<J> CreateItem(BigRleArrayRangeItem<J> value, long start = -1, long length = -1)
        {
            var actual_start = 1L;
            var actual_length = 1L;
            if (start != -1)
                actual_start = start;
            if (length != -1)
                actual_length = length;
            return new BigRleArrayRangeItem<J>(value.Value, actual_start, actual_length);
        }

        public void AddRange(J item, int count = -1)
        {
            if (count == -1)
                this.AddRange(new BigRleArrayRangeItem<J>(item, 1));
            else
                this.AddRange(new BigRleArrayRangeItem<J>(item, count));
        }

        public void InsertRange(int index, J item, int count = -1)
        {
            if (count == -1)
                this.InsertRange(index, new BigRleArrayRangeItem<J>(item, 1));
            else
                this.InsertRange(index, new BigRleArrayRangeItem<J>(item, count));
        }
        public void UpdateRange(int index, J item, int count = -1)
        {
            if (count == -1)
                this.UpdateRange(index, new BigRleArrayRangeItem<J>(item, 1));
            else
                this.UpdateRange(index, new BigRleArrayRangeItem<J>(item, count));
        }

        /// <summary>
        /// Returns a string that represents this object.
        /// </summary>
        public override string ToString()
        {
            StringBuilder str = new StringBuilder();
            if (0 < this.TotalCount)
            {
                str.Append(this[0].Value.ToString());
                for (int i = 1; i < TotalCount; i++)
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

            if (TotalCount == 0)
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
    public class BigRleArrayCollectionBase<T> : IEnumerable<T> where T : class, IRleArrayRangeItem
    {
        internal BigRangeList<T> _Nodes;
        
        public long TotalCount => _Nodes.TotalRangeCount;

        #region Init / Dispose
        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public BigRleArrayCollectionBase()
            : this(32)
        { }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public BigRleArrayCollectionBase(int initCapacity)
        {
            _Nodes = new BigRangeList<T>();
            __check_sanity__();
        }
        #endregion

        #region like list

        //互換性を保つため
        private T CreateItem(T value, long length = -1)
        {
            return this.CreateItem(value, 0, length);
        }

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
        /// Insert the specified value at specified index.
        /// </summary>
        /// <exception cref='ArgumentOutOfRangeException'>
        ///   Parameter <paramref name="index"/> is out of valid range.
        /// </exception>
        public void InsertRange(long index, T value)
        {
            if (index < 0 || TotalCount < index)
                throw new ArgumentOutOfRangeException("index");

            T insertedNode;
            InsertRange(index, value, out insertedNode);
        }

        void InsertRange(long index, T value, out T insertedNode)
        {
            Debug.Assert(0 <= index && index <= TotalCount,
                            "index is out of valid range. (index:" + index + ", Count:" + TotalCount + ")");

            long nodeIndex;
            long indexInNode;
            long prevNodeIndex;
            T prevNode;
            T theNode;

            // find the node which contains the insertion point
            FindNodeIndex(index, out nodeIndex, out indexInNode);
            if (nodeIndex < 0 || indexInNode < 0)
            {
                // there is no nodes following.
                Debug.Assert(index == TotalCount);
                if (0 < _Nodes.Count)
                {
                    prevNodeIndex = _Nodes.Count - 1;
                    prevNode = _Nodes.Get(prevNodeIndex);
                    if (value.EqualsValue(prevNode))
                    {
                        insertedNode = prevNode;
                        prevNode.length += value.length;
                        _Nodes.Set(prevNodeIndex, prevNode);
                    }
                    else
                    {
                        insertedNode = this.CreateItem(value, value.length);
                        _Nodes.Add(insertedNode);
                    }
                }
                else
                {
                    // Simply add a new node since the array was empty.
                    insertedNode = this.CreateItem(value, value.length);
                    _Nodes.Add(insertedNode);
                }

                __check_sanity__();
                return;
            }

            // get the node at one previous position
            theNode = _Nodes.Get(nodeIndex);
            prevNode = null;
            prevNodeIndex = -1;
            if (0 < nodeIndex)
            {
                prevNode = _Nodes.Get(nodeIndex - 1);
                prevNodeIndex = nodeIndex - 1;
            }

            // Insert the value
            if (indexInNode == 0)
            {
                // Take care of both left and right boundaries
                if (value.EqualsValue(theNode))
                {
                    insertedNode = theNode;
                    theNode.length += value.length;
                    _Nodes.Set(nodeIndex, theNode);
                }
                else if (prevNode != null && value.EqualsValue(prevNode))
                {
                    insertedNode = prevNode;
                    prevNode.length += value.length;
                    _Nodes.Set(prevNodeIndex, prevNode);
                }
                else
                {
                    insertedNode = this.CreateItem(value, value.length);
                    _Nodes.Insert(nodeIndex, insertedNode);
                }
            }
            else
            {
                // Take care of both right boundary
                if (value.EqualsValue(theNode))
                {
                    insertedNode = theNode;
                    theNode.length += value.length;
                    _Nodes.Set(nodeIndex, theNode);
                }
                else
                {
                    var orgNodeLen = theNode.length;

                    // split the node
                    theNode.length = indexInNode;
                    _Nodes.Set(nodeIndex, theNode);
                    _Nodes.Insert(nodeIndex + 1, this.CreateItem(theNode, orgNodeLen - indexInNode));

                    // insert a new node holding the new value
                    insertedNode = this.CreateItem(value, value.length);
                    _Nodes.Insert(nodeIndex + 1, insertedNode);
                }
            }

            __check_sanity__();
        }

        /// <summary>
        /// アイテムを削除します
        /// </summary>
        /// <param name="absolute_index">削除する絶対インデックス</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <remarks>アイテムの長さを変えます。0になった場合、アイテム自体が削除されます。</remarks>
        public void RemoveRange(long absolute_index, long count = 1)
        {
            for (long i = 0; i < count; i++)
                this.RemoveAt(absolute_index);
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
            if (index < 0 || TotalCount <= index)
                throw new ArgumentOutOfRangeException("index",
                                                        "Specified index is out of valid range. ("
                                                        + "index:" + index + ", this.TotalCount:" + TotalCount + ")");

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
                    _Nodes.GetAndUpdate(nodeIndex - 1, (item) => {
                        item.length += _Nodes.Get(nodeIndex).length;
                        return item;
                    });
                    _Nodes.RemoveAt(nodeIndex);
                }
            }
            else
            {
                _Nodes.GetAndUpdate(nodeIndex, (item) => {
                    item.length--;
                    return item;
                });
            }

            __check_sanity__();
        }

        /// <summary>
        ///　対応するIRleArrayRangeを取得する
        /// </summary>
        /// <param name="absolute_index">取得対象の絶対インデックス</param>
        /// <returns></returns>
        public T Get(long absolute_index)
        {
            return this.Get(absolute_index, out _);
        }

        /// <summary>
        ///　対応するIRleArrayRangeを取得する
        /// </summary>
        /// <param name="absolute_index">取得対象の絶対インデックス</param>
        /// <param name="index">IRleArrayRangeが存在するインデックス</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public T Get(long absolute_index, out long index)
        {
            var i = this.GetIndexFromAbsoluteIndexIntoRange(absolute_index);

            var container = _Nodes.Get(i);
            index = i;
            return container;
        }

        /// <summary>
        /// アイテムを取得します
        /// </summary>
        /// <param name="absolute_index">取得対象の絶対インデックス</param>
        /// <returns>絶対インデックスに該当するアイテムを返す</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public T GetValue(long absolute_index)
        {
            var i = this.GetIndexFromAbsoluteIndexIntoRange(absolute_index);

            var container = _Nodes.Get(i);
            return container;
        }

        /// <summary>
        /// アイテムを取得する
        /// </summary>
        /// <param name="index">インデックス</param>
        /// <returns></returns>
        public T GetAt(long index)
        {
            return _Nodes.Get(index);
        }
        
        /// <summary>
        /// 対応するインデックスを取得する
        /// </summary>
        /// <param name="absolute_index">取得対象の絶対インデックス</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public long IndexOf(long absolute_index)
        {
            var i = this.GetIndexFromAbsoluteIndexIntoRange(absolute_index);
            return i;
        }

        /// <summary>
        /// アイテムを更新します
        /// </summary>
        /// <param name="index">更新するインデックス</param>
        /// <param name="item">新しいアイテム</param>
        public void SetAt(long index, T item)
        {
            this._Nodes.Set(index, item);
        }

        /// <summary>
        /// 列挙子を返す
        /// </summary>
        /// <param name="absolute_index">開始インデックス</param>
        /// <param name="count">長さ</param>
        /// <returns>列挙子</returns>
        /// <remarks>IRleArrayRangeのstartとlengthは開始インデックスと長さの範囲内になるように調整されます</remarks>
        public IEnumerable<T> GetRanges(long absolute_index, long count)
        {
            return _Nodes.GetFromAbsoluteIndexIntoRange(absolute_index, count, (item, relative_start, total_fetched_count, left_count) =>
            {
                return this.CreateItem(item, item.start + total_fetched_count, item.length);
            });
        }

        /// <summary>
        /// 列挙子を返す
        /// </summary>
        /// <param name="absolute_index">開始インデックス</param>
        /// <param name="count">長さ</param>
        /// <returns>列挙子</returns>
        /// <remarks>IRleArrayRangeのstartとlengthは開始インデックスと長さの範囲内になるように調整されます</remarks>
        public IEnumerable<T> GetRangesAndClamp(long absolute_index, long count)
        {
            return _Nodes.GetFromAbsoluteIndexIntoRange(absolute_index, count, (item, relative_start, total_fetched_count, left_count) =>
            {
                var clamped_count = item.length;
                if (relative_start > 0)
                {
                    clamped_count = item.length - relative_start;
                    return this.CreateItem(item, item.start + relative_start + total_fetched_count, clamped_count);
                }
                else if (left_count < clamped_count)
                {
                    return this.CreateItem(item, item.start + total_fetched_count, left_count);
                }
                else
                {
                    return this.CreateItem(item, item.start + total_fetched_count, clamped_count);
                }
            });
        }

        /// <summary>
        /// 対応するアイテムのインデックスを取得する
        /// </summary>
        /// <param name="absolute_index">絶対インデックス</param>
        /// <param name="nearest_index">一番近くのアイテムのインデックス</param>
        /// <returns>インデックス</returns>
        public long TryIndexOfNearst(long absolute_index, out long nearest_index)
        {
            var index = 0L;
            nearest_index = 0L;

            if (absolute_index == 0)
            {
                if (this._Nodes.Count > 0)
                {
                }
                else
                {
                    nearest_index = -1;
                    return -1;
                }
            }
            else if (absolute_index == this.TotalCount)
            {
                index = this.Count - 1;
            }
            else
            {
                index = _Nodes.GetIndexFromAbsoluteIndexIntoRange(absolute_index, out nearest_index);
            }

            return index;
        }

        /// <summary>
        /// 対応するアイテムのインデックスを取得する
        /// </summary>
        /// <param name="absolute_index">絶対インデックス</param>
        /// <returns>インデックス</returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <remarks>存在しない場合、一番近くのインデックスを取得します。取得できないときは例外を吐きます</remarks>
        public long GetIndexFromAbsoluteIndexIntoRange(long absolute_index)
        {
            var nearest_index = 0L;
            var index = this.TryIndexOfNearst(absolute_index, out nearest_index);

            if (index == -1)
            {
                if (nearest_index == -1)
                {
                    var str = $"absoulte range is invaild or empty.absolute_index is {absolute_index}";
                    throw new InvalidOperationException(str);
                }
                index = nearest_index;
            }

            return index;
        }

        /// <summary>
        /// デフォルトのアイテム処理用メソッド
        /// </summary>
        /// <param name="container">処理対象のコンテナー</param>
        /// <param name="count">出力すべき数</param>
        /// <param name="input">入力アイテム</param>
        /// <returns>カスタム処理を実装したい場合、continerを複製してください。なお、countとinput_itemの長さが同じ場合は複製する必要はありません</returns>
        protected virtual T defaultProcessItem(T container, long count, T input_item)
        {
            return CreateItem(length: count, value: input_item);
        }

        /// <summary>
        /// アイテムを更新します
        /// </summary>
        /// <param name="absolute_index">更新する絶対インデックス</param>
        /// <param name="count">長さ</param>
        /// <param name="input_value">アイテム</param>
        /// <param name="processItem">処理用のメソッド。nullの場合、単純に上書きされます。arg1は処理対象のコンテナー、arg2は出力すべき数、arg3は入力アイテムを表します。</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <remarks>何もしない場合、input_valueの値で置き換えます。カスタム処理を実装したい場合、continerを複製してください。</remarks>
        public void UpdateRange(long absolute_index, T input_value, Func<T, long, T, T> processItem = null)
        {
            if (processItem == null)
            {
                processItem = defaultProcessItem;
            }

            for(long i = 0; i < input_value.length; i++)
            {
                this.UpdateOne(absolute_index + i, input_value, processItem);
            }
        }

        protected void UpdateOne(long index,T input_value, Func<T, long, T, T> processItem)
        {
            if (index < 0 || TotalCount <= index)
                throw new ArgumentOutOfRangeException();

            long thisNodeIndex;
            long indexInThisNode;
            long prevNodeIndex = -1;
            long nextNodexIndex = -1;
            T thisNode, prevNode, nextNode, value;


            // find the node at the index
            FindNodeIndex(index, out thisNodeIndex, out indexInThisNode);
            Debug.Assert(0 <= thisNodeIndex);

            // if the value at the index was already same as
            // the specified new value, do nothing
            thisNode = _Nodes.Get(thisNodeIndex);
            value = processItem(thisNode, 1, input_value);
            if (thisNode.EqualsValue(value))
            {
                return;
            }

            if (thisNode.length == 1)
            {
                Debug.Assert(indexInThisNode == 0);
                prevNodeIndex = (0 <= thisNodeIndex - 1) ? thisNodeIndex - 1 : -1;
                prevNode = (0 <= thisNodeIndex - 1) ? _Nodes.Get(thisNodeIndex - 1) : null;
                nextNodexIndex = (thisNodeIndex + 1 < _Nodes.Count) ? thisNodeIndex + 1 : -1;
                nextNode = (thisNodeIndex + 1 < _Nodes.Count) ? _Nodes.Get(thisNodeIndex + 1) : null;

                if (prevNode != null
                    && prevNode.EqualsValue(value))
                {
                    // combine this node and the previous node if they have same value
                    prevNode.length++; // 1 == thisNode.Length
                    _Nodes.Set(prevNodeIndex, prevNode);
                    _Nodes.RemoveAt(thisNodeIndex);

                    // combine this node and the next node if they have same value
                    if (nextNode != null
                        && nextNode.EqualsValue(value))
                    {
                        prevNode.length += nextNode.length;
                        _Nodes.Set(prevNodeIndex, prevNode);
                        _Nodes.RemoveAt(thisNodeIndex);
                    }
                }
                else if (nextNode != null
                    && nextNode.EqualsValue(value))
                {
                    // combine this node and the next node
                    _Nodes.Set(thisNodeIndex, value);
                    _Nodes.GetAndUpdate(thisNodeIndex, (item) => {
                        item.length += nextNode.length;
                        return item;
                    });
                    _Nodes.RemoveAt(thisNodeIndex + 1);
                }
                else
                {
                    // change the value of this node
                    _Nodes.Set(thisNodeIndex, value);
                }
            }
            else if (indexInThisNode == 0) // at left boundary
            {
                prevNodeIndex = (0 <= thisNodeIndex - 1) ? thisNodeIndex - 1 : -1;
                prevNode = (0 <= thisNodeIndex - 1) ? _Nodes.Get(thisNodeIndex - 1) : null;
                if (prevNode != null
                    && prevNode.EqualsValue(value))
                {
                    // combine this node and the previous node
                    prevNode.length++;
                    _Nodes.Set(prevNodeIndex, prevNode);
                }
                else
                {
                    // insert a new node which holds the new value
                    _Nodes.Insert(thisNodeIndex, this.CreateItem(value, 1));
                    thisNodeIndex++;
                }

                // decrement the length of this node,
                // or delete this node if it no longer holds data
                thisNode.length--;
                _Nodes.Set(thisNodeIndex, thisNode);
            }
            else if (indexInThisNode == thisNode.length - 1) // at right boundary
            {
                nextNodexIndex = (thisNodeIndex + 1 < _Nodes.Count) ? thisNodeIndex + 1 : -1;
                nextNode = (thisNodeIndex + 1 < _Nodes.Count) ? _Nodes.Get(thisNodeIndex + 1) : null;
                if (nextNode != null
                    && nextNode.EqualsValue(value))
                {
                    // combine this node and the next node
                    nextNode.length++;
                    _Nodes.Set(nextNodexIndex, nextNode);
                }
                else
                {
                    // insert a new node which holds the new value
                    _Nodes.Insert(thisNodeIndex + 1, this.CreateItem(value, 1));
                }

                // decrement the length of this node,
                // or delete this node if it no longer holds data
                thisNode.length--;
                _Nodes.Set(thisNodeIndex, thisNode);
            }
            else
            {
                //-- split this node and insert a new node holding the new value --
                var followingNodeLength = thisNode.length - indexInThisNode - 1;
                Debug.Assert(0 < indexInThisNode);
                Debug.Assert(0 < followingNodeLength);

                // chop this node off at the specified index
                thisNode.length = indexInThisNode;
                _Nodes.Set(thisNodeIndex, thisNode);

                // insert a new node which holds the new value
                _Nodes.Insert(thisNodeIndex + 1, this.CreateItem(value, 1));

                // insert a new node which holds the same value as this node
                _Nodes.Insert(thisNodeIndex + 2, this.CreateItem(thisNode, followingNodeLength));
            }

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
                if (index < 0 || TotalCount <= index)
                    throw new ArgumentOutOfRangeException();

                var nodeIndex = FindNodeIndex(index);
                Debug.Assert(0 <= nodeIndex,
                                "no node for the specified index was found!"
                                + " (index:" + index + ", TotalCount:" + TotalCount + ")");

                return _Nodes.Get(nodeIndex);
            }
            set
            {
                this.UpdateOne(index, value, defaultProcessItem);
            }
        }
        #endregion

        #region ICollection<T> + alpha
        /// <summary>
        /// Add the specified value.
        /// </summary>
        /// <param name='value'>The value to add.</param>
        public void AddRange(T value)
        {
            InsertRange(TotalCount, value);
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            _Nodes.Clear();
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

            for (int i = 0; i < TotalCount; i++)
            {
                array[arrayIndex + i] = this[i];
            }
        }

        /// <summary>
        /// Gets number of items stored in this array.
        /// </summary>
        public int Count
        {
            get { return _Nodes.Count; }
        }

        public long TotalRangeCount
        {
            get { return TotalCount; }
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
            for (long i = 0; i < _Nodes.Count; i++)
                yield return this.GetAt(i);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Utilities
        long FindNodeIndex(long index)
        {
            long nodeIndex, indexInNode;

            FindNodeIndex(index, out nodeIndex, out indexInNode);

            return nodeIndex;
        }

        void FindNodeIndex(long absolute_index, out long nodeIndex, out long indexInNode)
        {
            if(_Nodes.Count == 0)
            {
                //存在しない場合は初期値を返さないとうまく動かない
                nodeIndex = -1;
                indexInNode = -1;
                return;
            }

            long nearestIndex;
            nodeIndex = _Nodes.GetIndexFromAbsoluteIndexIntoRange(absolute_index, out nearestIndex);
            if(nodeIndex == -1)
            {
                if(nearestIndex == -1)
                {
                    nodeIndex = -1;
                    indexInNode = -1;
                }
                nodeIndex = nearestIndex;
            }

            var node= _Nodes.Get(nodeIndex);
            if(nodeIndex == _Nodes.Count - 1 && absolute_index == node.start + node.length)
            {
                //末尾かつabsolute_index == node.start + node.lengthの場合はこうしないといけない
                nodeIndex = -1;
                indexInNode = -1;

            }
            indexInNode = absolute_index - node.start;
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

            Debug.Assert(count == TotalCount);
        }
        #endregion
    }
}
