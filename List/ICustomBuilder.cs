using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection
{
    public interface ICustomBuilder<T>
    {
        FixedList<T> CreateList(long init_capacity, long maxcapacity);

        LeafNode<T> CreateLeafNode();

        LeafNode<T> CreateLeafNode(T item);

        LeafNode<T> CreateLeafNode(long count, FixedList<T> items);

        ConcatNode<T> CreateConcatNode(ConcatNode<T> node);

        ConcatNode<T> CreateConcatNode(Node<T> left, Node<T> right);
    }
}
