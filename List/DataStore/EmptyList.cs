using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection.DataStore
{
    internal class EmptyList
    {
        const int EMPTYLISTSIZE = 32;   //ひとまず2^32までとする
        Stack<DiskAllocationInfo>[] emptylist = new Stack<DiskAllocationInfo>[EMPTYLISTSIZE];

        static int Log2(int v)
        {
#if NET5_0_OR_GREATER
            return BitOperations.Log2((uint)v);
#else
            int r = 0xFFFF - v >> 31 & 0x10;
            v >>= r;
            int shift = 0xFF - v >> 31 & 0x8;
            v >>= shift;
            r |= shift;
            shift = 0xF - v >> 31 & 0x4;
            v >>= shift;
            r |= shift;
            shift = 0x3 - v >> 31 & 0x2;
            v >>= shift;
            r |= shift;
            r |= (v >> 1);
            return r;
#endif
        }

        public void SetEmptyList(DiskAllocationInfo Info)
        {
            int msb = Log2(Info.AlignedLength);

            if (this.emptylist[msb] == null)
            {
                this.emptylist[msb] = new Stack<DiskAllocationInfo>();
            }
            this.emptylist[msb].Push(Info);
        }

        DiskAllocationInfo FindEmptyList(int msb, int requireDataLength)
        {
            if(msb < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(msb));
            }

            for (int i = msb; i < this.emptylist.Length; i++)
            {
                if (this.emptylist[i] != null && this.emptylist[i].Count > 0)
                {
                    var popedInfo = this.emptylist[i].Pop();
                    var newInfo = new DiskAllocationInfo(popedInfo.Index + requireDataLength, popedInfo.AlignedLength - requireDataLength);
                    this.SetEmptyList(newInfo);
                    return new DiskAllocationInfo(popedInfo.Index, requireDataLength);
                }
            }
            return null;
        }

        public DiskAllocationInfo GetEmptyList(int requireDataLength)
        {
            if (requireDataLength == -1)
                return null;

            int msb = Log2(requireDataLength);

            if (this.emptylist[msb] == null || this.emptylist[msb].Count == 0)
            {
                return FindEmptyList(msb,requireDataLength);
            }

            return this.emptylist[msb].Pop();
        }

        public void Clear()
        {
            for (int i = 0; i < this.emptylist.Length;i++)
            {
                this.emptylist[i]?.Clear();
                this.emptylist[i] = null;
            }
        }
    }
}
