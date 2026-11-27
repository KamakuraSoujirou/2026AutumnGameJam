using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// UIの仮組み（PC画面）。背景の左の机にあるPC画面を、ときどき起動する演出（UiPcScreenBoot）を組み立てる
/// </summary>
public static partial class UiMockBuilder
{
    private const string PcScreenName = "PcScreen";

    /// <summary>PC画面の描画順（背景は 0、立ち絵は 10）</summary>
    private const int PcScreenSortingOrder = 1;

    /// <summary>
    /// すでにある試合画面のプレハブに、PC画面を足す。作り直さずにその場で足すので、プレハブを手で直した分は消えない
    /// </summary>
    [MenuItem("Tools/UI仮組/PC画面を足す")]
    public static void AddPcScreenToGame()
    {
        if (!File.Exists(GamePrefabPath))
        {
            Debug.LogWarning("UI_Game.prefab がありません。先に「試合画面を作る」を実行してください");
            return;
        }

        LoadOrCreateSkin();
        GameObject contents = PrefabUtility.LoadPrefabContents(GamePrefabPath);

        try
        {
            RectTransform screen = (RectTransform)contents.transform.Find("Screen");

            if (screen.Find(PcScreenName) != null)
            {
                Debug.Log("PC画面はすでに入っています");
                return;
            }

            BuildPcScreen(screen);
            contents.GetComponent<UiSkinApplier>().ApplyAll();
            PrefabUtility.SaveAsPrefabAsset(contents, GamePrefabPath);
            Debug.Log($"PC画面を足しました：{GamePrefabPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    /// <summary>
    /// PC画面を作る。背景のすぐ手前に置く。絵がないときは何も出さない
    /// </summary>
    /// <param name="screen">1920×1080 の土台</param>
    private static void BuildPcScreen(RectTransform screen)
    {
        EnsureFullRectSprite($"{ArtFolder}/BackGround/game_pc.png");

        RectTransform pcScreen = CreateStretch(PcScreenName, screen);
        Transform background = screen.Find("Background");

        if (background != null)
        {
            pcScreen.SetSiblingIndex(background.GetSiblingIndex() + 1);
        }

        AddSpriteArt(pcScreen, "ART-25", Color.clear, null, false, null, PcScreenSortingOrder);
        pcScreen.gameObject.AddComponent<UiPcScreenBoot>();
    }

    /// <summary>
    /// 画像を、透明な余白も含めた全体の大きさで読み込む（Full Rect）設定にする。
    /// 背景と同じ大きさで画面の部分だけを描いた絵は、Tight のままだと絵のある部分だけの大きさになり、四角に合わせるとき拡大されてしまう
    /// </summary>
    /// <param name="path">画像のパス</param>
    private static void EnsureFullRectSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
        {
            return;
        }

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);

        bool isSingle = importer.spriteImportMode == SpriteImportMode.Single;

        if (isSingle && settings.spriteMeshType == SpriteMeshType.FullRect)
        {
            return;
        }

        // Multiple のままだと、絵のある部分だけが自動で切り出されて、画面の大きさがわからなくなる
        importer.spriteImportMode = SpriteImportMode.Single;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    /// <summary>
    /// PC画面の絵の登録を返す
    /// </summary>
    /// <returns>素材の登録</returns>
    private static UiSkinEntry[] CreatePcScreenEntries()
    {
        return new[]
        {
            new UiSkinEntry("ART-25", "PC画面（点いたときの絵。背景と同じ 1920×1080 で、画面の部分だけ）", new Vector2(1920f, 1080f), "game_pc")
        };
    }
}
