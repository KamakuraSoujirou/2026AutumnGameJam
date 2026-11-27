/// <summary>
/// 同時押しの謎の種類（S1〜S5）
/// </summary>
public enum ChordPuzzleType
{
    /// <summary>S1 色指定（赤と青を同時に押せ）</summary>
    ColorPair,

    /// <summary>S2 色混ぜ（紫をつくれ → 赤と青）</summary>
    ColorMix,

    /// <summary>S3 同じ記号の2つ</summary>
    SameSymbol,

    /// <summary>S4 足して10になる2つ</summary>
    SumTen,

    /// <summary>S5 3つ同時（青以外を全部）</summary>
    ExceptOne
}
