/// <summary>
/// 配線の謎の種類（できているものだけ。T3・T6・T7・T8 はまだ）
/// </summary>
public enum WirePuzzleType
{
    /// <summary>T1 色指定（赤い★ → 赤の線）</summary>
    ColorMark,

    /// <summary>T2 文字と色の食い違い（青い字の「あか」→ 赤の線）</summary>
    ColorWord,

    /// <summary>T4 シリアル番号の条件</summary>
    Serial,

    /// <summary>T5 ランプ（点灯なら★の反対側、消灯なら★の線）</summary>
    Lamp
}
