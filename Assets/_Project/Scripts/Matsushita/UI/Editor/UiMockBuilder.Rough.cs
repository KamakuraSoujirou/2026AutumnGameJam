using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 爆弾・キャラのラフ絵を、2Dオブジェクト（SpriteRenderer）として並べた確認用シーンを作る
/// </summary>
public static partial class UiMockBuilder
{
    private const string RoughFolder = "Assets/_PrivateAssets/Art";
    private const string RoughScenePath = "Assets/_Project/Scenes/Matsushita/Rough_2D.unity";
    private const float RoughHeight = 6f;

    /// <summary>
    /// ラフ絵（P1・爆弾・P2）を並べたシーンを作って開く
    /// </summary>
    [MenuItem("Tools/UI仮組/ラフ絵を2Dオブジェクトで並べる")]
    public static void BuildRoughScene()
    {
        EnsureSpriteImport(RoughFolder + "/Characters/chr_p1_normal.png");
        EnsureSpriteImport(RoughFolder + "/Characters/chr_p2_normal.png");
        EnsureSpriteImport(RoughFolder + "/Bomb/bomb_rough.png");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 4.5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.18f, 0.2f, 0.28f);
        camera.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        CreateRoughSprite("Player1_Boy", RoughFolder + "/Characters/chr_p1_normal.png", new Vector3(-6.5f, 0f, 0f), 0);
        CreateRoughSprite("Bomb", RoughFolder + "/Bomb/bomb_rough.png", new Vector3(0f, 0f, 0f), 1);
        CreateRoughSprite("Player2_Girl", RoughFolder + "/Characters/chr_p2_normal.png", new Vector3(6.5f, 0f, 0f), 0);

        EnsureFolder("Assets/_Project/Scenes/Matsushita");
        EditorSceneManager.SaveScene(scene, RoughScenePath);
        Debug.Log("ラフ絵のシーンを作りました：" + RoughScenePath);
    }

    /// <summary>
    /// 画像をスプライトとして読み込む設定にする（なっていなければ）
    /// </summary>
    /// <param name="path">画像のパス</param>
    private static void EnsureSpriteImport(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
        {
            Debug.LogWarning("画像が見つかりません：" + path);
            return;
        }

        if (importer.textureType == TextureImporterType.Sprite)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }

    /// <summary>
    /// スプライトを、高さを揃えて1つ置く
    /// </summary>
    /// <param name="objectName">オブジェクト名</param>
    /// <param name="path">画像のパス</param>
    /// <param name="position">置く位置</param>
    /// <param name="sortingOrder">描画順</param>
    private static void CreateRoughSprite(string objectName, string path, Vector3 position, int sortingOrder)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

        if (sprite == null)
        {
            Debug.LogWarning("スプライトを読み込めません：" + path);
            return;
        }

        GameObject spriteObject = new GameObject(objectName);
        SpriteRenderer spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = sortingOrder;
        spriteObject.transform.position = position;

        float scale = RoughHeight / sprite.bounds.size.y;
        spriteObject.transform.localScale = new Vector3(scale, scale, 1f);
    }
}
