using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection.DataStore
{
    internal interface IAllocator
    {
        void ReleaseID(long id);
        long GetID();
        void SetEmptyList(DiskAllocationInfo Info);
        DiskAllocationInfo GetEmptyList(int requireDataLength);
        void Clear();
    }

    /// <summary>
    /// 特に再利用もしないのでデバック用に使うこと
    /// </summary>
    internal class SimpleAllocator : IAllocator
    {
        Stack<long> emptyIDList = new Stack<long>();
        long currentID = 0;
        long emptyIndex = 0;

        public void Clear()
        {
        }

        public DiskAllocationInfo GetEmptyList(int requireDataLength)
        {
            var result = new DiskAllocationInfo(emptyIndex, requireDataLength);
            this.emptyIndex += requireDataLength;
            return result;
        }

        public void ReleaseID(long id)
        {
            this.emptyIDList.Push(id);
        }

        public long GetID()
        {
            if (emptyIDList.Count == 0)
            {
                return ++currentID;
            }
            else
            {
                return this.emptyIDList.Pop();
            }
        }

        public void SetEmptyList(DiskAllocationInfo Info)
        {
        }
    }

    // TLSFメモリアロケータ
    // http://www.marupeke296.com/ALG_No2_TLSFMemoryAllocator.html
    internal class EmptyList : IAllocator
    {
        const int EMPTYLISTSIZE = 32;   //ひとまず2^32までとする
        Stack<DiskAllocationInfo>[] emptylist = new Stack<DiskAllocationInfo>[EMPTYLISTSIZE];
        Stack<long> emptyIDList = new Stack<long>();
        long currentID = 0;
        long emptyIndex = 0;

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

        public EmptyList(int maxmsb = EMPTYLISTSIZE)
        {
            if (maxmsb <= 0)
                throw new ArgumentOutOfRangeException("maxmsb must be grater 0.");
            this.emptylist = new Stack<DiskAllocationInfo>[maxmsb];
        }

        public void ReleaseID(long id)
        {
            this.emptyIDList.Push(id);
        }

        public long GetID()
        {
            if(emptyIDList.Count == 0)
            {
                return ++currentID;
            }
            else
            {
                return this.emptyIDList.Pop();
            }
        }

        public void SetEmptyList(DiskAllocationInfo Info)
        {
            int msb = Log2(Info.AlignedLength);
            if(msb > this.emptylist.Length)
            {
                msb = this.emptylist.Length - 1;
            }

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

            DiskAllocationInfo newInfo;
            for (int i = msb; i < this.emptylist.Length; i++)
            {
                if (this.emptylist[i] != null && this.emptylist[i].Count > 0)
                {
                    var popedInfo = this.emptylist[i].Pop();
                    newInfo = new DiskAllocationInfo(popedInfo.Index + requireDataLength, popedInfo.AlignedLength - requireDataLength);
                    this.SetEmptyList(newInfo);

                    return new DiskAllocationInfo(popedInfo.Index, requireDataLength);
                }
            }

            newInfo = new DiskAllocationInfo(emptyIndex, requireDataLength);
            emptyIndex += requireDataLength;

            return newInfo;
        }

        public DiskAllocationInfo GetEmptyList(int requireDataLength)
        {
            if (requireDataLength == -1)
                return null;

            int msb = Log2(requireDataLength);
            if (msb > this.emptylist.Length)
            {
                msb = this.emptylist.Length - 1;
            }

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
