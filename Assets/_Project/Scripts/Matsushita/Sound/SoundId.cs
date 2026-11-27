/// <summary>
/// 鳴らす音の種類。Notion「サウンド発注書」の素材IDと対応する
/// </summary>
public enum SoundId
{
    /// <summary>SE-01 チクタク（残り時間が1秒減るたび）</summary>
    Tick,

    /// <summary>SE-02 心拍（残りわずかのあいだループ）</summary>
    Heartbeat,

    /// <summary>SE-03 ボタン押下</summary>
    Button,

    /// <summary>SE-04 配線を切る</summary>
    WireCut,

    /// <summary>SE-05 不正解</summary>
    Wrong,

    /// <summary>SE-06 爆弾の着地（受け取ったとき）</summary>
    Land,

    /// <summary>SE-07 爆発</summary>
    Explosion,

    /// <summary>SE-08 爆発のあとの間抜けな音</summary>
    Funny,

    /// <summary>SE-09 解除開始</summary>
    DefuseStart,

    /// <summary>SE-10 解除成功</summary>
    DefuseSuccess,

    /// <summary>SE-11 ラウンド開始</summary>
    RoundStart,

    /// <summary>SE-12 カーソル移動</summary>
    Cursor,

    /// <summary>SE-12 決定</summary>
    Decide,

    /// <summary>SE-12 キャンセル</summary>
    Cancel,

    /// <summary>SE-13 ファンファーレ</summary>
    Fanfare,

    /// <summary>BGM-01 試合中</summary>
    BgmBattle,

    /// <summary>BGM-02 タイトル</summary>
    BgmTitle,

    /// <summary>BGM-03 リザルト</summary>
    BgmResult
}
