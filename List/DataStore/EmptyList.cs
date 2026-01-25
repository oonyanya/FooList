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

    /// <summary>
    /// アロケーター
    /// </summary>
    internal class EmptyAllocator : IAllocator
    {
        Stack<long> emptyIDList = new Stack<long>();
        LinkedList<DiskAllocationInfo> emptyList = new LinkedList<DiskAllocationInfo>();
        long currentID = 0;
        long emptyIndex = 0;

        public void Clear()
        {
        }

        public DiskAllocationInfo GetEmptyList(int requireDataLength)
        {
            foreach (var node in this.emptyList)
            {
                if(requireDataLength <= node.AlignedLength)
                {
                    var newInfo = new DiskAllocationInfo(node.Index, node.AlignedLength);
                    this.emptyList.Remove(node);
                    return newInfo;
                }
            }

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
            var copyInfo = new DiskAllocationInfo(Info.Index, Info.AlignedLength);
            this.emptyList.AddLast(copyInfo);
        }
    }
}
