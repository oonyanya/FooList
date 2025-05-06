using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection
{
    /// <summary>
    /// デフォルトの実装
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DefaultCustomConverter<T> : ICustomConverter<T>, ICustomBuilder<T>
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
            return new ConcatNode<T>(left, right);
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
            return new LeafNode<T>(list.Count, list);
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
