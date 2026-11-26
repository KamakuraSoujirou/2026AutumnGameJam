# UI仮組み（松下）

素材がなくても仮の四角で画面ができ、素材が届いたら `UiSkin` に画像を入れるだけで差し替わる UI の仮組みです。
今はタイトル画面です（モック画像に合わせた配置）。

## 使い方
1. Unity でスクリプトを読み込む（コンパイルが終わるまで待つ）
2. メニューバーの **Tools → UI仮組 → タイトルを作る**（下の「タイトル画面」を参照）
3. 素材が届いたら `Assets/_PrivateAssets/Art/` に置き、**Tools → UI仮組 → 素材を割り当てて反映**
   - ファイル名（下の表）か、素材IDで始まる名前（例：`art_01_bomb.png`）で自動で割り当てる
   - 手で `UiSkin.asset` に画像を入れてもよい（実行時は自動で反映、プレハブの見た目はこのメニューで更新）

## タイトル画面（Notion「画面構成」の旧案＝対戦設定＋操作の説明。2026-11-27 にこの形に決定）
1. メニューバーの **Tools → UI仮組 → タイトルを作る**
   - `Assets/_Project/Prefabs/Matsushita/UI/UI_Title.prefab`
   - `Assets/_Project/Scenes/Matsushita/UiMock_Title.unity`（確認用シーン。作ったあと開く）
2. 再生して操作を確認する
   - ↑↓（W S）：行を選ぶ ／ ←→（A D）：設定を変える ／ Space・Enter：ゲームスタート（今はコンソールに設定を出すだけ）
   - マウスでも、選択肢のクリックで設定を変え、「ゲームスタート」のクリックで始められる（EventSystem がなければ `UiEventSystem` が自動で作る）
   - 2人対戦のときは「CPUの強さ」の行を薄くして飛ばす

本番のゲーム進行からの使い方
- 選んだ設定は `UiTitleView` のプロパティで読む：`IsCpuMode` `CpuLevel` `WinsToMatch` `IsShortenEnabled` `IsVoiceEnabled`
- ゲームスタートは `UiTitleInput` の On Start Requested（Inspector）につなぐ（シーンの切り替えなど）

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
| `UiEventSystem` | マウスで押すための EventSystem を、なければ作る共通処理 |
| `UiChip` / `UiOptionRow` | 選択肢ボタンと、対戦設定の1行 |
| `UiTitleView` / `UiTitleInput` | タイトル画面の表示とキー・マウス操作 |
| `Editor/UiMockBuilder` | 上のメニュー。共通の部品は `UiMockBuilder.cs`、タイトル画面は `UiMockBuilder.Title.cs` |

## 残っていること
- 試合画面は別の PR（#20）。リザルト画面はこの PR から外した（コードはこのブランチの履歴 `171d89d` に残っている）
- ゲームスタート後のシーン切り替えは未接続（`UiTitleInput` の On Start Requested につなぐ）
- タイトルで選んだ設定（勝利条件・時間短縮など）を試合に渡す方法は未定（爆弾本体 #15 の `GameConfig` は JSON を読むだけなので、渡し方を相談する）
