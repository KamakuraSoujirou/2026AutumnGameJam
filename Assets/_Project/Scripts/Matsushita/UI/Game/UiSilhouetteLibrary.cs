using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 言葉に合うシルエットの画像を探す。大きさの問題（小さいものから順に押せ）で、ボタンに文字の代わりに出す。
/// 言葉と画像の対応は Resources/Puzzles/Silhouettes.json、画像は Resources/Silhouettes/ に置く
/// </summary>
public static class UiSilhouetteLibrary
{
    private const string MapPath = "Puzzles/Silhouettes";
    private const string SpriteFolder = "Silhouettes/";

    private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
    private static Dictionary<string, string> _icons;

    /// <summary>
    /// 言葉と画像名の対応表の1行
    /// </summary>
    [Serializable]
    private class Entry
    {
        public string name;
        public string icon;
    }

    /// <summary>
    /// 言葉と画像名の対応表
    /// </summary>
    [Serializable]
    private class EntryList
    {
        public Entry[] entries;
    }

    /// <summary>
    /// 言葉に合うシルエットを返す。対応表に無い、または画像が見つからないときは null（その場合は文字を出す）
    /// </summary>
    /// <param name="word">言葉（例：「ゾウ」）</param>
    /// <returns>シルエットの画像。無ければ null</returns>
    public static Sprite Find(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return null;
        }

        LoadMap();

        if (!_icons.TryGetValue(word, out string icon))
        {
            return null;
        }

        if (!Sprites.TryGetValue(icon, out Sprite sprite))
        {
            // 非公開の素材（サブモジュール）が無い環境でも動くように、見つからなければ null のままにする
            sprite = Resources.Load<Sprite>(SpriteFolder + icon);
            Sprites[icon] = sprite;
        }

        return sprite;
    }

    /// <summary>
    /// 対応表を読み込む（最初に1回だけ）
    /// </summary>
    private static void LoadMap()
    {
        if (_icons != null)
        {
            return;
        }

        _icons = new Dictionary<string, string>();
        TextAsset textAsset = Resources.Load<TextAsset>(MapPath);

        if (textAsset == null)
        {
            Debug.LogWarning($"UiSilhouetteLibrary: Resources/{MapPath}.json が見つかりません。大きさの問題は文字で出します");
            return;
        }

        EntryList list = JsonUtility.FromJson<EntryList>(textAsset.text);

        if (list == null || list.entries == null)
        {
            return;
        }

        foreach (Entry entry in list.entries)
        {
            if (entry != null && !string.IsNullOrEmpty(entry.name) && !string.IsNullOrEmpty(entry.icon))
            {
                _icons[entry.name] = entry.icon;
            }
        }
    }
}
