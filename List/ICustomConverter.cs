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
        Right
    }
    /// <summary>
    /// 変換用のインターフェイス
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ICustomConverter<T>
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

        /// <summary>
        /// Set State
        /// </summary>
        /// <param name="current">Node. If current is node, it have to create empty state.</param>
        /// <param name="totalLeftCountInList">Total sum of count in left node's item.</param>
        void SetState(Node<T> current, long totalLeftCountInList);
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

    /// <summary>
    /// デフォルトの実装
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DefaultCustomConverter<T> : ICustomConverter<T>,ICustomBuilder<T>
    {
        public ILeastFetch<T> LeastFetch { get; private set; }

        public T Convert(T item)
        {
            return item;
        }

        public T ConvertBack(T item)
        {
            return item;
        }

        public FixedList<T> CreateList(long init_capacity, long maxcapacity)
        {
            var list = new FixedList<T>((int)init_capacity, (int)maxcapacity);
            return list;
        }

        public ConcatNode<T> CreateConcatNode(ConcatNode<T> node)
        {
            return new ConcatNode<T>(node);
        }

        public ConcatNode<T> CreateConcatNode(Node<T> left, Node<T> right)
        {
            return new ConcatNode<T>(left,right);
        }

        public LeafNode<T> CreateLeafNode()
        {
            var newLeafNode = new LeafNode<T>();
            newLeafNode.items = this.CreateList(4, BigList<T>.MAXLEAF);
            return newLeafNode;
        }

        public LeafNode<T> CreateLeafNode(T item)
        {
            var list = this.CreateList(4, BigList<T>.MAXLEAF);
            list.Add(item);
            return new LeafNode<T>(list.Count,list);
        }

        public LeafNode<T> CreateLeafNode(long count, FixedList<T> items)
        {
            return new LeafNode<T>(count, items);
        }

        public void SetState(Node<T> current, long totalLeftCountInList)
        {
            this.LeastFetch = new LeastFetch<T>(current, totalLeftCountInList);
        }

        public void ResetState()
        {
            this.LeastFetch = null;
        }
    }
}
