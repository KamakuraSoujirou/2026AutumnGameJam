using UnityEngine;

/// <summary>
/// 解除（ひらめき謎）の問題を1問選び、4つの選択肢を上下左右にランダムに割り振って、答えを判定する
/// </summary>
/// <remarks>
/// 問題は Resources/Puzzles/Defuse.json から読む。同じ試合では、同じ問題を出さない（ResetMatch で戻す）
/// </remarks>
public class DefuseChallenge
{
    /// <summary>解除の問題の種類（JSON の type）</summary>
    public const string PuzzleType = "Defuse";

    private const int ChoiceCount = 4;

    private readonly PuzzleDatabase _database = new PuzzleDatabase(PuzzleDatabase.DefusePath);
    private readonly string[] _choices = new string[ChoiceCount];
    private readonly int[] _order = new int[ChoiceCount];

    private PuzzleData _puzzle;
    private InputDirection _correctDirection;

    /// <summary>
    /// 液晶に出す問題文（問題を選んでいなければ空）
    /// </summary>
    public string QuestionText => _puzzle != null ? _puzzle.Text : string.Empty;

    /// <summary>
    /// 正解の選択肢がある方向（CPUが使う）
    /// </summary>
    public InputDirection CorrectDirection => _correctDirection;

    /// <summary>
    /// 指定した方向のボタンに出す選択肢を返す
    /// </summary>
    /// <param name="direction">ボタンの方向</param>
    /// <returns>選択肢の文字（問題を選んでいなければ空）</returns>
    public string GetChoice(InputDirection direction)
    {
        return _choices[(int)direction] ?? string.Empty;
    }

    /// <summary>
    /// 出した問題の記録を消す。試合の最初に呼ぶ
    /// </summary>
    public void ResetMatch()
    {
        _database.ResetUsed();
        Clear();
    }

    /// <summary>
    /// まだこの試合で出していない問題を1つ選び、選択肢をシャッフルして上下左右に割り振る
    /// </summary>
    /// <returns>問題を選べたら true。問題が1問もなければ false</returns>
    public bool TryCreate()
    {
        PuzzleData puzzle = _database.Draw(PuzzleType);

        if (puzzle == null)
        {
            return false;
        }

        _puzzle = puzzle;
        ShuffleOrder();

        // choices の先頭が正解なので、0 番目が入った方向が正解になる
        for (int i = 0; i < ChoiceCount; i++)
        {
            _choices[i] = puzzle.Choices[_order[i]];

            if (_order[i] == 0)
            {
                _correctDirection = (InputDirection)i;
            }
        }

        return true;
    }

    /// <summary>
    /// 押された方向が正解か調べる
    /// </summary>
    /// <param name="direction">押された方向</param>
    /// <returns>正解なら true</returns>
    public bool IsCorrect(InputDirection direction)
    {
        return _puzzle != null && direction == _correctDirection;
    }

    /// <summary>
    /// 選んでいる問題を消す
    /// </summary>
    public void Clear()
    {
        _puzzle = null;

        for (int i = 0; i < ChoiceCount; i++)
        {
            _choices[i] = string.Empty;
        }
    }

    /// <summary>
    /// 0〜3 の並びをランダムに入れ替える（フィッシャー–イェーツ）
    /// </summary>
    private void ShuffleOrder()
    {
        for (int i = 0; i < ChoiceCount; i++)
        {
            _order[i] = i;
        }

        for (int i = ChoiceCount - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (_order[i], _order[j]) = (_order[j], _order[i]);
        }
    }
}
