/// <summary>
/// 配線の色。爆弾の4本の線はこの4色を1本ずつ使う
/// </summary>
public enum UiWireColor
{
    Red,
    Blue,
    Yellow,
    Green
}

/// <summary>
/// 配線の状態
/// </summary>
public enum UiWireState
{
    /// <summary>つながっている</summary>
    Normal,

    /// <summary>真ん中が切れた状態（正解の演出）</summary>
    Cut,

    /// <summary>線が消えている（不正解で切った線）</summary>
    Gone
}

/// <summary>
/// 解除の状態。爆弾の液晶の下段に出す
/// </summary>
public enum UiDefuseState
{
    /// <summary>まだ使えない（パス5回目から）</summary>
    Locked,

    /// <summary>使える</summary>
    Available,

    /// <summary>このラウンドで使用済み</summary>
    Used,

    /// <summary>挑戦中</summary>
    Challenging
}
