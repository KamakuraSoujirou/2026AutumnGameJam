using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 枠線デザイン（Wakusen_1.ai）の素材を、いまある分だけ UiSkin とプレハブに反映する
/// </summary>
public static partial class UiMockBuilder
{
    private const string WakusenFolder = ArtFolder + "/UI";

    /// <summary>
    /// 素材IDと、入れる画像のファイル名（拡張子なし）の対応。背景が透明な画像だけを使う
    /// </summary>
    private static readonly Dictionary<string, string> WakusenAssignments = new Dictionary<string, string>
    {
        { "UI-04b", "UI_Wakusen_1_alpha" },
        { "UI-14", "UI_Wakusen_1_alpha" },
        { "UI-14b", "UI_Wakusen_1_alpha_flip" },
        { "UI-13", "UI_Wakusen_2_alpha" }
    };

    /// <summary>
    /// 枠線の素材を UiSkin に入れ、タイトルと試合のプレハブに反映する（作り直さず、いまのプレハブを直す）
    /// </summary>
    [MenuItem("Tools/UI仮組/枠線の素材を反映")]
    public static void ApplyWakusenArt()
    {
        AssetDatabase.Refresh();
        UiSkin skin = LoadOrCreateSkin();
        int assignedCount = 0;

        foreach (KeyValuePair<string, string> pair in WakusenAssignments)
        {
            string path = $"{WakusenFolder}/{pair.Value}.png";
            EnsureSpriteImport(path);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            UiSkinEntry entry = skin.FindEntry(pair.Key);

            if (sprite == null || entry == null)
            {
                Debug.LogWarning($"枠線の素材を入れられません：{pair.Key} ← {path}");
                continue;
            }

            // 手で入れた画像は上書きしない
            if (entry.Sprite == null)
            {
                entry.Sprite = sprite;
                assignedCount++;
            }
        }

        EditorUtility.SetDirty(skin);
        AssetDatabase.SaveAssets();

        int retargetedCount = RetargetPrefabSlots(TitlePrefabPath, new[]
        {
            new SlotRetarget("ControlsPanel", null, "UI-04", "UI-04b"),
            new SlotRetarget("Drawer", null, "UI-04", "UI-04c")
        });
        retargetedCount += RetargetPrefabSlots(GamePrefabPath, new[]
        {
            new SlotRetarget("Bubble", "PlayerP2", "UI-14", "UI-14b"),
            new SlotRetarget("BarP2", null, "UI-15", "UI-15b")
        });

        ApplySkinToPrefabs();
        Debug.Log($"枠線の素材を {assignedCount} 件入れて、スロットを {retargetedCount} 件つなぎ替え、プレハブに反映しました");
    }

    /// <summary>
    /// プレハブの中のスロットの素材IDを付け替える指示
    /// </summary>
    private readonly struct SlotRetarget
    {
        public readonly string ObjectName;
        public readonly string ParentName;
        public readonly string OldId;
        public readonly string NewId;

        /// <param name="objectName">付け替えるオブジェクトの名前</param>
        /// <param name="parentName">親の名前（指定しなければ null）</param>
        /// <param name="oldId">いまの素材ID（これと同じときだけ付け替える）</param>
        /// <param name="newId">新しい素材ID</param>
        public SlotRetarget(string objectName, string parentName, string oldId, string newId)
        {
            ObjectName = objectName;
            ParentName = parentName;
            OldId = oldId;
            NewId = newId;
        }
    }

    /// <summary>
    /// プレハブの中のスロットの素材IDを付け替えて保存する
    /// </summary>
    /// <param name="prefabPath">プレハブのパス</param>
    /// <param name="retargets">付け替えの指示</param>
    /// <returns>付け替えた数</returns>
    private static int RetargetPrefabSlots(string prefabPath, SlotRetarget[] retargets)
    {
        if (!System.IO.File.Exists(prefabPath))
        {
            Debug.LogWarning($"{prefabPath} がありません。先に画面を作ってください");
            return 0;
        }

        int count = 0;
        GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);

        try
        {
            foreach (UiArtSlot slot in contents.GetComponentsInChildren<UiArtSlot>(true))
            {
                foreach (SlotRetarget retarget in retargets)
                {
                    bool isParentMatch = retarget.ParentName == null
                        || (slot.transform.parent != null && slot.transform.parent.name == retarget.ParentName);

                    if (slot.name != retarget.ObjectName || !isParentMatch || slot.ArtId != retarget.OldId)
                    {
                        continue;
                    }

                    SerializedObject serialized = new SerializedObject(slot);
                    serialized.FindProperty("_artId").stringValue = retarget.NewId;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    count++;
                }
            }

            if (count > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }

        return count;
    }
}
