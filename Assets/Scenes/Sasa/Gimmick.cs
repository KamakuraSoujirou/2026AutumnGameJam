using UnityEngine;

/// <summary>
/// 方向の入力
/// </summary>
public enum InputDirection { Up, Down, Left, Right }

/// <summary>
/// 入力した結果
/// </summary>
public enum AnswerResult { Continue, Correct, Wrong }

/// <summary>
/// すべてのギミックの元になるクラス
/// </summary>
public abstract class Gimmick : MonoBehaviour
{
    /// <summary>液晶に出す問題文</summary>
    public abstract string QuestionText { get; }

    /// <summary>次に押すと正解の方向（CPUが使う）</summary>
    public abstract InputDirection NextAnswer { get; }

    /// <summary>ラウンドの最初に呼ばれる</summary>
    public virtual void ResetRound() { }

    /// <summary>爆弾を受け取るたびに呼ばれる。stageは難易度の1〜4</summary>
    public abstract void CreatePuzzle(int stage);

    /// <summary>ボタンが押されたときに呼ばれる</summary>
    public abstract AnswerResult Answer(InputDirection direction);
}