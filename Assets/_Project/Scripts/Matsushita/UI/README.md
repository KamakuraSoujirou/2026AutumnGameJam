# UI仮組み（松下）

素材がなくても仮の四角で画面ができ、素材が届いたら `UiSkin` に画像を入れるだけで差し替わる UI の仮組みです。
今はタイトル画面だけです（モック画像「あと10秒」タイトルに合わせた配置）。

## 使い方
1. Unity でスクリプトを読み込む（コンパイルが終わるまで待つ。Mac は Cmd+R、Windows は Ctrl+R で更新）
2. メニューバーの **Tools → UI仮組 → タイトルを作る**
   - `Assets/_Project/Prefabs/Matsushita/UI/UI_Title.prefab`
   - `Assets/_Project/Prefabs/Matsushita/UI/UiSkin.asset`（素材の表）
   - `Assets/_Project/Scenes/Matsushita/UiMock_Title.unity`（確認用シーン。作ったあと開く）
3. 再生して操作を確認する
   - ↑↓（W S）：行を選ぶ ／ ←→（A D）：設定を変える ／ Space・Enter：ゲームスタート（今はコンソールに設定を出すだけ）。マウスでも、選択肢のクリックで設定を変え、「ゲームスタート」のクリックで始められる（EventSystem がなければ `UiTitleView` が自動で作る）
   - 2人対戦のときは「CPUの強さ」の行を薄くして飛ばす
4. 素材が届いたら `Assets/_PrivateAssets/Art/` に置き、**Tools → UI仮組 → 素材を割り当てて反映**
   - ファイル名（下の表）か、素材IDで始まる名前（例：`art_01_bomb.png`）で自動で割り当てる
   - 手で `UiSkin.asset` に画像を入れてもよい（実行時は自動で反映、プレハブの見た目はこのメニューで更新）

## 素材IDとファイル名
| 素材ID | 内容 | 大きさの目安 | ファイル名 |
| --- | --- | --- | --- |
| ART-13 | タイトル背景 | 1920×1080 | title_bg |
| ART-14 | タイトルロゴ | 700×130 | title_logo |
| ART-09_normal | キャラ P1（通常） | 600×900 | chr_p1_normal |
| ART-10_normal | キャラ P2（通常） | 600×900 | chr_p2_normal |
| ART-01 | 爆弾本体 | 800×800 | bomb_core |
| UI-01 | 選択肢ボタン（選択中） | 104×44 | ui_chip_on |
| UI-02 | 選択肢ボタン（未選択） | 104×44 | ui_chip_off |
| UI-03 | ゲームスタートボタン | 650×200 | ui_start |
| UI-04 | メニューの枠 | 456×478 | ui_panel |

UI-01〜04 は素材リストにない仮の番号です（発注書に足すかは未定）。
画像に9スライスの枠を設定すると、引き伸ばしても角が崩れません。

## 構成
| ファイル | 役割 |
| --- | --- |
| `UiSkin` / `UiSkinEntry` | 素材IDと画像・フォントの表 |
| `UiArtSlot` | 画像の差し込み口。画像がなければ仮の色の四角を出す |
| `UiFontSlot` / `UiFontRole` | フォントの差し込み口（見出し・数字・本文） |
| `UiSkinApplier` | 画面の一番上に付け、子の差し込み口へ表の中身を配る |
| `UiOsFont` | 日本語フォントが未準備の間、実行中だけPCの日本語フォント（ヒラギノ／游ゴシック／メイリオ）を使う |
| `UiChip` / `UiOptionRow` | 選択肢ボタンと、対戦設定の1行 |
| `UiTitleView` / `UiTitleInput` | タイトル画面の表示とキー・マウス操作 |
| `Editor/UiMockBuilder` | 上のメニュー。プレハブとシーンを組み立てる |

## 残っていること
- 一度も Unity でコンパイルしていない（Mac で読み込む前に Windows へ移った）。最初にコンソールのエラーを確認する
- `.meta` は Unity が初めて読み込んだときに作られるので、作られたらコミットする
- 日本語フォント（準備-3）が未準備。編集中の画面では日本語が□になる（再生中は `UiOsFont` で出る）
- ゲームスタート後のシーン切り替えは未接続（`UiTitleInput` の On Start Requested につなぐ）
- 操作説明・試合・リザルトの画面はまだ
- モックは Notion の現行「画面構成」（1人・2人ボタン＋操作説明ボタン）ではなく旧案に近い。モックに合わせた
