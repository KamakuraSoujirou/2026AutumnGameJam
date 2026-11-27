using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 試合画面のプレハブ（UI_Game.prefab）の導火線（UiFuse）に、導火線の絵と火花の絵を入れるエディター用のメニュー
/// </summary>
/// <remarks>
/// 絵は P2 側（右）向きに描かれているので、P1 側（左）は左右反転して使う。
/// 横の区間の絵が角（曲がり）も含んでいるので、横の区間は両端を太さの半分だけ伸ばし、縦の区間はその分だけ縮める
/// </remarks>
public static class FuseArtBuilder
{
    private const string GamePrefabPath = "Assets/_Project/Prefabs/Matsushita/UI/UI_Game.prefab";
    private const string ArtFolder = "Assets/_PrivateAssets/Art/Bomb/fuse";
    private const string ArtRootName = "FuseArt";
    private const string SparkArtName = "SparkArt";
    private const string SparkPlaceholderName = "Mark";
    private const string SparkFileName = "fuse_spark";

    /// <summary>折れ線の区間ごとに並べる絵のファイル名（爆弾に近い順）</summary>
    private static readonly string[][] LegFileNames =
    {
        new[] { "fuse_center" },
        new[] { "fuse_01_R", "fuse_02_R", "fuse_03_R" },
        new[] { "fuse_04_R", "fuse_05_R" },
    };

    /// <summary>
    /// 試合画面のプレハブの導火線に絵を入れる。すでにあれば作り直す（プレハブのほかの部分はそのまま）
    /// </summary>
    [MenuItem("Tools/UI仮組/導火線の絵を入れる")]
    public static void AddFuseArtToGame()
    {
        if (!File.Exists(GamePrefabPath))
        {
            Debug.LogWarning($"{GamePrefabPath} がありません。先に「試合画面を作る」を実行してください");
            return;
        }

        Sprite[][] legSprites = new Sprite[LegFileNames.Length][];

        for (int leg = 0; leg < LegFileNames.Length; leg++)
        {
            legSprites[leg] = new Sprite[LegFileNames[leg].Length];

            for (int i = 0; i < LegFileNames[leg].Length; i++)
            {
                legSprites[leg][i] = LoadSprite(LegFileNames[leg][i]);

                if (legSprites[leg][i] == null)
                {
                    return;
                }
            }
        }

        Sprite sparkSprite = LoadSprite(SparkFileName);

        if (sparkSprite == null)
        {
            return;
        }

        GameObject contents = PrefabUtility.LoadPrefabContents(GamePrefabPath);

        try
        {
            UiFuse[] fuses = contents.GetComponentsInChildren<UiFuse>(true);

            if (fuses.Length == 0)
            {
                Debug.LogWarning("UI_Game.prefab に UiFuse が見つかりません");
                return;
            }

            foreach (UiFuse fuse in fuses)
            {
                if (!BuildFuseArt(fuse, legSprites, sparkSprite))
                {
                    return;
                }
            }

            PrefabUtility.SaveAsPrefabAsset(contents, GamePrefabPath);
            Debug.Log($"導火線の絵を入れました：{GamePrefabPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    /// <summary>
    /// 1本の導火線に絵を並べ、UiFuse に登録する。火花も絵に差し替える
    /// </summary>
    /// <param name="fuse">導火線</param>
    /// <param name="legSprites">区間ごとの絵（爆弾に近い順）</param>
    /// <param name="sparkSprite">火花の絵</param>
    /// <returns>作れたら true</returns>
    private static bool BuildFuseArt(UiFuse fuse, Sprite[][] legSprites, Sprite sparkSprite)
    {
        SerializedObject serializedFuse = new SerializedObject(fuse);
        SerializedProperty pointsProperty = serializedFuse.FindProperty("_points");

        if (pointsProperty.arraySize != legSprites.Length + 1)
        {
            Debug.LogWarning($"{fuse.name} の折れ線の点の数が {legSprites.Length + 1} 個ではないので、絵を並べられません");
            return false;
        }

        Vector2[] points = new Vector2[pointsProperty.arraySize];

        for (int i = 0; i < points.Length; i++)
        {
            points[i] = pointsProperty.GetArrayElementAtIndex(i).vector2Value;
        }

        Transform old = fuse.transform.Find(ArtRootName);

        if (old != null)
        {
            Object.DestroyImmediate(old.gameObject);
        }

        RectTransform artRoot = CreateStretch(ArtRootName, fuse.transform);
        artRoot.SetAsFirstSibling();

        // 絵は右向きなので、横の区間が左へ伸びる導火線（P1側）は左右反転する
        bool isMirrored = points[2].x < points[1].x;
        float thickness = GetThickness(legSprites[0][0]);
        float halfThickness = thickness * 0.5f;
        SerializedProperty piecesProperty = serializedFuse.FindProperty("_pieces");
        piecesProperty.arraySize = 0;

        for (int leg = 0; leg < legSprites.Length; leg++)
        {
            Vector2 from = points[leg];
            Vector2 to = points[leg + 1];
            Vector2 direction = (to - from).normalized;
            bool isHorizontal = Mathf.Abs(direction.x) > Mathf.Abs(direction.y);
            bool hasPrevious = leg > 0;
            bool hasNext = leg < legSprites.Length - 1;

            // 横の区間の絵は角を含むので外へ伸ばし、縦の区間は角の分だけ縮める
            Vector2 start = isHorizontal ? from - direction * halfThickness : from + (hasPrevious ? direction * halfThickness : Vector2.zero);
            Vector2 end = isHorizontal ? to + direction * halfThickness : to - (hasNext ? direction * halfThickness : Vector2.zero);
            float legLength = Vector2.Distance(start, end);
            float artLength = 0f;

            foreach (Sprite sprite in legSprites[leg])
            {
                artLength += GetLength(sprite);
            }

            float placed = 0f;

            for (int i = 0; i < legSprites[leg].Length; i++)
            {
                Sprite sprite = legSprites[leg][i];
                float length = GetLength(sprite) / artLength * legLength;
                Vector2 center = start + direction * (placed + length * 0.5f);
                placed += length;

                Image piece = CreatePiece(LegFileNames[leg][i], artRoot, sprite, center, isHorizontal, length, thickness, isMirrored);
                piece.fillMethod = isHorizontal ? Image.FillMethod.Horizontal : Image.FillMethod.Vertical;

                // 爆弾側から塗る（＝外側から消えていく）。横は絵の左が爆弾側（反転すれば右）、縦は進む向きで決める
                piece.fillOrigin = isHorizontal ? (int)Image.OriginHorizontal.Left
                    : direction.y < 0f ? (int)Image.OriginVertical.Bottom : (int)Image.OriginVertical.Top;

                int index = piecesProperty.arraySize;
                piecesProperty.arraySize++;
                piecesProperty.GetArrayElementAtIndex(index).objectReferenceValue = piece;
            }
        }

        serializedFuse.ApplyModifiedPropertiesWithoutUndo();
        SetSparkArt(serializedFuse.FindProperty("_spark").objectReferenceValue as RectTransform, sparkSprite);
        return true;
    }

    /// <summary>
    /// 導火線の絵を1枚作る（塗りつぶしで減らせる Image）
    /// </summary>
    /// <param name="name">名前</param>
    /// <param name="parent">親</param>
    /// <param name="sprite">絵</param>
    /// <param name="center">真ん中の位置（画面の左上が原点）</param>
    /// <param name="isHorizontal">横向きの区間なら true</param>
    /// <param name="length">区間の向きの長さ</param>
    /// <param name="thickness">太さ</param>
    /// <param name="isMirrored">左右反転するなら true</param>
    /// <returns>作った絵</returns>
    private static Image CreatePiece(string name, Transform parent, Sprite sprite, Vector2 center, bool isHorizontal, float length, float thickness, bool isMirrored)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)gameObject.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(center.x, -center.y);
        rect.sizeDelta = isHorizontal ? new Vector2(length, thickness) : new Vector2(thickness, length);
        rect.localScale = new Vector3(isMirrored ? -1f : 1f, 1f, 1f);

        Image image = gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Filled;
        image.fillAmount = 1f;
        image.raycastTarget = false;
        return image;
    }

    /// <summary>
    /// 火花を絵に差し替える。仮の★と、UiSkin から絵を入れる仕組みは外す
    /// </summary>
    /// <param name="spark">火花</param>
    /// <param name="sparkSprite">火花の絵</param>
    private static void SetSparkArt(RectTransform spark, Sprite sparkSprite)
    {
        if (spark == null)
        {
            return;
        }

        UiArtSlot slot = spark.GetComponent<UiArtSlot>();

        if (slot != null)
        {
            Object.DestroyImmediate(slot);
        }

        Transform placeholder = spark.Find(SparkPlaceholderName);

        if (placeholder != null)
        {
            placeholder.gameObject.SetActive(false);
        }

        Image image = spark.GetComponent<Image>();

        if (image == null)
        {
            image = spark.gameObject.AddComponent<Image>();
        }

        image.sprite = sparkSprite;
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    /// <summary>
    /// 区間の向きに沿った、絵の長さ（長い辺）を返す
    /// </summary>
    /// <param name="sprite">絵</param>
    /// <returns>長さ</returns>
    private static float GetLength(Sprite sprite)
    {
        return Mathf.Max(sprite.rect.width, sprite.rect.height);
    }

    /// <summary>
    /// 導火線の太さ（短い辺）を返す
    /// </summary>
    /// <param name="sprite">絵</param>
    /// <returns>太さ</returns>
    private static float GetThickness(Sprite sprite)
    {
        return Mathf.Min(sprite.rect.width, sprite.rect.height);
    }

    /// <summary>
    /// 親いっぱいに広がる四角を作る
    /// </summary>
    /// <param name="name">名前</param>
    /// <param name="parent">親</param>
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
            Debug.LogWarning($"導火線の絵が見つかりません：{path}（サブモジュールを更新してください）");
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
