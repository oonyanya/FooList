using FooProject.Collection.DataStore;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
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
        /// <summary>
        /// 全ての範囲の合計値
        /// </summary>
        public long TotalRangeCount
        {
            get
            {
                var root = (IRangeNode)this.Root;
                return root.TotalRangeCount;
            }
        }

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
            LeastFetchStore.ResetState();
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
            return this.GetIndexFromAbsoluteIndexIntoRange(indexIntoRange, out _);
        }

        /// <summary>
        /// 絶対的な位置、すなわちインデックスに対応する要素の番号を返す
        /// </summary>
        /// <param name="index">0から始まる数値。絶対的な位置を指定しないといけない</param>
        /// <param name="nearestIndex">一番近いインデックスが設定される</param>
        /// <returns>0から始まる要素の番号。見つからない場合は-1を返す</returns>
        public virtual long GetIndexFromAbsoluteIndexIntoRange(long indexIntoRange,out long nearestIndex)
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
                nearestIndex = relativeNearIndex + myCustomConverter.customLeastFetch.TotalLeftCount;
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

        protected virtual T DefaultGetFromAbsoluteIndexIntoRangeFn(T item,long item_start_count,long fetchedTotalRangeCount,long LeftCount)
        {
            T result = (T)item.DeepCopy();
            result.start = item.start + fetchedTotalRangeCount;
            result.length = item.length;
            return result;
        }

        /// <summary>
        /// 列挙する
        /// </summary>
        /// <param name="absolteIndex">列挙を開始する開始インデックス</param>
        /// <param name="generate_fn">生成用の関数。nullでないなら、適切なTを返さないといけない。使い方はDefaultGetFromAbsoluteIndexIntoRangeFnを参照すること。入力元のアイテム、開始インデックス、今まで取得した範囲の数、残りの数である。</param>
        /// <returns></returns>
        /// <remarks>generate_fnで指定された関数がnullを返した場合、列挙が止まる</remarks>
        public IEnumerable<T> GetFromAbsoluteIndexIntoRange(long absolteIndex,long count,Func<T,long,long,long,T> generate_fn = null)
        {
            if (generate_fn == null)
                generate_fn = this.DefaultGetFromAbsoluteIndexIntoRangeFn;

            LeastFetchStore.ResetState();

            LeafNode<T> current = GetNodeFromAbsoluteIndexIntoRange(absolteIndex, out _);

            var leftCount = count;
            var fetchedTotalRangeCount = 0L;
            while (current != null)
            {
                using (var pinnedContent = CustomBuilder.DataStore.Get(current.container))
                {
                    var nodeItems = pinnedContent.Content;
                    foreach (T item in nodeItems)
                    {
                        var relative_start_index = 0L;
                        var item_start_absolute_index = item.start + fetchedTotalRangeCount;
                        var item_end_absoulte_index = item.start + item.length - 1 + fetchedTotalRangeCount;

                        if(item_end_absoulte_index < absolteIndex)
                        {
                            continue;
                        }
                        else if (absolteIndex >= item_start_absolute_index && absolteIndex <= item_end_absoulte_index)
                        {
                            relative_start_index = absolteIndex - item.start;
                        }
                        else if(item_start_absolute_index > absolteIndex + count)
                        {
                            yield break;
                        }

                        var result = generate_fn(item, relative_start_index, fetchedTotalRangeCount,leftCount);

                        if (result != null && result.length > 0)
                            yield return result;
                        else
                            yield break;

                        leftCount -= item.length - relative_start_index;
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
            LeastFetchStore.ResetState();


            var root = (IRangeNode)this.Root;

            LeastFetchStore.ResetState();

            LeafNode<T> current = GetNodeFromAbsoluteIndexIntoRange(0, out _);

            var fetchedTotalRangeCount = 0L;
            var leftCount = this.TotalRangeCount;
            while (current != null)
            {
                using (var pinnedContent = CustomBuilder.DataStore.Get(current.container))
                {
                    var nodeItems = pinnedContent.Content;
                    foreach (T item in nodeItems)
                    {
                        var result = this.DefaultGetFromAbsoluteIndexIntoRangeFn(item, 0L, fetchedTotalRangeCount, leftCount);

                        yield return result;

                        leftCount -= item.length;
                    }
                }

                var rangeLeafNode = (RangeLeafNode<T>)current;
                fetchedTotalRangeCount += rangeLeafNode.TotalRangeCount;
                current = current.Next;
            }
        }

        /// <summary>
        /// 範囲内の列挙子を取得する
        /// </summary>
        /// <param name="absolteIndex">開始インデックス</param>
        /// <param name="count">長さ</param>
        /// <returns>列挙子を取得する</returns>
        /// <remarks>IRangeインターフェイスのstartの値は変換される</remarks>
        public virtual IEnumerable<T> GetRangeFromAbsoluteIndexIntoRange(long absolteIndex, long count)
        {
            return this.GetFromAbsoluteIndexIntoRange(absolteIndex, count);
        }
    }

}
