/// <summary>
/// 爆弾本体の進行の状態
/// </summary>
public enum BombState
{
    /// <summary>ラウンド開始の待ち時間</summary>
    RoundStart,

    /// <summary>持っている人が問題に答えている</summary>
    Answering,

    /// <summary>爆弾が相手に飛んでいる</summary>
    Flying,

    /// <summary>爆発した</summary>
    Exploding,

    /// <summary>試合が終わった</summary>
    MatchOver
}
