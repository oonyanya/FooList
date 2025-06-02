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

## ベンチマーク

12GBまでの動作結果を掲載しています。

使用PC：Core i7 14700、DDR4 メモリー32GB、Nvme SSD 1TB、1TB HDD 5400rpm

ブロックサイズ32768、オンメモリーモード。
```
benchmark start
size:40000000
Allocated GC Memory:61,088bytes
add time:18541 ms
Allocated GC Memory:7,984,576,768bytes
replace 1 time:146495 ms
Allocated GC Memory:7,984,593,376bytes
replace 2 time:837038 ms
Allocated GC Memory:13,814,137,216bytes
replace 3 time:621522 ms
Allocated GC Memory:13,814,137,912bytes
enumratotion time:88196 ms
Allocated GC Memory:13,814,138,056bytes
clear buffer
Allocated GC Memory:82,728bytes
add line time:10398 ms
Allocated GC Memory:1,638,728,376bytes
update line time:1423 ms
Allocated GC Memory:1,638,739,600bytes
clear buffer
Allocated GC Memory:82,984bytes
Finished.Hit Any Key

```

ブロックサイズ32768、一時ファイルを作るモードで、一時ファイルはHDDに作成しています。文字列操作中のタスクマネージャーから見たメモリー使用量は500~900MB程度です。
```
benchmark start
size:120000000
Allocated GC Memory:66,304bytes
add time:173334 ms
Allocated GC Memory:101,257,168bytes
replace 1 time:816628 ms
Allocated GC Memory:101,247,232bytes
replace 2 time:1179279 ms
Allocated GC Memory:333,371,424bytes
replace 3 time:1886714 ms
Allocated GC Memory:333,257,000bytes
enumratotion time:1638579 ms
Allocated GC Memory:331,904,816bytes
clear buffer
Allocated GC Memory:66,304bytes
add line time:46649 ms
Allocated GC Memory:86,939,136bytes
update line time:77635 ms
Allocated GC Memory:87,272,912bytes
clear buffer
Allocated GC Memory:69,448bytes
Finished.Hit Any Key
```

## ライセンス
MITライセンスに従ってください。
なお、一部ファイルコピー元のライセンスを守るものとします。
