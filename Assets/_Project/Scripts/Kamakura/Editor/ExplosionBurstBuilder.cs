using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 試合画面のプレハブ（UI_Game.prefab）に、爆発の絵のエフェクト（UiExplosionBurst）を足すエディター用のメニュー
/// </summary>
public static class ExplosionBurstBuilder
{
    private const string GamePrefabPath = "Assets/_Project/Prefabs/Matsushita/UI/UI_Game.prefab";
    private const string ArtFolder = "Assets/_PrivateAssets/Art/Effects/Explosion";
    private const string BurstName = "ExplosionBurst";
    private const string ExplosionTextName = "ExplosionText";
    private const string WordFileName = "explosion_boom_text";

    /// <summary>爆発の絵の中心（左下を 0、右上を 1 とした位置）。ここを中心に膨らませる</summary>
    private static readonly Vector2 BurstPivot = new Vector2(0.44f, 0.5f);

    /// <summary>「BOOM」の文字の中心（左下を 0、右上を 1 とした位置）</summary>
    private static readonly Vector2 WordPivot = new Vector2(0.45f, 0.52f);

    /// <summary>重ねる絵のファイル名。奥（煙）から手前（光）の順</summary>
    private static readonly string[] LayerFileNames =
    {
        "explosion_brown",
        "explosion_orange",
        "explosion_yellow",
        "explosion_light",
    };

    /// <summary>
    /// 試合画面のプレハブに爆発のエフェクトを足す。すでにあれば作り直す（プレハブのほかの部分はそのまま）
    /// </summary>
    [MenuItem("Tools/UI仮組/爆発のエフェクトを足す")]
    public static void AddExplosionBurstToGame()
    {
        if (!File.Exists(GamePrefabPath))
        {
            Debug.LogWarning($"{GamePrefabPath} がありません。先に「試合画面を作る」を実行してください");
            return;
        }

        Sprite[] layerSprites = new Sprite[LayerFileNames.Length];

        for (int i = 0; i < LayerFileNames.Length; i++)
        {
            layerSprites[i] = LoadSprite(LayerFileNames[i]);

            if (layerSprites[i] == null)
            {
                return;
            }
        }

        Sprite wordSprite = LoadSprite(WordFileName);

        if (wordSprite == null)
        {
            return;
        }

        GameObject contents = PrefabUtility.LoadPrefabContents(GamePrefabPath);

        try
        {
            Transform screen = contents.transform.Find("Screen");
            UiGameView view = contents.GetComponentInChildren<UiGameView>(true);

            if (screen == null || view == null)
            {
                Debug.LogWarning("UI_Game.prefab に Screen か UiGameView が見つかりません");
                return;
            }

            Transform old = screen.Find(BurstName);

            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            UiExplosionBurst burst = BuildBurst(screen, layerSprites, wordSprite);
            SerializedObject serializedView = new SerializedObject(view);
            serializedView.FindProperty("_explosionBurst").objectReferenceValue = burst;
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(contents, GamePrefabPath);
            Debug.Log($"爆発のエフェクトを足しました：{GamePrefabPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    /// <summary>
    /// エフェクトを組み立てる。「ドカーン！」の文字のすぐ奥に置く
    /// </summary>
    /// <param name="screen">1920×1080 の土台</param>
    /// <param name="layerSprites">重ねる絵（奥から手前の順）</param>
    /// <param name="wordSprite">「BOOM」の文字の絵</param>
    /// <returns>作ったエフェクト</returns>
    private static UiExplosionBurst BuildBurst(Transform screen, Sprite[] layerSprites, Sprite wordSprite)
    {
        RectTransform root = CreateStretch(BurstName, screen, BurstPivot);
        Transform explosionText = screen.Find(ExplosionTextName);

        if (explosionText != null)
        {
            root.SetSiblingIndex(explosionText.GetSiblingIndex());
        }

        Image[] layers = new Image[layerSprites.Length];

        for (int i = 0; i < layerSprites.Length; i++)
        {
            layers[i] = CreateImage(LayerFileNames[i], root, layerSprites[i], BurstPivot);
        }

        Image word = CreateImage(WordFileName, root, wordSprite, WordPivot);

        UiExplosionBurst burst = root.gameObject.AddComponent<UiExplosionBurst>();
        SerializedObject serializedBurst = new SerializedObject(burst);
        SerializedProperty layersProperty = serializedBurst.FindProperty("_layers");
        layersProperty.arraySize = layers.Length;

        for (int i = 0; i < layers.Length; i++)
        {
            layersProperty.GetArrayElementAtIndex(i).objectReferenceValue = layers[i];
        }

        serializedBurst.FindProperty("_word").objectReferenceValue = word;
        serializedBurst.ApplyModifiedPropertiesWithoutUndo();

        root.gameObject.SetActive(false);
        return burst;
    }

    /// <summary>
    /// 画面いっぱいに広がる絵を作る（絵は画面と同じ 1920×1080）
    /// </summary>
    /// <param name="name">名前</param>
    /// <param name="parent">親</param>
    /// <param name="sprite">出す絵</param>
    /// <param name="pivot">拡大・回転の中心</param>
    /// <returns>作った絵</returns>
    private static Image CreateImage(string name, Transform parent, Sprite sprite, Vector2 pivot)
    {
        RectTransform rect = CreateStretch(name, parent, pivot);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        rect.gameObject.SetActive(false);
        return image;
    }

    /// <summary>
    /// 親いっぱいに広がる四角を作る
    /// </summary>
    /// <param name="name">名前</param>
    /// <param name="parent">親</param>
    /// <param name="pivot">拡大・回転の中心</param>
    /// <returns>作った四角</returns>
    private static RectTransform CreateStretch(string name, Transform parent, Vector2 pivot)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)gameObject.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = pivot;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    /// <summary>
    /// 絵を Sprite（1枚・余白込みの大きさ）として読み込む設定にしてから読み込む
    /// </summary>
    /// <param name="fileName">ファイル名（拡張子なし）</param>
    /// <returns>読み込んだ絵。なければ null</returns>
    private static Sprite LoadSprite(string fileName)
    {
        string path = $"{ArtFolder}/{fileName}.png";
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
        {
            Debug.LogWarning($"爆発の絵が見つかりません：{path}（サブモジュールを更新してください）");
            return null;
        }

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);

        bool isReady = importer.textureType == TextureImporterType.Sprite
            && importer.spriteImportMode == SpriteImportMode.Single
            && settings.spriteMeshType == SpriteMeshType.FullRect;

        if (!isReady)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
