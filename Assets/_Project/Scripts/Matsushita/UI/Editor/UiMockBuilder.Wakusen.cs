using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 枠線デザイン（Wakusen）の素材を UiSkin に入れ、いまあるプレハブを作り直さずに枠つきの作りへ直す
/// </summary>
public static partial class UiMockBuilder
{
    private const string WakusenFolder = ArtFolder + "/UI";
    // タイトルの爆弾（640×640 の四角を縮めて、左上を基準に置く）
    private const float TitleBombX = 726.4f;
    private const float TitleBombY = 14f;
    private const float TitleBombScale = 0.73f;

    // タイトルのキャラ（更新後の絵は 600×900。見本の頭の位置と足元の高さに合わせた）
    private const float TitleP1X = 364f;
    private const float TitleP1Y = 28.7f;
    private const float TitleP1Width = 556.8f;
    private const float TitleP1Height = 835.2f;
    private const float TitleP2X = 1008f;
    private const float TitleP2Y = 46.4f;
    private const float TitleP2Width = 516.6f;
    private const float TitleP2Height = 774.9f;

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
        { "UI-26", "UI_Wakusen_Result4" },
        { "ART-14", "UI_Title" }
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
            count += FitTitleLayout(root);
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
            // 表の中身（見出し・行・線・星）を、枠の内側にすべて収める
            count += FitResultTable(root);
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
    /// タイトルの爆弾・キャラ・ロゴ・サブタイトル・スタートボタンを、見本の構成に直す（何度やっても同じ結果になる）
    /// </summary>
    /// <param name="root">プレハブの一番上</param>
    /// <returns>直したら 1、見つからなければ 0</returns>
    private static int FitTitleLayout(GameObject root)
    {
        Transform screen = root.transform.Find("Screen");

        if (screen == null)
        {
            return 0;
        }

        Transform bomb = screen.Find("Bomb");
        Transform player1 = screen.Find("CharacterP1");
        Transform player2 = screen.Find("CharacterP2");
        Transform logo = screen.Find("Logo");
        Transform subtitle = screen.Find("Subtitle");

        if (bomb != null)
        {
            RectTransform bombRect = (RectTransform)bomb;
            bombRect.anchoredPosition = new Vector2(TitleBombX, -TitleBombY);
            bombRect.sizeDelta = new Vector2(640f, 640f);
            bombRect.localScale = new Vector3(TitleBombScale, TitleBombScale, 1f);

            if (bomb.Find("TitleBombParts") == null)
            {
                AddTitleBombParts(bombRect);
            }

            // 爆弾はキャラの奥に置く
            if (player1 != null && bomb.GetSiblingIndex() > player1.GetSiblingIndex())
            {
                bomb.SetSiblingIndex(player1.GetSiblingIndex());
            }
        }

        SetRect(root, "CharacterP1", null, TitleP1X, TitleP1Y, TitleP1Width, TitleP1Height);
        SetRect(root, "CharacterP2", null, TitleP2X, TitleP2Y, TitleP2Width, TitleP2Height);

        // ロゴとサブタイトルは、キャラの手前（キャラの足元に重なる）に置く
        if (logo != null && player2 != null && logo.GetSiblingIndex() < player2.GetSiblingIndex())
        {
            logo.SetSiblingIndex(player2.GetSiblingIndex());
        }

        if (logo != null && subtitle != null && subtitle.GetSiblingIndex() < logo.GetSiblingIndex())
        {
            subtitle.SetSiblingIndex(logo.GetSiblingIndex());
        }

        SetRect(root, "Logo", null, 605f, 442f, 670f, 442f);
        SetRect(root, "LogoText", "Logo", 0f, 0f, 670f, 442f);
        SetRect(root, "Subtitle", null, 360f, 884f, 1200f, 48f);

        TMP_Text subtitleText = subtitle != null ? subtitle.GetComponent<TMP_Text>() : null;

        if (subtitleText != null)
        {
            subtitleText.fontSize = 36f;
            subtitleText.text = "謎を解いて、10秒以内に爆弾を相手に押し付けろ！！";
        }

        // スタートボタンは、サブタイトルの下に低く置く
        SetRect(root, "StartFocusFrame", null, 596f, 918f, 674f, 124f);
        SetRect(root, "StartButton", null, 608f, 930f, 650f, 100f);
        SetRect(root, "Label", "StartButton", 0f, 0f, 650f, 100f);
        SetChildFontSize(FindDescendant(root.transform, "StartButton"), "Label", 66f);
        return 1;
    }

    /// <summary>
    /// タイトルの爆弾に、ひし形ボタン4つ・ランプ・時間の表示を重ねる（試合画面の爆弾と同じ位置。動きはなく、見た目だけ）
    /// </summary>
    /// <param name="bomb">爆弾の四角（640×640。本体の絵が付いているもの）</param>
    private static void AddTitleBombParts(RectTransform bomb)
    {
        Sprite circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        RectTransform parts = CreateStretch("TitleBombParts", bomb);

        // 画面の上・左・右・下の順。色は 上＝緑、左＝黄、右＝青、下＝赤（WireNames の 3・2・1・0 番目）
        string[] names = { "SwitchTop", "SwitchLeft", "SwitchRight", "SwitchBottom" };
        int[] colorIndexes = { 3, 2, 1, 0 };
        Vector2[] centers = { ArtPoint(400f, 145f), ArtPoint(255f, 263f), ArtPoint(545f, 263f), ArtPoint(400f, 385f) };
        bool[] isTabLefts = { true, true, false, false };

        for (int i = 0; i < names.Length; i++)
        {
            int color = colorIndexes[i];
            RectTransform button = CreateCentered(names[i], parts, centers[i].x, centers[i].y, ButtonBoxSize, ButtonBoxSize);
            bool isArtTabLeft = ButtonTabIsLeft[color];
            RectTransform visual = CreateDiamondArea($"Diamond_{WireNames[color]}", button, isTabLefts[i] != isArtTabLeft, isArtTabLeft);
            AddArt(visual, $"ART-05_{WireNames[color]}_normal", Color.clear, null, true, null);
        }

        Vector2 lampCenter = ArtPoint(400f, 263f);
        float lampSize = 60f * BombArtScale;
        RectTransform lamp = CreateCentered("Lamp", parts, lampCenter.x, lampCenter.y, lampSize, lampSize);
        AddArt(lamp, "ART-04_on", new Color(0.95f, 0.2f, 0.2f), circleSprite, true, null);

        RectTransform timer = CreateRect("TimerLcd", parts, 240f, 384f, 160f, 48f);
        TextMeshProUGUI time = AddText("Time", timer, 0f, 0f, 160f, 48f, "05:00", 34f, UiFontRole.Number, TextAlignmentOptions.Center);
        time.color = LcdTextColor;
    }

    /// <summary>
    /// 結果の表の中身を、表の枠（UI-26）の内側（1920×1080 で x165〜1720、y590〜965）にすべて収める
    /// </summary>
    /// <param name="root">プレハブの一番上</param>
    /// <returns>直したら 1、見つからなければ 0</returns>
    private static int FitResultTable(GameObject root)
    {
        Transform table = FindDescendant(root.transform, "Table");

        if (table == null)
        {
            return 0;
        }

        // ラウンド｜仕組み｜P1｜決着｜P2｜パス回数 の列の境目
        float[] edges = { 165f, 315f, 835f, 955f, 1145f, 1265f, 1720f };
        const float HeaderTop = 590f;
        const float HeaderBottom = 640f;
        const float TableBottom = 965f;
        const int RowCount = 5;
        const float StarSize = 48f;
        float rowHeight = (TableBottom - HeaderBottom) / RowCount;
        float tableLeft = edges[0];
        float tableWidth = edges[edges.Length - 1] - tableLeft;

        SetChildRect(table, "HeaderTopLine", tableLeft, HeaderTop, tableWidth, 2f);
        SetChildRect(table, "HeaderBottomLine", tableLeft, HeaderBottom, tableWidth, 3f);

        for (int i = 1; i <= RowCount; i++)
        {
            SetChildRect(table, $"RowLine{i}", tableLeft, HeaderBottom + i * rowHeight, tableWidth, 2f);
        }

        for (int i = 1; i < edges.Length - 1; i++)
        {
            SetChildRect(table, $"ColumnLine{i}", edges[i], HeaderTop, 3f, TableBottom - HeaderTop);
        }

        for (int i = 0; i < edges.Length - 1; i++)
        {
            SetChildRect(table, $"Header{i}", edges[i], HeaderTop + 2f, edges[i + 1] - edges[i], HeaderBottom - HeaderTop - 2f);
            SetChildFontSize(table, $"Header{i}", 29f);
        }

        // 列ごとの文字の大きさ：ラウンド・仕組み・（星）・決着・（星）・パス回数
        float[] fontSizes = { 44f, 42f, 0f, 35f, 0f, 44f };

        for (int i = 0; i < RowCount; i++)
        {
            Transform row = table.Find($"Row{i}");

            if (row == null)
            {
                continue;
            }

            SetChildRect(table, $"Row{i}", 0f, HeaderBottom + i * rowHeight, 1920f, rowHeight);

            for (int column = 0; column < fontSizes.Length; column++)
            {
                if (fontSizes[column] <= 0f)
                {
                    continue;
                }

                SetChildRect(row, $"Cell{column}", edges[column], 0f, edges[column + 1] - edges[column], rowHeight);
                SetChildFontSize(row, $"Cell{column}", fontSizes[column]);
            }

            FitResultStar(row, "Player1Star", (edges[2] + edges[3]) * 0.5f, rowHeight * 0.5f, StarSize);
            FitResultStar(row, "Player2Star", (edges[4] + edges[5]) * 0.5f, rowHeight * 0.5f, StarSize);
        }

        return 1;
    }

    /// <summary>
    /// 子の四角の位置と大きさを決める（左上基準）
    /// </summary>
    private static void SetChildRect(Transform parent, string childName, float x, float y, float width, float height)
    {
        RectTransform rect = parent.Find(childName) as RectTransform;

        if (rect == null)
        {
            return;
        }

        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }

    /// <summary>
    /// 子の文字の大きさを決める
    /// </summary>
    private static void SetChildFontSize(Transform parent, string childName, float fontSize)
    {
        TMP_Text text = parent.Find(childName) != null ? parent.Find(childName).GetComponent<TMP_Text>() : null;

        if (text != null)
        {
            text.fontSize = fontSize;
        }
    }

    /// <summary>
    /// 行の中の星（中心基準）の位置と大きさを決める
    /// </summary>
    private static void FitResultStar(Transform row, string starName, float centerX, float centerY, float size)
    {
        RectTransform star = row.Find(starName) as RectTransform;

        if (star == null)
        {
            return;
        }

        star.anchoredPosition = new Vector2(centerX, -centerY);
        star.sizeDelta = new Vector2(size, size);

        foreach (TMP_Text mark in star.GetComponentsInChildren<TMP_Text>(true))
        {
            ((RectTransform)mark.transform).sizeDelta = new Vector2(size, size);
            mark.fontSize = size * 0.9f;
        }
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
