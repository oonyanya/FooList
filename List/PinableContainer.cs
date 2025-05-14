using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection
{
    public class PinableContainer<T>
    {
        public long Index { get; private set; }
        public T Content { get; private set; }

        public PinableContainer(T content)
        {
            this.Content = content;
            this.Index = -1;
        }

        public void SetConent(long index, T content)
        {
            this.Content = content;
            this.Index = index;
        }

        public void RemoveContent()
        {
            this.Content = default(T);
        }
    }
}
