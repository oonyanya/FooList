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
        public T Content
        {
            get
            {
                return container.Content;
            }
        }

        IPinableContainer<T> container;

        IPinableContainerStore<T> DataStore;

        public PinnedContent(IPinableContainer<T> c, IPinableContainerStore<T> dataStore)
        {
            container = c;
            DataStore = dataStore;
        }

        public void RemoveContent()
        {
            container.RemoveContent();
        }

        public void Dispose()
        {
            DataStore.Set(container);
        }
    }
}
