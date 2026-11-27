using UnityEngine;

/// <summary>
/// クイックテキストの文。方向キー（画面の上・下・左・右）ごとに1文ずつ持つ
/// </summary>
/// <remarks>
/// 文を変えたいときは、この ScriptableObject の Texts を書き換える（空欄の方向は初期の文に戻る）。
/// 並びは「キーを押した側」の画面の位置：上＝W（↑）、下＝S（↓）、左＝A（←）、右＝D（→）。
/// 爆弾の上と下のボタンは入れ替えてあり、W は InputDirection.Down として届くので、InputDirection から文を引くときは GetText を使う
/// </remarks>
[CreateAssetMenu(fileName = "UiQuickTextSet", menuName = "UI/クイックテキストの文")]
public class UiQuickTextSet : ScriptableObject
{
    /// <summary>
    /// 文の数（画面の上・下・左・右）
    /// </summary>
    public const int SlotCount = 4;

    private static readonly string[] DefaultTexts = { "いくぞ！", "ざんねん〜", "まだまだ！", "ナイス！" };

    [SerializeField, Tooltip("文（画面の上＝W、下＝S、左＝A、右＝D の順）")] private string[] _texts = { "いくぞ！", "ざんねん〜", "まだまだ！", "ナイス！" };

    /// <summary>
    /// 方向（キー入力として届いた InputDirection）が、画面の上・下・左・右のどれにあたるかを返す
    /// </summary>
    /// <param name="direction">届いた方向</param>
    /// <returns>0：上（W）、1：下（S）、2：左（A）、3：右（D）</returns>
    public static int SlotOf(InputDirection direction)
    {
        switch (direction)
        {
            case InputDirection.Down:
                return 0;
            case InputDirection.Up:
                return 1;
            case InputDirection.Left:
                return 2;
            default:
                return 3;
        }
    }

    /// <summary>
    /// 方向に割り当てた文を返す。空欄や足りないときは初期の文を返す
    /// </summary>
    /// <param name="direction">届いた方向</param>
    /// <returns>文</returns>
    public string GetText(InputDirection direction)
    {
        return GetSlotText(SlotOf(direction));
    }

    /// <summary>
    /// 画面の位置（上・下・左・右）に割り当てた文を返す。空欄や足りないときは初期の文を返す
    /// </summary>
    /// <param name="slot">0：上、1：下、2：左、3：右</param>
    /// <returns>文</returns>
    public string GetSlotText(int slot)
    {
        if (_texts != null && slot < _texts.Length && !string.IsNullOrEmpty(_texts[slot]))
        {
            return _texts[slot];
        }

        return DefaultTexts[slot];
    }

    /// <summary>
    /// 初期の文を返す（文のセットが入っていないときに使う）
    /// </summary>
    /// <param name="slot">0：上、1：下、2：左、3：右</param>
    /// <returns>初期の文</returns>
    public static string GetDefaultSlotText(int slot)
    {
        return DefaultTexts[slot];
    }
}
