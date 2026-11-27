/// <summary>
/// 解除が使えるかどうか（液晶の下段の表示に使う）
/// </summary>
public enum DefuseStatus
{
    /// <summary>まだ使えない（パス回数が足りない、スタン中、爆弾を持っていない）</summary>
    Locked,

    /// <summary>持ち手が解除に挑戦できる</summary>
    Available,

    /// <summary>解除に挑戦中</summary>
    Challenging
}
