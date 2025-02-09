using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection
{

    public class LeafNodeEnumrator<T> : IEnumerable<Node<T>>
    {
        public LeafNode<T> FirstNode { get; set; }
        public LeafNode<T> LastNode { get; set; }

        public void Replace(LeafNode<T> target, LeafNode<T> replacementNode)
        {
            if (target == null)
                throw new ArgumentNullException("target");
            if (replacementNode == null)
                throw new ArgumentNullException("replacementNode");
            if (replacementNode.Next != null || replacementNode.Previous != null)
                throw new InvalidOperationException();

            var priviousNode = target.Previous;
            var nextNode = target.Next;
            if (priviousNode != null && nextNode != null)
            {
                replacementNode.Next = nextNode;
                replacementNode.Previous = priviousNode;
                priviousNode.Next = replacementNode;
                nextNode.Previous = replacementNode;
            }
            else if (priviousNode == null && nextNode != null)
            {
                nextNode.Previous = replacementNode;
                replacementNode.Next = nextNode;
                FirstNode = replacementNode;
            }
            else if (priviousNode.Next != null && nextNode == null)
            {
                replacementNode.Previous = priviousNode;
                priviousNode.Next = replacementNode;
                LastNode = replacementNode;
            }
        }

        public void AddLast(LeafNode<T> newNode)
        {
            AddNext(LastNode, newNode);
        }

        public void AddNext(LeafNode<T> target, LeafNode<T> newNode)
        {
            if (newNode == null)
                throw new ArgumentNullException("newNode");

            if (newNode.Next != null || newNode.Previous != null)
                throw new InvalidOperationException();

            if (target == null)
            {
                Debug.Assert(FirstNode == null && LastNode == null);
                //どちらのnullの場合は何もないとする
                FirstNode = newNode;
                LastNode = newNode;
                return;
            }

            var nextNode = target.Next;

            if (target.Next == null && target.Previous != null) //最後のノードかどうか
            {
                target.Next = newNode;
                newNode.Previous = target;
                LastNode = newNode;
            }
            else if (target.Next != null && target.Previous != null)    //途中のノードかどうか
            {
                newNode.Next = nextNode;
                newNode.Previous = target;
                target.Next = newNode;
            }
            else if (target.Next != null && target.Previous == null)    //最初のノードかどうか
            {
                newNode.Previous = target;
                newNode.Next = nextNode;
                target.Next = newNode;
            }
            else
            {
                //ノードが一つしかない場合
                Debug.Assert(FirstNode != null && LastNode != null);
                target.Next = newNode;
                newNode.Previous = target;
                LastNode = newNode;
            }
        }

        public void AddBefore(LeafNode<T> target, LeafNode<T> newNode)
        {
            if (newNode == null)
                throw new ArgumentNullException("newNode");

            if (newNode.Next != null || newNode.Previous != null)
                throw new InvalidOperationException();

            if (target == null)
            {
                Debug.Assert(FirstNode == null && LastNode == null);
                //どちらのnullの場合は何もないとする
                FirstNode = newNode;
                LastNode = newNode;
                return;
            }

            var nextNode = target.Next;
            var previousNode = target.Previous;

            if (target.Next == null && target.Previous != null) //最後のノードかどうか
            {
                newNode.Next = target;
                newNode.Previous = previousNode;
                previousNode.Next = newNode;
                target.Previous = newNode;
            }
            else if (target.Next != null && target.Previous != null)    //途中のノードかどうか
            {
                newNode.Next = target;
                newNode.Previous = previousNode;
                previousNode.Next = newNode;
                target.Previous = newNode;
            }
            else if (target.Next != null && target.Previous == null)    //最初のノードかどうか
            {
                newNode.Next = target;
                target.Previous = previousNode;
                FirstNode = newNode;
            }
            else
            {
                //ノードが一つしかない場合
                Debug.Assert(FirstNode != null && LastNode != null);
                target.Previous = newNode;
                newNode.Next = target;
                FirstNode = newNode;
            }
        }

        public void Remove(LeafNode<T> node)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            var priviousNode = (LeafNode<T>)node.Previous;
            var nextNode = (LeafNode<T>)node.Next;
            if (priviousNode != null && nextNode != null)
            {
                priviousNode.Next = nextNode;
                nextNode.Previous = priviousNode;
            }
            else if (priviousNode == null && nextNode != null)
            {
                nextNode.Previous = null;
                FirstNode = nextNode;
            }
            else if (priviousNode.Next != null && nextNode == null)
            {
                priviousNode.Next = null;
                LastNode = priviousNode;
            }
            node.Next = null;
            node.Previous = null;
        }

        public void Clear()
        {
            FirstNode = null;
            LastNode = null;
        }

        public IEnumerator<Node<T>> GetEnumerator()
        {
            Node<T> node = FirstNode;
            while (node != null)
            {
                yield return node;
                node = node.Next;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
