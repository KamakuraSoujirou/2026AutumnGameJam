using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// パス回数から、ギミックに渡す難易度の段階（1〜4）を決める
/// </summary>
public static class DifficultyCalculator
{
    private const int MinStage = 1;
    private const int MaxStage = 4;

    /// <summary>
    /// 受け取った回数から難易度の段階を返す
    /// （stagePasses が 1・3・5・7 なら、1〜2回目：1、3〜4回目：2、5〜6回目：3、7回目以降：4）
    /// </summary>
    /// <param name="receiveCount">そのラウンドで何回目の受け取りか（ラウンド最初の問題を1回目とする）</param>
    /// <param name="stagePasses">各段階が始まる受け取り回数（GameConfig.StagePasses）</param>
    /// <returns>難易度の段階（1〜4）</returns>
    public static int GetStage(int receiveCount, IReadOnlyList<int> stagePasses)
    {
        int stage = 0;

        for (int i = 0; i < stagePasses.Count; i++)
        {
            if (receiveCount >= stagePasses[i])
            {
                stage = i + 1;
            }
        }

        return Mathf.Clamp(stage, MinStage, MaxStage);
    }
}
