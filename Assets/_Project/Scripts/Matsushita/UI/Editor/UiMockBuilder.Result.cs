using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UIの仮組み（リザルト画面）。プロトタイプのリザルト（左右にキャラ、中央に各ラウンドの表、「次へ」でリスタート／タイトルへのメニュー）に合わせて組み立てる
/// </summary>
public static partial class UiMockBuilder
{
    private const string ResultPrefabPath = PrefabFolder + "/UI_Result.prefab";
    private const string ResultScenePath = SceneFolder + "/UiMock_Result.unity";
    private const int ResultRowCount = 5;

    private static readonly Color StarColor = new Color(0.95f, 0.55f, 0.1f);

    /// <summary>
    /// リザルト画面のプレハブとテストシーンを作る
    /// </summary>
    [MenuItem("Tools/UI仮組/リザルト画面を作る")]
    public static void BuildResultScreen()
    {
        if (File.Exists(ResultPrefabPath))
        {
            bool isOverwrite = EditorUtility.DisplayDialog(
                "UI仮組", "UI_Result.prefab を作り直します。プレハブを手で直した分は消えます。", "作り直す", "やめる");

            if (!isOverwrite)
            {
                return;
            }
        }

        EnsureFolder(PrefabFolder);
        EnsureFolder(SceneFolder);
        UiSkin skin = LoadOrCreateSkin();

        GameObject root = CreateCanvasRoot("UI_Result", skin);
        RectTransform screen = (RectTransform)root.transform.Find("Screen");
        BuildResultContents(root, screen);

        root.GetComponent<UiSkinApplier>().ApplyAll();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ResultPrefabPath);
        Object.DestroyImmediate(root);

        CreatePreviewScene(prefab, ResultScenePath, instance => instance.AddComponent<UiResultPreview>());
        Debug.Log($"リザルト画面の仮組みを作りました：{ResultPrefabPath} / {ResultScenePath}");

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(ResultScenePath);
        }
    }

    /// <summary>
    /// リザルト画面で使う素材の登録を返す（試合画面と共通の素材は、試合画面の登録を使う）
    /// </summary>
    /// <returns>素材の登録</returns>
    private static List<UiSkinEntry> CreateResultEntries()
    {
        return new List<UiSkinEntry>
        {
            new UiSkinEntry("ART-21", "リザルトの背景", new Vector2(1920f, 1080f), "result_bg"),
            new UiSkinEntry("ART-11_p1_win", "キャラ P1（勝ち顔）", new Vector2(600f, 900f), "chr_p1_win"),
            new UiSkinEntry("ART-11_p2_win", "キャラ P2（勝ち顔）", new Vector2(600f, 900f), "chr_p2_win"),
            new UiSkinEntry("UI-19", "リザルトの表の枠", new Vector2(700f, 730f), "ui_result_table"),
            new UiSkinEntry("UI-20", "リザルトのボタン（次へ・リスタート・タイトルへ）", new Vector2(480f, 110f), "ui_result_button")
        };
    }

    /// <summary>
    /// リザルト画面の中身を、奥に描くものから順に並べる（座標は 1920×1080 の左上から）
    /// </summary>
    /// <param name="root">画面の一番上</param>
    /// <param name="screen">1920×1080 の土台</param>
    private static void BuildResultContents(GameObject root, RectTransform screen)
    {
        Sprite roundSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        RectTransform background = CreateStretch("Background", screen);
        AddArt(background, "ART-21", Color.white, null, false, null);

        UiResultPlayer player1 = BuildResultPlayer(screen, 0);
        UiResultPlayer player2 = BuildResultPlayer(screen, 1);
        UiResultRow[] rows = BuildResultTable(screen, roundSprite);
        Button nextButton = BuildResultNextButton(screen, roundSprite);
        BuildResultMenu(screen, roundSprite, out GameObject menuRoot, out Button[] menuButtons, out GameObject[] menuFocusFrames);

        UiResultView view = root.AddComponent<UiResultView>();
        SetObjectArray(view, "_players", new Object[] { player1, player2 });
        SetObjectArray(view, "_rows", rows);
        SetObject(view, "_nextButton", nextButton);
        SetObject(view, "_menuRoot", menuRoot);
        SetObjectArray(view, "_menuButtons", menuButtons);
        SetObjectArray(view, "_menuFocusFrames", menuFocusFrames);
        root.AddComponent<UiResultInput>();

        // プレハブを単体で開いたときにも見られるよう、最初の表示を入れておく
        view.Show(0, "P1", "COM", new[] { 0, 1, 0 });
    }

    /// <summary>
    /// キャラ1人分（「勝ち」「負け」の札・名前・勝ち顔・アフロ）を作る
    /// </summary>
    /// <param name="screen">1920×1080 の土台</param>
    /// <param name="playerIndex">0：P1（左）、1：P2（右）</param>
    /// <returns>作ったキャラ</returns>
    private static UiResultPlayer BuildResultPlayer(RectTransform screen, int playerIndex)
    {
        const float Width = 440f;

        bool isLeft = playerIndex == 0;
        string tag = isLeft ? "P1" : "P2";
        string artId = isLeft ? "ART-09" : "ART-10";
        float x = isLeft ? 100f : ScreenWidth - 100f - Width;

        RectTransform player = CreateRect($"Player{tag}", screen, x, 190f, Width, 780f);

        TextMeshProUGUI tagText = AddText("Tag", player, 0f, 0f, Width, 90f, "勝ち", 72f, UiFontRole.Heading, TextAlignmentOptions.Center);
        TextMeshProUGUI nameText = AddText("Name", player, 0f, 90f, Width, 60f, tag, 44f, UiFontRole.Body, TextAlignmentOptions.Center);

        RectTransform character = CreateRect("Character", player, 0f, 170f, Width, 610f);
        GameObject winVisual = CreateResultVisual(character, "Win", $"ART-11_{tag.ToLowerInvariant()}_win", $"{artId}\n{tag}\n勝ち顔", Width);
        GameObject loseVisual = CreateResultVisual(character, "Lose", $"{artId}_afro", $"{artId}\n{tag}\nアフロ", Width);
        loseVisual.SetActive(false);

        UiResultPlayer resultPlayer = player.gameObject.AddComponent<UiResultPlayer>();
        SetObject(resultPlayer, "_winVisual", winVisual);
        SetObject(resultPlayer, "_loseVisual", loseVisual);
        SetObject(resultPlayer, "_tagText", tagText);
        SetObject(resultPlayer, "_nameText", nameText);
        return resultPlayer;
    }

    /// <summary>
    /// キャラの見た目1つ（勝ち顔かアフロ）を作る
    /// </summary>
    /// <param name="parent">置く場所</param>
    /// <param name="name">オブジェクト名</param>
    /// <param name="artId">素材ID</param>
    /// <param name="placeholderLabel">素材がないときに出す文字</param>
    /// <param name="width">幅</param>
    /// <returns>作った見た目</returns>
    private static GameObject CreateResultVisual(RectTransform parent, string name, string artId, string placeholderLabel, float width)
    {
        RectTransform visual = CreateStretch(name, parent);
        TextMeshProUGUI label = AddText("PlaceholderLabel", visual, 0f, 200f, width, 200f, placeholderLabel, 40f, UiFontRole.Body, TextAlignmentOptions.Center);
        label.textWrappingMode = TextWrappingModes.Normal;
        AddArt(visual, artId, GrayFill, null, true, label.gameObject);
        return visual.gameObject;
    }

    /// <summary>
    /// 中央の表（「リザルト」の見出しと、ラウンドごとの行）を作る
    /// </summary>
    /// <param name="screen">1920×1080 の土台</param>
    /// <param name="roundSprite">角丸の仮の形</param>
    /// <returns>上から順の行</returns>
    private static UiResultRow[] BuildResultTable(RectTransform screen, Sprite roundSprite)
    {
        const float RowHeight = 110f;

        RectTransform table = CreateRect("Table", screen, 610f, 110f, 700f, 730f);
        AddArt(table, "UI-19", LightGray, roundSprite, false, null);
        AddOutline(table);

        AddText("Heading", table, 0f, 30f, 700f, 100f, "リザルト", 72f, UiFontRole.Heading, TextAlignmentOptions.Center);

        UiResultRow[] rows = new UiResultRow[ResultRowCount];

        for (int i = 0; i < ResultRowCount; i++)
        {
            RectTransform row = CreateRect($"Row{i}", table, 40f, 170f + i * RowHeight, 620f, 100f);
            AddText("Label", row, 110f, 10f, 400f, 80f, $"Round {i + 1}", 52f, UiFontRole.Number, TextAlignmentOptions.Center);
            GameObject player1Star = CreateResultStar(row, "Player1Star", 70f);
            GameObject player2Star = CreateResultStar(row, "Player2Star", 550f);

            rows[i] = row.gameObject.AddComponent<UiResultRow>();
            SetObject(rows[i], "_label", row.Find("Label").GetComponent<TextMeshProUGUI>());
            SetObject(rows[i], "_player1Star", player1Star);
            SetObject(rows[i], "_player2Star", player2Star);
        }

        return rows;
    }

    /// <summary>
    /// ラウンドを取った側に出す★を作る
    /// </summary>
    /// <param name="row">置く行</param>
    /// <param name="name">オブジェクト名</param>
    /// <param name="centerX">中心の左からの位置</param>
    /// <returns>作った★</returns>
    private static GameObject CreateResultStar(RectTransform row, string name, float centerX)
    {
        RectTransform star = CreateCentered(name, row, centerX, 50f, 70f, 70f);
        TextMeshProUGUI mark = AddText("Mark", star, 0f, 0f, 70f, 70f, "★", 64f, UiFontRole.Body, TextAlignmentOptions.Center);
        mark.color = StarColor;
        AddArt(star, "ART-15_on", Color.clear, null, true, mark.gameObject);
        return star.gameObject;
    }

    /// <summary>
    /// 表の下の「次へ」ボタンを作る
    /// </summary>
    /// <param name="screen">1920×1080 の土台</param>
    /// <param name="roundSprite">角丸の仮の形</param>
    /// <returns>マウスで押すためのボタン</returns>
    private static Button BuildResultNextButton(RectTransform screen, Sprite roundSprite)
    {
        RectTransform button = CreateRect("NextButton", screen, 760f, 880f, 400f, 110f);
        AddArt(button, "UI-20", Orange, roundSprite, false, null);
        AddText("Label", button, 0f, 0f, 400f, 110f, "次へ  Space", 48f, UiFontRole.Heading, TextAlignmentOptions.Center);
        return AddClickArea(button);
    }

    /// <summary>
    /// 「次へ」で出るメニュー（リスタート／タイトルへ）を作る。画面を暗くして、後ろへのクリックを止める
    /// </summary>
    /// <param name="screen">1920×1080 の土台</param>
    /// <param name="roundSprite">角丸の仮の形</param>
    /// <param name="menuRoot">メニュー全体</param>
    /// <param name="buttons">ボタン（リスタート、タイトルへの順）</param>
    /// <param name="focusFrames">選んでいるボタンに出す枠（ボタンと同じ順）</param>
    private static void BuildResultMenu(RectTransform screen, Sprite roundSprite, out GameObject menuRoot, out Button[] buttons, out GameObject[] focusFrames)
    {
        RectTransform menu = CreateStretch("Menu", screen);
        Image dim = menu.gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.7f);
        dim.raycastTarget = true;

        RectTransform panel = CreateRect("Panel", menu, 660f, 300f, 600f, 480f);
        AddArt(panel, "UI-04", Color.white, roundSprite, false, null);
        AddOutline(panel);

        string[] labels = { "リスタート", "タイトルへ" };
        buttons = new Button[labels.Length];
        focusFrames = new GameObject[labels.Length];

        for (int i = 0; i < labels.Length; i++)
        {
            float y = 60f + i * 170f;

            RectTransform frame = CreateRect($"FocusFrame{i}", panel, 48f, y - 12f, 504f, 134f);
            Image frameImage = frame.gameObject.AddComponent<Image>();
            frameImage.sprite = roundSprite;
            frameImage.type = Image.Type.Sliced;
            frameImage.color = FocusFrameColor;
            frameImage.raycastTarget = false;
            frame.gameObject.SetActive(false);
            focusFrames[i] = frame.gameObject;

            RectTransform button = CreateRect($"Button{i}", panel, 60f, y, 480f, 110f);
            AddArt(button, "UI-20", Orange, roundSprite, false, null);
            AddText("Label", button, 0f, 0f, 480f, 110f, labels[i], 56f, UiFontRole.Heading, TextAlignmentOptions.Center);
            buttons[i] = AddClickArea(button);
        }

        AddText("Hint", panel, 0f, 400f, 600f, 50f, "↑↓で選ぶ　Spaceで決める　Escで戻る", 26f, UiFontRole.Body, TextAlignmentOptions.Center);

        menu.gameObject.SetActive(false);
        menuRoot = menu.gameObject;
    }
}
