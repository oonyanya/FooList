using FooProject.Collection.DataStore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection
{
    public class PinableContainer<T>
    {
        internal DiskAllocationInfo Info { get; private set; }

        public T Content { get; private set; }

        public PinableContainer(T content)
        {
            this.Content = content;
            this.Info = null;
        }

        public void SetConent(T content, byte[] data)
        {
            this.Info.SerializedData = data;
            this.Content = content;
        }

        public void SetConent(long index, T content, byte[] data,int length)
        {
            if(this.Info == null)
            {
                this.Info = new DiskAllocationInfo(index, length);
            }
            else
            {
                this.Info.Index = index;
                this.Info.AlignedLength = length;
            }
            this.Info.SerializedData = data;
            this.Content = content;
        }

        public void RemoveContent()
        {
            this.Content = default(T);
        }

        public void ReleaseInfo()
        {
            this.Info = null;
        }

    }
}
