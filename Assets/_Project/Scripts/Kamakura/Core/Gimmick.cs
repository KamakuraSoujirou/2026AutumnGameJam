using UnityEngine;

/// <summary>
/// すべてのギミックの元になるクラス。爆弾本体はこの形だけを知っていればよい
/// </summary>
public abstract class Gimmick : MonoBehaviour
{
    /// <summary>
    /// 液晶に出す問題文
    /// </summary>
    public abstract string QuestionText { get; }

    /// <summary>
    /// 次に押すと正解の方向（CPUが使う）
    /// </summary>
    public abstract InputDirection NextAnswer { get; }

    /// <summary>
    /// ラウンドの最初に呼ばれる
    /// </summary>
    public virtual void ResetRound()
    {
    }

    /// <summary>
    /// 爆弾を受け取るたびに呼ばれる
    /// </summary>
    /// <param name="stage">難易度の段階（1〜4）</param>
    public abstract void CreatePuzzle(int stage);

    /// <summary>
    /// ボタンが押されたときに呼ばれる
    /// </summary>
    /// <param name="direction">押された方向</param>
    /// <returns>入力した結果</returns>
    public abstract AnswerResult Answer(InputDirection direction);
}
