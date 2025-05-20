using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection.DataStore
{
    public class PinableContainer<T>
    {
        internal DiskAllocationInfo Info { get; set; }

        public T Content { get; internal set; }

        public PinableContainer(T content)
        {
            Content = content;
            Info = null;
        }

        public void RemoveContent()
        {
            Info.IsRemoved = true;
        }

    }
}
