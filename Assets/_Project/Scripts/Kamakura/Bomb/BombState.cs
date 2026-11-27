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

    /// <summary>持っている人が解除（4択）に挑戦している。残り時間は減り続ける</summary>
    Defusing,

    /// <summary>爆発した（時間切れか、解除の失敗）</summary>
    Exploding,

    /// <summary>解除に成功した（持っていた人がラウンドを取る）</summary>
    Defused,

    /// <summary>試合が終わった</summary>
    MatchOver
}
