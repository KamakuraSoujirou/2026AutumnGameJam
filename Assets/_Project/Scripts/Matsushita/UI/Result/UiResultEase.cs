using UnityEngine;

/// <summary>
/// リザルトの動きに使う、進み具合（0〜1）の変換
/// </summary>
public static class UiResultEase
{
    private const float BackOvershoot = 1.7f;

    /// <summary>
    /// 最初が速く、終わりにゆっくりになる
    /// </summary>
    /// <param name="t">進み具合（0〜1）</param>
    /// <returns>変換後の進み具合</returns>
    public static float OutCubic(float t)
    {
        float inverse = 1f - Mathf.Clamp01(t);
        return 1f - inverse * inverse * inverse;
    }

    /// <summary>
    /// 少し行き過ぎてから戻る（ポンと弾む）
    /// </summary>
    /// <param name="t">進み具合（0〜1）</param>
    /// <returns>変換後の進み具合（1を少し超えることがある）</returns>
    public static float OutBack(float t)
    {
        float shifted = Mathf.Clamp01(t) - 1f;
        return 1f + (BackOvershoot + 1f) * shifted * shifted * shifted + BackOvershoot * shifted * shifted;
    }

    /// <summary>
    /// 最初がゆっくりで、終わりに速くなる（落ちる動き）
    /// </summary>
    /// <param name="t">進み具合（0〜1）</param>
    /// <returns>変換後の進み具合</returns>
    public static float InQuad(float t)
    {
        float clamped = Mathf.Clamp01(t);
        return clamped * clamped;
    }
}
