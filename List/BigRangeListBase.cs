using FooProject.Collection.DataStore;
using System;
using System.Collections.Generic;
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
    public abstract class BigRangeListBase<T> : BigList<T>
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
    }

}
