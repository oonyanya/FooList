using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection.DataStore
{
    /// <summary>
    /// IPinableContainerStoreWithAutoDisposerやIDisposableを実装した抽象クラス
    /// </summary>
    /// <typeparam name="T"></typeparam>
    abstract public class PinableContentDataStoreWithAutoDisposerBase<T> : PinableContentDataStoreBase<T>,IPinableContainerStoreWithAutoDisposer<T>,IDisposable
    {
        bool disposedValue = false;

        /// <summary>
        /// 破棄される前に呼び出されるイベント
        /// </summary>
        public event Action<T> Disposeing;

        /// <summary>
        /// Disposeingイベントを呼び出し、放棄する必要があることを伝える
        /// </summary>
        /// <param name="item"></param>
        public virtual void OnDispoing(T item)
        {
            if (this.Disposeing != null)
                this.Disposeing(item);
        }

        /// <inheritdoc/>
        /// <remarks>継承先のクラスは必ず実装しなければならない</remarks>
        public virtual IEnumerable<T> ForEachAvailableContent()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposedValue)
            {
                return;
            }

            OnDispose(disposing);

            disposedValue = true;
        }

        /// <summary>
        /// Dispose時に呼び出される
        /// </summary>
        /// <param name="disposing">真ならマネージドリソースを破棄しなければならない</param>
        /// <remarks>継承先のクラスは必ず実装しなければならない</remarks>
        protected virtual void OnDispose(bool disposing)
        {
            throw new NotImplementedException();
        }

        ~PinableContentDataStoreWithAutoDisposerBase()
        {
            //GC時に実行されるデストラクタでは非管理リソースの削除のみ
            Dispose(false);
        }
    }
}
