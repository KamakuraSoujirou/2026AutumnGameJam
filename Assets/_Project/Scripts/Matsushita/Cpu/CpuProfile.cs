using UnityEngine;

/// <summary>
/// COMの強さごとの数値表と、「何秒考えるか」「正解するか」を決める処理（仕様書 v2「7.2 CPU対戦」）
/// 強さの調整は、このファイルの数値だけ見ればよい
/// </summary>
public class CpuProfile
{
    /// <summary>暗号・順番スイッチの2回目以降の入力までの間隔（秒）</summary>
    public const float FollowUpInterval = 0.3f;

    /// <summary>間違えるとき、その謎の定番の間違い（ひっかけ）を選ぶ確率</summary>
    public const float DecoyChance = 0.7f;

    private const float MinThinkWeight = 0.7f;
    private const float MaxThinkWeight = 1.6f;

    private static readonly CpuProfile Easy = new CpuProfile(1.8f, 2.6f, 0.6f, 0.80f);
    private static readonly CpuProfile Normal = new CpuProfile(1.2f, 1.8f, 0.4f, 0.90f);
    private static readonly CpuProfile Hard = new CpuProfile(0.7f, 1.2f, 0.3f, 0.97f);

    private readonly float _thinkMin;
    private readonly float _thinkMax;
    private readonly float _stageAddTime;
    private readonly float _accuracy;

    /// <summary>
    /// 数値を決めて作る
    /// </summary>
    /// <param name="thinkMin">1入力あたりの考える時間の下限（秒）</param>
    /// <param name="thinkMax">1入力あたりの考える時間の上限（秒）</param>
    /// <param name="stageAddTime">難易度の段階が1つ上がるごとに足す時間（秒）</param>
    /// <param name="accuracy">1入力あたりの正答率（0〜1）</param>
    private CpuProfile(float thinkMin, float thinkMax, float stageAddTime, float accuracy)
    {
        _thinkMin = thinkMin;
        _thinkMax = thinkMax;
        _stageAddTime = stageAddTime;
        _accuracy = accuracy;
    }

    /// <summary>
    /// 強さに合った数値表を返す
    /// </summary>
    /// <param name="level">COMの強さ</param>
    /// <returns>数値表</returns>
    public static CpuProfile Get(CpuLevel level)
    {
        switch (level)
        {
            case CpuLevel.Easy:
                return Easy;
            case CpuLevel.Hard:
                return Hard;
            default:
                return Normal;
        }
    }

    /// <summary>
    /// 次の入力までに考える時間を、乱数で決める
    /// </summary>
    /// <param name="stage">今の問題の難易度（1〜4）</param>
    /// <param name="thinkWeight">謎の重さ（Gimmick.ThinkWeight）。重いほど長く考える</param>
    /// <param name="isFollowUp">暗号・順番スイッチで、正解を押したあとの続きの入力か</param>
    /// <returns>考える時間（秒）</returns>
    public float RollThinkTime(int stage, float thinkWeight, bool isFollowUp)
    {
        if (isFollowUp)
        {
            return FollowUpInterval;
        }

        float weight = Mathf.Clamp(thinkWeight, MinThinkWeight, MaxThinkWeight);
        float baseTime = Random.Range(_thinkMin, _thinkMax) + _stageAddTime * Mathf.Max(0, stage - 1);
        return baseTime * weight;
    }

    /// <summary>
    /// この入力を正解するかを、正答率の確率で決める
    /// </summary>
    /// <returns>正解するなら true</returns>
    public bool RollIsCorrect()
    {
        return Random.value < _accuracy;
    }

    /// <summary>
    /// 間違えるとき、定番の間違い（ひっかけ）を選ぶかを、確率で決める
    /// </summary>
    /// <returns>ひっかけを選ぶなら true</returns>
    public bool RollUsesDecoy()
    {
        return Random.value < DecoyChance;
    }
}
