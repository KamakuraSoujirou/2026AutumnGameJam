using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UIの仮組み（試合画面）。インゲームのモック（戻る・R1・吹き出し・導火線・下の3つの帯）と、爆弾のラフ（ひし形ボタン・タイマー液晶・口の形の液晶）に合わせて組み立てる
/// </summary>
public static partial class UiMockBuilder
{
    private const string GamePrefabPath = PrefabFolder + "/UI_Game.prefab";
    private const string GameScenePath = SceneFolder + "/UiMock_Game.unity";

    private static readonly Color GrayFill = new Color(0.8f, 0.8f, 0.8f);
    private static readonly Color LightGray = new Color(0.9f, 0.9f, 0.9f);
    private static readonly Color SubTextColor = new Color(0.45f, 0.45f, 0.45f);
    private static readonly Color LcdTextColor = new Color(0.65f, 1f, 0.75f);
    private static readonly Color StarLitColor = new Color(0.08f, 0.08f, 0.08f);
    private static readonly Color StarOffColor = new Color(0.75f, 0.75f, 0.75f);
    private static readonly Color DangerRed = new Color(0.9f, 0.1f, 0.1f);
    private static readonly Color ButtonFrameColor = new Color(0.45f, 0.47f, 0.52f);

    private static readonly string[] WireNames = { "red", "blue", "yellow", "green" };

    /// <summary>ひし形ボタンの絵の状態（通常・マウスが重なった・押している間）</summary>
    private static readonly string[] ButtonLookNames = { "normal", "light", "blackout" };

    private static readonly Color[] WireColors =
    {
        new Color(1f, 0.28f, 0.4f),
        new Color(0.27f, 0.65f, 1f),
        new Color(1f, 0.9f, 0.3f),
        new Color(0.5f, 1f, 0.4f)
    };

    /// <summary>立ち絵の倍率（足元の中心を軸に大きくする）</summary>
    private const float CharacterScale = 1.3f;

    /// <summary>立ち絵を大きくしたぶん、足元を下げる量（1920×1080 の画面で）</summary>
    private const float CharacterDropY = 45f;

    /// <summary>立ち絵が大きくなったぶん、頭上の吹き出しを上げる量（1920×1080 の画面で）</summary>
    private const float BubbleRiseY = 50f;

    /// <summary>手に持つ爆弾（と、飛ぶ爆弾）の一辺（1920×1080 の画面で）</summary>
    private const float HeldBombSize = 130f;

    /// <summary>爆弾の本体の絵に含まれる口金・液晶の枠。使わなくなった素材ID（絵が空のものだけ片付ける）</summary>
    private static readonly string[] StaleArtIds =
    {
        "UI-05", "ART-19", "ART-05_normal", "UI-19", "UI-20",
        "ART-01_cap", "UI-06", "UI-17",
        "ART-05_red", "ART-05_blue", "ART-05_yellow", "ART-05_green"
    };

    /// <summary>立ち絵の描画順（大きいほど手前）</summary>
    private const int CharacterSortingOrder = 10;

    /// <summary>爆弾の本体の描画順（大きいほど手前）</summary>
    private const int BombBodySortingOrder = 20;

    /// <summary>素材を出す 2D オブジェクトの名前（立ち絵や爆弾の本体の子）</summary>
    private const string SpriteArtName = "Art";

    /// <summary>立ち絵の見た目の名前（通常・焦り顔・アフロ）</summary>
    private static readonly string[] CharacterPoseNames = { "Normal", "Panic", "Afro" };

    /// <summary>爆弾の本体の絵（800×800）を、爆弾の四角（640×640）に縮めた倍率</summary>
    private const float BombArtScale = 0.8f;

    /// <summary>ひし形ボタンの絵（200×200）を置く一辺（爆弾の四角の座標）</summary>
    private const float ButtonBoxSize = 148f;

    /// <summary>つまみが右にある絵の中で、ひし形の中心がある位置（絵の幅・高さに対する割合。上が 0）。つまみが左にある絵は左右が逆になる</summary>
    private static readonly Vector2 ButtonDiamondCenter = new Vector2(0.45f, 0.475f);

    /// <summary>絵のつまみが左にあるか（赤・青・黄・緑の順。赤と青は右、黄と緑は左に描かれている）</summary>
    private static readonly bool[] ButtonTabIsLeft = { false, false, true, true };

    /// <summary>配線の仮の線を一時的に隠す（配線の絵が届いたら false にして作り直す）</summary>
    private const bool IsWireHiddenForNow = true;

    /// <summary>ランプの消灯の見た目を一時的に隠す（消灯の絵 bomb_lamp_blackout が届いたので、今は隠さない）</summary>
    private const bool IsLampOffHiddenForNow = false;

    /// <summary>
    /// 試合画面のプレハブとテストシーンを作る
    /// </summary>
    [MenuItem("Tools/UI仮組/試合画面を作る")]
    public static void BuildGame()
    {
        if (File.Exists(GamePrefabPath))
        {
            bool isOverwrite = EditorUtility.DisplayDialog(
                "UI仮組", "UI_Game.prefab を作り直します。プレハブを手で直した分は消えます。", "作り直す", "やめる");

            if (!isOverwrite)
            {
                return;
            }
        }

        EnsureFolder(PrefabFolder);
        EnsureFolder(SceneFolder);
        UiSkin skin = LoadOrCreateSkin();

        GameObject root = CreateCanvasRoot("UI_Game", skin);
        RectTransform screen = (RectTransform)root.transform.Find("Screen");
        root.AddComponent<UiWorldCanvas>();
        BuildGameContents(root, screen);

        root.GetComponent<UiSkinApplier>().ApplyAll();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, GamePrefabPath);
        Object.DestroyImmediate(root);

        CreatePreviewScene(prefab, GameScenePath, instance =>
        {
            instance.AddComponent<PlayerInputReader>();
            instance.AddComponent<UiGamePreview>();
        });
        Debug.Log($"試合画面の仮組みを作りました：{GamePrefabPath} / {GameScenePath}");

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(GameScenePath);
        }
    }

    /// <summary>
    /// 試合画面で使う素材の登録を返す
    /// </summary>
    /// <returns>素材の登録</returns>
    private static List<UiSkinEntry> CreateGameEntries()
    {
        List<UiSkinEntry> entries = new List<UiSkinEntry>
        {
            new UiSkinEntry("ART-12", "試合の背景", new Vector2(1920f, 1080f), "game_bg"),
            new UiSkinEntry("ART-02", "導火線（1区間ぶん。伸縮する）", new Vector2(400f, 22f), "fuse"),
            new UiSkinEntry("ART-08", "火花", new Vector2(130f, 130f), "spark"),
            new UiSkinEntry("ART-01_held", "手に持つ爆弾（立ち絵の手に出す。飛ぶ爆弾も同じ絵）", new Vector2(260f, 260f), "bomb_held"),
            new UiSkinEntry("ART-04_on", "ランプ（点灯）", new Vector2(80f, 80f), "bomb_lamp"),
            new UiSkinEntry("ART-04_off", "ランプ（消灯。なければ点灯の絵を暗くして使う）", new Vector2(80f, 80f), "bomb_lamp_blackout"),
            new UiSkinEntry("ART-09_afro", "キャラ P1（アフロ）", new Vector2(600f, 900f), "chr_p1_afro"),
            new UiSkinEntry("ART-10_afro", "キャラ P2（アフロ）", new Vector2(600f, 900f), "chr_p2_afro"),
            new UiSkinEntry("ART-11_p1_panic", "キャラ P1（焦り顔）", new Vector2(600f, 900f), "chr_p1_panic"),
            new UiSkinEntry("ART-11_p2_panic", "キャラ P2（焦り顔）", new Vector2(600f, 900f), "chr_p2_panic"),
            new UiSkinEntry("ART-15_on", "勝ち星（点灯）", new Vector2(64f, 64f), "star_on"),
            new UiSkinEntry("ART-15_off", "勝ち星（消灯）", new Vector2(64f, 64f), "star_off"),
            new UiSkinEntry("ART-16", "ペナルティ表示（−2秒）", new Vector2(300f, 120f), "penalty"),
            new UiSkinEntry("ART-17", "ラウンド開始バナー枠", new Vector2(1200f, 400f), "banner"),
            new UiSkinEntry("ART-18", "解除中の演出", new Vector2(1000f, 300f), "defuse_caption"),
            new UiSkinEntry("ART-20", "爆発の文字", new Vector2(800f, 300f), "boom_text"),
            new UiSkinEntry("UI-07", "リザルトの枠", new Vector2(1000f, 580f), "ui_result"),
            new UiSkinEntry("UI-12", "戻るボタン", new Vector2(168f, 74f), "ui_back"),
            new UiSkinEntry("UI-13", "ラウンド表示の枠", new Vector2(234f, 100f), "ui_round"),
            new UiSkinEntry("UI-14", "吹き出しの枠", new Vector2(383f, 134f), "ui_bubble"),
            new UiSkinEntry("UI-15", "下の帯（プレイヤー名と勝ち星）", new Vector2(532f, 66f), "ui_bar"),
            new UiSkinEntry("UI-16", "下の帯（ギミック名）", new Vector2(363f, 66f), "ui_bar_gimmick"),
            new UiSkinEntry("UI-18", "解除ボタン", new Vector2(260f, 70f), "ui_defuse")
        };

        for (int i = 0; i < WireNames.Length; i++)
        {
            foreach (string look in ButtonLookNames)
            {
                entries.Add(new UiSkinEntry($"ART-05_{WireNames[i]}_{look}", $"ひし形ボタン（{WireNames[i]}・{look}）", new Vector2(200f, 200f), $"bomb_switch_{WireNames[i]}_{look}"));
            }

            entries.Add(new UiSkinEntry($"ART-03_{WireNames[i]}", $"配線（{WireNames[i]}）", new Vector2(400f, 40f), $"wire_{WireNames[i]}"));
            entries.Add(new UiSkinEntry($"ART-03_cut_{WireNames[i]}", $"配線（{WireNames[i]}・切断）", new Vector2(400f, 40f), $"wire_cut_{WireNames[i]}"));
        }

        return entries;
    }

    /// <summary>
    /// 試合画面の中身を、奥に描くものから順に並べる（座標は 1920×1080 の左上から）
    /// </summary>
    /// <param name="root">画面の一番上</param>
    /// <param name="screen">1920×1080 の土台</param>
    private static void BuildGameContents(GameObject root, RectTransform screen)
    {
        Sprite roundSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Sprite circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        RectTransform background = CreateStretch("Background", screen);
        AddArt(background, "ART-12", Color.white, null, false, null);
        AddWorldSprite(background.gameObject, 0, true);
        BuildPcScreen(screen);
        BuildConveyor(screen);

        UiFuse fuseLeft = BuildFuse(screen, "FuseP1", false);
        UiFuse fuseRight = BuildFuse(screen, "FuseP2", true);
        UiPlayerPanel panel1 = BuildPlayerPanel(screen, 0, roundSprite, circleSprite);
        UiPlayerPanel panel2 = BuildPlayerPanel(screen, 1, roundSprite, circleSprite);
        UiBombView bomb = BuildBomb(screen, roundSprite, circleSprite, out Button defuseClick);

        UiGameView view = root.AddComponent<UiGameView>();
        BuildTopHud(screen, view);
        BuildBottomBars(screen, view);

        // 飛ぶ爆弾は、立ち絵が手に持っている爆弾と同じ絵・同じ大きさ
        RectTransform flyingBomb = CreateCentered("FlyingBomb", screen, 960f, 610f, HeldBombSize, HeldBombSize);
        AddArt(flyingBomb, "ART-01_held", GrayFill, circleSprite, true, null);
        AddWorldSprite(flyingBomb.gameObject, 30, false);
        flyingBomb.gameObject.SetActive(false);

        TextMeshProUGUI[] penaltyTexts =
        {
            BuildPenaltyText(screen, "PenaltyP1", 180f, 100f),
            BuildPenaltyText(screen, "PenaltyP2", 1440f, 100f)
        };

        BuildBanner(screen, roundSprite, out GameObject bannerRoot, out TextMeshProUGUI bannerTitle, out TextMeshProUGUI bannerGimmick, out TextMeshProUGUI bannerDescription);

        GameObject dimmer = CreateOverlay("Dimmer", screen, new Color(0f, 0f, 0f, 0.65f));
        dimmer.SetActive(false);

        GameObject caption1 = BuildDefuseCaption(screen, "DefuseCaptionP1", 150f, 205f, roundSprite);
        GameObject caption2 = BuildDefuseCaption(screen, "DefuseCaptionP2", 1360f, 205f, roundSprite);
        SetObject(panel1, "_defuseCaption", caption1);
        SetObject(panel2, "_defuseCaption", caption2);

        GameObject explosionFlash = CreateOverlay("ExplosionFlash", screen, new Color(1f, 1f, 1f, 0.7f));
        explosionFlash.SetActive(false);
        RectTransform explosionText = BuildExplosionText(screen);

        BuildResult(screen, roundSprite, out GameObject resultRoot, out TextMeshProUGUI resultTitle, out TextMeshProUGUI resultScore, out Button[] resultButtons, out GameObject[] resultFocusFrames);

        SetObject(view, "_bomb", bomb);
        SetObject(view, "_defuseButton", defuseClick);
        BuildQuickText(view, new[] { panel1, panel2 });
        SetObjectArray(view, "_panels", new Object[] { panel1, panel2 });
        SetObjectArray(view, "_fuses", new Object[] { fuseLeft, fuseRight });
        SetObject(view, "_flyingBomb", flyingBomb);
        SetObjectArray(view, "_penaltyTexts", penaltyTexts);
        SetObject(view, "_bannerRoot", bannerRoot);
        SetObject(view, "_bannerTitle", bannerTitle);
        SetObject(view, "_bannerGimmick", bannerGimmick);
        SetObject(view, "_bannerDescription", bannerDescription);
        SetObject(view, "_dimmer", dimmer);
        SetObject(view, "_explosionFlash", explosionFlash.GetComponent<Image>());
        SetObject(view, "_explosionText", explosionText);
        SetObject(view, "_resultRoot", resultRoot);
        SetObject(view, "_resultTitle", resultTitle);
        SetObject(view, "_resultScore", resultScore);
        SetObjectArray(view, "_resultButtons", resultButtons);
        SetObjectArray(view, "_resultFocusFrames", resultFocusFrames);

        SetInitialGameState(view, bomb);
    }

    /// <summary>
    /// プレハブを単体で開いたときにも見られるよう、最初の表示を入れておく
    /// </summary>
    /// <param name="view">試合画面</param>
    /// <param name="bomb">爆弾</param>
    private static void SetInitialGameState(UiGameView view, UiBombView bomb)
    {
        for (int i = 0; i < 4; i++)
        {
            bomb.SetWire((InputDirection)i, (UiWireColor)i, UiWireState.Normal);
        }

        bomb.SetQuestion("赤い★の線を切れ");
        bomb.SetLamp(true);
        bomb.SetInput(string.Empty);
        bomb.SetDefuseState(UiDefuseState.Locked);

        view.SetRound(1);
        view.SetPass(1, 1);
        view.SetPlayerNames("P1", "COM");
        view.SetGimmickName("配線切断式");
        view.SetWins(1, 0, 2);
        view.SetHoldTime(10f);
        view.SetHolder(0);
        view.SetTime(10f);
    }

    /// <summary>
    /// 爆弾の口金から持ち主の側へ伸びる導火線を作る（折れ線：上へ → 横へ → 下へ。先端に火花）
    /// </summary>
    /// <param name="screen">1920×1080 の土台</param>
    /// <param name="name">オブジェクト名</param>
    /// <param name="isRight">P2側（右）なら true</param>
    /// <returns>作った導火線</returns>
    private static UiFuse BuildFuse(RectTransform screen, string name, bool isRight)
    {
        const float Thickness = 22f;

        float bombX = isRight ? 961f : 959f;
        float endX = isRight ? 1828f : 92f;
        Vector2[] points =
        {
            new Vector2(bombX, 300f),
            new Vector2(bombX, 151f),
            new Vector2(endX, 151f),
            new Vector2(endX, 570f)
        };

        RectTransform root = CreateStretch(name, screen);
        RectTransform[] segments = new RectTransform[points.Length - 1];

        for (int i = 0; i < segments.Length; i++)
        {
            segments[i] = CreateRect($"Segment{i}", root, 0f, 0f, 10f, Thickness);
            segments[i].pivot = new Vector2(0f, 0.5f);
            AddArt(segments[i], "ART-02", GrayFill, null, false, null);
        }

        RectTransform spark = CreateCentered("Spark", root, endX, 570f, 130f, 130f);
        TextMeshProUGUI mark = AddText("Mark", spark, 0f, 0f, 130f, 130f, "★", 120f, UiFontRole.Body, TextAlignmentOptions.Center);
        mark.color = new Color(0.95f, 0.55f, 0.1f);
        AddArt(spark, "ART-08", Color.clear, null, true, mark.gameObject);

        UiFuse fuse = root.gameObject.AddComponent<UiFuse>();
        SetObjectArray(fuse, "_segments", segments);
        SetVector2Array(fuse, "_points", points);
        SetFloat(fuse, "_thickness", Thickness);
        SetObject(fuse, "_spark", spark);
        fuse.SetRatio(1f);
        return fuse;
    }

    /// <summary>
    /// 画面の左右どちらかのプレイヤー（キャラ・頭上の吹き出し・残り3秒以下の赤い帯）を作る
    /// </summary>
    /// <param name="screen">1920×1080 の土台</param>
    /// <param name="playerIndex">0：P1（左）、1：P2（右）</param>
    /// <param name="roundSprite">角丸の仮の形</param>
    /// <param name="circleSprite">丸い仮の形（手の爆弾に使う）</param>
    /// <returns>作ったパネル</returns>
    private static UiPlayerPanel BuildPlayerPanel(RectTransform screen, int playerIndex, Sprite roundSprite, Sprite circleSprite)
    {
        const float PanelWidth = 342f;
        const float PanelHeight = 548f;
        const float PanelY = 370f;
        const float EdgeWidth = 48f;

        bool isLeft = playerIndex == 0;
        string tag = isLeft ? "P1" : "P2";
        string artId = isLeft ? "ART-09" : "ART-10";
        string panicId = isLeft ? "ART-11_p1_panic" : "ART-11_p2_panic";
        float panelX = isLeft ? 179f : 1400f;

        RectTransform panel = CreateRect($"Player{tag}", screen, panelX, PanelY, PanelWidth, PanelHeight);

        float bandX = isLeft ? -panelX : ScreenWidth - EdgeWidth - panelX;
        RectTransform band = CreateRect("EdgeBand", panel, bandX, -PanelY, EdgeWidth, ScreenHeight);
        Image bandImage = band.gameObject.AddComponent<Image>();
        bandImage.color = DangerRed;
        bandImage.raycastTarget = false;
        band.gameObject.SetActive(false);

        // 立ち絵は、足元の中心を軸に大きくする。足元は少し下げて、頭が吹き出しにぶつからないようにする
        RectTransform character = CreateStretch("Character", panel);
        character.pivot = new Vector2(0.5f, 0f);
        character.anchoredPosition = new Vector2(0f, -CharacterDropY);
        character.localScale = new Vector3(CharacterScale, CharacterScale, 1f);
        GameObject normal = CreateCharacterVisual(character, "Normal", $"{artId}_normal", $"{artId}\n{tag}");
        GameObject panic = CreateCharacterVisual(character, "Panic", panicId, $"{artId}\n{tag}\n焦り顔");
        GameObject afro = CreateCharacterVisual(character, "Afro", $"{artId}_afro", $"{artId}\n{tag}\nアフロ");
        panic.SetActive(false);
        afro.SetActive(false);

        // 手に持つ爆弾。置き場所は、真ん中（相手側）の手の仮の位置（立ち絵を大きくする前の位置を、足元の中心から見て同じ倍率で動かした）。立ち絵の手に合わせて、Unity で動かす
        float heldBombX = PanelWidth * 0.5f + (isLeft ? 137f : -123f) * CharacterScale;
        float heldBombY = PanelHeight + (296f - PanelHeight) * CharacterScale + CharacterDropY;
        RectTransform heldBomb = CreateCentered("HeldBomb", panel, heldBombX, heldBombY, HeldBombSize, HeldBombSize);
        AddArt(heldBomb, "ART-01_held", GrayFill, circleSprite, true, null);
        AddWorldSprite(heldBomb.gameObject, 12, false);
        heldBomb.gameObject.SetActive(false);

        float bubbleCenterX = isLeft ? 170.5f : 171.5f;
        RectTransform bubble = CreateCentered("Bubble", panel, bubbleCenterX, -91f - BubbleRiseY, 383f, 134f);
        AddArt(bubble, "UI-14", GrayFill, null, false, null);
        TextMeshProUGUI bubbleText = AddText("Text", bubble, 0f, 0f, 383f, 134f, "あと10秒！", 64f, UiFontRole.Body, TextAlignmentOptions.Center);

        UiPlayerPanel playerPanel = panel.gameObject.AddComponent<UiPlayerPanel>();
        SetObject(playerPanel, "_normalVisual", normal);
        SetObject(playerPanel, "_panicVisual", panic);
        SetObject(playerPanel, "_afroVisual", afro);
        SetObject(playerPanel, "_bubble", bubble);
        SetObject(playerPanel, "_bubbleText", bubbleText);
        SetObject(playerPanel, "_edgeBand", bandImage);
        SetObject(playerPanel, "_heldBomb", heldBomb);
        return playerPanel;
    }

    /// <summary>
    /// キャラの見た目1つ（通常・焦り顔・アフロのどれか）を作る
    /// </summary>
    /// <param name="parent">置く場所</param>
    /// <param name="name">オブジェクト名</param>
    /// <param name="artId">素材ID</param>
    /// <param name="placeholderLabel">素材がないときに出す文字</param>
    /// <returns>作った見た目</returns>
    private static GameObject CreateCharacterVisual(RectTransform parent, string name, string artId, string placeholderLabel)
    {
        RectTransform visual = CreateStretch(name, parent);
        TextMeshProUGUI label = AddText("PlaceholderLabel", visual, 0f, 200f, 342f, 200f, placeholderLabel, 40f, UiFontRole.Body, TextAlignmentOptions.Center);
        label.textWrappingMode = TextWrappingModes.Normal;
        AddSpriteArt(visual, artId, GrayFill, null, true, label.gameObject, CharacterSortingOrder);
        return visual.gameObject;
    }

    /// <summary>
    /// 画面中央の爆弾（本体・4つのひし形ボタンと配線・ランプ・タイマー液晶・口の形の液晶）を作る
    /// </summary>
    /// <param name="screen">1920×1080 の土台</param>
    /// <param name="roundSprite">角丸の仮の形</param>
    /// <param name="circleSprite">丸の仮の形</param>
    /// <param name="defuseClick">解除ボタンをマウスで押すためのボタン</param>
    /// <returns>作った爆弾</returns>
    private static UiBombView BuildBomb(RectTransform screen, Sprite roundSprite, Sprite circleSprite, out Button defuseClick)
    {
        RectTransform bomb = CreateRect("Bomb", screen, 640f, 290f, 640f, 640f);

        // 本体の絵（口金・X字の傷・左右のコネクタ・液晶の枠まで含む）。以下の座標は、800×800 の絵で測った位置に BombArtScale をかけたもの
        RectTransform body = CreateStretch("Body", bomb);
        AddSpriteArt(body, "ART-01", GrayFill, circleSprite, true, null, BombBodySortingOrder);

        // ひし形ボタンは、X字の4つの頂点に置く。上と下の位置は入れ替えてある（InputDirection の「上」が画面の下の頂点、「下」が画面の上の頂点）。
        // つまみは画面の上・左のボタンが左向き、右・下のボタンが右向き（絵のつまみは右向きなので、上と左は反転して使う）
        UiWire[] wires =
        {
            CreateWire(bomb, "WireUp", ArtPoint(400f, 385f), 394f, 182f, false),
            CreateWire(bomb, "WireDown", ArtPoint(400f, 145f), 80f, 166f, true),
            CreateWire(bomb, "WireLeft", ArtPoint(255f, 263f), 80f, 50f, true),
            CreateWire(bomb, "WireRight", ArtPoint(545f, 263f), 510f, 66f, false)
        };

        // ランプは X字の中心。消灯の絵が届くまでは、点灯の絵を暗くして使う
        Vector2 lampCenter = ArtPoint(400f, 263f);
        float lampSize = 60f * BombArtScale;
        RectTransform lampOn = CreateCentered("LampOn", bomb, lampCenter.x, lampCenter.y, lampSize, lampSize);
        AddArt(lampOn, "ART-04_on", new Color(0.95f, 0.2f, 0.2f), circleSprite, true, null);
        RectTransform lampOff = CreateCentered("LampOff", bomb, lampCenter.x, lampCenter.y, lampSize, lampSize);
        AddArt(lampOff, "ART-04_off", new Color(0.35f, 0.3f, 0.3f), circleSprite, true, null);
        SetArtFallback(lampOff, "ART-04_on", new Color(0.3f, 0.3f, 0.3f));

        // 液晶の枠は本体の絵に描いてあるので、ここは文字の置き場だけ（仮の四角は出さない）
        RectTransform timer = CreateRect("TimerLcd", bomb, 240f, 384f, 160f, 48f);
        TextMeshProUGUI time = AddText("Time", timer, 0f, 0f, 160f, 48f, "10:00", 34f, UiFontRole.Number, TextAlignmentOptions.Center);
        time.color = LcdTextColor;

        RectTransform mouth = CreateRect("MouthLcd", bomb, 130f, 440f, 380f, 120f);

        TextMeshProUGUI question = AddText("Question", mouth, 24f, 6f, 332f, 62f, "問題文がここに出る", 28f, UiFontRole.Body, TextAlignmentOptions.Center);
        question.color = LcdTextColor;
        question.textWrappingMode = TextWrappingModes.Normal;
        question.enableAutoSizing = true;
        question.fontSizeMin = 16f;
        question.fontSizeMax = 28f;

        TextMeshProUGUI defuseStatus = AddText("DefuseStatus", mouth, 50f, 74f, 170f, 30f, "解除：パス5回目から", 17f, UiFontRole.Body, TextAlignmentOptions.Left);
        defuseStatus.color = LcdTextColor;

        RectTransform defuseButton = CreateRect("DefuseButton", mouth, 232f, 74f, 110f, 30f);
        AddArt(defuseButton, "UI-18", Orange, roundSprite, false, null);
        AddText("Label", defuseButton, 0f, 0f, 110f, 30f, "解除", 20f, UiFontRole.Heading, TextAlignmentOptions.Center);
        defuseClick = AddClickArea(defuseButton);
        defuseButton.gameObject.SetActive(false);

        TextMeshProUGUI input = AddText("Input", bomb, 200f, 582f, 240f, 36f, string.Empty, 28f, UiFontRole.Body, TextAlignmentOptions.Center);
        input.gameObject.SetActive(false);

        UiBombView view = bomb.gameObject.AddComponent<UiBombView>();
        SetObjectArray(view, "_wires", wires);
        SetObject(view, "_timeText", time);
        SetObject(view, "_lampOn", lampOn.gameObject);
        SetObject(view, "_lampOff", lampOff.gameObject);
        SetBool(view, "_isLampOffHidden", IsLampOffHiddenForNow);
        SetObject(view, "_questionText", question);
        SetObject(view, "_defuseStatusText", defuseStatus);
        SetObject(view, "_defuseButton", defuseButton.gameObject);
        SetObject(view, "_inputText", input);
        SetObject(view, "_shakeTarget", bomb);
        return view;
    }

    /// <summary>
    /// 本体の絵（800×800）の上の位置を、爆弾の四角（640×640）の座標にする
    /// </summary>
    /// <param name="x">絵の左からの位置</param>
    /// <param name="y">絵の上からの位置</param>
    /// <returns>爆弾の四角の左上からの位置</returns>
    private static Vector2 ArtPoint(float x, float y)
    {
        return new Vector2(x * BombArtScale, y * BombArtScale);
    }

    /// <summary>
    /// 素材がないときに、別の素材IDの絵を色を変えて代わりに使う指定を付ける（UiArtSlot の後に呼ぶ）
    /// </summary>
    /// <param name="target">UiArtSlot が付いているオブジェクト</param>
    /// <param name="fallbackArtId">代わりに使う素材ID</param>
    /// <param name="fallbackColor">代わりの絵にかける色</param>
    private static void SetArtFallback(RectTransform target, string fallbackArtId, Color fallbackColor)
    {
        SerializedObject serialized = new SerializedObject(target.GetComponent<UiArtSlot>());
        serialized.FindProperty("_fallbackArtId").stringValue = fallbackArtId;
        serialized.FindProperty("_fallbackColor").colorValue = fallbackColor;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// ひし形ボタンの絵を置く四角を作る。ひし形の中心を軸にして、つまみの向きに合わせて左右反転できる
    /// </summary>
    /// <param name="name">オブジェクト名</param>
    /// <param name="parent">置く場所（ひし形の中心が親の中心）</param>
    /// <param name="isMirrored">左右反転するか</param>
    /// <param name="isArtTabLeft">反転する前の絵のつまみが左にあるか</param>
    /// <returns>作った四角</returns>
    private static RectTransform CreateDiamondArea(string name, Transform parent, bool isMirrored, bool isArtTabLeft)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)gameObject.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        // つまみが左にある絵は、ひし形の中心も左右が逆（右寄り）になる
        float centerX = isArtTabLeft ? 1f - ButtonDiamondCenter.x : ButtonDiamondCenter.x;
        rect.pivot = new Vector2(centerX, 1f - ButtonDiamondCenter.y);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(ButtonBoxSize, ButtonBoxSize);
        rect.localScale = new Vector3(isMirrored ? -1f : 1f, 1f, 1f);
        return rect;
    }

    /// <summary>
    /// ひし形ボタン1つと、そこから横へ伸びる配線（赤・青・黄・緑それぞれの、つながった状態と切れた状態）を作る
    /// </summary>
    /// <param name="bomb">爆弾</param>
    /// <param name="name">オブジェクト名</param>
    /// <param name="buttonCenter">ボタンの中心（爆弾の左上から）</param>
    /// <param name="wireX">配線の左端</param>
    /// <param name="wireWidth">配線の長さ</param>
    /// <param name="isTabLeft">ボタンのつまみを左に向けるか（配線が左へ伸びるなら true）。絵のつまみの向きは色ごとに違うので、合わなければ反転する</param>
    /// <returns>作った配線</returns>
    private static UiWire CreateWire(RectTransform bomb, string name, Vector2 buttonCenter, float wireX, float wireWidth, bool isTabLeft)
    {
        const float WireThickness = 16f;
        const float ButtonBox = ButtonBoxSize;
        const float DiamondSide = 78f;

        RectTransform wireRoot = CreateStretch(name, bomb);
        float wireY = buttonCenter.y - WireThickness * 0.5f;

        GameObject[] normalVisuals = new GameObject[WireNames.Length];
        GameObject[] cutVisuals = new GameObject[WireNames.Length];
        GameObject[] buttonVisuals = new GameObject[WireNames.Length];
        GameObject[] lightVisuals = new GameObject[WireNames.Length];
        GameObject[] blackoutVisuals = new GameObject[WireNames.Length];

        for (int i = 0; i < WireNames.Length; i++)
        {
            RectTransform normal = CreateRect($"Wire_{WireNames[i]}", wireRoot, wireX, wireY, wireWidth, WireThickness);
            AddArt(normal, $"ART-03_{WireNames[i]}", WireColors[i], null, false, null);
            normalVisuals[i] = normal.gameObject;

            RectTransform cut = CreateRect($"WireCut_{WireNames[i]}", wireRoot, wireX, wireY, wireWidth, WireThickness);
            RectTransform pieces = CreateStretch("Pieces", cut);
            CreateWirePiece(pieces, "Left", 0f, 0.42f, WireColors[i]);
            CreateWirePiece(pieces, "Right", 0.58f, 1f, WireColors[i]);
            AddArt(cut, $"ART-03_cut_{WireNames[i]}", Color.clear, null, false, pieces.gameObject);
            cutVisuals[i] = cut.gameObject;
        }

        RectTransform buttonRoot = CreateCentered("Button", wireRoot, buttonCenter.x, buttonCenter.y, ButtonBox, ButtonBox);
        CanvasGroup buttonGroup = buttonRoot.gameObject.AddComponent<CanvasGroup>();

        for (int i = 0; i < WireNames.Length; i++)
        {
            // 絵の中のひし形の中心が、ボタンの中心（X字の頂点）に来るように置く
            bool isArtTabLeft = ButtonTabIsLeft[i];
            RectTransform visual = CreateDiamondArea($"Diamond_{WireNames[i]}", buttonRoot, isTabLeft != isArtTabLeft, isArtTabLeft);
            float diamondCenterX = isArtTabLeft ? 1f - ButtonDiamondCenter.x : ButtonDiamondCenter.x;
            RectTransform quad = CreateCentered("Quad", visual, ButtonBox * diamondCenterX, ButtonBox * ButtonDiamondCenter.y, DiamondSide, DiamondSide);
            quad.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Image quadImage = quad.gameObject.AddComponent<Image>();
            quadImage.color = WireColors[i];
            quadImage.raycastTarget = false;
            Outline frame = quad.gameObject.AddComponent<Outline>();
            frame.effectColor = ButtonFrameColor;
            frame.effectDistance = new Vector2(5f, -5f);
            AddArt(visual, $"ART-05_{WireNames[i]}_normal", Color.clear, null, true, quad.gameObject);
            buttonVisuals[i] = visual.gameObject;

            // マウスが重なったとき（light）と押している間（blackout）の絵。両方そろっているときだけ、通常の絵の上に出る
            lightVisuals[i] = CreateButtonLookVisual(visual, "Light", $"ART-05_{WireNames[i]}_light");
            blackoutVisuals[i] = CreateButtonLookVisual(visual, "Blackout", $"ART-05_{WireNames[i]}_blackout");
        }

        // 絵が3状態そろっていないときに、明るく／暗くするために、ひし形の上へ重ねる画像（素材があるときは素材の形、ないときは仮の四角の形）。文字より下に置く
        RectTransform artTint = CreateDiamondArea("Tint_Art", buttonRoot, false, false);
        Image artTintImage = artTint.gameObject.AddComponent<Image>();
        artTintImage.color = Color.clear;
        artTintImage.raycastTarget = false;
        artTintImage.enabled = false;

        RectTransform placeholderTint = CreateCentered("Tint_Placeholder", buttonRoot, ButtonBox * 0.5f, ButtonBox * 0.5f, DiamondSide, DiamondSide);
        placeholderTint.localRotation = Quaternion.Euler(0f, 0f, 45f);
        Image placeholderTintImage = placeholderTint.gameObject.AddComponent<Image>();
        placeholderTintImage.color = Color.clear;
        placeholderTintImage.raycastTarget = false;

        TextMeshProUGUI label = AddText("Label", buttonRoot, 22f, 0f, ButtonBox - 44f, ButtonBox, "赤", 44f, UiFontRole.Heading, TextAlignmentOptions.Center);
        label.enableAutoSizing = true;
        label.fontSizeMin = 16f;
        label.fontSizeMax = 44f;
        TextMeshProUGUI symbol = AddText("Symbol", buttonRoot, 90f, 8f, 48f, 40f, string.Empty, 28f, UiFontRole.Body, TextAlignmentOptions.Center);
        symbol.gameObject.SetActive(false);

        UiWire wire = wireRoot.gameObject.AddComponent<UiWire>();
        SetObjectArray(wire, "_normalVisuals", normalVisuals);
        SetObjectArray(wire, "_cutVisuals", cutVisuals);
        SetObjectArray(wire, "_buttonVisuals", buttonVisuals);
        SetObjectArray(wire, "_lightVisuals", lightVisuals);
        SetObjectArray(wire, "_blackoutVisuals", blackoutVisuals);
        SetBool(wire, "_isWireHidden", IsWireHiddenForNow);
        SetObject(wire, "_buttonGroup", buttonGroup);
        SetObject(wire, "_clickButton", AddClickArea(buttonRoot));
        buttonRoot.gameObject.AddComponent<UiWirePointerRelay>();
        SetObject(wire, "_artTint", artTintImage);
        SetObject(wire, "_placeholderTint", placeholderTintImage);
        SetObject(wire, "_label", label);
        SetObject(wire, "_symbolLabel", symbol);
        return wire;
    }

    /// <summary>
    /// ひし形ボタンの絵の上に重ねる、light／blackout の絵の差し込み口を作る（最初は隠す）
    /// </summary>
    /// <param name="diamond">通常の絵の四角（この中に重ねる）</param>
    /// <param name="name">オブジェクト名</param>
    /// <param name="artId">素材ID</param>
    /// <returns>作ったオブジェクト</returns>
    private static GameObject CreateButtonLookVisual(RectTransform diamond, string name, string artId)
    {
        RectTransform look = CreateStretch(name, diamond);
        AddArt(look, artId, Color.clear, null, true, null);
        look.gameObject.SetActive(false);
        return look.gameObject;
    }

    /// <summary>
    /// 切れた配線の、片側の欠片を作る
    /// </summary>
    /// <param name="parent">置く場所</param>
    /// <param name="name">オブジェクト名</param>
    /// <param name="minX">左端（0〜1）</param>
    /// <param name="maxX">右端（0〜1）</param>
    /// <param name="color">色</param>
    private static void CreateWirePiece(RectTransform parent, string name, float minX, float maxX, Color color)
    {
        RectTransform piece = CreateStretch(name, parent);
        piece.anchorMin = new Vector2(minX, 0f);
        piece.anchorMax = new Vector2(maxX, 1f);
        piece.offsetMin = Vector2.zero;
        piece.offsetMax = Vector2.zero;

        Image image = piece.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    /// <summary>
    /// 上の表示（戻る・ラウンド・パス回数と難易度）を作る
    /// </summary>
    /// <param name="screen">1920×1080 の土台</param>
    /// <param name="view">試合画面（参照を入れる先）</param>
    private static void BuildTopHud(RectTransform screen, UiGameView view)
    {
        RectTransform back = CreateRect("BackButton", screen, 40f, 32f, 168f, 74f);
        AddArt(back, "UI-12", GrayFill, null, false, null);
        AddText("Label", back, 0f, 0f, 168f, 74f, "戻る", 52f, UiFontRole.Heading, TextAlignmentOptions.Center);
        SetObject(view, "_backButton", AddClickArea(back));
        TextMeshProUGUI hint = AddText("BackHint", screen, 232f, 46f, 320f, 36f, "Esc でタイトルへ", 22f, UiFontRole.Body, TextAlignmentOptions.Left);
        hint.color = SubTextColor;

        RectTransform round = CreateRect("Round", screen, 843f, 22f, 234f, 100f);
        AddArt(round, "UI-13", GrayFill, null, false, null);
        TextMeshProUGUI roundText = AddText("Text", round, 0f, 0f, 234f, 100f, "R1", 76f, UiFontRole.Heading, TextAlignmentOptions.Center);

        TextMeshProUGUI pass = AddText("Pass", screen, 1590f, 44f, 180f, 40f, "パス回数 1", 24f, UiFontRole.Body, TextAlignmentOptions.Left);
        pass.color = SubTextColor;
        TextMeshProUGUI stage = AddText("Stage", screen, 1780f, 44f, 130f, 40f, "難易度 1", 24f, UiFontRole.Body, TextAlignmentOptions.Left);
        stage.color = SubTextColor;

        SetObject(view, "_roundText", roundText);
        SetObject(view, "_passText", pass);
        SetObject(view, "_stageText", stage);
    }

    /// <summary>
    /// 下の3つの帯（P1の名前と勝ち星、ギミック名、勝ち星とP2またはCOMの名前）を作る
    /// </summary>
    /// <param name="screen">1920×1080 の土台</param>
    /// <param name="view">試合画面（参照を入れる先）</param>
    private static void BuildBottomBars(RectTransform screen, UiGameView view)
    {
        RectTransform barLeft = CreateRect("BarP1", screen, 125f, 963f, 532f, 66f);
        AddArt(barLeft, "UI-15", GrayFill, null, false, null);
        TextMeshProUGUI nameLeft = AddText("Name", barLeft, 91f, 0f, 200f, 66f, "P1", 40f, UiFontRole.Body, TextAlignmentOptions.Center);
        UiWinStars starsLeft = BuildWinStars(barLeft, "WinStars", 343f, 13f);

        RectTransform center = CreateRect("BarGimmick", screen, 778f, 963f, 363f, 66f);
        AddArt(center, "UI-16", GrayFill, null, false, null);
        TextMeshProUGUI gimmick = AddText("Text", center, 0f, 0f, 363f, 66f, "ギミック名", 40f, UiFontRole.Body, TextAlignmentOptions.Center);

        RectTransform barRight = CreateRect("BarP2", screen, 1262f, 963f, 532f, 66f);
        AddArt(barRight, "UI-15", GrayFill, null, false, null);
        TextMeshProUGUI nameRight = AddText("Name", barRight, 208f, 0f, 200f, 66f, "COM", 40f, UiFontRole.Body, TextAlignmentOptions.Center);
        UiWinStars starsRight = BuildWinStars(barRight, "WinStars", 58f, 13f);

        SetObjectArray(view, "_nameTexts", new Object[] { nameLeft, nameRight });
        SetObjectArray(view, "_winStars", new Object[] { starsLeft, starsRight });
        SetObject(view, "_gimmickText", gimmick);
    }

    /// <summary>
    /// 勝ち星を並べる（星は最大3つ）
    /// </summary>
    /// <param name="bar">置く帯</param>
    /// <param name="name">オブジェクト名</param>
    /// <param name="x">左からの位置</param>
    /// <param name="y">上からの位置</param>
    /// <returns>作った勝ち星</returns>
    private static UiWinStars BuildWinStars(RectTransform bar, string name, float x, float y)
    {
        const int StarCount = 3;
        const float StarSize = 40f;
        const float StarGap = 6f;

        RectTransform group = CreateRect(name, bar, x, y, StarCount * StarSize + (StarCount - 1) * StarGap, StarSize);
        GameObject[] stars = new GameObject[StarCount];
        GameObject[] lits = new GameObject[StarCount];

        for (int i = 0; i < StarCount; i++)
        {
            RectTransform star = CreateRect($"Star{i}", group, i * (StarSize + StarGap), 0f, StarSize, StarSize);
            CreateStarVisual(star, "Off", "ART-15_off", StarOffColor, StarSize);
            lits[i] = CreateStarVisual(star, "Lit", "ART-15_on", StarLitColor, StarSize);
            stars[i] = star.gameObject;
        }

        UiWinStars winStars = group.gameObject.AddComponent<UiWinStars>();
        SetObjectArray(winStars, "_stars", stars);
        SetObjectArray(winStars, "_litVisuals", lits);
        return winStars;
    }

    /// <summary>
    /// 星の見た目1つ（点灯または消灯）を作る。素材がないときは「★」の文字を出す
    /// </summary>
    /// <param name="parent">置く場所</param>
    /// <param name="name">オブジェクト名</param>
    /// <param name="artId">素材ID</param>
    /// <param name="color">素材がないときの色</param>
    /// <param name="size">星の大きさ</param>
    /// <returns>作った見た目</returns>
    private static GameObject CreateStarVisual(RectTransform parent, string name, string artId, Color color, float size)
    {
        RectTransform visual = CreateStretch(name, parent);
        TextMeshProUGUI mark = AddText("Mark", visual, 0f, 0f, size, size, "★", size * 0.95f, UiFontRole.Body, TextAlignmentOptions.Center);
        mark.color = color;
        AddArt(visual, artId, Color.clear, null, true, mark.gameObject);
        return visual.gameObject;
    }

    /// <summary>
    /// 不正解のときに出す「−2秒」を作る
    /// </summary>
    /// <param name="screen">1920×1080 の土台</param>
    /// <param name="name">オブジェクト名</param>
    /// <param name="x">左からの位置</param>
    /// <param name="y">上からの位置</param>
    /// <returns>作った文字</returns>
    private static TextMeshProUGUI BuildPenaltyText(RectTransform screen, string name, float x, float y)
    {
        TextMeshProUGUI text = AddText(name, screen, x, y, 300f, 100f, "−2秒", 88f, UiFontRole.Heading, TextAlignmentOptions.Center);
        text.color = DangerRed;
        text.gameObject.SetActive(false);
        return text;
    }

    /// <summary>
    /// ラウンド開始のバナーを作る
    /// </summary>
    /// <param name="screen">1920×1080 の土台</param>
    /// <param name="roundSprite">角丸の仮の形</param>
    /// <param name="root">バナー全体</param>
    /// <param name="title">見出し</param>
    /// <param name="gimmick">ギミック名</param>
    /// <param name="description">一行説明</param>
    private static void BuildBanner(RectTransform screen, Sprite roundSprite, out GameObject root, out TextMeshProUGUI title, out TextMeshProUGUI gimmick, out TextMeshProUGUI description)
    {
        RectTransform banner = CreateRect("Banner", screen, 360f, 340f, 1200f, 400f);
        AddArt(banner, "ART-17", LightGray, roundSprite, false, null);
        AddOutline(banner);

        title = AddText("Title", banner, 0f, 40f, 1200f, 90f, "ROUND 1", 72f, UiFontRole.Heading, TextAlignmentOptions.Center);
        gimmick = AddText("Gimmick", banner, 0f, 140f, 1200f, 140f, "配線切断式", 120f, UiFontRole.Heading, TextAlignmentOptions.Center);
        description = AddText("Description", banner, 0f, 290f, 1200f, 70f, "正しい線を1本切れ！", 44f, UiFontRole.Body, TextAlignmentOptions.Center);
        banner.pivot = new Vector2(0.5f, 0.5f);
        banner.anchoredPosition += new Vector2(600f, -200f);
        banner.gameObject.SetActive(false);
        root = banner.gameObject;
    }

    /// <summary>
    /// 画面いっぱいの色の幕を作る
    /// </summary>
    /// <param name="name">オブジェクト名</param>
    /// <param name="screen">1920×1080 の土台</param>
    /// <param name="color">幕の色</param>
    /// <returns>作った幕</returns>
    private static GameObject CreateOverlay(string name, RectTransform screen, Color color)
    {
        RectTransform overlay = CreateStretch(name, screen);
        Image image = overlay.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return overlay.gameObject;
    }

    /// <summary>
    /// 「解除中！」の表示を作る（解除に挑戦している相手の側の、吹き出しの位置に重ねて出す）
    /// </summary>
    /// <param name="screen">1920×1080 の土台</param>
    /// <param name="name">オブジェクト名</param>
    /// <param name="x">左からの位置</param>
    /// <param name="y">上からの位置</param>
    /// <param name="roundSprite">角丸の仮の形</param>
    /// <returns>作った表示</returns>
    private static GameObject BuildDefuseCaption(RectTransform screen, string name, float x, float y, Sprite roundSprite)
    {
        RectTransform caption = CreateRect(name, screen, x, y, 420f, 150f);
        AddArt(caption, "ART-18", Orange, roundSprite, true, null);
        AddText("Text", caption, 0f, 0f, 420f, 150f, "解除中！", 84f, UiFontRole.Heading, TextAlignmentOptions.Center);
        caption.gameObject.SetActive(false);
        return caption.gameObject;
    }

    /// <summary>
    /// 爆発の文字「ドカーン！」を作る
    /// </summary>
    /// <param name="screen">1920×1080 の土台</param>
    /// <returns>作った表示</returns>
    private static RectTransform BuildExplosionText(RectTransform screen)
    {
        RectTransform root = CreateCentered("ExplosionText", screen, 960f, 540f, 800f, 300f);
        TextMeshProUGUI text = AddText("Text", root, 0f, 0f, 800f, 300f, "ドカーン！", 180f, UiFontRole.Heading, TextAlignmentOptions.Center);
        text.color = new Color(0.95f, 0.35f, 0f);
        AddArt(root, "ART-20", Color.clear, null, true, text.gameObject);
        root.gameObject.SetActive(false);
        return root;
    }

    /// <summary>
    /// 立ち絵（通常・焦り顔・アフロ）と爆弾の本体を、UI の Image から 2D オブジェクト（SpriteRenderer の GameObject）に変える。
    /// すでにあるプレハブをその場で書き換えるので、他の部品の参照は壊れない
    /// </summary>
    [MenuItem("Tools/UI仮組/立ち絵と爆弾の本体をGameObjectにする")]
    public static void ConvertCharactersAndBombBodyToSprites()
    {
        if (!File.Exists(GamePrefabPath))
        {
            Debug.LogWarning("UI_Game.prefab がありません。先に「試合画面を作る」を実行してください");
            return;
        }

        GameObject contents = PrefabUtility.LoadPrefabContents(GamePrefabPath);

        try
        {
            Transform screen = contents.transform.Find("Screen");
            int convertedCount = 0;

            foreach (string tag in new[] { "P1", "P2" })
            {
                foreach (string pose in CharacterPoseNames)
                {
                    convertedCount += ConvertToSpriteArt(screen.Find($"Player{tag}/Character/{pose}"), CharacterSortingOrder) ? 1 : 0;
                }
            }

            convertedCount += ConvertToSpriteArt(screen.Find("Bomb/Body"), BombBodySortingOrder) ? 1 : 0;

            if (convertedCount == 0)
            {
                Debug.Log("変換するものはありませんでした（すでに GameObject になっているか、見つかりませんでした）");
                return;
            }

            contents.GetComponent<UiSkinApplier>().ApplyAll();
            PrefabUtility.SaveAsPrefabAsset(contents, GamePrefabPath);
            Debug.Log($"{convertedCount} 個を SpriteRenderer の GameObject にしました：{GamePrefabPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    /// <summary>
    /// 立ち絵（通常）を、新しいラフ絵（P1：chr_1p_rough_v1、P2：bomb_p2_rough_v1）に差し替えて、プレハブとタイトルに反映する。
    /// 「素材を割り当てて反映」は空いている登録にしか入れないので、すでに入っている絵はこちらで差し替える
    /// </summary>
    [MenuItem("Tools/UI仮組/立ち絵を新しいラフ絵に差し替える")]
    public static void ReplaceCharactersWithNewRough()
    {
        UiSkin skin = LoadOrCreateSkin();
        int replacedCount = 0;
        replacedCount += ReplaceSkinSprite(skin, "ART-09_normal", "chr_1p_rough_v1") ? 1 : 0;
        replacedCount += ReplaceSkinSprite(skin, "ART-10_normal", "bomb_p2_rough_v1") ? 1 : 0;

        EditorUtility.SetDirty(skin);
        AssetDatabase.SaveAssets();
        ApplySkinToPrefabs();
        Debug.Log($"立ち絵を {replacedCount} 件差し替えて、プレハブに反映しました");
    }

    /// <summary>
    /// UiSkin の登録に入っている絵を、Art/Characters の別の画像に差し替える（探すファイル名も合わせる）
    /// </summary>
    /// <param name="skin">素材の表</param>
    /// <param name="artId">差し替える素材ID</param>
    /// <param name="fileName">新しい画像のファイル名（拡張子なし。Art/Characters の中）</param>
    /// <returns>差し替えたら true</returns>
    private static bool ReplaceSkinSprite(UiSkin skin, string artId, string fileName)
    {
        string path = $"{ArtFolder}/Characters/{fileName}.png";
        EnsureSpriteImport(path);

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        UiSkinEntry entry = skin.FindEntry(artId);

        if (sprite == null || entry == null)
        {
            Debug.LogWarning($"差し替えできませんでした：{artId} ← {path}");
            return false;
        }

        entry.Sprite = sprite;
        skin.UpdateFileName(artId, fileName);
        return true;
    }

    /// <summary>
    /// UI の Image と素材の差し込み口を外し、同じ指定の 2D オブジェクト（子の Art）に置き換える
    /// </summary>
    /// <param name="frame">置き換える四角（なければ何もしない）</param>
    /// <param name="sortingOrder">描画順（大きいほど手前）</param>
    /// <returns>置き換えたら true</returns>
    private static bool ConvertToSpriteArt(Transform frame, int sortingOrder)
    {
        if (frame == null || frame.Find(SpriteArtName) != null)
        {
            return false;
        }

        UiArtSlot oldSlot = frame.GetComponent<UiArtSlot>();

        if (oldSlot == null)
        {
            return false;
        }

        SerializedObject old = new SerializedObject(oldSlot);
        string artId = old.FindProperty("_artId").stringValue;
        Color placeholderColor = old.FindProperty("_placeholderColor").colorValue;
        Sprite placeholderSprite = old.FindProperty("_placeholderSprite").objectReferenceValue as Sprite;
        bool isPreserveAspect = old.FindProperty("_isPreserveAspect").boolValue;
        GameObject placeholderObject = old.FindProperty("_placeholderObject").objectReferenceValue as GameObject;

        // 付け外しの順番は、依存の逆（UiWorldSprite → UiArtSlot → Image → CanvasRenderer）
        DestroyComponentIfExists<UiWorldSprite>(frame);
        Object.DestroyImmediate(oldSlot);
        DestroyComponentIfExists<UnityEngine.UI.Image>(frame);
        DestroyComponentIfExists<CanvasRenderer>(frame);

        AddSpriteArt((RectTransform)frame, artId, placeholderColor, placeholderSprite, isPreserveAspect, placeholderObject, sortingOrder);
        return true;
    }

    /// <summary>
    /// 付いていれば、コンポーネントを外す
    /// </summary>
    /// <typeparam name="T">外すコンポーネントの型</typeparam>
    /// <param name="target">外す先</param>
    private static void DestroyComponentIfExists<T>(Component target) where T : Component
    {
        T component = target.GetComponent<T>();

        if (component != null)
        {
            Object.DestroyImmediate(component);
        }
    }

    /// <summary>
    /// 四角の子に、素材を出す 2D オブジェクト（SpriteRenderer の GameObject）を作る。大きさと位置は四角に合わせる
    /// </summary>
    /// <param name="frame">この四角いっぱいに収まるよう出す</param>
    /// <param name="artId">素材ID</param>
    /// <param name="placeholderColor">素材がないときの色</param>
    /// <param name="placeholderSprite">素材がないときの形（null なら四角）</param>
    /// <param name="isPreserveAspect">素材の縦横比を保つか</param>
    /// <param name="placeholderObject">素材がないときだけ出すもの</param>
    /// <param name="sortingOrder">描画順（大きいほど手前）</param>
    private static void AddSpriteArt(RectTransform frame, string artId, Color placeholderColor, Sprite placeholderSprite, bool isPreserveAspect, GameObject placeholderObject, int sortingOrder)
    {
        GameObject art = new GameObject(SpriteArtName);
        art.transform.SetParent(frame, false);
        art.transform.SetAsFirstSibling();

        UiSpriteArtSlot slot = art.AddComponent<UiSpriteArtSlot>();
        SerializedObject serialized = new SerializedObject(slot);
        serialized.FindProperty("_artId").stringValue = artId;
        serialized.FindProperty("_placeholderColor").colorValue = placeholderColor;
        serialized.FindProperty("_placeholderSprite").objectReferenceValue = placeholderSprite;
        serialized.FindProperty("_isPreserveAspect").boolValue = isPreserveAspect;
        serialized.FindProperty("_placeholderObject").objectReferenceValue = placeholderObject;
        serialized.FindProperty("_sortingOrder").intValue = sortingOrder;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// 素材があるときに 2D オブジェクト（SpriteRenderer）としても出せるようにする（UiArtSlot の後に呼ぶ）
    /// </summary>
    /// <param name="target">UiArtSlot が付いているオブジェクト</param>
    /// <param name="sortingOrder">2D オブジェクトの描画順（大きいほど手前）</param>
    /// <param name="isSolidWhenEmpty">素材がないときも色の四角を出すか（背景用）</param>
    private static void AddWorldSprite(GameObject target, int sortingOrder, bool isSolidWhenEmpty)
    {
        UiWorldSprite worldSprite = target.AddComponent<UiWorldSprite>();
        SerializedObject serialized = new SerializedObject(worldSprite);
        serialized.FindProperty("_sortingOrder").intValue = sortingOrder;
        serialized.FindProperty("_isSolidWhenEmpty").boolValue = isSolidWhenEmpty;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// リザルトを作る
    /// </summary>
    /// <param name="screen">1920×1080 の土台</param>
    /// <param name="roundSprite">角丸の仮の形</param>
    /// <param name="root">リザルト全体</param>
    /// <param name="title">勝者の表示</param>
    /// <param name="score">勝ち星の数の表示</param>
    /// <param name="buttons">「再戦」「タイトルへ」のボタン（この順）</param>
    /// <param name="focusFrames">選んでいるボタンに出す枠（ボタンと同じ順）</param>
    private static void BuildResult(RectTransform screen, Sprite roundSprite, out GameObject root, out TextMeshProUGUI title, out TextMeshProUGUI score, out Button[] buttons, out GameObject[] focusFrames)
    {
        RectTransform result = CreateStretch("Result", screen);
        Image dim = result.gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.75f);
        dim.raycastTarget = false;

        RectTransform panel = CreateRect("Panel", result, 460f, 250f, 1000f, 580f);
        AddArt(panel, "UI-07", LightGray, roundSprite, false, null);
        AddOutline(panel);

        title = AddText("Title", panel, 0f, 60f, 1000f, 170f, "P1の勝ち！", 140f, UiFontRole.Heading, TextAlignmentOptions.Center);
        score = AddText("Score", panel, 0f, 250f, 1000f, 130f, "2 − 0", 110f, UiFontRole.Number, TextAlignmentOptions.Center);
        string[] labels = { "再戦", "タイトルへ" };
        float[] buttonXs = { 70f, 530f };
        buttons = new Button[labels.Length];
        focusFrames = new GameObject[labels.Length];

        for (int i = 0; i < labels.Length; i++)
        {
            RectTransform frame = CreateRect($"ResultFocusFrame{i}", panel, buttonXs[i] - 12f, 408f, 424f, 134f);
            AddFocusFrame(frame, roundSprite, Image.Type.Sliced);
            focusFrames[i] = frame.gameObject;

            RectTransform button = CreateRect($"ResultButton{i}", panel, buttonXs[i], 420f, 400f, 110f);
            Image buttonImage = button.gameObject.AddComponent<Image>();
            buttonImage.sprite = roundSprite;
            buttonImage.type = Image.Type.Sliced;
            buttonImage.color = Orange;
            AddText("Label", button, 0f, 0f, 400f, 110f, labels[i], 56f, UiFontRole.Heading, TextAlignmentOptions.Center);
            buttons[i] = AddClickArea(button);
        }

        result.gameObject.SetActive(false);
        root = result.gameObject;
    }

    /// <summary>
    /// 中心を基準に、位置と大きさを指定して四角を作る（回転させたいものに使う）
    /// </summary>
    /// <param name="name">オブジェクト名</param>
    /// <param name="parent">置く場所</param>
    /// <param name="centerX">中心の左からの位置</param>
    /// <param name="centerY">中心の上からの位置</param>
    /// <param name="width">幅</param>
    /// <param name="height">高さ</param>
    /// <returns>作った四角</returns>
    private static RectTransform CreateCentered(string name, Transform parent, float centerX, float centerY, float width, float height)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)gameObject.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(centerX, -centerY);
        rect.sizeDelta = new Vector2(width, height);
        return rect;
    }

    /// <summary>
    /// private の Vector2 の配列のフィールドに値を入れる
    /// </summary>
    /// <param name="target">入れる先のコンポーネント</param>
    /// <param name="fieldName">フィールド名</param>
    /// <param name="values">入れる値</param>
    private static void SetVector2Array(Object target, string fieldName, Vector2[] values)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(fieldName);
        property.arraySize = values.Length;

        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).vector2Value = values[i];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// private の小数のフィールドに値を入れる
    /// </summary>
    /// <param name="target">入れる先のコンポーネント</param>
    /// <param name="fieldName">フィールド名</param>
    /// <param name="value">入れる値</param>
    private static void SetFloat(Object target, string fieldName, float value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(fieldName).floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// private の true／false のフィールドに値を入れる
    /// </summary>
    /// <param name="target">入れる先のコンポーネント</param>
    /// <param name="fieldName">フィールド名</param>
    /// <param name="value">入れる値</param>
    private static void SetBool(Object target, string fieldName, bool value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(fieldName).boolValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
