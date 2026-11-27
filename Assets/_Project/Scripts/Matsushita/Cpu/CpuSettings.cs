using UnityEngine;

/// <summary>
/// タイトルで選んだ「CPU対戦か・COMの強さ」を、シーンをまたいで持つ
/// アプリを閉じるか Play Mode を止めると、初期値（2人対戦）に戻る
/// </summary>
public static class CpuSettings
{
    private static bool _isCpuMode;
    private static CpuLevel _level = CpuLevel.Normal;

    /// <summary>CPU対戦か（オンのとき、P2がCOMになる）</summary>
    public static bool IsCpuMode => _isCpuMode;

    /// <summary>COMの強さ</summary>
    public static CpuLevel Level => _level;

    /// <summary>
    /// 対戦の相手を設定する
    /// </summary>
    /// <param name="isCpuMode">CPU対戦なら true</param>
    /// <param name="level">COMの強さ</param>
    public static void Set(bool isCpuMode, CpuLevel level)
    {
        _isCpuMode = isCpuMode;
        _level = level;
    }

    /// <summary>
    /// タイトルの行の番号（0：やさしい、1：ふつう、2：つよい）から、強さを設定する。範囲外の番号は端に寄せる
    /// </summary>
    /// <param name="isCpuMode">CPU対戦なら true</param>
    /// <param name="levelIndex">タイトルで選んだ強さの番号</param>
    public static void SetFromTitle(bool isCpuMode, int levelIndex)
    {
        Set(isCpuMode, (CpuLevel)Mathf.Clamp(levelIndex, (int)CpuLevel.Easy, (int)CpuLevel.Hard));
    }

    /// <summary>
    /// Play Mode を始めるたびに初期値へ戻す（ドメインのリロードを切っていても、前の設定が残らないように）
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        _isCpuMode = false;
        _level = CpuLevel.Normal;
    }
}
