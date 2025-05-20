using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection.DataStore
{
    public class PinnedContent<T> : IDisposable
    {
        public T Content
        {
            get
            {
                return container.Content;
            }
        }

        PinableContainer<T> container;

        IPinableContainerStore<T> DataStore;

        internal PinnedContent(PinableContainer<T> c, IPinableContainerStore<T> dataStore)
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
