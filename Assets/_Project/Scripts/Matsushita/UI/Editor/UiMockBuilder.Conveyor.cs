using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// UIの仮組み（ベルトコンベアの粒）。背景のベルトの上を流れる粒（UiConveyorFlow）を組み立てる
/// </summary>
public static partial class UiMockBuilder
{
    private const string ConveyorName = "Conveyor";

    /// <summary>粒の描画順（背景は 0、立ち絵は 10）</summary>
    private const int ConveyorSortingOrder = 1;

    /// <summary>
    /// 粒が通る線（左のベルト、右のベルトの順）。x・y は 1920×1080 の左上からの位置、z は大きさの倍率。外から中央への順
    /// </summary>
    private static readonly Vector3[][] ConveyorLanes =
    {
        new[]
        {
            new Vector3(200f, 513f, 1.15f), new Vector3(310f, 467f, 1.0f), new Vector3(458f, 405f, 0.65f), new Vector3(527f, 382f, 0.55f),
            new Vector3(593f, 378f, 0.42f), new Vector3(670f, 372f, 0.36f), new Vector3(735f, 370f, 0.3f)
        },
        new[]
        {
            new Vector3(1745f, 505f, 1.1f), new Vector3(1645f, 468f, 1.0f), new Vector3(1541f, 432f, 0.9f), new Vector3(1441f, 394f, 0.75f),
            new Vector3(1342f, 380f, 0.5f), new Vector3(1270f, 378f, 0.38f), new Vector3(1222f, 375f, 0.3f), new Vector3(1180f, 372f, 0.26f)
        }
    };

    /// <summary>
    /// すでにある試合画面のプレハブに、ベルトの粒を足す。作り直さずにその場で足すので、プレハブを手で直した分は消えない
    /// </summary>
    [MenuItem("Tools/UI仮組/ベルトの粒を足す")]
    public static void AddConveyorToGame()
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

            if (screen.Find(ConveyorName) != null)
            {
                Debug.Log("ベルトの粒はすでに入っています");
                return;
            }

            BuildConveyor(screen);
            contents.GetComponent<UiSkinApplier>().ApplyAll();
            PrefabUtility.SaveAsPrefabAsset(contents, GamePrefabPath);
            Debug.Log($"ベルトの粒を足しました：{GamePrefabPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    /// <summary>
    /// ベルトの粒を作る。背景のすぐ手前（粒より奥に描くのは背景だけ）に置く
    /// </summary>
    /// <param name="screen">1920×1080 の土台</param>
    private static void BuildConveyor(RectTransform screen)
    {
        RectTransform conveyor = CreateStretch(ConveyorName, screen);
        Transform background = screen.Find("Background");

        if (background != null)
        {
            conveyor.SetSiblingIndex(background.GetSiblingIndex() + 1);
        }

        UiConveyorFlow flow = conveyor.gameObject.AddComponent<UiConveyorFlow>();
        SerializedObject serialized = new SerializedObject(flow);
        SerializedProperty lanes = serialized.FindProperty("_lanes");
        lanes.arraySize = ConveyorLanes.Length;

        for (int i = 0; i < ConveyorLanes.Length; i++)
        {
            SerializedProperty points = lanes.GetArrayElementAtIndex(i).FindPropertyRelative("Points");
            points.arraySize = ConveyorLanes[i].Length;

            for (int k = 0; k < ConveyorLanes[i].Length; k++)
            {
                points.GetArrayElementAtIndex(k).vector3Value = ConveyorLanes[i][k];
            }
        }

        serialized.FindProperty("_sortingOrder").intValue = ConveyorSortingOrder;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// ベルトの粒の絵の登録を返す
    /// </summary>
    /// <returns>素材の登録</returns>
    private static UiSkinEntry[] CreateConveyorEntries()
    {
        return new[]
        {
            new UiSkinEntry("ART-24_pink", "ベルトの粒（ピンク）", new Vector2(130f, 88f), "belt_dot_pink"),
            new UiSkinEntry("ART-24_green", "ベルトの粒（緑）", new Vector2(130f, 88f), "belt_dot_green"),
            new UiSkinEntry("ART-24_yellow", "ベルトの粒（黄）", new Vector2(130f, 88f), "belt_dot_yellow")
        };
    }
}
