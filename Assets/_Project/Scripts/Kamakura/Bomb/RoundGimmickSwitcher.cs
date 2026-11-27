using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ラウンドごとにギミックを切り替える。爆弾本体からは、これが1つのギミックに見える（本体-11）
/// 順番は固定しない。すべてのギミックを1回ずつ出し切るまで同じものは出さず、
/// 出し切ったら混ぜ直す。混ぜ直したあとの最初のギミックは、直前のラウンドと同じにならない
/// </summary>
public class RoundGimmickSwitcher : Gimmick
{
    [SerializeField] private BombManager _bombManager;

    // 以前は「1ラウンド目に必ず出すギミック」だった。いまは他と同じ扱い（プレハブを変えないため項目は残している）
    [SerializeField] private Gimmick _firstRoundGimmick;
    [SerializeField] private Gimmick[] _laterRoundGimmicks;

    private readonly List<Gimmick> _order = new List<Gimmick>();
    private Gimmick _current;

    /// <summary>
    /// 今のラウンドで使っているギミック
    /// </summary>
    public Gimmick Current => _current;

    /// <summary>
    /// 今のラウンドのギミックの名前
    /// </summary>
    public override string DisplayName => _current != null ? _current.DisplayName : base.DisplayName;

    /// <summary>
    /// 今のラウンドのギミックの一行説明
    /// </summary>
    public override string Description => _current != null ? _current.Description : base.Description;

    /// <summary>
    /// 液晶に出す問題文
    /// </summary>
    public override string QuestionText => _current != null ? _current.QuestionText : string.Empty;

    /// <summary>
    /// 次に押すと正解の方向（CPUが使う）
    /// </summary>
    public override InputDirection NextAnswer => _current != null ? _current.NextAnswer : InputDirection.Up;

    /// <summary>
    /// 今のラウンドのギミックの、同時押しの受付時間（キーボード）
    /// </summary>
    public override float ChordWindow => _current != null ? _current.ChordWindow : 0f;

    /// <summary>
    /// 今のラウンドのギミックの、同時押しの受付時間（クリック）
    /// </summary>
    public override float ChordMouseWindow => _current != null ? _current.ChordMouseWindow : 0f;

    /// <summary>
    /// 今のラウンドのギミックの、同時押しの答え（CPUが使う）
    /// </summary>
    public override IReadOnlyList<InputDirection> ChordAnswers => _current != null ? _current.ChordAnswers : null;

    /// <summary>
    /// 受付時間のあいだに押されたボタンを、今のギミックに渡す
    /// </summary>
    /// <param name="pressed">ここまでに押された方向</param>
    /// <param name="isWindowClosed">受付時間が終わったか</param>
    /// <returns>入力した結果</returns>
    public override AnswerResult AnswerChord(IReadOnlyList<InputDirection> pressed, bool isWindowClosed)
    {
        if (_current == null)
        {
            return AnswerResult.Continue;
        }

        return _current.AnswerChord(pressed, isWindowClosed);
    }

    /// <summary>
    /// 今のラウンドのギミックの、謎の重さ
    /// </summary>
    public override float ThinkWeight => _current != null ? _current.ThinkWeight : 1f;

    /// <summary>
    /// 今のラウンドのギミックの、定番の間違い
    /// </summary>
    /// <param name="decoy">定番の間違いの方向</param>
    /// <returns>定番の間違いがあれば true</returns>
    public override bool TryGetDecoy(out InputDirection decoy)
    {
        if (_current != null)
        {
            return _current.TryGetDecoy(out decoy);
        }

        decoy = InputDirection.Up;
        return false;
    }

    /// <summary>
    /// 試合が始まる前は、どのギミックも見せない
    /// </summary>
    private void Awake()
    {
        BuildOrder();

        foreach (Gimmick gimmick in _order)
        {
            gimmick.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// ラウンドの最初に呼ばれる。何ラウンド目かを調べて、そのラウンドのギミックに切り替える
    /// </summary>
    public override void ResetRound()
    {
        if (_order.Count == 0)
        {
            Debug.LogError("[RoundGimmickSwitcher] ギミックが1つも入っていません");
            return;
        }

        // 爆弾本体は、勝ち数を数え直してから ResetRound を呼ぶので、勝ち数の合計で何ラウンド目かが分かる
        int roundIndex = _bombManager.GetWins(0) + _bombManager.GetWins(1);

        // 1周するたび（試合の最初も含む）に、順を混ぜ直す
        if (roundIndex % _order.Count == 0)
        {
            BuildOrder();
        }

        Select(_order[roundIndex % _order.Count]);
        _current.ResetRound();
        Debug.Log($"[RoundGimmickSwitcher] {roundIndex + 1}ラウンド目のギミック：{_current.name}");
    }

    /// <summary>
    /// 爆弾を受け取るたびに呼ばれる。今のギミックに問題を作らせる
    /// </summary>
    /// <param name="stage">難易度の段階（1〜4）</param>
    public override void CreatePuzzle(int stage)
    {
        if (_current == null)
        {
            return;
        }

        _current.CreatePuzzle(stage);
    }

    /// <summary>
    /// ボタンが押されたときに呼ばれる。今のギミックに答えを渡す
    /// </summary>
    /// <param name="direction">押された方向</param>
    /// <returns>入力した結果</returns>
    public override AnswerResult Answer(InputDirection direction)
    {
        if (_current == null)
        {
            Debug.LogWarning("[RoundGimmickSwitcher] ResetRound の前に Answer が呼ばれました");
            return AnswerResult.Continue;
        }

        return _current.Answer(direction);
    }

    /// <summary>
    /// ラウンドの順を作る。すべてのギミックをランダムな順に並べる。
    /// 先頭が直前のラウンドのギミックと同じにならないようにする
    /// </summary>
    private void BuildOrder()
    {
        _order.Clear();

        if (_firstRoundGimmick != null)
        {
            _order.Add(_firstRoundGimmick);
        }

        if (_laterRoundGimmicks != null)
        {
            foreach (Gimmick gimmick in _laterRoundGimmicks)
            {
                if (gimmick != null && !_order.Contains(gimmick))
                {
                    _order.Add(gimmick);
                }
            }
        }

        for (int i = _order.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (_order[i], _order[j]) = (_order[j], _order[i]);
        }

        // 前のラウンドと同じギミックが先頭に来たら、ほかのギミックと入れ替える
        if (_order.Count > 1 && _order[0] == _current)
        {
            int swapIndex = Random.Range(1, _order.Count);
            (_order[0], _order[swapIndex]) = (_order[swapIndex], _order[0]);
        }
    }

    /// <summary>
    /// 指定したギミックだけを見せて、ほかは隠す
    /// </summary>
    /// <param name="gimmick">このラウンドで使うギミック</param>
    private void Select(Gimmick gimmick)
    {
        foreach (Gimmick other in _order)
        {
            other.gameObject.SetActive(other == gimmick);
        }

        _current = gimmick;
    }
}
