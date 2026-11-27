/// <summary>
/// リザルトの表に出す、1ラウンド分の結果
/// </summary>
public readonly struct UiResultRound
{
    /// <summary>
    /// 1ラウンド分の結果を作る
    /// </summary>
    /// <param name="gimmickName">そのラウンドの仕組み（ギミック）の名前</param>
    /// <param name="winnerIndex">ラウンドを取った側（0：P1、1：P2）</param>
    /// <param name="isDefused">解除で決着したら true、爆発で決着したら false</param>
    /// <param name="passCount">そのラウンドのパス回数</param>
    public UiResultRound(string gimmickName, int winnerIndex, bool isDefused, int passCount)
    {
        GimmickName = gimmickName;
        WinnerIndex = winnerIndex;
        IsDefused = isDefused;
        PassCount = passCount;
    }

    /// <summary>
    /// そのラウンドの仕組み（ギミック）の名前
    /// </summary>
    public string GimmickName { get; }

    /// <summary>
    /// ラウンドを取った側（0：P1、1：P2）
    /// </summary>
    public int WinnerIndex { get; }

    /// <summary>
    /// 解除で決着したか（false なら爆発）
    /// </summary>
    public bool IsDefused { get; }

    /// <summary>
    /// そのラウンドのパス回数
    /// </summary>
    public int PassCount { get; }
}
