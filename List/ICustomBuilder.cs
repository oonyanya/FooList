using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection
{
    public interface ICustomBuilder<T>
    {
        FixedList<T> CreateList(int init_capacity, int maxcapacity);

        LeafNode<T> CreateLeafNode();

        LeafNode<T> CreateLeafNode(T item);

        LeafNode<T> CreateLeafNode(int count, FixedList<T> items);

        ConcatNode<T> CreateConcatNode(ConcatNode<T> node);

        ConcatNode<T> CreateConcatNode(Node<T> left, Node<T> right);
    }
}
