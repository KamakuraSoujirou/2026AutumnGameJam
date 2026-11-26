/// <summary>
/// ギミックに入力した結果
/// </summary>
public enum AnswerResult
{
    /// <summary>途中まで合っている。爆弾本体は何もしない</summary>
    Continue,

    /// <summary>問題を解き終えた。爆弾本体は相手に爆弾を渡す</summary>
    Correct,

    /// <summary>間違えた。爆弾本体は時間を減らしてスタンさせる</summary>
    Wrong
}
