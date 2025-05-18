using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection
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
            this.container = c;
            this.DataStore = dataStore;
        }

        public void RemoveContent()
        {
            this.container.RemoveContent();
        }

        public void Dispose()
        {
            this.DataStore.Set(this.container);
        }
    }
}
