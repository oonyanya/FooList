# FooList

https://github.com/timdetering/Wintellect.PowerCollections

のBigListからIList<T>のメンバーだけ移植しました。もともとの方ではノードの共有をすることがありますが、テキストエディタだとノートを共有する必要性がまったくないので、あえてこの機能は省きました。

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
