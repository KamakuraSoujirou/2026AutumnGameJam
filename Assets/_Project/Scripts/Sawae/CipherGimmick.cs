using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ギミック2：暗号。3文字の暗号（B1〜B5）を解いて、上下左右のボタン（○△□☆）で入力する。
/// 入力は爆弾本体から Answer() で受け取る。謎の作り方は CipherPuzzle に書く
/// </summary>
public class CipherGimmick : Gimmick
{
    // 同じ種類を続けて出してよい回数（3回続けない → 2回まで）
    private const int MaxSameTypeInRow = 2;
    private const string EmptySlotText = "_";

    // 各ボタンの文字を出すテキスト（InputDirection の並び：Up, Down, Left, Right の順）
    [SerializeField] private Text[] _buttonLabelTexts = new Text[4];

    // 入力済みの文字を出すテキスト
    [SerializeField] private Text _enteredText;

    // 難易度ごとに出す謎の種類（仕様書「難易度の段階」の表のとおり）
    [SerializeField] private CipherPuzzleType[] _stage1Types = { CipherPuzzleType.Same };
    [SerializeField] private CipherPuzzleType[] _stage2Types = { CipherPuzzleType.Same, CipherPuzzleType.Reverse };
    [SerializeField] private CipherPuzzleType[] _stage3Types = { CipherPuzzleType.Table, CipherPuzzleType.Calc, CipherPuzzleType.Reverse };
    [SerializeField] private CipherPuzzleType[] _stage4Types = { CipherPuzzleType.Table, CipherPuzzleType.Calc, CipherPuzzleType.Count };

    private CipherPuzzle _puzzle;
    private int _enteredCount;
    private CipherPuzzleType _lastType;
    private int _sameTypeCount;

    /// <summary>
    /// 液晶に出す問題文
    /// </summary>
    public override string QuestionText => _puzzle != null ? _puzzle.QuestionText : string.Empty;

    /// <summary>
    /// 次に押すと正解の方向（CPUが使う）
    /// </summary>
    public override InputDirection NextAnswer
    {
        get
        {
            if (_puzzle == null)
            {
                return InputDirection.Up;
            }

            int index = Mathf.Min(_enteredCount, CipherPuzzle.AnswerLength - 1);
            return _puzzle.Answers[index];
        }
    }

    /// <summary>
    /// ラウンドの最初に呼ばれる。入力と、同じ種類の連続回数をリセットする
    /// </summary>
    public override void ResetRound()
    {
        _enteredCount = 0;
        _sameTypeCount = 0;
    }

    /// <summary>
    /// 爆弾を受け取るたびに呼ばれる。段階に合った謎を選んで作る
    /// </summary>
    /// <param name="stage">難易度の段階（1〜4）</param>
    public override void CreatePuzzle(int stage)
    {
        CipherPuzzleType type = PickPuzzleType(stage);

        _puzzle = CipherPuzzle.Create(type);
        _enteredCount = 0;

        ShowButtonLabels();
        ShowEntered();

        Debug.Log($"[暗号] 段階{stage}（{type}）：{_puzzle.QuestionText}");
    }

    /// <summary>
    /// ボタンが押されたときに呼ばれる。途中まで合っていればContinue、3文字そろえばCorrect、間違えたらWrong
    /// </summary>
    /// <param name="direction">押された方向</param>
    /// <returns>入力の結果</returns>
    public override AnswerResult Answer(InputDirection direction)
    {
        if (_puzzle == null)
        {
            Debug.LogWarning("[暗号] CreatePuzzle の前に Answer が呼ばれました");
            return AnswerResult.Wrong;
        }

        if (direction != _puzzle.Answers[_enteredCount])
        {
            // 間違えたら入力を消して最初から。謎の答えは変えない（時間減少とスタンは爆弾本体が行う）
            _enteredCount = 0;
            ShowEntered();
            Debug.Log($"[暗号] {_puzzle.GetLabel(direction)} → Wrong（入力を消した）");

            // TODO: 間違えたときのギミック側の演出（後で書く）
            // ApplyPenalty();

            return AnswerResult.Wrong;
        }

        _enteredCount++;
        ShowEntered();

        if (_enteredCount >= CipherPuzzle.AnswerLength)
        {
            Debug.Log($"[暗号] {_puzzle.GetLabel(direction)} → Correct");
            return AnswerResult.Correct;
        }

        Debug.Log($"[暗号] {_puzzle.GetLabel(direction)} → Continue（{_enteredCount}/{CipherPuzzle.AnswerLength}）");
        return AnswerResult.Continue;
    }

    /// <summary>
    /// 段階に合う謎の種類を均等に選ぶ。同じ種類が3回続かないようにする
    /// </summary>
    /// <param name="stage">難易度の段階（1〜4）</param>
    /// <returns>選んだ謎の種類</returns>
    private CipherPuzzleType PickPuzzleType(int stage)
    {
        List<CipherPuzzleType> candidates = new List<CipherPuzzleType>(GetStageTypes(stage));

        // 2回続いていたら外す（候補が1つしかない段階1は外せないのでそのまま）
        if (_sameTypeCount >= MaxSameTypeInRow && candidates.Count > 1)
        {
            candidates.Remove(_lastType);
        }

        CipherPuzzleType picked = candidates[Random.Range(0, candidates.Count)];

        if (_sameTypeCount > 0 && picked == _lastType)
        {
            _sameTypeCount++;
        }
        else
        {
            _sameTypeCount = 1;
        }

        _lastType = picked;
        return picked;
    }

    /// <summary>
    /// 段階に合う謎の種類の一覧を返す
    /// </summary>
    /// <param name="stage">難易度の段階（1〜4）</param>
    /// <returns>出してよい謎の種類</returns>
    private CipherPuzzleType[] GetStageTypes(int stage)
    {
        CipherPuzzleType[] types;

        switch (stage)
        {
            case 1:
                types = _stage1Types;
                break;
            case 2:
                types = _stage2Types;
                break;
            case 3:
                types = _stage3Types;
                break;
            case 4:
                types = _stage4Types;
                break;
            default:
                types = null;
                break;
        }

        if (types == null || types.Length == 0)
        {
            Debug.LogWarning($"[暗号] 段階{stage}の設定がないので、B1（そのまま）を出します");
            return new[] { CipherPuzzleType.Same };
        }

        return types;
    }

    /// <summary>
    /// 各ボタンに、今の謎の文字（○△□☆ や 1〜4）を表示する
    /// </summary>
    private void ShowButtonLabels()
    {
        for (int i = 0; i < _buttonLabelTexts.Length; i++)
        {
            if (_buttonLabelTexts[i] != null)
            {
                _buttonLabelTexts[i].text = _puzzle.ButtonLabels[i];
            }
        }
    }

    /// <summary>
    /// 入力済みの文字を表示する（まだの所は「_」）
    /// </summary>
    private void ShowEntered()
    {
        if (_enteredText == null)
        {
            return;
        }

        string[] slots = new string[CipherPuzzle.AnswerLength];

        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = i < _enteredCount ? _puzzle.GetLabel(_puzzle.Answers[i]) : EmptySlotText;
        }

        _enteredText.text = string.Join(" ", slots);
    }
}