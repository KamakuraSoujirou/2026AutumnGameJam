using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// UIで使う画像とフォントをまとめた表。素材が届いたらここに入れるだけで画面が差し替わる
/// </summary>
[CreateAssetMenu(menuName = "2026AutumnGameJam/UiSkin", fileName = "UiSkin")]
public class UiSkin : ScriptableObject
{
    [SerializeField, Tooltip("素材の一覧")] private List<UiSkinEntry> _entries = new List<UiSkinEntry>();
    [SerializeField, Tooltip("ロゴ・見出しのフォント（Dela Gothic One）")] private TMP_FontAsset _headingFont;
    [SerializeField, Tooltip("数字のフォント（DotGothic16）")] private TMP_FontAsset _numberFont;
    [SerializeField, Tooltip("本文のフォント（Zen Kaku Gothic New）")] private TMP_FontAsset _bodyFont;
    [SerializeField, Tooltip("爆弾に書いてある文字のフォント（M PLUS 1）")] private TMP_FontAsset _bombFont;

    /// <summary>
    /// 素材の一覧
    /// </summary>
    public IReadOnlyList<UiSkinEntry> Entries => _entries;

    /// <summary>
    /// 素材IDから登録を探す
    /// </summary>
    /// <param name="id">素材ID</param>
    /// <returns>見つかった登録。なければ null</returns>
    public UiSkinEntry FindEntry(string id)
    {
        foreach (UiSkinEntry entry in _entries)
        {
            if (entry.Id == id)
            {
                return entry;
            }
        }

        return null;
    }

    /// <summary>
    /// 素材IDから画像を取り出す
    /// </summary>
    /// <param name="id">素材ID</param>
    /// <returns>画像。まだ入っていなければ null</returns>
    public Sprite GetSprite(string id)
    {
        UiSkinEntry entry = FindEntry(id);
        return entry != null ? entry.Sprite : null;
    }

    /// <summary>
    /// 役割に合ったフォントを取り出す
    /// </summary>
    /// <param name="role">文字の役割</param>
    /// <returns>フォント。まだ入っていなければ null</returns>
    public TMP_FontAsset GetFont(UiFontRole role)
    {
        switch (role)
        {
            case UiFontRole.Heading:
                return _headingFont;
            case UiFontRole.Number:
                return _numberFont;
            case UiFontRole.Bomb:
                return _bombFont;
            default:
                return _bodyFont;
        }
    }

    /// <summary>
    /// 画像がまだ入っていない登録だけを消す（使わなくなった素材IDの片付け用。エディタの組み立て用）
    /// </summary>
    /// <param name="id">消す素材ID</param>
    /// <returns>消したら true</returns>
    public bool RemoveEntryIfEmpty(string id)
    {
        UiSkinEntry entry = FindEntry(id);

        if (entry == null || entry.Sprite != null)
        {
            return false;
        }

        _entries.Remove(entry);
        return true;
    }

    /// <summary>
    /// 登録の「探すファイル名」を変える（素材のファイル名が決まったときの直し用。エディタの組み立て用）
    /// </summary>
    /// <param name="id">素材ID</param>
    /// <param name="fileName">新しいファイル名（拡張子なし）</param>
    /// <returns>変えたら true</returns>
    public bool UpdateFileName(string id, string fileName)
    {
        UiSkinEntry entry = FindEntry(id);

        if (entry == null || entry.FileName == fileName)
        {
            return false;
        }

        entry.FileName = fileName;
        return true;
    }

    /// <summary>
    /// 同じIDがまだなければ登録を足す（エディタの組み立て用）
    /// </summary>
    /// <param name="entry">足す登録</param>
    /// <returns>足したら true</returns>
    public bool AddEntryIfMissing(UiSkinEntry entry)
    {
        if (FindEntry(entry.Id) != null)
        {
            return false;
        }

        _entries.Add(entry);
        return true;
    }
}
