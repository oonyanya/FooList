using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public abstract class CustomConverterBase<T> : IStateStore<T>, ICustomBuilder<T>
    {
        public IPinableContainerStore<IComposableList<T>> DataStore { get; set; }

        public virtual ILeastFetch<T> LeastFetch { get; private set; }

        public T Convert(T item)
        {
            return item;
        }

        public T ConvertBack(T item)
        {
            return item;
        }

        public virtual IComposableList<T> CreateList(long init_capacity, long maxcapacity, IEnumerable<T> collection = null)
        {
            var list = new FixedList<T>((int)init_capacity, (int)maxcapacity);
            if (collection != null)
            {
                list.AddRange(collection);
            }
            return list;
        }

        public ConcatNode<T> CreateConcatNode(Node<T> left, Node<T> right)
        {
            return OnCreateConcatNode(left, right);
        }

        protected virtual ConcatNode<T> OnCreateConcatNode(Node<T> left, Node<T> right)
        {
            return new ConcatNode<T>(left, right);
        }

        protected virtual LeafNode<T> OnCreateLeafNode()
        {
            return new LeafNode<T>();
         }

        protected virtual LeafNode<T> OnCreateLeafNode(long count, IPinableContainer<IComposableList<T>> container)
        {
            return new LeafNode<T>(count, container);
        }

        public LeafNode<T> CreateLeafNode(int blocksize)
        {
            var newLeafNode = OnCreateLeafNode();
            var container = this.DataStore.CreatePinableContainer(this.CreateList(4, blocksize));
            newLeafNode.container = container;
            this.DataStore.Set(container);
            return newLeafNode;
        }

        public LeafNode<T> CreateLeafNode(T item, int blocksize)
        {
            var list = this.CreateList(4, blocksize, new T[] {item});
            var container = this.DataStore.CreatePinableContainer(list);
            this.DataStore.Set(container);
            return OnCreateLeafNode(list.Count, container);
        }

        public LeafNode<T> CreateLeafNode(long count, IPinableContainer<IComposableList<T>> container)
        {
            this.DataStore.Set(container);
            return OnCreateLeafNode(count, container);
        }

        public virtual void SetState(Node<T> current, long totalLeftCountInList)
        {
            this.LeastFetch = new LeastFetch<T>(current, totalLeftCountInList);
        }

        public virtual void ResetState()
        {
            this.LeastFetch = null;
        }
    }

    public class DefaultCustomConverter<T> : CustomConverterBase<T>
    {
    }

}
