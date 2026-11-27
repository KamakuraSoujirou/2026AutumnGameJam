# サウンド（効果音・BGM）

シーンに何も置かなくても、ゲームを起動すると `SoundPlayer` が自動でできて音が鳴ります。

## 鳴らし方

どのスクリプトからでも、次のように1行で呼べます。

```cs
SoundPlayer.PlaySe(SoundId.DefuseStart);   // 効果音を1回鳴らす
SoundPlayer.PlayBgm(SoundId.BgmResult);    // BGMを切り替える
SoundPlayer.PlayLoop(SoundId.Heartbeat);   // ループで鳴らす（StopLoop で止める）
```

`SoundId` は Notion「サウンド発注書」の素材IDと対応しています（`SoundId.cs` のコメントを参照）。

## もう鳴るようになっているところ

| いつ | 音 | どこで鳴らしているか |
| --- | --- | --- |
| タイトル画面 | BGM-02 | `SoundPlayer`（Title シーンを読み込んだとき） |
| 試合中 | BGM-01 | `SoundPlayer`（Game シーンを読み込んだとき）、`BombSoundHooks`（再戦のとき） |
| ラウンド開始 | SE-11 | `BombSoundHooks` |
| 残り時間が1秒減るたび | SE-01 | `BombSoundHooks` |
| 残りわずか（GameConfig の WarningTime） | SE-02 をループ | `BombSoundHooks` |
| ボタンを押した | SE-03（配線ギミックのときは SE-04） | `BombSoundHooks` |
| 不正解 | SE-05 | `BombSoundHooks` |
| 爆弾を受け取った | SE-06 | `BombSoundHooks` |
| 爆発 | SE-07、少しあとに SE-08 | `BombSoundHooks` |
| 試合終了 | SE-13 のあと BGM-03 | `BombSoundHooks` |
| タイトルとリザルトのメニュー | SE-12（カーソル・決定・キャンセル） | `UiTitleView` / `UiTitleInput` / `UiResultView` |

`BombSoundHooks` は、シーンに `BombManager` があれば自動で同じオブジェクトに付きます。`BombManager` と `BombTimer` のイベントを聞いているだけなので、ほかの人のスクリプトは変えていません。

## まだのもの

- **SE-09 解除開始 / SE-10 解除成功**：解除（ひらめき謎）ができたら、始めたときと成功したときに `SoundPlayer.PlaySe(SoundId.DefuseStart)` / `SoundPlayer.PlaySe(SoundId.DefuseSuccess)` を呼んでください。
- **ボイス（VO-01〜06）**：まだ入れていません。

## 音ファイルの入れ方

音ファイル（バックアップ素材）はリポジトリに入れず、`BackupSound.unitypackage` で配っています。

1. 松下から `BackupSound.unitypackage` を受け取る（公開の場所には置かない）
2. Unity で **Assets > Import Package > Custom Package...** から読み込み、全部にチェックを入れたまま Import する
3. `Assets/_PrivateAssets/ThirdParty/BackupSound/` に入れば完了。`SoundLibrary.asset` が自動でつながる

- 効果音ラボのボイスなど再配布できない素材が入っているので、チーム外に渡したり、公開リポジトリにコミットしたりしないでください。
- 入れていなくても、音が鳴らないだけでエラーにはなりません。

## 音の差し替え

- 対応表は `Assets/Resources/Sound/SoundLibrary.asset` です。Inspector で Clip と音量を入れ替えれば、コードを変えなくても音が変わります。
- 今入っているのは、`Assets/_PrivateAssets/ThirdParty/BackupSound/` のバックアップ素材の1つ目の候補です（試聴していません）。
- `SoundLibrary.asset` がないときは、エディタを開いたときに自動で作られます。最初の状態に戻したいときは、メニューの **Tools > サウンド > SoundLibrary をバックアップ素材で作り直す** を使います。
- 音ファイルが入っていない（サブモジュールがない）ときは、音が鳴らないだけでエラーにはなりません。
