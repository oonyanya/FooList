using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FooProject.Collection.DataStore;

namespace FooProject.Collection
{
    /// <summary>
    /// IPinableContainerを格納する奴
    /// </summary>
    /// <typeparam name="T">格納対象の型</typeparam>
    /// <remarks>BigListで使用する場合、IComposableListを指定する必要があります</remarks>
    public interface IPinableContainerStore<T>
    {
        /// <summary>
        /// 固定可能なやつからコンテナ―を取得し、固定したものを返す
        /// </summary>
        /// <param name="pinableContainer">固定対象の奴</param>
        /// <returns>固定された奴</returns>
        IPinnedContent<T> Get(IPinableContainer<T> pinableContainer);

        /// <summary>
        /// 取得及び固定を試みる
        /// </summary>
        /// <param name="pinableContainer">固定対象の奴</param>
        /// <param name="result">固定された奴</param>
        /// <returns>取得及び固定できたら、真を返す。そうでなければ、偽を返す</returns>
        bool TryGet(IPinableContainer<T> pinableContainer, out IPinnedContent<T> result);

        /// <summary>
        /// 固定可能なやつを格納する
        /// </summary>
        /// <param name="pinableContainer">固定可能なやつ</param>
        void Set(IPinableContainer<T> pinableContainer);

        /// <summary>
        /// 固定可能なやつを更新する
        /// </summary>
        /// <param name="pinableContainer">固定可能なやつ</param>
        /// <param name="newcontent">新しいコンテント</param>
        /// <param name="oldstart">更新前のコンテントの開始位置</param>
        /// <param name="oldcount">更新前のコンテントの長さ</param>
        /// <param name="newstart">更新後のコンテントの開始位置</param>
        /// <param name="newcount">更新後のコンテントの長さ</param>
        /// <returns></returns>
        IPinableContainer<T> Update(IPinableContainer<T> pinableContainer,T newcontent,long oldstart, long oldcount, long newstart, long newcount);

        /// <summary>
        /// 固定可能なやつを作成する
        /// </summary>
        /// <param name="content">格の対象のコンテント</param>
        /// <returns></returns>
        IPinableContainer<T> CreatePinableContainer(T content);

        /// <summary>
        /// コンテンツを複製可能かどうか
        /// </summary>
        /// <param name="pin">複製対象のコンテンツが格納されているIPinableContainer</param>
        /// <returns>データーストア自身でコンテンツの複製ができるなら、真を返す。そうでなければ、偽を返す。この場合、ユーザー自身の手でコンテンツを複製する必要がある。</returns>
        bool IsCanCloneContent(IPinableContainer<IComposableList<char>> pin);

        /// <summary>
        /// 複製する
        /// </summary>
        /// <param name="pin">複製対象</param>
        /// <param name="cloned_content">コンテンツ自体も複製したい場合は複製済みのコンテンツを渡す。nullの場合はTによって動作が変わる。</param>
        /// <returns></returns>
        IPinableContainer<T> Clone(IPinableContainer<T> pin, T cloned_content = default(T));

        /// <summary>
        /// ストア内部でキャッシュされているものをストアに書き出す
        /// </summary>
        void Commit();
    }

    /// <summary>
    /// IPinableContainerを格納する奴。コンテナ―内部で破棄される前にコンテント自身に何かしらの処理を行うことができる。
    /// </summary>
    /// <typeparam name="T">格納対象の型</typeparam>
    /// <remarks>BigListで使用する場合、IComposableListを指定する必要があります</remarks>
    public interface IPinableContainerStoreWithAutoDisposer<T> : IPinableContainerStore<T>
    {
        /// <summary>
        /// 固定可能な奴が破棄されるときに呼び出されるイベント
        /// </summary>
        event Action<T> Disposeing;

        /// <summary>
        /// 破棄されていない奴かつストア内部でキャッシュされているものを全て列挙する
        /// </summary>
        /// <returns></returns>
        IEnumerable<T> ForEachAvailableContent();
    }
}
