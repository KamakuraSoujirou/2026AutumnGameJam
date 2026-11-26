using System.Text;
using UnityEngine;

/// <summary>
/// いつも同じ問題を出す仮のギミック。本物のギミックができるまで、爆弾本体の確認に使う
/// </summary>
public class FixedTestGimmick : Gimmick
{
    [SerializeField] private InputDirection[] _answers = { InputDirection.Up, InputDirection.Right };

    private int _inputCount;

    /// <summary>
    /// 液晶に出す問題文（日本語フォントが入るまでは英語で出す）
    /// </summary>
    public override string QuestionText => BuildQuestionText();

    /// <summary>
    /// 次に押すと正解の方向
    /// </summary>
    public override InputDirection NextAnswer => _answers[_inputCount];

    /// <summary>
    /// ラウンドの最初に入力をなかったことにする
    /// </summary>
    public override void ResetRound()
    {
        _inputCount = 0;
    }

    /// <summary>
    /// 問題はいつも同じなので、入力だけを最初に戻す
    /// </summary>
    /// <param name="stage">難易度の段階（1〜4）。このギミックでは使わない</param>
    public override void CreatePuzzle(int stage)
    {
        _inputCount = 0;
    }

    /// <summary>
    /// 押された方向が、決めておいた順番と合っているかを調べる
    /// </summary>
    /// <param name="direction">押された方向</param>
    /// <returns>途中まで合えば Continue、最後まで合えば Correct、違えば Wrong</returns>
    public override AnswerResult Answer(InputDirection direction)
    {
        if (direction != _answers[_inputCount])
        {
            _inputCount = 0;
            return AnswerResult.Wrong;
        }

        _inputCount++;

        if (_inputCount >= _answers.Length)
        {
            _inputCount = 0;
            return AnswerResult.Correct;
        }

        return AnswerResult.Continue;
    }

    /// <summary>
    /// 「UP > RIGHT」のような問題文を作る。入力済みの方向には印を付ける
    /// </summary>
    /// <returns>問題文</returns>
    private string BuildQuestionText()
    {
        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < _answers.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(" > ");
            }

            string name = _answers[i].ToString().ToUpper();
            builder.Append(i < _inputCount ? $"[{name}]" : name);
        }

        return builder.ToString();
    }
}
