using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 手作りの謎（Resources/Puzzles の JSON）を読み込み、同じ試合で重複しないように1問ずつ渡す
/// </summary>
/// <example>
/// <code>
/// PuzzleDatabase orderDatabase = new PuzzleDatabase(PuzzleDatabase.OrderSwitchPath);
/// PuzzleData puzzle = orderDatabase.Draw("C2");
/// </code>
/// </example>
public class PuzzleDatabase
{
    /// <summary>謎A T3 連想のファイル</summary>
    public const string WireAssociationPath = "Puzzles/WireAssociation";

    /// <summary>謎C C2〜C4 のファイル</summary>
    public const string OrderSwitchPath = "Puzzles/OrderSwitch";

    /// <summary>解除のファイル</summary>
    public const string DefusePath = "Puzzles/Defuse";

    private const int ItemCount = 4;
    private const int ChoiceCount = 4;

    private readonly List<PuzzleData> _puzzles = new List<PuzzleData>();
    private readonly HashSet<PuzzleData> _usedPuzzles = new HashSet<PuzzleData>();

    /// <summary>
    /// 読み込めた謎の一覧（書き方が間違っている謎は入らない）
    /// </summary>
    public IReadOnlyList<PuzzleData> Puzzles => _puzzles;

    /// <summary>
    /// Resources の下にある JSON を読み込む
    /// </summary>
    /// <param name="resourcePath">Resources からのパス（拡張子なし）。WireAssociationPath などを使う</param>
    public PuzzleDatabase(string resourcePath)
    {
        Load(resourcePath);
    }

    /// <summary>
    /// 指定した種類の謎をすべて返す
    /// </summary>
    /// <param name="type">謎の種類（T3、C2 など）</param>
    /// <returns>その種類の謎の一覧</returns>
    public List<PuzzleData> GetPuzzles(string type)
    {
        return _puzzles.Where(puzzle => puzzle.Type == type).ToList();
    }

    /// <summary>
    /// 指定した種類から、まだ出していない謎をランダムに1問選ぶ。全部出し終えたら最初から選び直す
    /// </summary>
    /// <param name="type">謎の種類（T3、C2 など）</param>
    /// <returns>選んだ謎。その種類が1問もなければ null</returns>
    public PuzzleData Draw(string type)
    {
        List<PuzzleData> candidates = _puzzles
            .Where(puzzle => puzzle.Type == type && !_usedPuzzles.Contains(puzzle))
            .ToList();

        if (candidates.Count == 0)
        {
            _usedPuzzles.RemoveWhere(puzzle => puzzle.Type == type);
            candidates = GetPuzzles(type);
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning($"PuzzleDatabase: 種類 {type} の謎がありません");
            return null;
        }

        PuzzleData selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        _usedPuzzles.Add(selected);
        return selected;
    }

    /// <summary>
    /// 出した謎の記録を消す。試合の最初に呼ぶ
    /// </summary>
    public void ResetUsed()
    {
        _usedPuzzles.Clear();
    }

    /// <summary>
    /// JSON を読み込み、正しく書けている謎だけを一覧に入れる
    /// </summary>
    /// <param name="resourcePath">Resources からのパス（拡張子なし）</param>
    private void Load(string resourcePath)
    {
        TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
        if (textAsset == null)
        {
            Debug.LogError($"PuzzleDatabase: Resources/{resourcePath}.json が見つかりません");
            return;
        }

        PuzzleDataList dataList;
        try
        {
            dataList = JsonUtility.FromJson<PuzzleDataList>(textAsset.text);
        }
        catch (ArgumentException exception)
        {
            Debug.LogError($"PuzzleDatabase: {resourcePath}.json の書き方が間違っています（カンマや括弧を確認）\n{exception.Message}");
            return;
        }

        if (dataList == null)
        {
            Debug.LogError($"PuzzleDatabase: {resourcePath}.json が空です");
            return;
        }

        PuzzleData[] puzzles = dataList.Puzzles;
        for (int i = 0; i < puzzles.Length; i++)
        {
            string error = FindError(puzzles[i]);
            if (error != null)
            {
                Debug.LogWarning($"PuzzleDatabase: {resourcePath}.json の {i + 1} 問目を読み飛ばしました（{error}）");
                continue;
            }

            _puzzles.Add(puzzles[i]);
        }
    }

    /// <summary>
    /// 謎の書き方の間違いを探す
    /// </summary>
    /// <param name="puzzle">調べる謎</param>
    /// <returns>間違いの説明。問題がなければ null</returns>
    private static string FindError(PuzzleData puzzle)
    {
        if (puzzle == null)
        {
            return "中身が空";
        }

        if (string.IsNullOrWhiteSpace(puzzle.Type))
        {
            return "type が空";
        }

        if (string.IsNullOrWhiteSpace(puzzle.Text))
        {
            return "text が空";
        }

        switch (puzzle.Gimmick)
        {
            case PuzzleData.GimmickWire:
                if (string.IsNullOrWhiteSpace(puzzle.Answer))
                {
                    return "answer（線の色）が空";
                }

                return null;

            case PuzzleData.GimmickOrder:
                if (puzzle.Items.Length != ItemCount || puzzle.Items.Any(string.IsNullOrWhiteSpace))
                {
                    return $"items は空でない言葉を {ItemCount} つ";
                }

                if (puzzle.Items.Distinct().Count() != ItemCount)
                {
                    return "items に同じ言葉がある";
                }

                return null;

            case PuzzleData.GimmickDefuse:
                if (puzzle.Choices.Length != ChoiceCount || puzzle.Choices.Any(string.IsNullOrWhiteSpace))
                {
                    return $"choices は空でない選択肢を {ChoiceCount} つ";
                }

                if (puzzle.Choices.Distinct().Count() != ChoiceCount)
                {
                    return "choices に同じ選択肢がある";
                }

                if (puzzle.Answer != puzzle.Choices[0])
                {
                    return "answer が choices の先頭と違う";
                }

                return null;

            default:
                return $"gimmick「{puzzle.Gimmick}」は A・C・Defuse のどれか";
        }
    }
}
