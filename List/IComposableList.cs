using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection
{
    /// <summary>
    /// BigList内部で使用するコレクションを表す。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <remarks>今のバージョンだとLeafNodeに格納するために使用している。リードオンリーの場合、CopyTo、GetEnumerator、GetRange、Sliceだけは実装する必要があります</remarks>
    public interface IComposableList<T> : IEnumerable<T>, IList<T>
    {
        /// <summary>
        /// 追加可能かどうか
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="collection_length"></param>
        /// <returns>追加できるなら真を返す</returns>
        bool QueryAddRange(IEnumerable<T> collection, int collection_length = -1);
        /// <summary>
        /// 挿入可能かどうか
        /// </summary>
        /// <param name="index"></param>
        /// <param name="collection"></param>
        /// <param name="collection_length"></param>
        /// <returns>挿入可能なら真を返す</returns>
        bool QueryInsertRange(int index, IEnumerable<T> collection, int collection_length = -1);
        /// <summary>
        /// 削除可能かどうか
        /// </summary>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns>削除できるなら真を返す</returns>
        bool QueryRemoveRange(int index, int count);
        /// <summary>
        /// 更新可能かどうか
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        /// <returns>更新可能なら真を返す</returns>
        bool QueryUpdate(int index,T item);
        /// <summary>
        /// まとめて追加する
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="collection_length"></param>
        void AddRange(IEnumerable<T> collection, int collection_length = -1);
        /// <summary>
        /// まとめて挿入する
        /// </summary>
        /// <param name="index"></param>
        /// <param name="collection"></param>
        /// <param name="collection_length"></param>
        void InsertRange(int index, IEnumerable<T> collection, int collection_length = -1);
        /// <summary>
        /// まとめて削除する
        /// </summary>
        /// <param name="index"></param>
        /// <param name="count"></param>
        void RemoveRange(int index, int count);
        /// <summary>
        /// 特定の範囲の要素を列挙する
        /// </summary>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        IEnumerable<T> GetRange(int index, int count);
        /// <summary>
        /// 特定の範囲のスライスを作成する
        /// </summary>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        ReadOnlySequence<T> Slice(int index, int count);
    }
}
