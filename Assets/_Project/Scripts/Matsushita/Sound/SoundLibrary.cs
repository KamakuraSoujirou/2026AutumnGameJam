using UnityEngine;

/// <summary>
/// 音の種類と音ファイルの対応表。Resources/Sound/SoundLibrary に置くと SoundPlayer が読み込む。
/// 音を差し替えるときは、この asset の Clip を入れ替えるだけでよい
/// </summary>
[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Game/SoundLibrary")]
public class SoundLibrary : ScriptableObject
{
    [SerializeField, Tooltip("音の種類ごとの設定")] private SoundEntry[] _entries = new SoundEntry[0];

    /// <summary>
    /// 音の種類に対応する設定を探す
    /// </summary>
    /// <param name="id">音の種類</param>
    /// <param name="entry">見つかった設定</param>
    /// <returns>音ファイルが入っていれば true</returns>
    public bool TryGet(SoundId id, out SoundEntry entry)
    {
        foreach (SoundEntry candidate in _entries)
        {
            if (candidate != null && candidate.Id == id && candidate.Clip != null)
            {
                entry = candidate;
                return true;
            }
        }

        entry = null;
        return false;
    }

    /// <summary>
    /// 設定をまとめて入れ替える（エディタの SoundLibraryBuilder から使う）
    /// </summary>
    /// <param name="entries">新しい設定</param>
    public void SetEntries(SoundEntry[] entries)
    {
        _entries = entries;
    }
}
