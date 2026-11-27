# UI仮組み（松下）

素材がなくても仮の四角で画面ができ、素材が届いたら `UiSkin` に画像を入れるだけで差し替わる UI の仮組みです。
タイトル画面と試合画面があります（モック画像に合わせた配置）。

## 使い方
1. Unity でスクリプトを読み込む（コンパイルが終わるまで待つ。Mac は Cmd+R、Windows は Ctrl+R で更新）
2. メニューバーの **Tools → UI仮組 → タイトルを作る**
   - `Assets/_Project/Prefabs/Matsushita/UI/UI_Title.prefab`
   - `Assets/_Project/Prefabs/Matsushita/UI/UiSkin.asset`（素材の表）
   - `Assets/_Project/Scenes/Matsushita/UiMock_Title.unity`（確認用シーン。作ったあと開く）
3. 再生して操作を確認する
   - ↑↓（W S）：行を選ぶ ／ ←→（A D）：設定を変える ／ Space・Enter：ゲームスタート（今はコンソールに設定を出すだけ）。マウスでも、選択肢のクリックで設定を変え、「ゲームスタート」のクリックで始められる（EventSystem がなければ `UiTitleView` が自動で作る）
   - 2人対戦のときは「CPUの強さ」の行を薄くして飛ばす
   - ルール：右端の「ルール」タブをクリック、または R で、パネルが右から飛び出す（`UiRulesTab`。スクロールなしで全文を出す）。閉じるのは Esc・暗い所のクリック・もう一度 R。開いている間は後ろを暗くして、設定の操作・Space・Enter・ゲームスタートのクリックを止める。閉じているときにタブへマウスを乗せると少し顔を出す。ルールの文章は `UiMockBuilder.cs` の `BuildRules` にあり、増やすときは `DrawerHeight` と本文の高さも足す
   - すでにあるプレハブを作り直さずに、ルールだけタブに差し替えるには **Tools → UI仮組 → タイトルのルールをタブにする**（Title シーン側の設定が外れないよう、こちらを先に使う）
4. 素材が届いたら `Assets/_PrivateAssets/Art/` に置き、**Tools → UI仮組 → 素材を割り当てて反映**
   - ファイル名（下の表）か、素材IDで始まる名前（例：`art_01_bomb.png`）で自動で割り当てる
   - 手で `UiSkin.asset` に画像を入れてもよい（実行時は自動で反映、プレハブの見た目はこのメニューで更新）

## 試合画面（インゲームのモックと爆弾のラフに合わせた配置）
1. メニューバーの **Tools → UI仮組 → 試合画面を作る**
   - `Assets/_Project/Prefabs/Matsushita/UI/UI_Game.prefab`
   - `Assets/_Project/Scenes/Matsushita/UiMock_Game.unity`（確認用。`UiGamePreview` 付き）
2. 再生して、キーで動きを確かめる（ゲームの進行はまだ無くても動く）

| キー | 動き |
| --- | --- |
| Space | 正解（導火線が消え、手に持っている爆弾が弧を描いて相手の手へ0.4秒で飛ぶ。中央の爆弾はその場で消え、着地すると戻る。相手側に導火線が伸び、吹き出しが弾む） |
| 方向キー（P1：WASD、P2：矢印）かマウス | 持っている側の入力。液晶に出た色の線が正解で、押し込みの見た目が終わってから爆弾が飛ぶ。違う線なら−2秒 |
| X | 不正解（−2秒の表示） |
| Q | 解除の挑戦（パス5回目から。画面が暗くなり、相手側に「解除中！」） |
| B | 爆発（文字が弾み、閃光が薄れる。負けた側がアフロ、勝ち星が増える） |
| T | 考え中の吹き出し「…」 |
| Enter | リザルトを出す |
| Esc | 「戻る」（コンソールに出すだけ。本番はタイトルへ） |
| R | ラウンドをやり直す（バナーが出る） |
| C | 待機中の側にランダムなクイックテキスト（CPUの確認用） |
| 待機中の側の方向キー（P1：WASD、P2：矢印）を押し続けて、離す | クイックテキスト（下の「クイックテキスト」を参照） |

画面の配置（1920×1080）
- 上：左に「戻る」＋「Esc でタイトルへ」、中央に「R1」、右に「パス回数」「難易度」。
- 中央：爆弾（640×640）。上の口金から導火線が出て、持っている側のキャラの横まで伸び、先に火花が付く。
- 爆弾の中：ひし形ボタン4つ（画面の位置は上・左・右・下。ただし方向の「上」は画面の下、「下」は画面の上のボタン。色は配線の色）、中央の赤いランプ、その下のタイマー液晶、下半分の口の形の液晶（問題文と解除の状態）。本体の絵が届いたあとは、液晶の枠は絵に含まれ、文字だけを重ねる。
- 左右：キャラ（P1が左、P2が右）。立ち絵は 1.3 倍（足元の中心を軸に拡大し、足元を45px下げ、頭上の吹き出しを50px上げてある。倍率などは `UiMockBuilder.Game.cs` の `CharacterScale`・`CharacterDropY`・`BubbleRiseY`）。頭上の吹き出しが、持っている側は「あとN秒！」、持っていない側は「待機中」。
- 下：左「P1 ★★★」、中央「ギミック名」、右「★★★ COM」（2人対戦のときは P2）。

動き（プレビューで確認できる）
- 導火線：残り時間に比例して、火花が爆弾へ進みながら短くなる。持ち主が変わると、反対側に伸び直す（0.35秒）。火花はちらつき、残り3秒以下は速くなる。
- 爆弾の受け渡し：導火線が消え、立ち絵が手に持っている爆弾（`UiPlayerPanel` の `HeldBomb`）が、投げる側の手から受ける側の手へ弧を描いて飛ぶ（大きさは変わらない）。中央の爆弾は動かず、飛び始めで見えなくなり、届いたら戻る。飛んでいる間は時間が止まる。
- 立ち絵（`Character/Normal`・`Panic`・`Afro`）と爆弾の本体（`Bomb/Body`）は、UI の Image ではなく、子の `Art`（SpriteRenderer ＋ `UiSpriteArtSlot`）という普通の GameObject。親の四角に収まるよう大きさと位置が自動で合う（四角を変えたら `Art` を右クリック →「四角に合わせる」）。描画順は `Art` の `Sorting Order`。画面に映るのは Canvas が `UiWorldCanvas` でカメラに映っているときだけ。既存のプレハブは、メニュー「Tools/UI仮組/立ち絵と爆弾の本体をGameObjectにする」でその場で変換できる（作り直さないので参照は壊れない）。
- 手の爆弾：持っている側の手に出る（素材ID `ART-01_held`。飛ぶ爆弾も同じ絵）。位置は立ち絵ごとに、プレハブの `PlayerP1/HeldBomb`・`PlayerP2/HeldBomb` を動かして手に合わせる。素材が届くまでは丸い仮の形。立ち絵に爆弾が描かれる場合は、爆弾だけ別の絵にしてもらう。
- 吹き出し：文字が変わるたびに弾む。
- 残り3秒以下：タイマーが赤くなる／爆弾が揺れる／持っている側の画面の端の赤い帯が点滅する／持っている側のキャラが焦り顔になる。
- 爆発：文字が弾んで閃光が薄れ、負けた側がアフロになる。バナー：ポンと弾んで出る。

本番のゲーム進行からの使い方
- `UiGameView` のメソッドを呼ぶ：`SetTime` `SetHoldTime` `SetHolder` `SetRound` `SetPass` `SetPlayerNames` `SetGimmickName` `SetWins` `ShowBanner` `ShowPenalty` `SetDefuse` `ThrowBomb` `ShowExplosion` `ShowResult`。「戻る」は `RequestBack()`（または Inspector の On Back Requested）。
- 爆弾の中身は `UiGameView.Bomb`：`SetWire` `SetButtonLabel` `SetWireSymbol` `SetQuestion` `SetLamp` `SetInput` `SetDefuseState`。方向は `InputDirection` と同じ。シリアル番号は廃止した（`SetSerial` は何もしないメソッドとして残してあるので、呼び出しは消してよい）。
- 暗号・順番スイッチのボタンの文字は `SetButtonLabel`（空にすると色名の頭文字に戻る）。配線のギミックでは、色名の頭文字（赤・青・黄・緑）がボタンに出る。
- プレイヤー入力：`PlayerInputReader` がキーボードを読む（P1：WASD と Q、P2：矢印キーと 右Shift か /、戻る：Esc）。爆弾の上と下のボタンを入れ替えてあるので、上のキー（W・↑）は方向 `Down`、下のキー（S・↓）は方向 `Up` として知らせる（押したキーと同じ側のボタンが反応する）。`DirectionPressed(プレイヤー, 方向)` `DefusePressed(プレイヤー)` `BackPressed` で知らせる。押した瞬間だけで、長押しは1回。CPU対戦では `IsCpuMode` をオンにすると、どちらのキーセットでもP1を操作できる。持っているか・スタン中かの判断はゲーム進行側。ゲームパッドは使わない。
- 入力の見た目：ひし形ボタンは、マウスが重なると明るく、押している間は暗くなる。絵が3状態（normal・light・blackout）そろっていれば light／blackout の絵に切り替え、そろっていなければ白を25%・黒を30%重ねる。ちょんと押しても、暗い状態を最低0.15秒は見せる。キーで押したときは `UiBombView.PressButton(方向)` を呼ぶと、0.15秒だけ暗くなって戻る。濃さと長さは `UiWire` の Inspector で変えられる（確認用シーンでは、持っている側の入力だけ反応する）。
- 正解したあとの流れ：`ThrowBomb` は、ボタンの押し込みの見た目が終わるまで爆弾を見せてから飛ばす。待った分は飛ぶ時間から引くので、届く時刻は変わらない（飛ぶ時間の下限は0.12秒）。爆弾の中の表示（切れた線など）を戻したいときは、`ThrowBomb` の `onTakeOff`（飛び始めに呼ばれる）の中で行う。
- 試合画面を作り直すと（`Tools/UI仮組/…`）、明るさ用の画像（`Tint_Art` `Tint_Placeholder`）と `UiWirePointerRelay` が入る。古いプレハブのままでは、明るさは変わらない。
- マウス：爆弾のひし形ボタンのクリックは `UiGameView.DirectionClicked(方向)`（マウスは誰の入力か分からないので、持ち主の入力として扱うかはゲーム進行側で決める。確認用シーンでは持ち主の入力として扱う）。
- マウス：「戻る」のクリックは `RequestBack`（On Back Requested）、爆弾の液晶の「解除」ボタンのクリックは `UiGameView.DefuseRequested` イベントで知らせる（解除できるかの判断はゲーム進行側。ボタンは解除できるときだけ表示される）。EventSystem がなければ `UiGameView` が自動で作る。
- タイマーの表示は「05:00」の形（ラフに合わせた。区切りは `UiGameView.TimeSeparator`）。
- 試合画面の素材ID：UI-12〜16・18、ART-01（爆弾本体）、ART-02（導火線）、ART-03_（配線 `wire_red` など、切断は `wire_cut_red` など）、ART-04_（ランプ）、ART-05_（ひし形ボタン。色×3状態）、ART-08（火花）、ART-09/10 のアフロと ART-11（焦り顔）、ART-12、ART-15_（星）、ART-16〜18・20、UI-07。大きさは `UiSkin` の各登録に書いてある。
- 爆弾の絵（`Assets/_PrivateAssets/Art/Bomb/`）の入れ先：

| 絵のファイル | 素材ID | 入れ先（`UI_Game` の中） |
| --- | --- | --- |
| `core/bomb_core.png`（800×800） | `ART-01` | `Bomb/Body`。640×640 に縮めて出す。口金・X字の傷・左右のコネクタ・タイマーと口の液晶の枠まで絵に含まれる（`ART-01_cap`・`UI-06`・`UI-17` は廃止） |
| `lamp/bomb_lamp.png`（80×80） | `ART-04_on` | `Bomb/LampOn`。X字の中心。消灯の絵は `lamp/bomb_lamp_blackout.png`（`ART-04_off`、`Bomb/LampOff`）。空の間は点灯の絵を暗くして出す |
| `switch/<色>/bomb_switch_<色>_normal.png` | `ART-05_<色>_normal` | `Wire○/Button/Diamond_<色>`（通常） |
| `switch/<色>/bomb_switch_<色>_light.png` | `ART-05_<色>_light` | 同 `Diamond_<色>/Light`（マウスが重なっている間） |
| `switch/<色>/bomb_switch_<色>_blackout.png` | `ART-05_<色>_blackout` | 同 `Diamond_<色>/Blackout`（押している間。キーで押したときも） |

  - 色は red・blue・yellow・green の4色。各 200×200。絵のつまみは、赤と青が右、黄と緑が左に描かれている。つまみは、配線が伸びる側（上と左は左、右と下は右）に向けたいので、合わない色は `Diamond_<色>` の Scale X を −1 にして反転してある（文字は反転しない）
  - ひし形の中心は絵の中心より少し上で、つまみと反対側に寄っている（つまみが右の絵は 45%、左の絵は 55%。縦は 47.5%）。`Diamond_<色>` のピボットをそこに置いてある。ボタンの中心は本体の絵のX字の頂点：画面の上 (400,145)・左 (255,263)・右 (545,263)・下 (400,385)（800×800 の絵の座標）。方向の「上」（`InputDirection.Up`）のボタンが画面の下の頂点、「下」のボタンが画面の上の頂点にある（入れ替えてある）
  - 3状態がそろっているときだけ絵で切り替え、そろっていないときは、これまでの白・黒の重ねで明るさを変える
  - 位置は目で測った値。Unity で重ねて見ながら直す
- 配線・ボタンの色は、素材がないときはコードの色で塗る。

## リザルト画面（見本画像に合わせた配置）
1. メニューバーの **Tools → UI仮組 → リザルト画面を作る**
   - `Assets/Resources/UI/UI_Result.prefab`（試合中に `UiResultRecorder` が `Resources.Load` で読むので、Resources の下に置く）
   - `Assets/_Project/Scenes/Matsushita/UiMock_Result.unity`（確認用。`UiResultPreview` 付き）
2. 再生して操作を確かめる

| 操作 | 動き |
| --- | --- |
| ←→（A D） | 左下の「HOME」と右下の「再戦」を選ぶ（最初は再戦） |
| Space・Enter | 選んでいるボタンを決める（出てくる動きの途中なら、動きを飛ばして最後まで出す） |
| Esc | HOME（タイトルへ） |
| マウス | 「HOME」「再戦」をクリックできる |
| 1 / 2（確認用） | 勝者を P1 / P2 に切り替える |

画面の配置（見本画像 2000×1125 を、座標のまま 0.96 倍して 1920×1080 に合わせてある）
- 上：左にP1、右にP2のキャラ。名前（P1・P2）は大きくキャラの下に重ねる。勝った側は勝ち顔と「WIN」（内側の高い位置）、負けた側はアフロと「LOSE」（内側の低い位置）。
- 上の中央：両脇に縦線を引いた、勝ち数「P1の勝ち数 ー P2の勝ち数」。
- 下：表（ラウンド｜仕組み｜P1｜決着｜P2｜パス回数）。取った側に★、決着は「爆発」か「解除」、パス回数は「00回」の形。最大5行。
- 左下：「HOME」（タイトルへ）。右下：丸い「再戦」ボタン（もう一度）。選んでいるほうに青い枠が出る。

出てくる動き（`Show` を呼ぶたびに最初から。約4秒）：キャラ → 「WIN」（大きくポン）／「LOSE」（上から落ちる） → 勝ち数の数え上げ → 表を1行ずつ（★は少し遅れてポン） → ボタン。終わるまでボタンは選べない。`UiResultView` の Is Intro Enabled をオフにすると、最初から全部出す。

試合からのつなぎ込み（自動。シーンにもプレハブにも何も置かない）
- `Result/UiResultRecorder` が、シーンに `BombManager` があれば自動で付く（`BombSoundHooks` と同じ仕組み）。`BombManager` の知らせを見て、爆発のたびにラウンドの結果（仕組み名・取った側・パス回数）を貯め、試合が終わったら `Resources/UI/UI_Result` を試合画面の上に重ねて出す。
- 再戦：リザルトを閉じて `BombManager.TryRematch()`。HOME：`Title` シーンを読み込む。
- 試合画面にあった簡易リザルトの呼び出しは、`BombUiPresenter` の `MatchOver` から外した（`UiGameView.ShowResult` 自体は残してある）。
- 決着は今は常に「爆発」（`BombManager` に、解除でラウンドが終わる処理がないため）。入ったら `UiResultRecorder.RecordRound` の `isDefused` に渡す。

使い方（単体）
- `UiResultView.Show(勝者, P1の名前, P2の名前, UiResultRound[])` で表示する。勝者は 0：P1、1：P2。
- `UiResultRound` は1ラウンド分（仕組みの名前・取った側・解除で決着したか・パス回数）。勝ち数は、この配列から数えて上に出す。
- 選ばれたときは `RestartRequested`（再戦）と `TitleRequested`（HOME）が呼ばれる。シーンの切り替えなどはここにつなぐ。
- 試合画面（`UiGameView`）にも簡易のリザルトが残っているが、本番では使っていない（上のとおり呼び出しを外した）。不要なら `UiGameView` から消してよい。
- リザルト画面の素材ID：ART-21（背景）、ART-11_p1_win・ART-11_p2_win（勝ち顔）、ART-22（WIN）、ART-23（LOSE）、UI-21（HOME）、UI-22（再戦・丸）。アフロは試合画面の ART-09_afro・ART-10_afro、★は ART-15_on を使う。表の線は仮の四角で引いてあり、背景に絵を入れるときは線も絵に含めてよい。

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
| `Game/UiGameView` | 試合画面の表示をまとめる。進行（時間・持ち主・パス回数など）を渡すと画面に反映する |
| `Game/UiBombView` / `Game/UiWire` | 爆弾（タイマー液晶・ランプ・口の形の液晶）と、ひし形ボタン＋配線1組（重なりで明るく、押下で暗く） |
| `Game/UiWirePointerRelay` | ひし形ボタンへの重なり・押下を `UiWire` に渡す（Button と同じオブジェクトに付ける） |
| `Game/UiFuse` | 導火線。火花が爆弾へ進み、残り時間に比例して短くなる |
| `Game/UiPlayerPanel` | キャラ（通常・焦り顔・アフロ）、頭上の吹き出し（待機中はクイックテキストの文も）、クイックテキストの候補、残り3秒以下の赤い帯 |
| `Game/QuickTextInput` / `Game/UiQuickTextWheel` / `UiQuickTextSet` | クイックテキストの入力（押し続ける→離す・取り消し）、円形の候補、文のセット |
| `Game/UiWinStars` | 勝ち星 |
| `Game/UiGamePreview` | 試合画面の確認用（キーで動きを再現）。本番では外す |
| `UiEventSystem` | マウスで押すための EventSystem を、なければ作る共通処理 |
| `Result/UiResultView` / `Result/UiResultInput` | リザルト画面の表示・HOMEと再戦の選択・マウス操作と、キー操作 |
| `Result/UiResultPlayer` / `Result/UiResultRow` / `Result/UiResultRound` | リザルトのキャラ1人分、表の1行、1ラウンド分の結果（Player と Row は出てくる動きの進み具合も受け持つ） |
| `Result/UiResultRecorder` | 試合のラウンドの結果を集めて、試合が終わったらリザルトを重ねて出す。再戦・HOME の処理もここ（自動で付く） |
| `Result/UiResultEase` | 出てくる動きの変換（ゆっくり止まる・弾む・落ちる） |
| `Result/UiResultPreview` | リザルト画面の確認用（仮の結果を出す）。本番では外す |
| `Editor/UiMockBuilder` | 上のメニュー。プレハブとシーンを組み立てる |

## クイックテキスト（待機中の側が、頭上の「待機中」を定型文に変える）
爆弾を持っていない側が方向キー（P1：WASD、P2：矢印）を押し続けると、0.2秒後に円形の候補が出る（上・下・左・右の4つ。押している方向の扇が黄色）。キーを離すと、その方向の文が頭上の吹き出しに1.5秒出て「待機中」に戻る。短く押して離したときも出る。押している間に解除キー（Q／右Shift）を押すと取り消し（キーを全部離すまで何も出ない）。使えるのは、待機中で、相手が解除中でなく、バナー・爆発・リザルトも出ていないとき。残り時間や爆弾の入力には関わらない。仕様は `docs/spec-ato10byo-v2.md` の「4.1 クイックテキスト」。

1. すでにある `UI_Game.prefab` には、メニューバーの **Tools → UI仮組 → クイックテキストを足す** で、作り直さずにその場で足す（候補と、文のセット `UiQuickTextSet.asset`）。「試合画面を作る」で作り直したときは、最初から入る
2. 文を変えるときは `Assets/_Project/Prefabs/Matsushita/UI/UiQuickTextSet.asset` の Texts（上＝W・↑、下＝S・↓、左＝A・←、右＝D・→ の順）を書き換える。空欄は初期の文（いくぞ！／ざんねん〜／まだまだ！／ナイス！）
3. 本番の使い方：`UiGameView` が起動時に `QuickTextInput` を自分に付けて、`PlayerInputReader`（同じオブジェクトか、シーンから探す）の `DirectionPressed` `DirectionReleased` `DefusePressed` を見る。ゲーム進行側は何もしなくてよい。「使えるか」を別の条件にしたいときは `QuickTextInput.CanUse` に処理を渡す
4. 呼び出し：`UiGameView.ShowQuickText(プレイヤー, 方向)`（方向はキー入力として届いた `InputDirection`）、`ShowRandomQuickText(プレイヤー)`（CPU用。`CpuPlayer` が人間の回答中に5〜11秒おきに呼ぶ）、`SetQuickTextWheel(プレイヤー, 開くか, 選んでいる方向)`、`CanUseQuickText(プレイヤー)`。文を出しておく時間は `UiGameView` の Quick Text Seconds（1.5秒）、候補が出るまでは `QuickTextInput` の Wheel Delay Seconds（0.2秒）
5. 上と下のボタンを入れ替えてあるため、W は `InputDirection.Down` として届く。文の並びは「押したキーの画面の位置」（W＝上）で、`UiQuickTextSet.SlotOf` が対応させる
6. 候補の円は素材なしの仮の形（扇は `Image` の塗りつぶし）。絵にするときは、扇・中央の丸の `Image` に絵を入れる

## 残っていること
- Windows の Unity（6000.3.28f1）のバッチ実行で、「タイトルを作る」「試合画面を作る」の実行と、試合画面の表示・動き（キー入力を送った再生）を確認した。実機のエディタで開いた見た目と、本物の画像を入れたときの見た目は未確認
- `.meta` は Unity が読み込んだときに作られるので、作られたらコミットする
- 試合画面は表示だけ。ゲームの進行（MatchController など）と、`Gimmick` の `QuestionText` などを `UiBombView` につなぐのはこれから
- 配線の仮の線は、一時的に隠してある（`UiMockBuilder.Game.cs` の `IsWireHiddenForNow`。配線の絵が決まったら false にして試合画面を作り直す。プレハブ上では `UiWire` の Is Wire Hidden）。ランプの消灯も隠せる（`IsLampOffHiddenForNow`、`UiBombView` の Is Lamp Off Hidden）が、消灯の絵が届いたので今は隠していない
- 爆弾の本体・ランプ・ひし形ボタンは絵が届いた（`Art/Bomb/`。Unity での作り直しと見た目の確認はこれから）。配線・導火線・火花・キャラは仮の四角。ラフ（爆弾・赤髪の女の子・青髪の男の子）は配置の参考にしただけで、画像としては取り込んでいない。P1＝青髪の男の子（ART-09）、P2＝赤髪の女の子（ART-10）として扱う。男の子のラフは左向きなので、左側のP1に置くと相手と反対を向く（絵を反転するか、向きを描き直す）
- 日本語フォント（準備-3）が未準備。編集中の画面では日本語が□になる（再生中は `UiOsFont` で出る）
- ゲームスタート後のシーン切り替えは未接続（`UiTitleInput` の On Start Requested につなぐ）
- リザルト画面は、コードのコンパイルまで確認した。Unity で「リザルト画面を作る」を実行しての表示と操作は未確認
- リザルト画面の背景・勝ち顔などの素材はこれから（絵がないところは灰色の仮の形）。ゲーム進行からのつなぎ込み（`UiResultRecorder`）は入れた
- 操作説明の画面はまだ
- モックは Notion の現行「画面構成」（1人・2人ボタン＋操作説明ボタン）ではなく旧案に近い。モックに合わせた
