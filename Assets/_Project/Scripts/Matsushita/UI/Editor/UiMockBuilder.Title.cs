using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UIの仮組みのうち、タイトル画面を作る部分
/// </summary>
public static partial class UiMockBuilder
{
    private const string TitlePrefabPath = PrefabFolder + "/UI_Title.prefab";
    private const string TitleScenePath = SceneFolder + "/UiMock_Title.unity";

    private static readonly Color PaleOrange = new Color(0.98f, 0.86f, 0.68f);
    private static readonly Color FocusRowColor = new Color(1f, 0.94f, 0.55f);
    private static readonly Color FocusFrameColor = new Color(0.12f, 0.42f, 1f);

    /// <summary>
    /// タイトル画面のプレハブとテストシーンを作る
    /// </summary>
    [MenuItem("Tools/UI仮組/タイトルを作る")]
    public static void BuildTitle()
    {
        if (File.Exists(TitlePrefabPath))
        {
            bool isOverwrite = EditorUtility.DisplayDialog(
                "UI仮組", "UI_Title.prefab を作り直します。プレハブを手で直した分は消えます。", "作り直す", "やめる");

            if (!isOverwrite)
            {
                return;
            }
        }

        EnsureFolder(PrefabFolder);
        EnsureFolder(SceneFolder);
        UiSkin skin = LoadOrCreateSkin();

        GameObject root = CreateCanvasRoot("UI_Title", skin);
        RectTransform screen = (RectTransform)root.transform.Find("Screen");
        BuildTitleContents(root, screen);

        root.GetComponent<UiSkinApplier>().ApplyAll();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, TitlePrefabPath);
        Object.DestroyImmediate(root);

        CreatePreviewScene(prefab, TitleScenePath);
        Debug.Log($"タイトルの仮組みを作りました：{TitlePrefabPath} / {TitleScenePath}");

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(TitleScenePath);
        }
    }

    /// <summary>
    /// タイトル画面で使う素材の登録を作る
    /// </summary>
    /// <returns>素材の登録の一覧</returns>
    private static List<UiSkinEntry> CreateTitleEntries()
    {
        return new List<UiSkinEntry>
        {
            new UiSkinEntry("ART-13", "タイトル背景", new Vector2(1920f, 1080f), "title_bg"),
            new UiSkinEntry("ART-14", "タイトルロゴ", new Vector2(700f, 130f), "title_logo"),
            new UiSkinEntry("UI-01", "選択肢ボタン（選択中）", new Vector2(104f, 44f), "ui_chip_on"),
            new UiSkinEntry("UI-02", "選択肢ボタン（未選択）", new Vector2(104f, 44f), "ui_chip_off"),
            new UiSkinEntry("UI-03", "ゲームスタートボタン", new Vector2(650f, 200f), "ui_start"),
            new UiSkinEntry("UI-04", "メニューの枠", new Vector2(456f, 478f), "ui_panel")
        };
    }

    /// <summary>
    /// タイトル画面の中身を並べる（座標は 1920×1080 の左上から）
    /// </summary>
    /// <param name="root">画面の一番上</param>
    /// <param name="screen">1920×1080 の土台</param>
    private static void BuildTitleContents(GameObject root, RectTransform screen)
    {
        Sprite roundSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Sprite circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        RectTransform background = CreateStretch("Background", screen);
        AddArt(background, "ART-13", Color.white, null, false, null);

        RectTransform logo = CreateRect("Logo", screen, 610f, 25f, 700f, 130f);
        TextMeshProUGUI logoText = AddText("LogoText", logo, 0f, 0f, 700f, 130f, "あと10秒", 110f, UiFontRole.Heading, TextAlignmentOptions.Center);
        AddArt(logo, "ART-14", Color.clear, null, true, logoText.gameObject);

        AddText("Subtitle", screen, 360f, 140f, 1200f, 64f, "謎を解いて、10秒以内に爆弾を相手に押し付けろ", 44f, UiFontRole.Body, TextAlignmentOptions.Center);

        UiOptionRow[] rows = BuildSettingsMenu(screen, roundSprite, out CanvasGroup cpuLevelGroup);
        BuildCharacters(screen, circleSprite);
        BuildControlsMenu(screen, roundSprite);
        BuildRules(screen);
        GameObject startFocusFrame = BuildStartButton(screen, roundSprite, out Button startButton);

        UiTitleView view = root.AddComponent<UiTitleView>();
        SetObjectArray(view, "_rows", rows);
        SetObject(view, "_cpuLevelGroup", cpuLevelGroup);
        SetObject(view, "_startFocusFrame", startFocusFrame);
        SetObject(view, "_startButton", startButton);
        root.AddComponent<UiTitleInput>();
    }

    /// <summary>
    /// 左の「対戦設定メニュー」を作る
    /// </summary>
    /// <param name="screen">1920×1080 の土台</param>
    /// <param name="roundSprite">角丸の仮の形</param>
    /// <param name="cpuLevelGroup">CPUの強さの行（薄くする用）</param>
    /// <returns>上から順の5行</returns>
    private static UiOptionRow[] BuildSettingsMenu(RectTransform screen, Sprite roundSprite, out CanvasGroup cpuLevelGroup)
    {
        AddText("SettingsHeading", screen, 40f, 240f, 440f, 64f, "対戦設定メニュー", 44f, UiFontRole.Heading, TextAlignmentOptions.Left);

        RectTransform panel = CreateRect("SettingsPanel", screen, 20f, 320f, 456f, 478f);
        AddArt(panel, "UI-04", Color.white, roundSprite, false, null);
        AddOutline(panel);

        UiOptionRow[] rows =
        {
            CreateOptionRow(panel, 0, "モード", new[] { "2人対戦", "CPU対戦" }, 0, roundSprite),
            CreateOptionRow(panel, 1, "CPUの強さ", new[] { "やさしい", "ふつう", "つよい" }, 1, roundSprite),
            CreateOptionRow(panel, 2, "勝利条件", new[] { "2本先取", "3本先取" }, 0, roundSprite),
            CreateOptionRow(panel, 3, "パス10回目から持ち時間を1秒ずつ短くする", new[] { "あり", "なし" }, 0, roundSprite),
            CreateOptionRow(panel, 4, "「あと10秒」の読み上げ", new[] { "あり", "なし" }, 0, roundSprite)
        };

        cpuLevelGroup = rows[1].gameObject.AddComponent<CanvasGroup>();
        return rows;
    }

    /// <summary>
    /// 中央のキャラ2人と爆弾を置く
    /// </summary>
    /// <param name="screen">1920×1080 の土台</param>
    /// <param name="circleSprite">丸の仮の形</param>
    private static void BuildCharacters(RectTransform screen, Sprite circleSprite)
    {
        CreateCharacter(screen, "CharacterP1", "ART-09_normal", "ART-09\nP1", 566f, 240f, 240f, 310f, new Color(1f, 0f, 0.16f));
        CreateCharacter(screen, "CharacterP2", "ART-10_normal", "ART-10\nP2", 1037f, 528f, 212f, 293f, new Color(0f, 0.1f, 1f));

        RectTransform bomb = CreateRect("Bomb", screen, 833f, 453f, 190f, 200f);
        AddArt(bomb, "ART-01", Color.black, circleSprite, true, null);
    }

    /// <summary>
    /// 右上の「操作メニュー」を作る（表示だけで、選べない）
    /// </summary>
    /// <param name="screen">1920×1080 の土台</param>
    /// <param name="roundSprite">角丸の仮の形</param>
    private static void BuildControlsMenu(RectTransform screen, Sprite roundSprite)
    {
        AddText("ControlsHeading", screen, 1290f, 245f, 440f, 64f, "操作メニュー", 44f, UiFontRole.Heading, TextAlignmentOptions.Left);

        RectTransform panel = CreateRect("ControlsPanel", screen, 1265f, 305f, 518f, 202f);
        AddArt(panel, "UI-04", Color.white, roundSprite, false, null);
        AddOutline(panel);

        AddText("MoveLabel", panel, 24f, 26f, 175f, 44f, "上下左右を選ぶ", 22f, UiFontRole.Body, TextAlignmentOptions.Left);
        CreateChip(panel, "MoveP1", "WASD", 205f, 26f, 100f, 44f, roundSprite).SetSelected(true);
        CreateChip(panel, "MoveP2", "↑←↓→", 335f, 26f, 100f, 44f, roundSprite).SetSelected(true);

        AddText("DefuseLabel", panel, 24f, 120f, 175f, 44f, "解除に挑戦", 22f, UiFontRole.Body, TextAlignmentOptions.Left);
        CreateChip(panel, "DefuseP1", "Q", 205f, 120f, 100f, 44f, roundSprite).SetSelected(true);
        CreateChip(panel, "DefuseP2", "右Shift か /", 335f, 120f, 170f, 44f, roundSprite).SetSelected(true);
    }

    /// <summary>
    /// 右下の「ルール」を作る
    /// </summary>
    /// <param name="screen">1920×1080 の土台</param>
    private static void BuildRules(RectTransform screen)
    {
        AddText("RulesHeading", screen, 1290f, 560f, 400f, 64f, "ルール", 44f, UiFontRole.Heading, TextAlignmentOptions.Left);

        string rules =
            "・爆弾を受け取ったら10秒以内に謎を解いて相手に返す\n" +
            "・間違えると残り時間−2秒\n" +
            "・解除方法は爆弾に書いてある\n" +
            "・5回目のパスからは解除に挑戦できる。正解すれば即勝利、外せば即爆発。解除は1ラウンド1回まで";
        TextMeshProUGUI rulesText = AddText("RulesText", screen, 1280f, 620f, 630f, 240f, rules, 25f, UiFontRole.Body, TextAlignmentOptions.TopLeft);
        rulesText.textWrappingMode = TextWrappingModes.Normal;
        rulesText.lineSpacing = 20f;
    }

    /// <summary>
    /// 下の「ゲームスタート」ボタンを作る
    /// </summary>
    /// <param name="screen">1920×1080 の土台</param>
    /// <param name="roundSprite">角丸の仮の形</param>
    /// <param name="startButton">マウスで押すためのボタン</param>
    /// <returns>選んでいるときに出す枠</returns>
    private static GameObject BuildStartButton(RectTransform screen, Sprite roundSprite, out Button startButton)
    {
        RectTransform focusFrame = CreateRect("StartFocusFrame", screen, 596f, 843f, 674f, 224f);
        Image frameImage = focusFrame.gameObject.AddComponent<Image>();
        frameImage.sprite = roundSprite;
        frameImage.type = Image.Type.Sliced;
        frameImage.color = FocusFrameColor;
        frameImage.raycastTarget = false;

        RectTransform button = CreateRect("StartButton", screen, 608f, 855f, 650f, 200f);
        AddArt(button, "UI-03", Orange, roundSprite, false, null);
        AddText("Label", button, 0f, 0f, 650f, 200f, "ゲームスタート", 84f, UiFontRole.Heading, TextAlignmentOptions.Center);
        startButton = AddClickArea(button);

        return focusFrame.gameObject;
    }

    /// <summary>
    /// 対戦設定の1行（見出しと選択肢）を作る
    /// </summary>
    /// <param name="panel">対戦設定の枠</param>
    /// <param name="index">上から何行目か</param>
    /// <param name="label">見出し</param>
    /// <param name="options">選択肢の文字</param>
    /// <param name="selectedIndex">最初に選んでおく選択肢</param>
    /// <param name="roundSprite">角丸の仮の形</param>
    /// <returns>作った行</returns>
    private static UiOptionRow CreateOptionRow(RectTransform panel, int index, string label, string[] options, int selectedIndex, Sprite roundSprite)
    {
        const float RowHeight = 93f;
        const float ChipWidth = 104f;
        const float ChipGap = 20f;

        RectTransform row = CreateRect($"Row{index}", panel, 8f, 6f + index * RowHeight, 440f, 90f);

        RectTransform focus = CreateStretch("FocusFrame", row);
        Image focusImage = focus.gameObject.AddComponent<Image>();
        focusImage.sprite = roundSprite;
        focusImage.type = Image.Type.Sliced;
        focusImage.color = FocusRowColor;
        focusImage.raycastTarget = false;
        focus.gameObject.SetActive(false);

        TextMeshProUGUI labelText = AddText("Label", row, 24f, 4f, 410f, 34f, label, 22f, UiFontRole.Body, TextAlignmentOptions.Left);
        labelText.enableAutoSizing = true;
        labelText.fontSizeMin = 14f;
        labelText.fontSizeMax = 22f;

        UiChip[] chips = new UiChip[options.Length];

        for (int i = 0; i < options.Length; i++)
        {
            chips[i] = CreateChip(row, $"Chip{i}", options[i], 30f + i * (ChipWidth + ChipGap), 40f, ChipWidth, 44f, roundSprite);
            chips[i].SetSelected(i == selectedIndex);
            SetObject(chips[i], "_button", AddClickArea((RectTransform)chips[i].transform));
        }

        UiOptionRow optionRow = row.gameObject.AddComponent<UiOptionRow>();
        SetObjectArray(optionRow, "_chips", chips);
        SetObject(optionRow, "_focusFrame", focus.gameObject);
        SetInt(optionRow, "_selectedIndex", selectedIndex);
        return optionRow;
    }

    /// <summary>
    /// 選択肢のボタンを作る
    /// </summary>
    /// <param name="parent">置く場所</param>
    /// <param name="name">オブジェクト名</param>
    /// <param name="label">ボタンの文字</param>
    /// <param name="x">左からの位置</param>
    /// <param name="y">上からの位置</param>
    /// <param name="width">幅</param>
    /// <param name="height">高さ</param>
    /// <param name="roundSprite">角丸の仮の形</param>
    /// <returns>作ったボタン</returns>
    private static UiChip CreateChip(RectTransform parent, string name, string label, float x, float y, float width, float height, Sprite roundSprite)
    {
        RectTransform chip = CreateRect(name, parent, x, y, width, height);

        RectTransform on = CreateStretch("On", chip);
        AddArt(on, "UI-01", Orange, roundSprite, false, null);

        RectTransform off = CreateStretch("Off", chip);
        AddArt(off, "UI-02", PaleOrange, roundSprite, false, null);

        TextMeshProUGUI labelText = AddText("Label", chip, 0f, 0f, width, height, label, 22f, UiFontRole.Body, TextAlignmentOptions.Center);
        labelText.enableAutoSizing = true;
        labelText.fontSizeMin = 12f;
        labelText.fontSizeMax = 22f;

        UiChip uiChip = chip.gameObject.AddComponent<UiChip>();
        SetObject(uiChip, "_onVisual", on.gameObject);
        SetObject(uiChip, "_offVisual", off.gameObject);
        SetObject(uiChip, "_label", labelText);
        return uiChip;
    }

    /// <summary>
    /// キャラの立ち絵の差し込み口を作る
    /// </summary>
    /// <param name="parent">置く場所</param>
    /// <param name="name">オブジェクト名</param>
    /// <param name="artId">素材ID</param>
    /// <param name="placeholderLabel">素材がないときに出す文字</param>
    /// <param name="x">左からの位置</param>
    /// <param name="y">上からの位置</param>
    /// <param name="width">幅</param>
    /// <param name="height">高さ</param>
    /// <param name="color">素材がないときの色</param>
    private static void CreateCharacter(RectTransform parent, string name, string artId, string placeholderLabel, float x, float y, float width, float height, Color color)
    {
        RectTransform character = CreateRect(name, parent, x, y, width, height);
        TextMeshProUGUI label = AddText("PlaceholderLabel", character, 0f, 0f, width, height, placeholderLabel, 36f, UiFontRole.Body, TextAlignmentOptions.Center);
        label.color = Color.white;
        AddArt(character, artId, color, null, true, label.gameObject);
    }
}
