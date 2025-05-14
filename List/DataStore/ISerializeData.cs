using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection.DataStore
{
    public interface ISerializeData<T>
    {
        T DeSerialize(byte[] inputData);
        byte[] Serialize(T data);
    }
}
