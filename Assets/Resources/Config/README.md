# ゲームの調整値

`GameConfig.json` に、ゲームの調整値をまとめています。
値の意味と初期値は Notion「データと調整パラメータ」と同じです。

## 読み込み方

`Assets/_Project/Scripts/Kamakura/Core/GameConfig.cs` を使います。

```cs
private GameConfig _config;

private void Awake()
{
    _config = GameConfig.Load(); // 最初の1回だけ JSON を読み、あとは同じものを返す
}

float limit = _config.TimeLimit;
```

- JSON に書いていない値は、`GameConfig.cs` に書いた初期値のままになります。
- 書き方を間違えたとき（カンマや括弧）は、Console にエラーを出して、全部初期値で動きます。
- JSON はコメントを書けません。値の意味は下の表を見てください。

## キーと Notion の名前

| JSON のキー | Notion の名前 | 初期値 | 内容 |
| --- | --- | --- | --- |
| `holdTime` | `HOLD_TIME` | 10.0 | 受け取ったときの持ち時間（秒） |
| `warningTime` | （なし） | 3.0 | 残り何秒から警告の表示にするか（秒） |
| `missPenalty` | `MISS_PENALTY` | 2.0 | 不正解で減る時間（秒） |
| `stunTime` | `STUN_TIME` | 0.5 | 不正解のあと入力できない時間（秒） |
| `flightTime` | `FLIGHT_TIME` | 0.4 | 爆弾が飛んでいる時間（秒） |
| `arriveLock` | `ARRIVE_LOCK` | 0.15 | 受け取ってから入力を受け付けない時間（秒） |
| `introTime` | `INTRO_TIME` | 1.8 | ラウンド開始の演出（秒） |
| `boomTime` | `BOOM_TIME` | 2.0 | 爆発の演出（秒） |
| `roundsToWin` | `ROUNDS_TO_WIN` | 2 | 試合に勝つためのラウンド数（3も可） |
| `stagePasses` | `STAGE_PASSES` | [1, 3, 5, 7] | 難易度の各段階が始まる受け取り回数 |
| `defuseMinPass` | `DEFUSE_MIN_PASS` | 5 | 解除が使えるパス回数 |
| `isShrinkEnabled` | （`SHRINK_FROM_PASS` を無効にする） | true | 時間短縮を使うか |
| `shrinkFromPass` | `SHRINK_FROM_PASS` | 10 | 持ち時間を減らし始めるパス回数 |
| `shrinkMin` | `SHRINK_MIN` | 5.0 | 持ち時間の下限（秒） |
| `lampInterval` | `LAMP_INTERVAL` | 0.6 | ギミックDのランプが色を変える間隔（秒） |
| `idleReturn` | `IDLE_RETURN` | 30.0 | 展示用：無操作でタイトルに戻るまで（秒） |
