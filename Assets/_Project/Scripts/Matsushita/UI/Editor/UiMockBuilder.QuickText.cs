using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UIの仮組み（クイックテキスト）。待機中の側の方向キーを押し続けたときに出る円形の候補と、文のセットを組み立てる
/// </summary>
public static partial class UiMockBuilder
{
    private const string QuickTextSetPath = PrefabFolder + "/UiQuickTextSet.asset";
    private const string QuickTextWheelName = "QuickTextWheel";

    /// <summary>円の絵の素材ID（UiSkin）と、入れる画像のファイル名（拡張子なし）</summary>
    private const string QuickTextArtId = "UI-27";
    private const string QuickTextArtFileName = "UI_Wakusen_Aori 1";

    /// <summary>候補の円の幅（1920×1080 の画面で）</summary>
    private const float WheelSize = 520f;

    /// <summary>円の絵（UI-27。484×476）を、縦横比そのままで置いたときの高さ</summary>
    private const float WheelArtHeight = WheelSize * 476f / 484f;

    /// <summary>候補の円の中心の位置（パネルの左上から。立ち絵の胴のあたり）</summary>
    private const float WheelCenterX = 171f;
    private const float WheelCenterY = 215f;

    /// <summary>選んでいる扇に影を掛ける円の直径（円の絵の、外側の黒い線より内側に収める）</summary>
    private const float WheelHighlightSize = 480f;

    /// <summary>円の中心から、文を置く位置までの距離</summary>
    private const float WheelLabelRadius = 172f;

    /// <summary>扇1つが円のどれだけを占めるか（0.25 より少し小さくして、絵の白い区切り線を隠さない）</summary>
    private const float WheelSliceFill = 0.235f;

    /// <summary>円の絵がまだ入っていないときの、仮の円の色</summary>
    private static readonly Color WheelPlaceholderColor = new Color(0.54f, 0.82f, 0.8f, 0.95f);

    /// <summary>選んでいない扇の色（透明。円の絵をそのまま見せる）</summary>
    private static readonly Color WheelWedgeColor = new Color(0f, 0f, 0f, 0f);

    /// <summary>文字の色（円の絵の水色の上で読みやすい濃い色）</summary>
    private static readonly Color WheelTextColor = new Color(0.1f, 0.16f, 0.2f, 1f);

    /// <summary>扇の並び：画面の上・下・左・右（W・S・A・D のキーの位置）</summary>
    private static readonly string[] WheelSlotNames = { "Up", "Down", "Left", "Right" };

    /// <summary>扇の並びを、円の上から時計回りに数えた番号にする（上 0、右 1、下 2、左 3）</summary>
    private static readonly int[] WheelClockwiseIndex = { 0, 2, 3, 1 };

    /// <summary>円の中心から見た向き（画面の座標なので、下が +y）。上から時計回り</summary>
    private static readonly Vector2[] WheelClockwiseDirections = { new Vector2(0f, -1f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(-1f, 0f) };

    /// <summary>扇に書くキー名（P1、P2の順。それぞれ上・下・左・右の順）</summary>
    private static readonly string[][] WheelKeyNames =
    {
        new[] { "W", "S", "A", "D" },
        new[] { "↑", "↓", "←", "→" }
    };

    /// <summary>候補の下に出す、取り消しの案内（P1、P2の順）</summary>
    private static readonly string[] WheelCancelCaptions = { "離すと送信 ／ Q で取り消し", "離すと送信 ／ 右Shift で取り消し" };

    /// <summary>
    /// すでにある試合画面のプレハブに、クイックテキストの候補と文のセットを足す。候補がすでにあれば（円の絵を替えたときなど）、候補だけを作り直す。プレハブのほかの部分は触らない
    /// </summary>
    [MenuItem("Tools/UI仮組/クイックテキストを足す")]
    public static void AddQuickTextToGame()
    {
        if (!File.Exists(GamePrefabPath))
        {
            Debug.LogWarning("UI_Game.prefab がありません。先に「試合画面を作る」を実行してください");
            return;
        }

        AssignQuickTextArt();
        GameObject contents = PrefabUtility.LoadPrefabContents(GamePrefabPath);

        try
        {
            Transform screen = contents.transform.Find("Screen");
            UiPlayerPanel[] panels =
            {
                screen.Find("PlayerP1").GetComponent<UiPlayerPanel>(),
                screen.Find("PlayerP2").GetComponent<UiPlayerPanel>()
            };

            // 前に作った候補は、作り直すために消す（候補は組み立てで作るものなので、消しても困らない）
            foreach (UiPlayerPanel panel in panels)
            {
                Transform oldWheel = panel.transform.Find(QuickTextWheelName);

                if (oldWheel != null)
                {
                    Object.DestroyImmediate(oldWheel.gameObject);
                }
            }

            BuildQuickText(contents.GetComponent<UiGameView>(), panels);
            contents.GetComponent<UiSkinApplier>().ApplyAll();
            PrefabUtility.SaveAsPrefabAsset(contents, GamePrefabPath);
            Debug.Log($"クイックテキストの候補と文のセットを足しました：{GamePrefabPath} / {QuickTextSetPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    /// <summary>
    /// 両方のプレイヤーに候補を作り、試合画面に文のセットを入れる
    /// </summary>
    /// <param name="view">試合画面（文のセットを入れる先）</param>
    /// <param name="panels">プレイヤーのパネル（P1、P2の順）</param>
    private static void BuildQuickText(UiGameView view, UiPlayerPanel[] panels)
    {
        Sprite roundSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Sprite circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        for (int i = 0; i < panels.Length; i++)
        {
            UiQuickTextWheel wheel = BuildQuickTextWheel((RectTransform)panels[i].transform, i, roundSprite, circleSprite);
            SetObject(panels[i], "_quickTextWheel", wheel);
        }

        SetObject(view, "_quickTextSet", LoadOrCreateQuickTextSet());
    }

    /// <summary>
    /// 円の絵（UI_Wakusen_Aori 1）を、UiSkin の UI-27 に入れる（手で入れた別の画像は上書きしない）
    /// </summary>
    private static void AssignQuickTextArt()
    {
        UiSkin skin = LoadOrCreateSkin();
        string path = $"{WakusenFolder}/{QuickTextArtFileName}.png";
        EnsureSpriteImport(path);
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        UiSkinEntry entry = skin.FindEntry(QuickTextArtId);

        if (sprite == null || entry == null)
        {
            Debug.LogWarning($"クイックテキストの円の絵を入れられません：{QuickTextArtId} ← {path}");
            return;
        }

        bool isReplaceable = entry.Sprite == null || entry.Sprite.name.StartsWith("UI_Wakusen_");

        if (isReplaceable && entry.Sprite != sprite)
        {
            entry.Sprite = sprite;
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();
        }
    }

    /// <summary>
    /// 文のセットを読み込む。なければ初期の文で作る
    /// </summary>
    /// <returns>文のセット</returns>
    private static UiQuickTextSet LoadOrCreateQuickTextSet()
    {
        UiQuickTextSet set = AssetDatabase.LoadAssetAtPath<UiQuickTextSet>(QuickTextSetPath);

        if (set != null)
        {
            return set;
        }

        EnsureFolder(PrefabFolder);
        set = ScriptableObject.CreateInstance<UiQuickTextSet>();
        AssetDatabase.CreateAsset(set, QuickTextSetPath);
        AssetDatabase.SaveAssets();
        return set;
    }

    /// <summary>
    /// 円形の候補を作る（4つの扇、文とキー名、中央の案内、下の取り消しの案内）。最初は隠しておく
    /// </summary>
    /// <param name="panel">置くプレイヤーのパネル</param>
    /// <param name="playerIndex">0：P1、1：P2</param>
    /// <param name="roundSprite">角丸の仮の形</param>
    /// <param name="circleSprite">丸の仮の形</param>
    /// <returns>作った候補</returns>
    private static UiQuickTextWheel BuildQuickTextWheel(RectTransform panel, int playerIndex, Sprite roundSprite, Sprite circleSprite)
    {
        const float Half = WheelSize * 0.5f;
        float sliceCenterDegrees = WheelSliceFill * 360f * 0.5f;

        RectTransform wheelRect = CreateCentered(QuickTextWheelName, panel, WheelCenterX, WheelCenterY, WheelSize, WheelSize);

        // 円の絵（X字で4分割）。扇・文より奥に描くため、最初に作る
        RectTransform circle = CreateCentered("Circle", wheelRect, Half, Half, WheelSize, WheelArtHeight);
        AddArt(circle, QuickTextArtId, WheelPlaceholderColor, circleSprite, false, null);

        Image[] wedges = new Image[WheelSlotNames.Length];

        for (int slot = 0; slot < wedges.Length; slot++)
        {
            // 円の上から時計回りに塗った扇（選んだときだけ色が付く）を、向きに合わせて回す（塗った扇の真ん中は、上から時計回りに sliceCenterDegrees の位置）
            RectTransform wedge = CreateCentered($"Wedge_{WheelSlotNames[slot]}", wheelRect, Half, Half, WheelHighlightSize, WheelHighlightSize);
            Image image = wedge.gameObject.AddComponent<Image>();
            image.sprite = circleSprite;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = (int)Image.Origin360.Top;
            image.fillClockwise = true;
            image.fillAmount = WheelSliceFill;
            image.color = WheelWedgeColor;
            image.raycastTarget = false;
            wedge.localRotation = Quaternion.Euler(0f, 0f, sliceCenterDegrees - 90f * WheelClockwiseIndex[slot]);
            wedges[slot] = image;
        }

        TextMeshProUGUI[] texts = new TextMeshProUGUI[WheelSlotNames.Length];
        TextMeshProUGUI[] keyTexts = new TextMeshProUGUI[WheelSlotNames.Length];

        for (int slot = 0; slot < texts.Length; slot++)
        {
            Vector2 direction = WheelClockwiseDirections[WheelClockwiseIndex[slot]];
            float centerX = Half + direction.x * WheelLabelRadius;
            float centerY = Half + direction.y * WheelLabelRadius;

            keyTexts[slot] = AddText($"Key_{WheelSlotNames[slot]}", wheelRect, centerX - 50f, centerY - 48f, 100f, 36f, WheelKeyNames[playerIndex][slot], 26f, UiFontRole.Heading, TextAlignmentOptions.Center);
            keyTexts[slot].color = WheelTextColor;

            TextMeshProUGUI text = AddText($"Text_{WheelSlotNames[slot]}", wheelRect, centerX - 85f, centerY - 10f, 170f, 60f, UiQuickTextSet.GetDefaultSlotText(slot), 36f, UiFontRole.Body, TextAlignmentOptions.Center);
            text.color = WheelTextColor;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.enableAutoSizing = true;
            text.fontSizeMin = 20f;
            text.fontSizeMax = 36f;
            texts[slot] = text;
        }

        RectTransform caption = CreateCentered("CancelCaption", wheelRect, Half, WheelSize + 32f, 520f, 46f);
        Image captionImage = caption.gameObject.AddComponent<Image>();
        captionImage.sprite = roundSprite;
        captionImage.type = Image.Type.Sliced;
        captionImage.color = new Color(0f, 0f, 0f, 0.65f);
        captionImage.raycastTarget = false;
        TextMeshProUGUI captionText = AddText("Text", caption, 0f, 0f, 520f, 46f, WheelCancelCaptions[playerIndex], 26f, UiFontRole.Body, TextAlignmentOptions.Center);
        captionText.color = Color.white;

        UiQuickTextWheel wheel = wheelRect.gameObject.AddComponent<UiQuickTextWheel>();
        SetObjectArray(wheel, "_wedges", wedges);
        SetObjectArray(wheel, "_texts", texts);
        SetObjectArray(wheel, "_keyTexts", keyTexts);
        wheelRect.gameObject.SetActive(false);
        return wheel;
    }
}
