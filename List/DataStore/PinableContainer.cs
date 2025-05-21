using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection.DataStore
{
    public class PinableContainer<T>
    {
        internal const long NOTCACHED = -1;

        internal DiskAllocationInfo Info { get; set; }

        internal long CacheIndex { get; set; }

        public T Content { get; internal set; }

        public bool IsRemoved { get; set; }


        public PinableContainer(T content)
        {
            Content = content;
            Info = null;
            CacheIndex = NOTCACHED;
            IsRemoved = false;
        }

        public void RemoveContent()
        {
            this.IsRemoved = true;
        }

    }
}
