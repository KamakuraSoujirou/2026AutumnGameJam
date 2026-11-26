using UnityEngine;

/// <summary>
/// パス回数から、ギミックに渡す難易度の段階（1〜4）を決める
/// </summary>
public static class DifficultyCalculator
{
    private const int MinStage = 1;
    private const int MaxStage = 4;
    private const int PassesPerStage = 2;

    /// <summary>
    /// 受け取った回数から難易度の段階を返す
    /// （1〜2回目：1、3〜4回目：2、5〜6回目：3、7回目以降：4）
    /// </summary>
    /// <param name="receiveCount">そのラウンドで何回目の受け取りか（ラウンド最初の問題を1回目とする）</param>
    /// <returns>難易度の段階（1〜4）</returns>
    public static int GetStage(int receiveCount)
    {
        int stage = (receiveCount + PassesPerStage - 1) / PassesPerStage;
        return Mathf.Clamp(stage, MinStage, MaxStage);
    }
}
