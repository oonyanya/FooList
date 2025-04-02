using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FooProject.Collection
{
    public interface ILeastFetch<T>
    {
        Node<T> Node { get; }
        int TotalLeftCount { get; }
    }
    public enum NodeWalkDirection
    {
        Left,
        Right
    }
    public interface ICustomConverter<T>
    {
        ILeastFetch<T> LeastFetch { get; }

        void ResetState();

        T Convert(T item);

        T ConvertBack(T item);

        /// <summary>
        /// Set State
        /// </summary>
        /// <param name="current">Node. If current is node, it have to create empty state.</param>
        /// <param name="totalLeftCountInList">Total sum of count in left node's item.</param>
        void SetState(Node<T> current, int totalLeftCountInList);
    }

    public struct LeastFetch<T> : ILeastFetch<T>
    {
        public Node<T> Node { get; private set; }
        public int TotalLeftCount { get; private set; }
        public LeastFetch(Node<T> node, int totalLeft)
        {
            Node = node;
            TotalLeftCount = totalLeft;
        }
    }

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

        public FixedList<T> CreateList(int init_capacity,int maxcapacity)
        {
            var list = new FixedList<T>(init_capacity, maxcapacity);
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

        public LeafNode<T> CreateLeafNode(int count, FixedList<T> items)
        {
            return new LeafNode<T>(count, items);
        }

        public void SetState(Node<T> current, int totalLeftCountInList)
        {
            this.LeastFetch = new LeastFetch<T>(current, totalLeftCountInList);
        }

        public void ResetState()
        {
            this.LeastFetch = null;
        }
    }
}
