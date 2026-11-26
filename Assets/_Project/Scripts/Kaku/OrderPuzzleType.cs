/// <summary>
/// 順番スイッチの謎の種類（C1〜C5）
/// </summary>
public enum OrderPuzzleType
{
    /// <summary>C1 数字の小さい順</summary>
    Number,

    /// <summary>C2 五十音順</summary>
    Alphabet,

    /// <summary>C3 時間の順</summary>
    Time,

    /// <summary>C4 大きさの順</summary>
    Size,

    /// <summary>C5 逆順（C1〜C4 のどれかを、逆の順で押す）</summary>
    Reverse
}
