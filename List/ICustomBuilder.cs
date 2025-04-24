using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection
{
    /// <summary>
    /// 生成用のインターフェイス
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ICustomBuilder<T>
    {
        /// <summary>
        /// リストを作成する
        /// </summary>
        /// <param name="init_capacity">最小の容量</param>
        /// <param name="maxcapacity">最大の容量</param>
        /// <returns></returns>
        FixedList<T> CreateList(long init_capacity, long maxcapacity);

        /// <summary>
        /// 空のリーフノードを作成する
        /// </summary>
        /// <returns>リーフノード</returns>
        LeafNode<T> CreateLeafNode();

        /// <summary>
        /// リーフノードを作成する
        /// </summary>
        /// <param name="item">追加するアイテム</param>
        /// <returns>リーフノードを返すが、アイテムは追加済みでなければならない</returns>
        LeafNode<T> CreateLeafNode(T item);

        /// <summary>
        /// リーフノードを作成する
        /// </summary>
        /// <param name="count">アイテム数</param>
        /// <param name="items">アイテム</param>
        /// <returns></returns>
        LeafNode<T> CreateLeafNode(long count, FixedList<T> items);

        /// <summary>
        /// 幹を表すノードを作成する
        /// </summary>
        /// <param name="node">幹を表すノード</param>
        /// <returns>幹を表すノードを返す。</returns>
        ConcatNode<T> CreateConcatNode(ConcatNode<T> node);

        /// <summary>
        /// 幹を表すノードを作成する
        /// </summary>
        /// <param name="left">左側のノード。リーフノードか幹を表すノードのうちどちらかがセットされる</param>
        /// <param name="right">右側のノード。リーフノードか幹を表すノードのうちどちらかがセットされる</param>
        /// <returns>幹を表すノードを返す。左右それぞれにノードを設定した状態で返さなければならない。</returns>
        ConcatNode<T> CreateConcatNode(Node<T> left, Node<T> right);
    }
}
