/// <summary>
/// 配線の謎の種類（T1〜T8）。
/// プレハブには番号で保存されるので、足すときは必ず最後に足す
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
    Lamp,

    /// <summary>T7 仲間外れ（○○△○ → △の線。数字版は偶数3つと奇数1つ、またはその逆）</summary>
    OddOneOut,

    /// <summary>T8 記憶（相手がさっき切ったのと同じ位置／同じ色の線。ラウンドの最初には出さない）</summary>
    Memory,

    /// <summary>T6 逆さ読み（「れきをりどみ」→ みどりをきれ → 緑の線）</summary>
    Reverse,

    /// <summary>T3 連想（「空と海に共通する色」→ 青の線。問題は Resources/Puzzles/WireAssociation.json）</summary>
    Association
}
