using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FooProject.Collection.DataStore;

namespace FooProject.Collection
{
    /// <summary>
    /// デフォルトの実装
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DefaultCustomConverter<T> : ICustomConverter<T>, ICustomBuilder<T>
    {
        public IPinableContainerStore<FixedList<T>> DataStore { get; set; }

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

        public LeafNode<T> CreateLeafNode(int blocksize)
        {
            var newLeafNode = new LeafNode<T>();
            var container = new PinableContainer<FixedList<T>>(this.CreateList(4, blocksize));
            newLeafNode.container = container;
            this.DataStore.Set(container);
            return newLeafNode;
        }

        public LeafNode<T> CreateLeafNode(T item, int blocksize)
        {
            var list = this.CreateList(4, blocksize);
            list.Add(item);
            var container = new PinableContainer<FixedList<T>>(list);
            this.DataStore.Set(container);
            return new LeafNode<T>(list.Count, container);
        }

        public LeafNode<T> CreateLeafNode(long count, FixedList<T> items)
        {
            var container = new PinableContainer<FixedList<T>>(items);
            this.DataStore.Set(container);
            return new LeafNode<T>(count, container);
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
