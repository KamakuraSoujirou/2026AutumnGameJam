using System;
using UnityEngine;

/// <summary>
/// 手作りの謎1問分のデータ（JSON の1行に対応する）
/// </summary>
/// <remarks>
/// JsonUtility は変数名を JSON のキーとして使うため、フィールドだけは _ を付けずに JSON と同じ名前にしている。
/// </remarks>
[Serializable]
public class PuzzleData
{
    /// <summary>配線ギミックの値</summary>
    public const string GimmickWire = "A";

    /// <summary>順番スイッチギミックの値</summary>
    public const string GimmickOrder = "C";

    /// <summary>解除（ひらめき謎）の値</summary>
    public const string GimmickDefuse = "Defuse";

    [SerializeField] private string gimmick;
    [SerializeField] private string type;
    [SerializeField] private string text;
    [SerializeField] private string[] items;
    [SerializeField] private string[] choices;
    [SerializeField] private string answer;

    /// <summary>
    /// ギミック（A：配線、C：順番スイッチ、Defuse：解除）
    /// </summary>
    public string Gimmick => gimmick ?? string.Empty;

    /// <summary>
    /// 謎の種類（T3、C2、C3、C4、Defuse）
    /// </summary>
    public string Type => type ?? string.Empty;

    /// <summary>
    /// 液晶に出す問題文
    /// </summary>
    public string Text => text ?? string.Empty;

    /// <summary>
    /// 順番スイッチ用。正解の順番に並んだ4つの言葉
    /// </summary>
    public string[] Items => items ?? Array.Empty<string>();

    /// <summary>
    /// 解除用。4択の選択肢（先頭が正解）
    /// </summary>
    public string[] Choices => choices ?? Array.Empty<string>();

    /// <summary>
    /// 正解（T3は線の色、解除は正解の選択肢）
    /// </summary>
    public string Answer => answer ?? string.Empty;
}
