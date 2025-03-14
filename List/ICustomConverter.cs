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
        ILeastFetch<T> LeastFetch { get; set; }


        void ResetState();

        void NodeWalk(Node<T> current, NodeWalkDirection dir);

        T Convert(T item);

        T ConvertBack(T item);

        LeafNode<T> CreateLeafNode();

        LeafNode<T> CreateLeafNode(T item);

        LeafNode<T> CreateLeafNode(int count, FixedList<T> items);

        ConcatNode<T> CreateConcatNode(ConcatNode<T> node);

        ConcatNode<T> CreateConcatNode(Node<T> left, Node<T> right);
    }

    public class DefaultCustomConverter<T> : ICustomConverter<T>
    {
        public ILeastFetch<T> LeastFetch { get; set; }

        public T Convert(T item)
        {
            return item;
        }

        public T ConvertBack(T item)
        {
            return item;
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
            return new LeafNode<T>();
        }

        public LeafNode<T> CreateLeafNode(T item)
        {
            return new LeafNode<T>(item);
        }

        public LeafNode<T> CreateLeafNode(int count, FixedList<T> items)
        {
            return new LeafNode<T>(count, items);
        }

        public void NodeWalk(Node<T> current, NodeWalkDirection dir)
        {
        }

        public void ResetState()
        {
        }
    }
}
