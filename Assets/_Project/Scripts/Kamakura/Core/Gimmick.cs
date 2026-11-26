using UnityEngine;

/// <summary>
/// すべてのギミックの元になるクラス。爆弾本体はこの形だけを知っていればよい
/// </summary>
public abstract class Gimmick : MonoBehaviour
{
    [SerializeField, Tooltip("ラウンド開始のバナーと画面下に出すギミックの名前（例：暗号ロック式）")] private string _displayName;
    [SerializeField, Tooltip("ラウンド開始のバナーに出す一行説明")] private string _description;

    /// <summary>
    /// ギミックの名前（空なら、画面側の仮の名前を出す）
    /// </summary>
    public virtual string DisplayName => _displayName;

    /// <summary>
    /// ギミックの一行説明
    /// </summary>
    public virtual string Description => _description;

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
