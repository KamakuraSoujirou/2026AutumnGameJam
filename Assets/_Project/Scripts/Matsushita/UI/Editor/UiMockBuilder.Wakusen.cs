using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 枠線デザイン（Wakusen）の素材を UiSkin に入れ、いまあるプレハブを作り直さずに枠つきの作りへ直す
/// </summary>
public static partial class UiMockBuilder
{
    private const string WakusenFolder = ArtFolder + "/UI";
    private const float BubbleTextWidth = 383f;
    private const float BubbleTextHeight = 134f;
    private const float BubbleFrameWidth = 431f;
    private const float BubbleFrameHeight = 132f;

    /// <summary>
    /// 素材IDと、入れる画像のファイル名（拡張子なし）の対応
    /// </summary>
    private static readonly Dictionary<string, string> WakusenAssignments = new Dictionary<string, string>
    {
        { "UI-04", "UI_Wakusen_Title1" },
        { "UI-04b", "UI_Wakusen_Title2" },
        { "UI-04c", "UI_Wakusen_Title3" },
        { "UI-13", "UI_Wakusen_Play2" },
        { "UI-14", "UI_Wakusen_Play1" },
        { "UI-14b", "UI_Wakusen_Play3" },
        { "UI-15", "UI_Wakusen_Play4" },
        { "UI-15b", "UI_Wakusen_Play6" },
        { "UI-16", "UI_Wakusen_Play5" },
        { "UI-27", "UI_Wakusen_Aori 1" },
        { "UI-23", "UI_Wakusen_Result2" },
        { "UI-24", "UI_Wakusen_Result1" },
        { "UI-25", "UI_Wakusen_Result3" },
        { "UI-26", "UI_Wakusen_Result4" }
    };

    /// <summary>
    /// 枠の絵だけを置く四角（中身より奥に描くため、先に作る）を作る
    /// </summary>
    /// <param name="parent">置く場所</param>
    /// <param name="name">オブジェクト名</param>
    /// <param name="artId">素材ID</param>
    /// <param name="x">左からの位置（1920×1080 の座標）</param>
    /// <param name="y">上からの位置</param>
    /// <param name="width">幅</param>
    /// <param name="height">高さ</param>
    /// <param name="placeholderColor">素材がないときの色</param>
    /// <param name="placeholderSprite">素材がないときの形（空なら四角）</param>
    /// <returns>作った四角</returns>
    private static RectTransform AddFrameArt(Transform parent, string name, string artId, float x, float y, float width, float height, Color placeholderColor, Sprite placeholderSprite)
    {
        RectTransform frame = CreateRect(name, parent, x, y, width, height);
        AddArt(frame, artId, placeholderColor, placeholderSprite, false, null);
        return frame;
    }

    /// <summary>
    /// 枠線の素材を UiSkin に入れ、タイトル・試合・リザルトのプレハブを枠つきの作りへ直す（作り直さないので、手で直した分は消えない）
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

            // 手で入れた画像は上書きしない（前に枠線の素材として入れたものは入れ替える）
            bool isReplaceable = entry.Sprite == null || entry.Sprite.name.StartsWith("UI_Wakusen_");

            if (isReplaceable && entry.Sprite != sprite)
            {
                entry.Sprite = sprite;
                assignedCount++;
            }
        }

        EditorUtility.SetDirty(skin);
        AssetDatabase.SaveAssets();

        Sprite roundSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        int changedCount = 0;

        changedCount += PatchPrefab(TitlePrefabPath, root =>
        {
            int count = 0;
            count += ConvertPanelToFrame(root, "SettingsPanel", "SettingsFrame", "UI-04", 40f, 304f, 462f, 560f, Color.white, roundSprite);
            count += ConvertPanelToFrame(root, "ControlsPanel", "ControlsFrame", "UI-04b", 1224f, 293f, 613f, 227f, Color.white, roundSprite);
            // 操作メニューの中身は、左上の欠けた角を避けて右下へずらす
            count += SetRect(root, "ControlsPanel", null, 1290f, 319f, null, null);
            return count;
        });

        changedCount += PatchPrefab(GamePrefabPath, root =>
        {
            int count = 0;
            count += ConvertPanelToFrame(root, "Round", "RoundFrame", "UI-13", 763f, 35f, 393f, 126f, GrayFill, null);
            count += ConvertPanelToFrame(root, "BarP1", "BarP1Frame", "UI-15", 76f, 904f, 601f, 140f, GrayFill, null);
            count += ConvertPanelToFrame(root, "BarGimmick", "BarGimmickFrame", "UI-16", 759f, 887f, 449f, 151f, GrayFill, null);
            count += ConvertPanelToFrame(root, "BarP2", "BarP2Frame", "UI-15b", 1281f, 898f, 601f, 140f, GrayFill, null);
            count += ResizeBubble(root, "PlayerP1", "UI-14", -1f, 0f);
            count += ResizeBubble(root, "PlayerP2", "UI-14b", 32f, -25f);
            // 文字は、枠の中身（内側）の中央に置く
            count += SetRect(root, "Round", null, 842f, 48f, null, null);
            count += SetBubbleText(root, "PlayerP1");
            count += SetBubbleText(root, "PlayerP2");
            count += SetRect(root, "BarP1", null, 171f, 944f, 411f, 69f);
            count += SetRect(root, "Name", "BarP1", 10f, 0f, 200f, 69f);
            count += SetRect(root, "WinStars", "BarP1", 267f, 15f, null, null);
            count += SetRect(root, "BarGimmick", null, 801.5f, 929f, null, null);
            count += SetRect(root, "BarP2", null, 1375f, 938f, 411f, 69f);
            count += SetRect(root, "Name", "BarP2", 199f, 0f, 200f, 69f);
            count += SetRect(root, "WinStars", "BarP2", 12f, 15f, null, null);
            return count;
        });

        changedCount += PatchPrefab(ResultPrefabPath, root =>
        {
            int count = 0;
            count += AddFrameIfMissing(root, "PlayerP1", "Hexagon", "UI-24", 76f, 140f, 519f, 353f, 0);
            count += AddFrameIfMissing(root, "PlayerP2", "Hexagon", "UI-25", 1324f, 140f, 518f, 353f, 0);
            count += AddFrameIfMissing(root, "Screen", "ScorePlate", "UI-23", 590f, 19f, 756f, 165f, 1);
            count += AddFrameIfMissing(root, "Table", "TableFrame", "UI-26", 36f, 532f, 1802f, 457f, 0);
            // 勝ち数は、斜線の板の中の「あ ー あ」の位置（左から 790・970・1165）に置く
            count += SetRect(root, "Player1Score", null, 684.4f, null, null, null);
            count += SetRect(root, "ScoreDash", null, 864.4f, null, null, null);
            count += SetRect(root, "Player2Score", null, 1059.4f, null, null, null);
            return count;
        });

        ApplySkinToPrefabs();
        ApplySkinToPrefab(ResultPrefabPath);
        Debug.Log($"枠線の素材を {assignedCount} 件入れて、プレハブを {changedCount} か所直し、反映しました");
    }

    /// <summary>
    /// プレハブを開いて直し、直したところがあれば保存する
    /// </summary>
    /// <param name="prefabPath">プレハブのパス</param>
    /// <param name="patch">直す処理（直した数を返す）</param>
    /// <returns>直した数</returns>
    private static int PatchPrefab(string prefabPath, System.Func<GameObject, int> patch)
    {
        if (!System.IO.File.Exists(prefabPath))
        {
            Debug.LogWarning($"{prefabPath} がありません。先に画面を作ってください");
            return 0;
        }

        GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
        int count;

        try
        {
            count = patch(contents);

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

    /// <summary>
    /// プレハブ1つに UiSkin の中身を反映して保存する
    /// </summary>
    /// <param name="prefabPath">プレハブのパス</param>
    private static void ApplySkinToPrefab(string prefabPath)
    {
        if (!System.IO.File.Exists(prefabPath))
        {
            return;
        }

        GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
        UiSkinApplier applier = contents.GetComponent<UiSkinApplier>();

        if (applier != null)
        {
            applier.ApplyAll();
            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
        }

        PrefabUtility.UnloadPrefabContents(contents);
    }

    /// <summary>
    /// 名前で、プレハブの中から子孫を探す
    /// </summary>
    /// <param name="root">探し始める場所</param>
    /// <param name="objectName">探す名前</param>
    /// <returns>見つかった子孫（なければ null）</returns>
    private static Transform FindDescendant(Transform root, string objectName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName)
            {
                return child;
            }
        }

        return null;
    }

    /// <summary>
    /// 枠の絵を持っていた四角から絵を外し、すぐ後ろ（奥）に枠だけの四角を足す
    /// </summary>
    /// <returns>直したら 1、すでに直っていたか見つからなければ 0</returns>
    private static int ConvertPanelToFrame(GameObject root, string ownerName, string frameName, string artId, float x, float y, float width, float height, Color placeholderColor, Sprite placeholderSprite)
    {
        Transform owner = FindDescendant(root.transform, ownerName);

        if (owner == null || owner.parent.Find(frameName) != null)
        {
            return 0;
        }

        // 枠の絵は、中身より奥に描くため、持ち主の1つ前に置く
        RectTransform frame = AddFrameArt(owner.parent, frameName, artId, x, y, width, height, placeholderColor, placeholderSprite);
        frame.SetSiblingIndex(owner.GetSiblingIndex());

        Outline outline = owner.GetComponent<Outline>();

        if (outline != null)
        {
            Object.DestroyImmediate(outline);
        }

        UiArtSlot slot = owner.GetComponent<UiArtSlot>();

        if (slot != null)
        {
            Object.DestroyImmediate(slot);
        }

        Image image = owner.GetComponent<UnityEngine.UI.Image>();

        if (image != null)
        {
            Object.DestroyImmediate(image);
        }

        return 1;
    }

    /// <summary>
    /// 親の中の決まった順番に、枠だけの四角を足す（すでにあれば何もしない）
    /// </summary>
    /// <returns>足したら 1、そうでなければ 0</returns>
    private static int AddFrameIfMissing(GameObject root, string parentName, string frameName, string artId, float x, float y, float width, float height, int siblingIndex)
    {
        Transform parent = parentName == "Screen" ? root.transform.Find("Screen") : FindDescendant(root.transform, parentName);

        if (parent == null || parent.Find(frameName) != null)
        {
            return 0;
        }

        RectTransform frame = AddFrameArt(parent, frameName, artId, x, y, width, height, Color.clear, null);
        frame.SetSiblingIndex(Mathf.Min(siblingIndex, parent.childCount - 1));
        return 1;
    }

    /// <summary>
    /// 名前で見つけた四角の位置と大きさを決める（左上基準。決めない値は null。何度やっても同じ結果になる）
    /// </summary>
    /// <param name="root">プレハブの一番上</param>
    /// <param name="objectName">四角の名前</param>
    /// <param name="parentName">親の名前（指定しなければ null）</param>
    /// <param name="x">左からの位置</param>
    /// <param name="y">上からの位置</param>
    /// <param name="width">幅</param>
    /// <param name="height">高さ</param>
    /// <returns>見つけて決めたら 1、見つからなければ 0</returns>
    private static int SetRect(GameObject root, string objectName, string parentName, float? x, float? y, float? width, float? height)
    {
        Transform target = null;

        if (parentName == null)
        {
            target = FindDescendant(root.transform, objectName);
        }
        else
        {
            Transform parent = FindDescendant(root.transform, parentName);
            target = parent != null ? parent.Find(objectName) : null;
        }

        if (target == null)
        {
            return 0;
        }

        RectTransform rect = (RectTransform)target;
        Vector2 position = rect.anchoredPosition;
        Vector2 size = rect.sizeDelta;
        rect.anchoredPosition = new Vector2(x ?? position.x, y.HasValue ? -y.Value : position.y);
        rect.sizeDelta = new Vector2(width ?? size.x, height ?? size.y);
        return 1;
    }

    /// <summary>
    /// 吹き出しの文字を、枠の中央に置く
    /// </summary>
    /// <param name="root">プレハブの一番上</param>
    /// <param name="panelName">プレイヤーのパネルの名前</param>
    /// <returns>直したら 1、見つからなければ 0</returns>
    private static int SetBubbleText(GameObject root, string panelName)
    {
        Transform panel = FindDescendant(root.transform, panelName);
        RectTransform text = panel != null ? panel.Find("Bubble/Text") as RectTransform : null;

        if (text == null)
        {
            return 0;
        }

        text.anchoredPosition = new Vector2((BubbleFrameWidth - BubbleTextWidth) * 0.5f, -(BubbleFrameHeight - BubbleTextHeight) * 0.5f);
        return 1;
    }

    /// <summary>
    /// 吹き出しを、枠の絵の大きさにする（文字の位置は動かさない）
    /// </summary>
    /// <param name="root">プレハブの一番上</param>
    /// <param name="panelName">プレイヤーのパネルの名前</param>
    /// <param name="artId">吹き出しの素材ID</param>
    /// <param name="shiftX">文字の中心から見た、枠の中心の右へのずれ</param>
    /// <param name="shiftY">文字の中心から見た、枠の中心の下へのずれ</param>
    /// <returns>直したら 1、すでに直っていたか見つからなければ 0</returns>
    private static int ResizeBubble(GameObject root, string panelName, string artId, float shiftX, float shiftY)
    {
        Transform panel = FindDescendant(root.transform, panelName);
        Transform bubble = panel != null ? panel.Find("Bubble") : null;

        if (bubble == null)
        {
            return 0;
        }

        RectTransform bubbleRect = (RectTransform)bubble;
        RectTransform textRect = bubble.Find("Text") as RectTransform;
        bool isOldSize = Mathf.Approximately(bubbleRect.sizeDelta.x, BubbleTextWidth) && Mathf.Approximately(bubbleRect.sizeDelta.y, BubbleTextHeight);

        UiArtSlot slot = bubble.GetComponent<UiArtSlot>();

        if (slot != null && slot.ArtId != artId)
        {
            SerializedObject serialized = new SerializedObject(slot);
            serialized.FindProperty("_artId").stringValue = artId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        if (!isOldSize || textRect == null)
        {
            return 0;
        }

        bubbleRect.anchoredPosition += new Vector2(shiftX, -shiftY);
        bubbleRect.sizeDelta = new Vector2(BubbleFrameWidth, BubbleFrameHeight);
        textRect.anchoredPosition = new Vector2((BubbleFrameWidth - BubbleTextWidth) * 0.5f - shiftX, -((BubbleFrameHeight - BubbleTextHeight) * 0.5f - shiftY));
        return 1;
    }
}
