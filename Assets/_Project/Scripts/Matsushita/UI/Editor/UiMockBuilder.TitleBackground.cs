using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// UIの仮組み（タイトルの背景）。タイトルの背景（ART-13）の絵が届くまで、試合の背景（ART-12、何もないラボ）を使う
/// </summary>
public static partial class UiMockBuilder
{
    private const string TitleBackgroundFallbackArtId = "ART-12";

    /// <summary>
    /// すでにあるタイトルのプレハブの背景を、タイトルの背景（ART-13）が空のあいだ、試合の背景（ART-12）にする。作り直さずにその場で直す
    /// </summary>
    [MenuItem("Tools/UI仮組/タイトルの背景をラボにする")]
    public static void SetTitleBackgroundToLab()
    {
        if (!File.Exists(TitlePrefabPath))
        {
            Debug.LogWarning("UI_Title.prefab がありません。先に「タイトルを作る」を実行してください");
            return;
        }

        LoadOrCreateSkin();
        GameObject contents = PrefabUtility.LoadPrefabContents(TitlePrefabPath);

        try
        {
            Transform background = contents.transform.Find("Screen/Background");
            UiArtSlot slot = background != null ? background.GetComponent<UiArtSlot>() : null;

            if (slot == null)
            {
                Debug.LogWarning("タイトルの背景（Screen/Background）が見つかりません");
                return;
            }

            SetArtFallback(slot, TitleBackgroundFallbackArtId);
            contents.GetComponent<UiSkinApplier>().ApplyAll();
            PrefabUtility.SaveAsPrefabAsset(contents, TitlePrefabPath);
            Debug.Log($"タイトルの背景を、ART-13 が空のあいだ {TitleBackgroundFallbackArtId}（試合の背景）にしました：{TitlePrefabPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    /// <summary>
    /// 素材がないときに、代わりに使う素材IDを入れる
    /// </summary>
    /// <param name="slot">素材の差し込み口</param>
    /// <param name="fallbackArtId">代わりの素材ID</param>
    private static void SetArtFallback(UiArtSlot slot, string fallbackArtId)
    {
        SerializedObject serialized = new SerializedObject(slot);
        serialized.FindProperty("_fallbackArtId").stringValue = fallbackArtId;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
