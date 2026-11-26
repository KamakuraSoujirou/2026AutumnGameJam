# 手作りの謎のデータ

手作りの謎（問題文だけで素材ではない）を JSON で置く場所です。
仕様は Notion「データと調整パラメータ」「謎A：配線」「謎C：順番スイッチ」「解除（ひらめき謎）」を参照。

## 読み込み方

`Assets/Scripts/Kamakura/Puzzle/PuzzleDatabase.cs` を使います。

```cs
PuzzleDatabase orderDatabase = new PuzzleDatabase(PuzzleDatabase.OrderSwitchPath);

PuzzleData puzzle = orderDatabase.Draw("C2"); // まだ出していない C2 から1問ランダム
string question = puzzle.Text;                // 「五十音順に押せ」
string[] correctOrder = puzzle.Items;         // 正解の順番

orderDatabase.ResetUsed();                    // 試合の最初に呼ぶ（出した記録を消す）
```

- 同じ種類を全部出し終えたら、自動で最初から選び直します。
- 書き方が間違っている問題は読み飛ばし、Console に「○問目を読み飛ばしました（理由）」と警告が出ます。

## ファイル

| ファイル | 中身 | 目標の数 | 主な担当 |
| --- | --- | --- | --- |
| `WireAssociation.json` | 謎A T3 連想 | 16問 | 配線（笹） |
| `OrderSwitch.json` | 謎C C2 五十音・C3 時間・C4 大きさ | 8組・7組・6組（入力済み） | 順番スイッチ（賀来） |
| `Defuse.json` | 解除（ひらめき謎、4択） | 15問 | 解除 |

## 形式（v1）

どのファイルも `{ "puzzles": [ ... ] }` の形で、1問を1行のオブジェクトで書きます。
`JsonUtility` で読むため、**使わないキーも消さずに空（`[]` や `""`）で残してください**。

| キー | 型 | 内容 |
| --- | --- | --- |
| `gimmick` | 文字列 | `A`（配線）／`C`（順番スイッチ）／`Defuse`（解除） |
| `type` | 文字列 | 謎の種類。`T3`、`C2`、`C3`、`C4`、`Defuse` |
| `text` | 文字列 | 液晶に出す問題文 |
| `items` | 文字列の配列 | C用。**正解の順番どおり**に4つ書く（スイッチへの割り振りはゲーム側でランダム） |
| `choices` | 文字列の配列 | 解除用。4択の選択肢。**先頭に正解**を書く（並びはゲーム側で毎回シャッフル） |
| `answer` | 文字列 | T3は正解の線の色、解除は正解の選択肢（`choices` の先頭と同じ文字列）。Cは空 |

## コピー用のひな形

```json
{ "gimmick": "A", "type": "T3", "text": "", "items": [], "choices": [], "answer": "" },
{ "gimmick": "C", "type": "C2", "text": "五十音順に押せ", "items": ["", "", "", ""], "choices": [], "answer": "" },
{ "gimmick": "Defuse", "type": "Defuse", "text": "", "items": [], "choices": ["正解", "", "", ""], "answer": "正解" },
```

## 書くときの注意

- 最後の1問のあとにはカンマを付けない（付けると読み込みエラーになる）
- JSON にはコメントが書けない
- T3 の `answer` は配線の色名で、`赤` `青` `黄` `緑` のどれか（`あか` `あお` `きいろ` `みどり` `黄色` でも読める）。それ以外の答えの問題は、代わりに T1 が出る
- T3 の `text` は液晶にそのまま出す。1行に収まるよう14文字くらいまでにする
- 解除の問題は同じ試合で重複させないので、15問すべて違う問題にする
