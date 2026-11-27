using System;
using UnityEngine;

/// <summary>
/// 音の種類1つ分の設定（どの音ファイルを、どの音量で鳴らすか）
/// </summary>
[Serializable]
public class SoundEntry
{
    [SerializeField, Tooltip("音の種類")] private SoundId _id;
    [SerializeField, Tooltip("鳴らす音ファイル。空なら鳴らさない")] private AudioClip _clip;
    [SerializeField, Range(0f, 1f), Tooltip("音量")] private float _volume = 1f;

    /// <summary>
    /// 空の設定を作る（Inspector で要素を足したときに使われる）
    /// </summary>
    public SoundEntry()
    {
    }

    /// <summary>
    /// 設定を作る（SoundLibrary をエディタで作るときに使う）
    /// </summary>
    /// <param name="id">音の種類</param>
    /// <param name="clip">鳴らす音ファイル</param>
    /// <param name="volume">音量（0〜1）</param>
    public SoundEntry(SoundId id, AudioClip clip, float volume)
    {
        _id = id;
        _clip = clip;
        _volume = volume;
    }

    /// <summary>音の種類</summary>
    public SoundId Id => _id;

    /// <summary>鳴らす音ファイル</summary>
    public AudioClip Clip => _clip;

    /// <summary>音量（0〜1）</summary>
    public float Volume => _volume;
}
