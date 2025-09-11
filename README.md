# FooList

https://github.com/timdetering/Wintellect.PowerCollections

のBigListからIList<T>のメンバーだけ移植しました。もともとの方ではノードの共有をすることがありますが、テキストエディタだとノートを共有する必要性がまったくないので、あえてこの機能は省きました。

## 使い方

オンメモリーで動かしたい場合は普通に初期化すれば動きます。

```
using FooProject.Collection;

BigList<char> buf = new Foo.BigList<char>();
buf.Add('1');
```

ディスクに書き込みたい場合はISerializeData<FixedList<T>>を実装したクラスを用意したうえで以下のようにすれば動きます。
詳しい実装の仕方はEditorDemo/StringBuffer.csを見ればわかります。

```
using FooProject.Collection;
using FooProject.Collection.DataStore;

BigList<char> buf = new Foo.BigList<char>();
var serializer = new StringBufferSerializer();
var dataStore = new DiskPinableContentDataStore<FixedList<char>>(serializer);
buf.CustomBuilder.DataStore = dataStore;
buf.Add('1');
```

行からバイトに変換するテーブルを作りたい場合はEditorDemo/Program.csを見てください。
BigRangeList()を初期化しているところ以降を読めばにテーブルの作り方や操作の仕方が書いてあります。

Immutableにしたい場合、ImmutableListTestクラスを参照してください。

## 特徴

LOH入りすることがありません。挿入と削除はList<T>よりも早いです。その代わりノードの取得が少し遅くなることがあります。要素の列挙はList<T>と同じ速度で動きます。

## 計算量

| 操作 | BigList |
| --- | --- |
| 取得 | O(1) or O(Log N) |
| 追加・挿入・削除 | O(Log N) |
| 列挙 | O(N) |

※BigRangeListの計算量はBigListとほぼ同じです。ですが、範囲を追加したり、挿入したり、削除する場合は O(Log N) + Mかかります。また、範囲に対応する要素番号を取得する場合はO(1) + O(Log M)もしくはO(Log N) + O(Log M)かかります。

## ライセンス
MITライセンスに従ってください。
なお、一部ファイルコピー元のライセンスを守るものとします。
