using System;
using UnityEngine;

/// <summary>
/// 手作りの謎の JSON ファイル1つ分（{ "puzzles": [ ... ] }）
/// </summary>
/// <remarks>
/// JsonUtility は変数名を JSON のキーとして使うため、フィールドだけは _ を付けずに JSON と同じ名前にしている。
/// </remarks>
[Serializable]
public class PuzzleDataList
{
    [SerializeField] private PuzzleData[] puzzles;

    /// <summary>
    /// ファイルに書かれている謎の一覧
    /// </summary>
    public PuzzleData[] Puzzles => puzzles ?? Array.Empty<PuzzleData>();
}
