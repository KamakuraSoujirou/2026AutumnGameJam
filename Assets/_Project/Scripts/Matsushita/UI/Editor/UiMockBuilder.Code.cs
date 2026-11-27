using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 爆弾の配線に、コードの絵（つながっている・切れた）を入れる
/// </summary>
public static partial class UiMockBuilder
{
    private const string CodeArtFolder = ArtFolder + "/Bomb/code";

    /// <summary>
    /// 試合画面のプレハブの配線に、コードの絵を入れる。
    /// すでにあるプレハブをその場で書き換えるので、手で直した分や他の部品の参照は壊れない
    /// </summary>
    [MenuItem("Tools/UI仮組/配線にコードの絵を入れる")]
    public static void ApplyCodeArt()
    {
        if (!File.Exists(GamePrefabPath))
        {
            Debug.LogWarning("UI_Game.prefab がありません。先に「試合画面を作る」を実行してください");
            return;
        }

        UiSkin skin = LoadOrCreateSkin();
        int assignedCount = 0;

        for (int i = 0; i < WireNames.Length; i++)
        {
            assignedCount += AssignCodeSprite(skin, $"ART-03_{WireNames[i]}", i, false) ? 1 : 0;
            assignedCount += AssignCodeSprite(skin, $"ART-03_cut_{WireNames[i]}", i, true) ? 1 : 0;
        }

        EditorUtility.SetDirty(skin);
        AssetDatabase.SaveAssets();

        GameObject contents = PrefabUtility.LoadPrefabContents(GamePrefabPath);

        try
        {
            int wireCount = 0;

            foreach (UiWire wire in contents.GetComponentsInChildren<UiWire>(true))
            {
                PlaceCodeVisuals(wire);
                wireCount++;
            }

            contents.GetComponent<UiSkinApplier>().ApplyAll();
            PrefabUtility.SaveAsPrefabAsset(contents, GamePrefabPath);
            Debug.Log($"コードの絵を {assignedCount} 枚登録して、配線 {wireCount} 本に入れました：{GamePrefabPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    /// <summary>
    /// コードの絵を置く四角を、爆弾の四角（640×640）の座標で返す
    /// </summary>
    /// <param name="colorIndex">色の番号（赤・青・黄・緑の順）</param>
    /// <returns>左上の位置と大きさ（上が 0）</returns>
    private static Rect GetCodeRect(int colorIndex)
    {
        Rect art = CodeArtRects[colorIndex];
        return new Rect(art.x * BombArtScale, art.y * BombArtScale, art.width * BombArtScale, art.height * BombArtScale);
    }

    /// <summary>
    /// コードの絵のファイル名を返す
    /// </summary>
    /// <param name="colorIndex">色の番号（赤・青・黄・緑の順）</param>
    /// <param name="isCut">切れた絵なら true</param>
    /// <returns>ファイル名（拡張子なし。例：code_red_disconnect）</returns>
    private static string GetCodeFileName(int colorIndex, bool isCut)
    {
        return $"code_{WireNames[colorIndex]}_{(isCut ? "disconnect" : "normal")}";
    }

    /// <summary>
    /// UiSkin の登録に、コードの絵を入れる（探すファイル名も合わせる）
    /// </summary>
    /// <param name="skin">素材の表</param>
    /// <param name="artId">素材ID</param>
    /// <param name="colorIndex">色の番号（赤・青・黄・緑の順）</param>
    /// <param name="isCut">切れた絵なら true</param>
    /// <returns>入れたら true</returns>
    private static bool AssignCodeSprite(UiSkin skin, string artId, int colorIndex, bool isCut)
    {
        string fileName = GetCodeFileName(colorIndex, isCut);
        string path = $"{CodeArtFolder}/{WireNames[colorIndex]}/{fileName}.png";
        EnsureSpriteImport(path);

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        UiSkinEntry entry = skin.FindEntry(artId);

        if (sprite == null || entry == null)
        {
            Debug.LogWarning($"コードの絵を登録できませんでした：{artId} ← {path}");
            return false;
        }

        entry.Sprite = sprite;
        skin.UpdateFileName(artId, fileName);
        return true;
    }

    /// <summary>
    /// 配線1本の、つながっている・切れたコードの見た目を、その色のコードの位置に置き直して、線を出すようにする
    /// </summary>
    /// <param name="wire">配線</param>
    private static void PlaceCodeVisuals(UiWire wire)
    {
        SerializedObject serializedWire = new SerializedObject(wire);
        serializedWire.FindProperty("_isWireHidden").boolValue = false;
        serializedWire.ApplyModifiedPropertiesWithoutUndo();

        SerializedProperty normalVisuals = serializedWire.FindProperty("_normalVisuals");
        SerializedProperty cutVisuals = serializedWire.FindProperty("_cutVisuals");

        for (int i = 0; i < WireNames.Length; i++)
        {
            PlaceCodeVisual(normalVisuals.GetArrayElementAtIndex(i).objectReferenceValue as GameObject, i);
            PlaceCodeVisual(cutVisuals.GetArrayElementAtIndex(i).objectReferenceValue as GameObject, i);
        }
    }

    /// <summary>
    /// コードの見た目1つを、その色のコードの位置に置き直す。仮の線（素材がないときの色と欠片）は消す
    /// </summary>
    /// <param name="visual">コードの見た目（なければ何もしない）</param>
    /// <param name="colorIndex">色の番号（赤・青・黄・緑の順）</param>
    private static void PlaceCodeVisual(GameObject visual, int colorIndex)
    {
        if (visual == null)
        {
            return;
        }

        Rect codeRect = GetCodeRect(colorIndex);
        RectTransform rect = (RectTransform)visual.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(codeRect.x, -codeRect.y);
        rect.sizeDelta = codeRect.size;

        UiArtSlot slot = visual.GetComponent<UiArtSlot>();

        if (slot == null)
        {
            return;
        }

        SerializedObject serializedSlot = new SerializedObject(slot);
        serializedSlot.FindProperty("_placeholderColor").colorValue = Color.clear;
        SerializedProperty placeholderObject = serializedSlot.FindProperty("_placeholderObject");

        if (placeholderObject.objectReferenceValue != null)
        {
            Object.DestroyImmediate(placeholderObject.objectReferenceValue);
            placeholderObject.objectReferenceValue = null;
        }

        serializedSlot.ApplyModifiedPropertiesWithoutUndo();
    }
}
