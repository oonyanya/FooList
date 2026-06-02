using FooProject.Collection.DataStore;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection
{

    /// <summary>
    /// 範囲を変換するための基底クラス
    /// </summary>
    /// <remarks>連続した範囲でないとうまく動きません</remarks>
    public abstract class BigRangeListBase<T> : BigList<T> where T : IRange
    {

        protected override bool IsAllowDirectUseCollection(IComposableList<T> collection)
        {
            if (collection is FixedRangeList<T>)
                return true;
            throw new NotSupportedException("FixedRangeList以外を使用することはできません");
        }

        /// <summary>
        /// 要素を取得する
        /// </summary>
        /// <param name="index">0から始まる数値</param>
        /// <returns>Tを返す。IRangeインターフェイスのstartの値は変換されない</returns>
        public override T Get(long index)
        {
            var result = GetRawData(index);
            return result;
        }

        /// <summary>
        /// 対応する変換前のアイテムを返す
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        /// <remarks>必ず実装する必要があります</remarks>
        public virtual T GetRawData(long index)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 要素を設定する
        /// </summary>
        /// <param name="index">0から始まる数値</param>
        /// <param name="value">設定したいT</param>
        /// <remarks>valueの値は内部で相対的インデックスに変換されるので、変換する必要はない</remarks>
        public override void Set(long index, T value)
        {
            var args = new BigListArgs<T>(CustomBuilder, LeastFetchStore, this.BlockSize, UpdateType.Overwrite);
            Root.SetAtInPlace(index, value, leafNodeEnumrator, args);
        }

        protected int IndexOfNearest<J>(IList<T> collection,J start, Func<J,T,int> fn,out long nearIndex)
        {
            nearIndex = -1;
            if (collection.Count == 0)
                return -1;

            if (start.Equals(default(T)) && collection.Count > 0)
                return 0;

            T line;

            int left = 0, right = collection.Count - 1, mid;
            while (left <= right)
            {
                mid = (left + right) / 2;
                line = collection[mid];
                var result = fn(start, line);
                if (result == 0)
                {
                    return mid;
                }
                if (result < 0)
                {
                    right = mid - 1;
                }
                else
                {
                    left = mid + 1;
                }
            }

            System.Diagnostics.Debug.Assert(left >= 0 || right >= 0);
            nearIndex = left >= 0 ? left : right;
            if (nearIndex > collection.Count - 1)
                nearIndex = right;

            return -1;
        }

        /// <summary>
        /// 対応するノードを返す
        /// </summary>
        /// <param name="indexIntoRange"></param>
        /// <param name="resultRelativeIndexIntoRange"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        /// <remarks>必ず実装する必要があります</remarks>
        protected virtual LeafNode<T> GetNodeFromAbsoluteIndexIntoRange(long indexIntoRange, out long resultRelativeIndexIntoRange)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 絶対的な位置、すなわちインデックスに対応する要素の番号を返す
        /// </summary>
        /// <param name="index">0から始まる数値。絶対的な位置を指定しないといけない</param>
        /// <returns>0から始まる要素の番号。見つからない場合は-1を返す</returns>
        public virtual long GetIndexFromAbsoluteIndexIntoRange(long indexIntoRange)
        {
            RangeConverter<T> myCustomConverter = (RangeConverter<T>)LeastFetchStore;
            long relativeIndexIntoRange;

            var node = this.GetNodeFromAbsoluteIndexIntoRange(indexIntoRange, out relativeIndexIntoRange);

            long relativeIndex, relativeNearIndex;
            var leafNode = (LeafNode<T>)node;
            using (var pinnedContent = CustomBuilder.DataStore.Get(leafNode.container))
            {
                var leafNodeItems = pinnedContent.Content;
                relativeIndex = this.IndexOfNearest(leafNodeItems, relativeIndexIntoRange, out relativeNearIndex);
            }

            if (relativeIndex == -1)
            {
                myCustomConverter.ResetState();
                return -1;
            }
            return relativeIndex + myCustomConverter.customLeastFetch.TotalLeftCount;
        }

        /// <summary>
        /// 一番近い要素を返す
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="start"></param>
        /// <param name="nearIndex"></param>
        /// <returns></returns>
        protected int IndexOfNearest(IList<T> collection, long start, out long nearIndex)
        {
            return this.IndexOfNearest(collection, start, (s, line) => {
                var lineHeadIndex = line.start;
                if (s >= lineHeadIndex && s < lineHeadIndex + line.length)
                {
                    return 0;
                }
                if (start < lineHeadIndex)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            }, out nearIndex);
        }

        /// <summary>
        /// 列挙する
        /// </summary>
        /// <param name="absolteIndex">列挙を開始する開始インデックス</param>
        /// <returns></returns>
        protected virtual IEnumerable<T> GetFromAbsoluteIndexIntoRange(long absolteIndex)
        {
            LeastFetchStore.ResetState();

            LeafNode<T> current = GetNodeFromAbsoluteIndexIntoRange(absolteIndex, out _);

            var fetchedTotalRangeCount = 0L;
            while (current != null)
            {
                using (var pinnedContent = CustomBuilder.DataStore.Get(current.container))
                {
                    var nodeItems = pinnedContent.Content;
                    foreach (T item in nodeItems)
                    {
                        T result = (T)item.DeepCopy();
                        result.start = item.start + fetchedTotalRangeCount;
                        result.length = item.length;
                        yield return result;
                    }
                }

                var rangeLeafNode = (RangeLeafNode<T>)current;
                fetchedTotalRangeCount += rangeLeafNode.TotalRangeCount;
                current = current.Next;
            }
        }

        /// <summary>
        /// 列挙子を取得する
        /// </summary>
        /// <returns>列挙子を取得する</returns>
        /// <remarks>IRangeインターフェイスのstartの値は変換される</remarks>
        public override IEnumerator<T> GetEnumerator()
        {
            foreach (var item in this.GetFromAbsoluteIndexIntoRange(0))
            {
                yield return item;
            }
        }

        /// <summary>
        /// 範囲内の列挙子を取得する
        /// </summary>
        /// <param name="absolteIndex">開始インデックス</param>
        /// <param name="count">長さ</param>
        /// <returns>列挙子を取得する</returns>
        /// <remarks>IRangeインターフェイスのstartの値は変換される</remarks>
        public IEnumerable<T> GetRangeFromAbsoluteIndexIntoRange(long absolteIndex, long count)
        {
            var leftCount = count;
            foreach (var item in this.GetFromAbsoluteIndexIntoRange(0))
            {
                yield return item;

                if (leftCount < 0)
                    yield break;

                if (absolteIndex >= item.start && absolteIndex <= item.start + item.length)
                {
                    leftCount -= item.length - (absolteIndex - item.start);
                }
                else
                {
                    leftCount -= item.length;
                }
            }
        }
    }

}
