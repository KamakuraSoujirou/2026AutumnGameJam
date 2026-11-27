using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// UIの仮組みをメニューから作る。素材がなくても仮の四角で画面が成立し、UiSkin に画像を入れれば差し替わる
/// </summary>
public static partial class UiMockBuilder
{
    private const string PrefabFolder = "Assets/_Project/Prefabs/Matsushita/UI";
    private const string SceneFolder = "Assets/_Project/Scenes/Matsushita";
    private const string ArtFolder = "Assets/_PrivateAssets/Art";
    private const string SkinPath = PrefabFolder + "/UiSkin.asset";
    private const string TitlePrefabPath = PrefabFolder + "/UI_Title.prefab";
    private const string TitleScenePath = SceneFolder + "/UiMock_Title.unity";
    private const float ScreenWidth = 1920f;
    private const float ScreenHeight = 1080f;
    private const string RulesTabName = "RulesTab";

    private static readonly Color Orange = new Color(0.92f, 0.54f, 0f);
    private static readonly Color PaleOrange = new Color(0.98f, 0.86f, 0.68f);
    private static readonly Color FocusRowColor = new Color(1f, 0.94f, 0.55f);
    private static readonly Color FocusFrameColor = new Color(0.12f, 0.42f, 1f);
    private static readonly Color TextColor = new Color(0.08f, 0.08f, 0.08f);
    private static readonly Color PanelLineColor = new Color(0.6f, 0.6f, 0.6f);

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
    /// Art フォルダの画像をファイル名で UiSkin に割り当て、プレハブの見た目を更新する
    /// </summary>
    [MenuItem("Tools/UI仮組/素材を割り当てて反映")]
    public static void AssignArtAndApply()
    {
        UiSkin skin = LoadOrCreateSkin();
        int assignedCount = AssignSpritesByName(skin);
        ApplySkinToPrefabs();
        Debug.Log($"素材を {assignedCount} 件割り当てて、プレハブに反映しました");
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
        SetArtFallback(background.GetComponent<UiArtSlot>(), TitleBackgroundFallbackArtId);

        RectTransform logo = CreateRect("Logo", screen, 610f, 25f, 700f, 130f);
        TextMeshProUGUI logoText = AddText("LogoText", logo, 0f, 0f, 700f, 130f, "あと10秒", 110f, UiFontRole.Heading, TextAlignmentOptions.Center);
        AddArt(logo, "ART-14", Color.clear, null, true, logoText.gameObject);

        AddText("Subtitle", screen, 360f, 140f, 1200f, 64f, "謎を解いて、10秒以内に爆弾を相手に押し付けろ", 44f, UiFontRole.Body, TextAlignmentOptions.Center);

        UiOptionRow[] rows = BuildSettingsMenu(screen, roundSprite, out CanvasGroup cpuLevelGroup);
        BuildCharacters(screen, circleSprite);
        BuildControlsMenu(screen, roundSprite);
        GameObject startFocusFrame = BuildStartButton(screen, roundSprite, out Button startButton);
        UiRulesTab rulesTab = BuildRules(screen, roundSprite);

        UiTitleView view = root.AddComponent<UiTitleView>();
        SetObjectArray(view, "_rows", rows);
        SetObject(view, "_cpuLevelGroup", cpuLevelGroup);
        SetObject(view, "_startFocusFrame", startFocusFrame);
        SetObject(view, "_startButton", startButton);
        SetObject(view, "_rulesTab", rulesTab);
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

        // 枠の絵は、中身より奥に描くため先に作る（見本の位置と大きさ）
        AddFrameArt(screen, "SettingsFrame", "UI-04", 40f, 304f, 462f, 560f, Color.white, roundSprite);
        RectTransform panel = CreateRect("SettingsPanel", screen, 20f, 320f, 456f, 478f);

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

        AddFrameArt(screen, "ControlsFrame", "UI-04b", 1224f, 293f, 613f, 227f, Color.white, roundSprite);
        RectTransform panel = CreateRect("ControlsPanel", screen, 1290f, 319f, 518f, 202f);

        AddText("MoveLabel", panel, 24f, 26f, 175f, 44f, "上下左右を選ぶ", 22f, UiFontRole.Body, TextAlignmentOptions.Left);
        CreateChip(panel, "MoveP1", "WASD", 205f, 26f, 100f, 44f, roundSprite).SetSelected(true);
        CreateChip(panel, "MoveP2", "↑←↓→", 335f, 26f, 100f, 44f, roundSprite).SetSelected(true);

        AddText("DefuseLabel", panel, 24f, 120f, 175f, 44f, "解除に挑戦", 22f, UiFontRole.Body, TextAlignmentOptions.Left);
        CreateChip(panel, "DefuseP1", "Q", 205f, 120f, 100f, 44f, roundSprite).SetSelected(true);
        CreateChip(panel, "DefuseP2", "右Shift か /", 335f, 120f, 170f, 44f, roundSprite).SetSelected(true);
    }

    /// <summary>
    /// すでにあるタイトルのプレハブを作り直さずに、固定表示だったルールを「飛び出すタブ」に差し替える
    /// （プレハブを作り直すと中身のIDが変わり、Title シーン側の設定が外れることがあるため）
    /// </summary>
    [MenuItem("Tools/UI仮組/タイトルのルールをタブにする")]
    public static void ReplaceTitleRulesWithTab()
    {
        if (!File.Exists(TitlePrefabPath))
        {
            Debug.LogWarning("UI_Title.prefab がありません。先に「タイトルを作る」を実行してください");
            return;
        }

        GameObject contents = PrefabUtility.LoadPrefabContents(TitlePrefabPath);

        try
        {
            RectTransform screen = (RectTransform)contents.transform.Find("Screen");
            DestroyChildIfExists(screen, "RulesHeading");
            DestroyChildIfExists(screen, "RulesText");
            DestroyChildIfExists(screen, RulesTabName);

            Sprite roundSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            UiRulesTab rulesTab = BuildRules(screen, roundSprite);
            SetObject(contents.GetComponent<UiTitleView>(), "_rulesTab", rulesTab);

            contents.GetComponent<UiSkinApplier>().ApplyAll();
            PrefabUtility.SaveAsPrefabAsset(contents, TitlePrefabPath);
            Debug.Log("タイトルのルールを飛び出すタブに差し替えました");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    /// <summary>
    /// 名前で見つかった子を消す（なければ何もしない）
    /// </summary>
    /// <param name="parent">探す親</param>
    /// <param name="childName">消す子の名前</param>
    private static void DestroyChildIfExists(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);

        if (child != null)
        {
            Object.DestroyImmediate(child.gameObject);
        }
    }

    /// <summary>
    /// 右端から飛び出す「ルール」のタブを作る。画面の一番手前に置く（開いている間は後ろを暗くして、押せなくする）
    /// </summary>
    /// <param name="screen">1920×1080 の土台</param>
    /// <param name="roundSprite">角丸の仮の形</param>
    /// <returns>作ったタブの制御</returns>
    private static UiRulesTab BuildRules(RectTransform screen, Sprite roundSprite)
    {
        const float DrawerWidth = 720f;
        const float DrawerHeight = 540f;
        const float DrawerTop = 500f;
        const float TabWidth = 88f;
        const float TabHeight = 250f;
        const float TabTop = 145f;
        const float Padding = 44f;

        // 画面の外にいるパネルが、横に広い画面ではみ出して見えないよう、土台の範囲で切る
        RectTransform root = CreateStretch(RulesTabName, screen);
        root.gameObject.AddComponent<RectMask2D>();

        RectTransform dim = CreateStretch("Dim", root);
        Image dimImage = dim.gameObject.AddComponent<Image>();
        dimImage.color = new Color(0.04f, 0.05f, 0.07f, 0.45f);
        CanvasGroup dimGroup = dim.gameObject.AddComponent<CanvasGroup>();
        dimGroup.alpha = 0f;
        dimGroup.blocksRaycasts = false;
        Button dimButton = AddClickArea(dim);

        RectTransform drawer = CreateRect("Drawer", root, ScreenWidth, DrawerTop, DrawerWidth, DrawerHeight);
        AddArt(drawer, "UI-04c", Color.white, roundSprite, false, null);
        AddOutline(drawer);
        // パネルの上のクリックが、後ろの暗い幕に抜けて閉じてしまわないようにする
        drawer.GetComponent<Image>().raycastTarget = true;

        RectTransform tab = CreateRect("Tab", drawer, -TabWidth, TabTop, TabWidth, TabHeight);
        AddArt(tab, "UI-01", Orange, roundSprite, false, null);
        AddOutline(tab);
        Button tabButton = AddClickArea(tab);

        // タブの文字だけ縦書きにする。TextMeshPro に縦書きはないので、1字ずつ改行して縦に積む（長音「ー」は縦棒「｜」にする）
        AddText("Label", tab, 0f, 0f, TabWidth, TabHeight, "ル\n｜\nル", 44f, UiFontRole.Heading, TextAlignmentOptions.Center);

        // パネルの中は、見出しも本文もすべて横書き
        AddText("Heading", drawer, Padding, 36f, 400f, 64f, "ルール", 44f, UiFontRole.Heading, TextAlignmentOptions.Left);

        // スクロールは付けず、全文が収まる高さに固定する。切り詰めや自動縮小もしない
        string rules =
            "・爆弾を受け取ったら10秒以内に謎を解いて相手に返す\n" +
            "・間違えると残り時間−2秒\n" +
            "・解除方法は爆弾に書いてある\n" +
            "・5回目のパスからは解除に挑戦できる。正解すれば即勝利、外せば即爆発。解除は1ラウンド1回まで";
        TextMeshProUGUI rulesText = AddText("Body", drawer, Padding, 108f, DrawerWidth - Padding * 2f, 364f, rules, 26f, UiFontRole.Body, TextAlignmentOptions.TopLeft);
        rulesText.textWrappingMode = TextWrappingModes.Normal;
        rulesText.overflowMode = TextOverflowModes.Overflow;
        rulesText.enableAutoSizing = false;
        rulesText.lineSpacing = 20f;

        TextMeshProUGUI hint = AddText("Hint", drawer, Padding, DrawerHeight - 60f, DrawerWidth - Padding * 2f, 40f, "R か Esc で閉じる", 22f, UiFontRole.Body, TextAlignmentOptions.Right);
        hint.color = SubTextColor;

        UiRulesTab rulesTab = root.gameObject.AddComponent<UiRulesTab>();
        SetObject(rulesTab, "_drawer", drawer);
        SetObject(rulesTab, "_dim", dimGroup);
        SetObject(rulesTab, "_tabButton", tabButton);
        SetObject(rulesTab, "_dimButton", dimButton);
        return rulesTab;
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

    /// <summary>
    /// Canvas と 1920×1080 の土台を作る
    /// </summary>
    /// <param name="name">画面の名前</param>
    /// <param name="skin">素材の表</param>
    /// <returns>画面の一番上</returns>
    private static GameObject CreateCanvasRoot(string name, UiSkin skin)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ScreenWidth, ScreenHeight);
        scaler.matchWidthOrHeight = 0.5f;

        UiSkinApplier applier = root.AddComponent<UiSkinApplier>();
        SetObject(applier, "_skin", skin);

        GameObject screenObject = new GameObject("Screen", typeof(RectTransform));
        RectTransform screen = (RectTransform)screenObject.transform;
        screen.SetParent(root.transform, false);
        screen.anchorMin = new Vector2(0.5f, 0.5f);
        screen.anchorMax = new Vector2(0.5f, 0.5f);
        screen.pivot = new Vector2(0.5f, 0.5f);
        screen.sizeDelta = new Vector2(ScreenWidth, ScreenHeight);
        return root;
    }

    /// <summary>
    /// 左上を基準に、位置と大きさを指定して四角を作る
    /// </summary>
    /// <param name="name">オブジェクト名</param>
    /// <param name="parent">置く場所</param>
    /// <param name="x">左からの位置</param>
    /// <param name="y">上からの位置</param>
    /// <param name="width">幅</param>
    /// <param name="height">高さ</param>
    /// <returns>作った四角</returns>
    private static RectTransform CreateRect(string name, Transform parent, float x, float y, float width, float height)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)gameObject.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
        return rect;
    }

    /// <summary>
    /// 親いっぱいに広がる四角を作る
    /// </summary>
    /// <param name="name">オブジェクト名</param>
    /// <param name="parent">置く場所</param>
    /// <returns>作った四角</returns>
    private static RectTransform CreateStretch(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)gameObject.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    /// <summary>
    /// 素材の差し込み口を付ける（Image は子より奥に描かれるので、仮の文字は上に出る）
    /// </summary>
    /// <param name="target">付ける四角</param>
    /// <param name="artId">素材ID</param>
    /// <param name="placeholderColor">素材がないときの色</param>
    /// <param name="placeholderSprite">素材がないときの形</param>
    /// <param name="isPreserveAspect">素材の縦横比を保つか</param>
    /// <param name="placeholderObject">素材がないときだけ出すもの</param>
    private static void AddArt(RectTransform target, string artId, Color placeholderColor, Sprite placeholderSprite, bool isPreserveAspect, GameObject placeholderObject)
    {
        Image image = target.gameObject.AddComponent<Image>();
        image.raycastTarget = false;

        UiArtSlot slot = target.gameObject.AddComponent<UiArtSlot>();
        SerializedObject serialized = new SerializedObject(slot);
        serialized.FindProperty("_artId").stringValue = artId;
        serialized.FindProperty("_placeholderColor").colorValue = placeholderColor;
        serialized.FindProperty("_placeholderSprite").objectReferenceValue = placeholderSprite;
        serialized.FindProperty("_isPreserveAspect").boolValue = isPreserveAspect;
        serialized.FindProperty("_placeholderObject").objectReferenceValue = placeholderObject;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// マウスで押せるように Button を付ける。見た目は変えず、四角いクリック範囲だけを足す
    /// </summary>
    /// <param name="target">付ける四角</param>
    /// <returns>付けた Button</returns>
    private static Button AddClickArea(RectTransform target)
    {
        // 見た目用の Image があればそれを、なければ透明な Image を、クリックを受ける当たり判定にする
        Image hitArea = target.GetComponent<Image>();

        if (hitArea == null)
        {
            hitArea = target.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;
        }

        hitArea.raycastTarget = true;
        // 透明な Image は描画から外されてクリックも届かなくなるので、外されないようにする
        hitArea.canvasRenderer.cullTransparentMesh = false;

        Button button = target.gameObject.AddComponent<Button>();
        button.targetGraphic = hitArea;
        // 見た目の切り替えは自前で行う。矢印キーによる選択移動は UiTitleInput と取り合うので切る
        button.transition = Selectable.Transition.None;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        return button;
    }

    /// <summary>
    /// 枠に細い線を付ける
    /// </summary>
    /// <param name="target">付ける四角</param>
    private static void AddOutline(RectTransform target)
    {
        Outline outline = target.gameObject.AddComponent<Outline>();
        outline.effectColor = PanelLineColor;
        outline.effectDistance = new Vector2(2f, -2f);
    }

    /// <summary>
    /// 文字を作る
    /// </summary>
    /// <param name="name">オブジェクト名</param>
    /// <param name="parent">置く場所</param>
    /// <param name="x">左からの位置</param>
    /// <param name="y">上からの位置</param>
    /// <param name="width">幅</param>
    /// <param name="height">高さ</param>
    /// <param name="text">文字</param>
    /// <param name="fontSize">文字の大きさ</param>
    /// <param name="role">文字の役割</param>
    /// <param name="alignment">文字の寄せ方</param>
    /// <returns>作った文字</returns>
    private static TextMeshProUGUI AddText(string name, Transform parent, float x, float y, float width, float height, string text, float fontSize, UiFontRole role, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(name, parent, x, y, width, height);
        TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = TextColor;
        tmp.alignment = alignment;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;

        UiFontSlot fontSlot = rect.gameObject.AddComponent<UiFontSlot>();
        SerializedObject serialized = new SerializedObject(fontSlot);
        serialized.FindProperty("_role").enumValueIndex = (int)role;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return tmp;
    }

    /// <summary>
    /// private のフィールドに参照を入れる
    /// </summary>
    /// <param name="target">入れる先のコンポーネント</param>
    /// <param name="fieldName">フィールド名</param>
    /// <param name="value">入れる参照</param>
    private static void SetObject(Object target, string fieldName, Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(fieldName).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// private の配列のフィールドに参照を入れる
    /// </summary>
    /// <param name="target">入れる先のコンポーネント</param>
    /// <param name="fieldName">フィールド名</param>
    /// <param name="values">入れる参照</param>
    private static void SetObjectArray(Object target, string fieldName, Object[] values)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(fieldName);
        property.arraySize = values.Length;

        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// private の整数のフィールドに値を入れる
    /// </summary>
    /// <param name="target">入れる先のコンポーネント</param>
    /// <param name="fieldName">フィールド名</param>
    /// <param name="value">入れる値</param>
    private static void SetInt(Object target, string fieldName, int value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(fieldName).intValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// UiSkin を読み込む。なければ作り、足りない素材の登録を足す（入れた画像は消さない）
    /// </summary>
    /// <returns>素材の表</returns>
    private static UiSkin LoadOrCreateSkin()
    {
        EnsureFolder(PrefabFolder);
        UiSkin skin = AssetDatabase.LoadAssetAtPath<UiSkin>(SkinPath);

        if (skin == null)
        {
            skin = ScriptableObject.CreateInstance<UiSkin>();
            AssetDatabase.CreateAsset(skin, SkinPath);
        }

        UiSkinEntry[] titleEntries =
        {
            new UiSkinEntry("ART-13", "タイトル背景", new Vector2(1920f, 1080f), "title_bg"),
            new UiSkinEntry("ART-14", "タイトルロゴ", new Vector2(700f, 130f), "title_logo"),
            new UiSkinEntry("ART-09_normal", "キャラ P1（通常）", new Vector2(600f, 900f), "chr_p1_normal"),
            new UiSkinEntry("ART-10_normal", "キャラ P2（通常）", new Vector2(600f, 900f), "chr_p2_normal"),
            new UiSkinEntry("ART-01", "爆弾本体", new Vector2(800f, 800f), "bomb_core"),
            new UiSkinEntry("UI-01", "選択肢ボタン（選択中）", new Vector2(104f, 44f), "ui_chip_on"),
            new UiSkinEntry("UI-02", "選択肢ボタン（未選択）", new Vector2(104f, 44f), "ui_chip_off"),
            new UiSkinEntry("UI-03", "ゲームスタートボタン", new Vector2(650f, 200f), "ui_start"),
            new UiSkinEntry("UI-04", "メニューの枠（対戦設定。網点）", new Vector2(456f, 478f), "ui_panel"),
            new UiSkinEntry("UI-04b", "メニューの枠（操作メニュー。斜線）", new Vector2(518f, 202f), "ui_panel_controls"),
            new UiSkinEntry("UI-04c", "メニューの枠（ルール。灰色の斜線の内枠）", new Vector2(720f, 540f), "ui_panel_rules")
        };

        bool isChanged = false;

        foreach (UiSkinEntry entry in titleEntries)
        {
            isChanged |= skin.AddEntryIfMissing(entry);
        }

        foreach (UiSkinEntry entry in CreateGameEntries())
        {
            isChanged |= skin.AddEntryIfMissing(entry);
        }

        foreach (UiSkinEntry entry in CreateResultEntries())
        {
            isChanged |= skin.AddEntryIfMissing(entry);
        }

        foreach (UiSkinEntry entry in CreateConveyorEntries())
        {
            isChanged |= skin.AddEntryIfMissing(entry);
        }

        foreach (UiSkinEntry entry in CreatePcScreenEntries())
        {
            isChanged |= skin.AddEntryIfMissing(entry);
        }

        // 爆弾の素材のファイル名が決まったので、すでにある登録の探すファイル名を合わせる
        isChanged |= skin.UpdateFileName("ART-04_on", "bomb_lamp");
        isChanged |= skin.UpdateFileName("ART-04_off", "bomb_lamp_blackout");

        foreach (string staleId in StaleArtIds)
        {
            isChanged |= skin.RemoveEntryIfEmpty(staleId);
        }

        if (isChanged)
        {
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();
        }

        return skin;
    }

    /// <summary>
    /// Art フォルダの画像を、ファイル名か素材IDで UiSkin の空いている登録に割り当てる
    /// </summary>
    /// <param name="skin">素材の表</param>
    /// <returns>割り当てた数</returns>
    private static int AssignSpritesByName(UiSkin skin)
    {
        if (!AssetDatabase.IsValidFolder(ArtFolder))
        {
            return 0;
        }

        int assignedCount = 0;

        foreach (string textureGuid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtFolder }))
        {
            EnsureSpriteImport(AssetDatabase.GUIDToAssetPath(textureGuid));
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Sprite", new[] { ArtFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            string fileName = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();

            foreach (UiSkinEntry entry in skin.Entries)
            {
                if (entry.Sprite != null || sprite == null)
                {
                    continue;
                }

                string idName = entry.Id.ToLowerInvariant().Replace('-', '_');
                bool isMatch = fileName == entry.FileName
                    || fileName.EndsWith("_" + entry.FileName)
                    || fileName == idName
                    || fileName.StartsWith(idName + "_");

                if (isMatch)
                {
                    entry.Sprite = sprite;
                    assignedCount++;
                }
            }
        }

        EditorUtility.SetDirty(skin);
        AssetDatabase.SaveAssets();
        return assignedCount;
    }

    /// <summary>
    /// 仮組みのプレハブすべてに、UiSkin の中身を反映して保存する
    /// </summary>
    private static void ApplySkinToPrefabs()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            UiSkinApplier applier = contents.GetComponent<UiSkinApplier>();

            if (applier != null)
            {
                applier.ApplyAll();
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }

            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    /// <summary>
    /// 確認用のシーンを作る（今開いているシーンは閉じない）
    /// </summary>
    /// <param name="prefab">置くプレハブ</param>
    /// <param name="scenePath">保存先</param>
    /// <param name="onInstantiated">プレハブをシーンに置いた直後に呼ぶ処理（確認用の部品を足すなど）。不要なら null</param>
    private static void CreatePreviewScene(GameObject prefab, string scenePath, System.Action<GameObject> onInstantiated = null)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        SceneManager.MoveGameObjectToScene(cameraObject, scene);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        onInstantiated?.Invoke(instance);
        EditorSceneManager.SaveScene(scene, scenePath);
        EditorSceneManager.CloseScene(scene, true);
    }

    /// <summary>
    /// フォルダがなければ、親から順に作る
    /// </summary>
    /// <param name="folderPath">Assets から始まるフォルダのパス</param>
    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folderPath));
    }
}
