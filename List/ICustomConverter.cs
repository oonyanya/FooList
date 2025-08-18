using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FooProject.Collection
{
    /// <summary>
    /// 一番最後に取得したノードの情報を表すインターフェイス
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ILeastFetch<T>
    {
        Node<T> Node { get; }
        long TotalLeftCount { get; }
    }
    /// <summary>
    /// ノードの移動方向
    /// </summary>
    public enum NodeWalkDirection
    {
        Left,
        Right,
        GoBack,
        None
    }

    /// <summary>
    /// 取得したノードなどの状態を格納するインターフェイス
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IStateStore<T>
    {
        /// <summary>
        /// 一番最後に取得したノードの情報を表す
        /// </summary>
        ILeastFetch<T> LeastFetch { get; }

        /// <summary>
        /// 一番最後に取得したノードの情報を破棄する
        /// </summary>
        void ResetState();

        /// <summary>
        /// Set State
        /// </summary>
        /// <param name="current">Node. If current is node, it have to create empty state.</param>
        /// <param name="totalLeftCountInList">Total sum of count in left node's item.</param>
        void SetState(Node<T> current, long totalLeftCountInList);
    }

    /// <summary>
    /// 変換用のインターフェイス
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ICustomConverter<T> : IStateStore<T>
    {
        /// <summary>
        /// 変換する
        /// </summary>
        /// <param name="item">変換前の値</param>
        /// <returns>変換後の値</returns>

        T Convert(T item);

        /// <summary>
        /// 逆変換する
        /// </summary>
        /// <param name="item">変換前の値</param>
        /// <returns>変換後の値</returns>
        T ConvertBack(T item);
    }

    /// <summary>
    /// デフォルトの実装
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct LeastFetch<T> : ILeastFetch<T>
    {
        public Node<T> Node { get; private set; }
        public long TotalLeftCount { get; private set; }
        public LeastFetch(Node<T> node, long totalLeft)
        {
            Node = node;
            TotalLeftCount = totalLeft;
        }
    }
}
