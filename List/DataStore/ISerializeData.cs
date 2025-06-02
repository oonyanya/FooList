using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection.DataStore
{
    /// <summary>
    /// シリアライズを実行するためのインターフェイス
    /// </summary>
    /// <typeparam name="T">シリアライズの対象となる型</typeparam>
    /// <remarks>BigListで内部的に使用しています。この場合、FixedListを正しく機能するようにコードを書く必要があります</remarks>
    /// <example>コードの書き方はEditorDemoのStringBuffer.csを参照してください</example>
    public interface ISerializeData<T>
    {
        T DeSerialize(byte[] inputData);
        byte[] Serialize(T data);
    }
}
