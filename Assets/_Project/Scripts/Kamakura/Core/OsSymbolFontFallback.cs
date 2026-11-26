using TMPro;
using UnityEngine;

/// <summary>
/// 日本語フォント（M PLUS 1）にない記号（★☆○△□ など）を出すため、
/// ゲームの開始時に、PC に入っているフォントを TextMeshPro の予備フォントに足す（仮。Windows 用）
/// 記号の入ったフォントをプロジェクトに入れたら、このファイルは消してよい
/// </summary>
public static class OsSymbolFontFallback
{
    // 上から順に探して、記号がそろっている最初のフォントを使う（どれも Windows に入っている）
    private static readonly string[] FamilyNames = { "BIZ UDGothic", "Yu Gothic UI", "Yu Gothic", "Meiryo", "MS Gothic" };
    private const string StyleName = "Regular";
    private const string RequiredCharacters = "★☆○△□●";

    private static TMP_FontAsset _added;

    /// <summary>
    /// 最初のシーンを読む前に、予備フォントを足す
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AddFallback()
    {
        if (_added != null)
        {
            return;
        }

        foreach (string familyName in FamilyNames)
        {
            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(familyName, StyleName);

            if (fontAsset == null)
            {
                continue;
            }

            if (!fontAsset.HasCharacters(RequiredCharacters, out _, false, true))
            {
                Object.Destroy(fontAsset);
                continue;
            }

            fontAsset.name = $"{familyName} (OS)";
            TMP_Settings.fallbackFontAssets.Add(fontAsset);
            _added = fontAsset;

            // エディタでは TMP Settings のアセットを書き換えたままにしないよう、終わるときに外す
            Application.quitting += RemoveFallback;
            return;
        }

        Debug.LogWarning("[OsSymbolFontFallback] 記号の入ったフォントが PC に見つからないので、★○△□ などは □ で出ます");
    }

    /// <summary>
    /// 足した予備フォントを外す
    /// </summary>
    private static void RemoveFallback()
    {
        Application.quitting -= RemoveFallback;

        if (_added == null)
        {
            return;
        }

        TMP_Settings.fallbackFontAssets.Remove(_added);
        _added = null;
    }
}
