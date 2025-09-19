using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FooProject.Collection.DataStore;

namespace FooProject.Collection
{
    /// <summary>
    /// 生成用のインターフェイス
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ICustomBuilder<T>
    {
        /// <summary>
        /// データーストア
        /// </summary>
        IPinableContainerStore<IComposableList<T>> DataStore { get; set; }

        /// <summary>
        /// リストを作成する
        /// </summary>
        /// <param name="init_capacity">最小の容量</param>
        /// <param name="maxcapacity">最大の容量</param>
        /// <param name="collection">取り込むコレクション。nullを指定した場合は空のコレクションが作成される</param>
        /// <returns></returns>
        IComposableList<T> CreateList(long init_capacity, long maxcapacity, IEnumerable<T> collection = null);

        /// <summary>
        /// 空のリーフノードを作成する
        /// </summary>
        /// <returns>リーフノード</returns>
        LeafNode<T> CreateLeafNode(int blocksize);

        /// <summary>
        /// リーフノードを作成する
        /// </summary>
        /// <param name="item">追加するアイテム</param>
        /// <param name="blocksize">ブロックサイズ</param>
        /// <returns>リーフノードを返すが、アイテムは追加済みでなければならない</returns>
        LeafNode<T> CreateLeafNode(T item, int blocksize);

        /// <summary>
        /// リーフノードを作成する
        /// </summary>
        /// <param name="count">アイテム数</param>
        /// <param name="items">アイテム</param>
        /// <returns></returns>
        LeafNode<T> CreateLeafNode(long count, IComposableList<T> items);

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
