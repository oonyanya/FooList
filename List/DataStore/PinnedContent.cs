using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection.DataStore
{
    /// <summary>
    /// 固定されたコンテナ―を表す
    /// </summary>
    /// <typeparam name="T">格納対象の型</typeparam>
    public interface IPinnedContent<T> : IDisposable
    {
        T Content { get; }
        void RemoveContent();
    }

    public class PinnedContent<T> : IPinnedContent<T>
    {
        /// <inheritdoc/>
        public T Content
        {
            get
            {
                if (this.disposedValue)
                    throw new InvalidOperationException("already disposed");
                return container.Content;
            }
        }

        IPinableContainer<T> container;

        IPinableContainerStore<T> DataStore;
        private bool disposedValue;

        public PinnedContent(IPinableContainer<T> c, IPinableContainerStore<T> dataStore)
        {
            container = c;
            DataStore = dataStore;
        }

        /// <inheritdoc/>
        public void RemoveContent()
        {
            if (this.disposedValue)
                return;
            container.RemoveContent();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    DataStore.Set(container);
                }

                // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、ファイナライザーをオーバーライドします
                this.container = null;
                this.DataStore = null;

                disposedValue = true;
            }
        }

        // // TODO: 'Dispose(bool disposing)' にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします
        // ~PinnedContent()
        // {
        //     // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
