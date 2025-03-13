# FooList

https://github.com/timdetering/Wintellect.PowerCollections

のBigListからIList<T>のメンバーだけ移植しました。もともとの方ではノードの共有をすることがありますが、テキストエディタだとノートを共有する必要性がまったくないので、あえてこの機能は省きました。

## 計算量

|---|---|
| 取得 | O(1) or O(Log N) |
| 追加・挿入・削除 | O(Log N) |
| 列挙 | O(N) |

## ライセンス
MITライセンスに従ってください。
なお、BigList<T>、Node<T>、GapBuffer<T>だけは移植元のライセンスを守るものとします。
