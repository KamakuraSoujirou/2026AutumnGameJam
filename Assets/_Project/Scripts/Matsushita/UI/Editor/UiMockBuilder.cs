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
    private const float ScreenWidth = 1920f;
    private const float ScreenHeight = 1080f;

    private static readonly Color Orange = new Color(0.92f, 0.54f, 0f);
    private static readonly Color TextColor = new Color(0.08f, 0.08f, 0.08f);
    private static readonly Color PanelLineColor = new Color(0.6f, 0.6f, 0.6f);

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
        // 見た目の切り替えは自前で行う。矢印キーによる選択移動は入力処理と取り合うので切る
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

        UiSkinEntry[] sharedEntries =
        {
            new UiSkinEntry("ART-09_normal", "キャラ P1（通常）", new Vector2(600f, 900f), "chr_p1_normal"),
            new UiSkinEntry("ART-10_normal", "キャラ P2（通常）", new Vector2(600f, 900f), "chr_p2_normal"),
            new UiSkinEntry("ART-01", "爆弾本体", new Vector2(800f, 800f), "bomb_core")
        };

        bool isChanged = false;

        foreach (UiSkinEntry entry in sharedEntries)
        {
            isChanged |= skin.AddEntryIfMissing(entry);
        }

        foreach (UiSkinEntry entry in CreateTitleEntries())
        {
            isChanged |= skin.AddEntryIfMissing(entry);
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
