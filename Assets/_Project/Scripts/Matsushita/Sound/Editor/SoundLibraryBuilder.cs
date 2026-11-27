using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// バックアップ用サウンド（_PrivateAssets/ThirdParty/BackupSound）から SoundLibrary を作る。
/// SoundLibrary がまだなければ、エディタを開いたときに自動で作る。作り直すときは Tools > サウンド から
/// </summary>
[InitializeOnLoad]
public static class SoundLibraryBuilder
{
    private const string LibraryFolder = "Assets/Resources/Sound";
    private const string LibraryAssetPath = LibraryFolder + "/SoundLibrary.asset";
    private const string BackupRoot = "Assets/_PrivateAssets/ThirdParty/BackupSound";
    private const float SeVolume = 0.8f;
    private const float BgmVolume = 0.35f;

    /// <summary>
    /// 最初に入れる音。候補が複数あるものは、ひとまず1つ目を選んでいる（聞いてから asset 側で差し替えてよい）
    /// </summary>
    private static readonly (SoundId Id, string Path, float Volume)[] DefaultClips =
    {
        (SoundId.Tick, "SE/SE-01_tick/se_tick_01.wav", 0.5f),
        (SoundId.Heartbeat, "SE/SE-02_heartbeat/se_heartbeat_fast_01.wav", 0.7f),
        (SoundId.Button, "SE/SE-03_button/se_button_01.wav", SeVolume),
        (SoundId.WireCut, "SE/SE-04_cut/se_cut_01.ogg", SeVolume),
        (SoundId.Wrong, "SE/SE-05_wrong/se_wrong_01.ogg", SeVolume),
        (SoundId.Land, "SE/SE-06_land/se_land_01.wav", SeVolume),
        (SoundId.Explosion, "SE/SE-07_explosion/se_explosion_01.ogg", 1f),
        (SoundId.Funny, "SE/SE-08_funny/se_funny_boing_02.ogg", SeVolume),
        (SoundId.DefuseStart, "SE/SE-09_defuse_start/se_defuse_start_01.ogg", SeVolume),
        (SoundId.DefuseSuccess, "SE/SE-10_defuse_success/se_defuse_success_01.ogg", SeVolume),
        (SoundId.RoundStart, "SE/SE-11_round_start/se_round_start_gong_01.ogg", SeVolume),
        (SoundId.Cursor, "SE/SE-12_cursor_decide/se_cursor_01.wav", 0.6f),
        (SoundId.Decide, "SE/SE-12_cursor_decide/se_decide_02.ogg", SeVolume),
        (SoundId.Cancel, "SE/SE-12_cursor_decide/se_cancel_01.ogg", SeVolume),
        (SoundId.Fanfare, "SE/SE-13_fanfare/se_fanfare_01.ogg", SeVolume),
        (SoundId.BgmBattle, "BGM/BGM-01_battle/bgm_battle_comical_01.ogg", BgmVolume),
        (SoundId.BgmTitle, "BGM/BGM-02_title/bgm_title_01.ogg", BgmVolume),
        (SoundId.BgmResult, "BGM/BGM-03_result/bgm_result_01.mp3", BgmVolume),
    };

    /// <summary>
    /// エディタの読み込みが落ち着いてから、SoundLibrary がなければ作る
    /// </summary>
    static SoundLibraryBuilder()
    {
        EditorApplication.delayCall += CreateIfMissing;
    }

    /// <summary>
    /// SoundLibrary をバックアップ用サウンドで作り直す（手で差し替えた音は元に戻る）
    /// </summary>
    [MenuItem("Tools/サウンド/SoundLibrary をバックアップ素材で作り直す")]
    private static void Rebuild()
    {
        bool isConfirmed = EditorUtility.DisplayDialog(
            "SoundLibrary を作り直す",
            "バックアップ用サウンドの1つ目の候補で作り直します。手で差し替えた音は元に戻ります。",
            "作り直す",
            "やめる");

        if (isConfirmed)
        {
            Build();
        }
    }

    /// <summary>
    /// SoundLibrary がなく、バックアップ用サウンドがあるときだけ作る
    /// </summary>
    private static void CreateIfMissing()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<SoundLibrary>(LibraryAssetPath) != null)
        {
            return;
        }

        if (!AssetDatabase.IsValidFolder(BackupRoot))
        {
            return;
        }

        Build();
    }

    /// <summary>
    /// 音ファイルを読み込んで SoundLibrary に入れ、保存する
    /// </summary>
    private static void Build()
    {
        List<SoundEntry> entries = new List<SoundEntry>();
        List<string> missing = new List<string>();

        foreach ((SoundId id, string path, float volume) in DefaultClips)
        {
            string assetPath = $"{BackupRoot}/{path}";
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);

            if (clip == null)
            {
                missing.Add(assetPath);
            }

            entries.Add(new SoundEntry(id, clip, volume));
        }

        if (missing.Count == DefaultClips.Length)
        {
            Debug.LogWarning("SoundLibraryBuilder: バックアップ用サウンドが1つも読み込めなかったので、SoundLibrary は作りませんでした");
            return;
        }

        SoundLibrary library = AssetDatabase.LoadAssetAtPath<SoundLibrary>(LibraryAssetPath);

        if (library == null)
        {
            Directory.CreateDirectory(LibraryFolder);
            AssetDatabase.Refresh();
            library = ScriptableObject.CreateInstance<SoundLibrary>();
            AssetDatabase.CreateAsset(library, LibraryAssetPath);
        }

        library.SetEntries(entries.ToArray());
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();

        foreach (string path in missing)
        {
            Debug.LogWarning($"SoundLibraryBuilder: 見つからなかった音：{path}");
        }

        Debug.Log($"SoundLibraryBuilder: {LibraryAssetPath} を作りました（{entries.Count - missing.Count}/{entries.Count} 件）", library);
    }
}
