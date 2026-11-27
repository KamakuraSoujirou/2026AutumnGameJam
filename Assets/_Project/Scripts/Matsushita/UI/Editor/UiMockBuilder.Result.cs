using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UIの仮組み（リザルト画面）。見本画像（上に勝ち数とキャラ、下に各ラウンドの表、左下に HOME、右下に再戦）に合わせて組み立てる
/// </summary>
public static partial class UiMockBuilder
{
    // 試合中に UiResultRecorder が Resources.Load で読むので、Resources の下に置く
    private const string ResultPrefabFolder = "Assets/Resources/UI";
    private const string ResultPrefabPath = ResultPrefabFolder + "/UI_Result.prefab";
    private const string ResultScenePath = SceneFolder + "/UiMock_Result.unity";
    private const int ResultRowCount = 5;

    /// <summary>
    /// 見本画像（2000×1125）の座標を、1920×1080 に合わせる倍率
    /// </summary>
    private const float MockScale = 0.96f;

    private static readonly Color StarColor = new Color(0.95f, 0.55f, 0.1f);
    private static readonly Color WinTextColor = new Color(0.95f, 0.8f, 0.1f);
    private static readonly Color LoseTextColor = new Color(0.5f, 0.5f, 0.5f);
    private static readonly Color StrongLineColor = new Color(0.1f, 0.1f, 0.1f);
    private static readonly Color WeakLineColor = new Color(0.6f, 0.6f, 0.6f);

    // 表の列の境目（見本画像の座標）：ラウンド｜仕組み｜P1｜決着｜P2｜パス回数
    private static readonly float[] ResultColumnEdges = { 0f, 190f, 813f, 938f, 1152f, 1278f, 2000f };

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

        EnsureFolder(ResultPrefabFolder);
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
            new UiSkinEntry("ART-22", "「WIN」の文字", new Vector2(380f, 150f), "result_win"),
            new UiSkinEntry("ART-23", "「LOSE」の文字", new Vector2(400f, 150f), "result_lose"),
            new UiSkinEntry("UI-23", "リザルトのスコア板（斜線）", new Vector2(730f, 145f), "ui_result_score"),
            new UiSkinEntry("UI-24", "リザルトのP1の後ろの六角形（網点）", new Vector2(535f, 360f), "ui_result_hex_p1"),
            new UiSkinEntry("UI-25", "リザルトのP2の後ろの六角形（UI-24の鏡写し）", new Vector2(535f, 360f), "ui_result_hex_p2"),
            new UiSkinEntry("UI-26", "リザルトの結果表の枠（左オレンジ・右ピンクの網点）", new Vector2(1870f, 440f), "ui_result_table"),
            new UiSkinEntry("UI-21", "リザルトのHOMEボタン", new Vector2(135f, 115f), "ui_result_home"),
            new UiSkinEntry("UI-22", "リザルトの再戦ボタン（丸）", new Vector2(146f, 146f), "ui_result_restart")
        };
    }

    /// <summary>
    /// 見本画像の値を、1920×1080 の値に直す
    /// </summary>
    /// <param name="value">見本画像での値</param>
    /// <returns>1920×1080 での値</returns>
    private static float M(float value)
    {
        return value * MockScale;
    }

    /// <summary>
    /// 見本画像の座標（左上基準）で四角を作る
    /// </summary>
    /// <param name="name">オブジェクト名</param>
    /// <param name="parent">置く場所</param>
    /// <param name="x">左からの位置（見本画像の値）</param>
    /// <param name="y">上からの位置（見本画像の値）</param>
    /// <param name="width">幅（見本画像の値）</param>
    /// <param name="height">高さ（見本画像の値）</param>
    /// <returns>作った四角</returns>
    private static RectTransform MockRect(string name, Transform parent, float x, float y, float width, float height)
    {
        return CreateRect(name, parent, M(x), M(y), M(width), M(height));
    }

    /// <summary>
    /// 見本画像の座標で、色つきの細い線（四角）を作る
    /// </summary>
    /// <param name="name">オブジェクト名</param>
    /// <param name="parent">置く場所</param>
    /// <param name="x">左からの位置（見本画像の値）</param>
    /// <param name="y">上からの位置（見本画像の値）</param>
    /// <param name="width">幅（見本画像の値）</param>
    /// <param name="height">高さ（見本画像の値）</param>
    /// <param name="color">線の色</param>
    private static void CreateMockLine(string name, Transform parent, float x, float y, float width, float height, Color color)
    {
        RectTransform line = MockRect(name, parent, x, y, width, height);
        Image image = line.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    /// <summary>
    /// リザルト画面の中身を、奥に描くものから順に並べる（座標は見本画像の左上から）
    /// </summary>
    /// <param name="root">画面の一番上</param>
    /// <param name="screen">1920×1080 の土台</param>
    private static void BuildResultContents(GameObject root, RectTransform screen)
    {
        Sprite roundSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Sprite circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        RectTransform background = CreateStretch("Background", screen);
        AddArt(background, "ART-21", Color.white, null, false, null);
        CreateMockLine("Horizon", screen, 0f, 180f, 2000f, 2f, WeakLineColor);

        UiResultPlayer player1 = BuildResultPlayer(screen, 0);
        UiResultPlayer player2 = BuildResultPlayer(screen, 1);
        BuildResultScore(screen, out TextMeshProUGUI player1Score, out TextMeshProUGUI player2Score);
        UiResultRow[] rows = BuildResultTable(screen);
        BuildResultButtons(screen, roundSprite, circleSprite, out Button homeButton, out Button restartButton, out GameObject[] focusFrames);

        UiResultView view = root.AddComponent<UiResultView>();
        SetObjectArray(view, "_players", new Object[] { player1, player2 });
        SetObject(view, "_player1ScoreText", player1Score);
        SetObject(view, "_player2ScoreText", player2Score);
        SetObjectArray(view, "_rows", rows);
        SetObject(view, "_homeButton", homeButton);
        SetObject(view, "_restartButton", restartButton);
        SetObjectArray(view, "_focusFrames", focusFrames);
        root.AddComponent<UiResultInput>();

        // プレハブを単体で開いたときにも見られるよう、最初の表示を入れておく（見本画像と同じ並び）
        view.Show(0, "P1", "P2", new[]
        {
            new UiResultRound("ああああああ", 0, false, 0),
            new UiResultRound("ああああああ", 1, true, 0),
            new UiResultRound("ああああああ", 1, false, 0),
            new UiResultRound("ああああああ", 0, false, 0),
            new UiResultRound("ああああああ", 0, false, 0)
        });
    }

    /// <summary>
    /// キャラ1人分（キャラ・「P1」「P2」の名前・「WIN」「LOSE」の文字）を作る。左がP1、右がP2
    /// </summary>
    /// <param name="screen">1920×1080 の土台</param>
    /// <param name="playerIndex">0：P1（左）、1：P2（右）</param>
    /// <returns>作ったキャラ</returns>
    private static UiResultPlayer BuildResultPlayer(RectTransform screen, int playerIndex)
    {
        bool isLeft = playerIndex == 0;
        string tag = isLeft ? "P1" : "P2";
        string artId = isLeft ? "ART-09" : "ART-10";

        RectTransform player = CreateStretch($"Player{tag}", screen);

        // キャラの後ろの六角形（見本画像で、左は x75〜610、右は x1465〜2000、y190〜550）。キャラより奥に描く
        RectTransform hexagon = MockRect("Hexagon", player, isLeft ? 75f : 1465f, 190f, 535f, 360f);
        AddArt(hexagon, isLeft ? "UI-24" : "UI-25", Color.clear, null, false, null);

        // キャラは中心が見本画像の 342（左）／1735（右）あたり。勝ちの文字は内側の高い位置、負けの文字は内側の低い位置
        float characterX = isLeft ? 195f : 1588f;
        RectTransform character = MockRect("Character", player, characterX, 55f, 295f, 493f);
        GameObject winVisual = CreateResultVisual(character, "Win", $"ART-11_{tag.ToLowerInvariant()}_win", $"{artId}\n{tag}\n勝ち顔", M(295f));
        GameObject loseVisual = CreateResultVisual(character, "Lose", $"{artId}_afro", $"{artId}\n{tag}\nアフロ", M(295f));
        loseVisual.SetActive(false);

        TextAlignmentOptions nameAlignment = isLeft ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
        float nameX = isLeft ? 40f : 1700f;
        TextMeshProUGUI nameText = AddText("Name", player, M(nameX), M(410f), M(265f), M(130f), tag, M(130f), UiFontRole.Heading, nameAlignment);

        float winCenterX = isLeft ? 706f : 1294f;
        float loseCenterX = isLeft ? 619f : 1381f;
        GameObject winTag = CreateResultTag(player, "WinTag", "WIN", "ART-22", winCenterX, 283f, 380f, WinTextColor);
        GameObject loseTag = CreateResultTag(player, "LoseTag", "LOSE", "ART-23", loseCenterX, 430f, 400f, LoseTextColor);

        UiResultPlayer resultPlayer = player.gameObject.AddComponent<UiResultPlayer>();
        SetObject(resultPlayer, "_winVisual", winVisual);
        SetObject(resultPlayer, "_loseVisual", loseVisual);
        SetObject(resultPlayer, "_winTag", winTag);
        SetObject(resultPlayer, "_loseTag", loseTag);
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
        TextMeshProUGUI label = AddText("PlaceholderLabel", visual, 0f, 200f, width, 200f, placeholderLabel, 36f, UiFontRole.Body, TextAlignmentOptions.Center);
        label.textWrappingMode = TextWrappingModes.Normal;
        AddArt(visual, artId, GrayFill, null, true, label.gameObject);
        return visual.gameObject;
    }

    /// <summary>
    /// 「WIN」「LOSE」の文字を作る（素材があれば画像に差し替わる）
    /// </summary>
    /// <param name="parent">置く場所</param>
    /// <param name="name">オブジェクト名</param>
    /// <param name="text">仮の文字</param>
    /// <param name="artId">素材ID</param>
    /// <param name="centerX">中心の左からの位置（見本画像の値）</param>
    /// <param name="centerY">中心の上からの位置（見本画像の値）</param>
    /// <param name="width">幅（見本画像の値）</param>
    /// <param name="color">仮の文字の色</param>
    /// <returns>作った文字</returns>
    private static GameObject CreateResultTag(RectTransform parent, string name, string text, string artId, float centerX, float centerY, float width, Color color)
    {
        const float Height = 150f;

        RectTransform tag = MockRect(name, parent, centerX - width * 0.5f, centerY - Height * 0.5f, width, Height);
        TextMeshProUGUI label = AddText("Label", tag, 0f, 0f, M(width), M(Height), text, M(150f), UiFontRole.Heading, TextAlignmentOptions.Center);
        label.color = color;
        AddArt(tag, artId, Color.clear, null, true, label.gameObject);
        return tag.gameObject;
    }

    /// <summary>
    /// 上の中央の勝ち数（「P1の勝ち数 ー P2の勝ち数」と、両脇の縦線）を作る
    /// </summary>
    /// <param name="screen">1920×1080 の土台</param>
    /// <param name="player1Score">P1の勝ち数の文字</param>
    /// <param name="player2Score">P2の勝ち数の文字</param>
    private static void BuildResultScore(RectTransform screen, out TextMeshProUGUI player1Score, out TextMeshProUGUI player2Score)
    {
        // 勝ち数の後ろの斜線の板（見本画像で x640〜1400、y15〜165）
        RectTransform plate = MockRect("ScorePlate", screen, 640f, 15f, 760f, 150f);
        AddArt(plate, "UI-23", Color.clear, null, false, null);

        CreateMockLine("ScoreDividerLeft", screen, 935f, 8f, 5f, 540f, StrongLineColor);
        CreateMockLine("ScoreDividerRight", screen, 1148f, 0f, 5f, 548f, StrongLineColor);

        player1Score = AddText("Player1Score", screen, M(740f), 0f, M(220f), M(180f), "0", M(150f), UiFontRole.Heading, TextAlignmentOptions.Center);
        AddText("ScoreDash", screen, M(930f), 0f, M(220f), M(180f), "ー", M(150f), UiFontRole.Heading, TextAlignmentOptions.Center);
        player2Score = AddText("Player2Score", screen, M(1133f), 0f, M(220f), M(180f), "0", M(150f), UiFontRole.Heading, TextAlignmentOptions.Center);
    }

    /// <summary>
    /// 下の表（見出しと、ラウンドごとの行）を作る
    /// </summary>
    /// <param name="screen">1920×1080 の土台</param>
    /// <returns>上から順の行</returns>
    private static UiResultRow[] BuildResultTable(RectTransform screen)
    {
        const float HeaderTop = 548f;
        const float HeaderBottom = 590f;
        const float TableBottom = 998f;
        const float RowHeight = (TableBottom - HeaderBottom) / ResultRowCount;

        RectTransform table = CreateStretch("Table", screen);

        // 表の後ろの大きい斜めの枠（見本画像で x45〜1915、y560〜1020）。線や文字より奥に描く
        RectTransform tableFrame = MockRect("TableFrame", table, 45f, 560f, 1870f, 460f);
        AddArt(tableFrame, "UI-26", Color.clear, null, false, null);

        CreateMockLine("HeaderTopLine", table, 0f, HeaderTop, 2000f, 2f, WeakLineColor);
        CreateMockLine("HeaderBottomLine", table, 0f, HeaderBottom, 2000f, 3f, StrongLineColor);

        for (int i = 1; i <= ResultRowCount; i++)
        {
            CreateMockLine($"RowLine{i}", table, 0f, HeaderBottom + i * RowHeight, 2000f, 2f, WeakLineColor);
        }

        for (int i = 1; i < ResultColumnEdges.Length - 1; i++)
        {
            CreateMockLine($"ColumnLine{i}", table, ResultColumnEdges[i], HeaderTop, 3f, TableBottom - HeaderTop, StrongLineColor);
        }

        string[] headers = { "ラウンド", "仕組み", "P1", "決着", "P2", "パス回数" };

        for (int i = 0; i < headers.Length; i++)
        {
            float left = ResultColumnEdges[i];
            float width = ResultColumnEdges[i + 1] - left;
            AddText($"Header{i}", table, M(left), M(HeaderTop), M(width), M(HeaderBottom - HeaderTop), headers[i], M(32f), UiFontRole.Body, TextAlignmentOptions.Center);
        }

        UiResultRow[] rows = new UiResultRow[ResultRowCount];

        for (int i = 0; i < ResultRowCount; i++)
        {
            rows[i] = BuildResultRow(table, i, HeaderBottom + i * RowHeight, RowHeight);
        }

        return rows;
    }

    /// <summary>
    /// 表の1行を作る
    /// </summary>
    /// <param name="table">表</param>
    /// <param name="index">上から何行目か</param>
    /// <param name="top">行の上端（見本画像の値）</param>
    /// <param name="height">行の高さ（見本画像の値）</param>
    /// <returns>作った行</returns>
    private static UiResultRow BuildResultRow(RectTransform table, int index, float top, float height)
    {
        RectTransform row = MockRect($"Row{index}", table, 0f, top, 2000f, height);

        TextMeshProUGUI roundText = AddResultCell(row, 0, height, (index + 1).ToString(), 64f, UiFontRole.Number);
        TextMeshProUGUI gimmickText = AddResultCell(row, 1, height, "ああああああ", 48f, UiFontRole.Heading);
        GameObject player1Star = CreateResultStar(row, "Player1Star", (ResultColumnEdges[2] + ResultColumnEdges[3]) * 0.5f, height * 0.5f);
        TextMeshProUGUI resultText = AddResultCell(row, 3, height, "爆発", 40f, UiFontRole.Heading);
        GameObject player2Star = CreateResultStar(row, "Player2Star", (ResultColumnEdges[4] + ResultColumnEdges[5]) * 0.5f, height * 0.5f);
        TextMeshProUGUI passText = AddResultCell(row, 5, height, "00回", 64f, UiFontRole.Number);

        UiResultRow resultRow = row.gameObject.AddComponent<UiResultRow>();
        SetObject(resultRow, "_roundText", roundText);
        SetObject(resultRow, "_gimmickText", gimmickText);
        SetObject(resultRow, "_player1Star", player1Star);
        SetObject(resultRow, "_player2Star", player2Star);
        SetObject(resultRow, "_resultText", resultText);
        SetObject(resultRow, "_passText", passText);
        return resultRow;
    }

    /// <summary>
    /// 行の中の1つの欄（文字）を作る
    /// </summary>
    /// <param name="row">置く行</param>
    /// <param name="columnIndex">何列目か（0から）</param>
    /// <param name="height">行の高さ（見本画像の値）</param>
    /// <param name="text">仮の文字</param>
    /// <param name="fontSize">文字の大きさ（見本画像の値）</param>
    /// <param name="role">文字の役割</param>
    /// <returns>作った文字</returns>
    private static TextMeshProUGUI AddResultCell(RectTransform row, int columnIndex, float height, string text, float fontSize, UiFontRole role)
    {
        float left = ResultColumnEdges[columnIndex];
        float width = ResultColumnEdges[columnIndex + 1] - left;
        return AddText($"Cell{columnIndex}", row, M(left), 0f, M(width), M(height), text, M(fontSize), role, TextAlignmentOptions.Center);
    }

    /// <summary>
    /// ラウンドを取った側に出す★を作る
    /// </summary>
    /// <param name="row">置く行</param>
    /// <param name="name">オブジェクト名</param>
    /// <param name="centerX">中心の左からの位置（見本画像の値）</param>
    /// <param name="centerY">中心の上からの位置（見本画像の値）</param>
    /// <returns>作った★</returns>
    private static GameObject CreateResultStar(RectTransform row, string name, float centerX, float centerY)
    {
        const float Size = 64f;

        RectTransform star = CreateCentered(name, row, M(centerX), M(centerY), M(Size), M(Size));
        TextMeshProUGUI mark = AddText("Mark", star, 0f, 0f, M(Size), M(Size), "★", M(56f), UiFontRole.Body, TextAlignmentOptions.Center);
        mark.color = StarColor;
        AddArt(star, "ART-15_on", Color.clear, null, true, mark.gameObject);
        return star.gameObject;
    }

    /// <summary>
    /// 左下の「HOME」と右下の「再戦」のボタンを作る（選んでいるときに出す枠つき）
    /// </summary>
    /// <param name="screen">1920×1080 の土台</param>
    /// <param name="roundSprite">角丸の仮の形</param>
    /// <param name="circleSprite">丸の仮の形</param>
    /// <param name="homeButton">HOME のボタン</param>
    /// <param name="restartButton">再戦のボタン</param>
    /// <param name="focusFrames">選んでいるボタンに出す枠（HOME、再戦の順）</param>
    private static void BuildResultButtons(RectTransform screen, Sprite roundSprite, Sprite circleSprite, out Button homeButton, out Button restartButton, out GameObject[] focusFrames)
    {
        RectTransform homeFrame = MockRect("HomeFocusFrame", screen, 8f, 988f, 159f, 137f);
        AddFocusFrame(homeFrame, roundSprite, Image.Type.Sliced);

        RectTransform restartFrame = MockRect("RestartFocusFrame", screen, 1785f, 950f, 170f, 170f);
        AddFocusFrame(restartFrame, circleSprite, Image.Type.Simple);

        RectTransform home = MockRect("HomeButton", screen, 20f, 1000f, 135f, 115f);
        AddArt(home, "UI-21", Orange, roundSprite, false, null);
        AddText("Label", home, 0f, 0f, M(135f), M(115f), "HOME", M(32f), UiFontRole.Heading, TextAlignmentOptions.Center);
        homeButton = AddClickArea(home);

        RectTransform restart = MockRect("RestartButton", screen, 1797f, 962f, 146f, 146f);
        AddArt(restart, "UI-22", Orange, circleSprite, false, null);
        AddText("Label", restart, 0f, 0f, M(146f), M(146f), "再戦", M(44f), UiFontRole.Heading, TextAlignmentOptions.Center);
        restartButton = AddClickArea(restart);

        focusFrames = new[] { homeFrame.gameObject, restartFrame.gameObject };
    }

    /// <summary>
    /// ボタンを選んでいるときに出す枠の見た目を付ける
    /// </summary>
    /// <param name="frame">枠の四角</param>
    /// <param name="sprite">枠の仮の形</param>
    /// <param name="type">画像の伸ばし方</param>
    private static void AddFocusFrame(RectTransform frame, Sprite sprite, Image.Type type)
    {
        Image image = frame.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = type;
        image.color = FocusFrameColor;
        image.raycastTarget = false;
        frame.gameObject.SetActive(false);
    }
}
