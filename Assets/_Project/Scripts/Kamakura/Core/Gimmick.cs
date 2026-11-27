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
    /// 謎の重さ（0.7〜1.6、初期値1）。COMの考える時間に掛けるので、考えるのに時間がかかる謎ほど大きくする
    /// 上書きしなければ1のまま
    /// </summary>
    public virtual float ThinkWeight => 1f;

    /// <summary>
    /// 今の問題の「定番の間違い」の方向を返す（COMが間違えるとき、ひっかけに引っかかる動きに使う）
    /// 上書きしなければ「なし」で、COMはランダムに間違える
    /// </summary>
    /// <param name="decoy">定番の間違いの方向。正解とは違う方向を返すこと</param>
    /// <returns>定番の間違いがあれば true</returns>
    public virtual bool TryGetDecoy(out InputDirection decoy)
    {
        decoy = InputDirection.Up;
        return false;
    }

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
