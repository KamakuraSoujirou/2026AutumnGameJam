using System;
using UnityEngine;

/// <summary>
/// 素材1つ分の登録。素材IDと、差し替える画像を持つ
/// </summary>
[Serializable]
public class UiSkinEntry
{
    [SerializeField, Tooltip("素材ID（例：ART-01）")] private string _id;
    [SerializeField, Tooltip("素材の名前")] private string _label;
    [SerializeField, Tooltip("画面上の大きさ（px）。比率の目安")] private Vector2 _size;
    [SerializeField, Tooltip("自動で割り当てるときに探すファイル名（拡張子なし）")] private string _fileName;
    [SerializeField, Tooltip("差し替える画像。空なら仮の四角を出す")] private Sprite _sprite;

    /// <summary>
    /// 素材ID
    /// </summary>
    public string Id => _id;

    /// <summary>
    /// 素材の名前
    /// </summary>
    public string Label => _label;

    /// <summary>
    /// 画面上の大きさ（px）
    /// </summary>
    public Vector2 Size => _size;

    /// <summary>
    /// 自動で割り当てるときに探すファイル名
    /// </summary>
    public string FileName => _fileName;

    /// <summary>
    /// 差し替える画像
    /// </summary>
    public Sprite Sprite
    {
        get => _sprite;
        set => _sprite = value;
    }

    /// <summary>
    /// 素材の登録を作る
    /// </summary>
    /// <param name="id">素材ID</param>
    /// <param name="label">素材の名前</param>
    /// <param name="size">画面上の大きさ（px）</param>
    /// <param name="fileName">探すファイル名</param>
    public UiSkinEntry(string id, string label, Vector2 size, string fileName)
    {
        _id = id;
        _label = label;
        _size = size;
        _fileName = fileName;
    }
}
