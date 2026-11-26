/// <summary>
/// 暗号の謎の種類（B1〜B5）
/// </summary>
public enum CipherPuzzleType
{
    /// <summary>B1 そのまま</summary>
    Same,

    /// <summary>B2 対応表</summary>
    Table,

    /// <summary>B3 計算</summary>
    Calc,

    /// <summary>B4 逆順</summary>
    Reverse,

    /// <summary>B5 数える</summary>
    Count,
}