using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ラウンドごとにギミックを切り替える。爆弾本体からは、これが1つのギミックに見える（本体-11）
/// 1ラウンド目は最初のギミック（配線）、2・3ラウンド目は残りをランダムな順で出す（仮）。
/// 4ラウンド目からは、また最初のギミックから同じように繰り返す
/// </summary>
public class RoundGimmickSwitcher : Gimmick
{
    [SerializeField] private BombManager _bombManager;
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

        // 1周するたび（試合の最初も含む）に、2ラウンド目からの順を混ぜ直す
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
    /// ラウンドの順を作る。最初のギミックを先頭にして、残りはランダムな順に並べる
    /// </summary>
    private void BuildOrder()
    {
        _order.Clear();

        List<Gimmick> later = new List<Gimmick>();

        foreach (Gimmick gimmick in _laterRoundGimmicks)
        {
            if (gimmick != null)
            {
                later.Add(gimmick);
            }
        }

        for (int i = later.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (later[i], later[j]) = (later[j], later[i]);
        }

        if (_firstRoundGimmick != null)
        {
            _order.Add(_firstRoundGimmick);
        }

        _order.AddRange(later);
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
