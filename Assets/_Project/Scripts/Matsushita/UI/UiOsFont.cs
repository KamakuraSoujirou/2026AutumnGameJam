using TMPro;
using UnityEngine;

/// <summary>
/// 日本語フォントがまだ用意されていない間の代わり。PCに入っている日本語フォントを実行中だけ使う
/// </summary>
public static class UiOsFont
{
    private static readonly string[,] Candidates =
    {
        { "Hiragino Sans", "W3" },
        { "Hiragino Kaku Gothic ProN", "W3" },
        { "Yu Gothic", "Regular" },
        { "Meiryo", "Regular" },
        { "MS Gothic", "Regular" }
    };

    private static TMP_FontAsset _cachedFont;
    private static bool _hasTried;

    /// <summary>
    /// PCに入っている日本語フォントから TextMeshPro 用のフォントを作る（1回だけ）
    /// </summary>
    /// <returns>作れたフォント。見つからなければ null</returns>
    public static TMP_FontAsset GetJapaneseFont()
    {
        if (_hasTried)
        {
            return _cachedFont;
        }

        _hasTried = true;

        for (int i = 0; i < Candidates.GetLength(0); i++)
        {
            _cachedFont = TMP_FontAsset.CreateFontAsset(Candidates[i, 0], Candidates[i, 1]);

            if (_cachedFont != null)
            {
                break;
            }
        }

        if (_cachedFont == null)
        {
            Debug.LogWarning("日本語フォントが見つかりませんでした。UiSkin にフォントを入れてください");
        }

        return _cachedFont;
    }
}
