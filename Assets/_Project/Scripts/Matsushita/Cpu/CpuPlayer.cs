using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// P2の席に座るCOM。爆弾を持ったら、考える時間を置いてから、人間と同じ入口（BombManager.TryPress）で入力する
/// 謎は解かない。正解（Gimmick.NextAnswer）を知っている前提で、考える時間と正答率だけで動かす（HTML版プロトタイプと同じ）
/// </summary>
/// <remarks>
/// 考える時間は謎の重さ（Gimmick.ThinkWeight）で伸び縮みし、間違えるときは定番の間違い（Gimmick.TryGetDecoy）を選びやすい。
/// 取り付け：Game シーンの空のオブジェクトに付けて、参照を3つ入れる
/// </remarks>
public class CpuPlayer : MonoBehaviour
{
    /// <summary>COMが座る席（P2）</summary>
    private const int CpuIndex = 1;

    /// <summary>入力がまだ受け付けられなかったとき、もう一度試すまでの時間（秒）</summary>
    private const float RetryDelay = 0.05f;

    /// <summary>待機中に、COMがクイックテキストを出す間隔の下限と上限（秒）。ときどき1文出す程度にする</summary>
    private const float QuickTextMinInterval = 5f;
    private const float QuickTextMaxInterval = 11f;

    private static readonly InputDirection[] AllDirections =
    {
        InputDirection.Up,
        InputDirection.Down,
        InputDirection.Left,
        InputDirection.Right
    };

    [SerializeField, Tooltip("爆弾本体の進行役")] private BombManager _bombManager;
    [SerializeField, Tooltip("今のラウンドのギミック（BombManager に付けているものと同じ RoundGimmickSwitcher）")] private Gimmick _gimmick;
    [SerializeField, Tooltip("試合画面（「…」を出す）")] private UiGameView _view;
    [SerializeField, Tooltip("テスト用：タイトルを通さずに Game シーンを直接再生するとき、CPU対戦にする。ふだんは切っておく")] private bool _forceCpu;
    [SerializeField, Tooltip("テスト用：上をオンにしたときの強さ")] private CpuLevel _forcedLevel = CpuLevel.Normal;

    // 間違えるときに選べない方向（その問題で押して間違えた方向、順番スイッチで押し済みの方向）
    private readonly List<InputDirection> _triedWrong = new List<InputDirection>();
    private readonly List<InputDirection> _pressedCorrect = new List<InputDirection>();
    private readonly List<InputDirection> _candidates = new List<InputDirection>();
    private readonly List<InputDirection> _chordButtons = new List<InputDirection>();

    private CpuProfile _profile;
    private float _lockWait;
    private float _thinkTimer;
    private float _thinkWeight = 1f;
    private float _quickTextTimer;
    private bool _isPlanned;
    private bool _hasMissed;
    private InputDirection _lastPressed;

    /// <summary>
    /// 設定を読む。CPU対戦でなければ、何もしない
    /// </summary>
    private void Awake()
    {
        if (_forceCpu)
        {
            CpuSettings.Set(true, _forcedLevel);
        }

        if (!CpuSettings.IsCpuMode)
        {
            enabled = false;
            return;
        }

        _profile = CpuProfile.Get(CpuSettings.Level);
        _quickTextTimer = Random.Range(QuickTextMinInterval, QuickTextMaxInterval);
    }

    /// <summary>
    /// 間違えた知らせを受け取れるようにする
    /// </summary>
    private void OnEnable()
    {
        _bombManager.OnMissed += OnMissed;
    }

    /// <summary>
    /// 間違えた知らせを受け取るのをやめる
    /// </summary>
    private void OnDisable()
    {
        _bombManager.OnMissed -= OnMissed;
    }

    /// <summary>
    /// COMが爆弾を持って答えている間、考える時間を数えて、来たら入力する
    /// </summary>
    private void Update()
    {
        bool isCpuTurn = _bombManager.State == BombState.Answering && _bombManager.Holder == CpuIndex;

        if (!isCpuTurn)
        {
            if (_isPlanned)
            {
                EndTurn();
            }

            UpdateQuickText();
            return;
        }

        if (!_isPlanned)
        {
            BeginTurn();
        }

        // 受け取った直後の入力ロックと、不正解のスタンの間は、考える時間も進めない
        if (_lockWait > 0f)
        {
            _lockWait -= Time.deltaTime;
            return;
        }

        if (_bombManager.IsStunned)
        {
            return;
        }

        _thinkTimer -= Time.deltaTime;

        if (_thinkTimer <= 0f)
        {
            Act();
        }
    }

    /// <summary>
    /// 人間が爆弾を持って答えている間（COMは待機中）、ときどきクイックテキストを1文出す
    /// </summary>
    private void UpdateQuickText()
    {
        if (_bombManager.State != BombState.Answering)
        {
            return;
        }

        _quickTextTimer -= Time.deltaTime;

        if (_quickTextTimer > 0f)
        {
            return;
        }

        _view.ShowRandomQuickText(CpuIndex);
        _quickTextTimer = Random.Range(QuickTextMinInterval, QuickTextMaxInterval);
    }

    /// <summary>
    /// 爆弾を受け取ったので、考え始める
    /// </summary>
    private void BeginTurn()
    {
        _isPlanned = true;
        _triedWrong.Clear();
        _pressedCorrect.Clear();
        _lockWait = GameConfig.Load().ReceiveLockDuration;
        _thinkWeight = _gimmick.ThinkWeight;
        StartThinking(false);
    }

    /// <summary>
    /// 爆弾を渡したか、ラウンドが終わったので、吹き出しの「…」を消す
    /// </summary>
    private void EndTurn()
    {
        _isPlanned = false;
        _view.SetThinking(CpuIndex, false);
    }

    /// <summary>
    /// 次の入力までの考える時間を決めて、吹き出しを「…」にする
    /// </summary>
    /// <param name="isFollowUp">正解を押したあとの続きの入力か</param>
    private void StartThinking(bool isFollowUp)
    {
        _thinkTimer = _profile.RollThinkTime(_bombManager.Stage, _thinkWeight, isFollowUp);
        _view.SetThinking(CpuIndex, true);
    }

    /// <summary>
    /// 考える時間が終わったので、1回入力する
    /// </summary>
    private void Act()
    {
        IReadOnlyList<InputDirection> chordAnswers = _gimmick.ChordAnswers;

        if (chordAnswers != null && chordAnswers.Count > 0)
        {
            ActChord(chordAnswers);
            return;
        }

        InputDirection expected = _gimmick.NextAnswer;
        InputDirection direction = expected;

        if (!_profile.RollIsCorrect() && TryPickWrong(expected, out InputDirection wrong))
        {
            direction = wrong;
        }

        _lastPressed = direction;
        _hasMissed = false;

        if (!_bombManager.TryPress(CpuIndex, direction))
        {
            _thinkTimer = RetryDelay;
            return;
        }

        // 間違えたときは OnMissed の中で考え直している。正解で、まだ入力が続くときは、0.3秒後にまた押す
        if (!_hasMissed && _bombManager.State == BombState.Answering)
        {
            _pressedCorrect.Add(direction);
            StartThinking(true);
        }
    }

    /// <summary>
    /// 同時押しのギミックで、答えのボタンを同じフレームに全部押す。間違えるときは、答えのうち1つを別のボタンに替える
    /// （替えたボタンは最後に押すので、その時点で不正解になる）
    /// </summary>
    /// <param name="answers">同時押しの答えの方向</param>
    private void ActChord(IReadOnlyList<InputDirection> answers)
    {
        _chordButtons.Clear();

        for (int i = 0; i < answers.Count; i++)
        {
            _chordButtons.Add(answers[i]);
        }

        if (!_profile.RollIsCorrect() && TryPickChordWrong(answers, out InputDirection wrong))
        {
            // 答えのうち1つを、間違いのボタンに替える
            _chordButtons.RemoveAt(Random.Range(0, _chordButtons.Count));
            _chordButtons.Add(wrong);
        }

        _hasMissed = false;

        // 最初のボタンが受け付けられなければ、まだ何も押していないので、少し待ってやり直す
        if (!_bombManager.TryPress(CpuIndex, _chordButtons[0]))
        {
            _thinkTimer = RetryDelay;
            return;
        }

        _lastPressed = _chordButtons[0];

        for (int i = 1; i < _chordButtons.Count; i++)
        {
            _lastPressed = _chordButtons[i];
            _bombManager.TryPress(CpuIndex, _chordButtons[i]);
        }
    }

    /// <summary>
    /// 同時押しで間違えるときに、答えの1つと入れ替える、答えにないボタンを選ぶ
    /// </summary>
    /// <param name="answers">同時押しの答えの方向</param>
    /// <param name="wrong">選んだ間違いの方向</param>
    /// <returns>選べたら true（答えにないボタンがすべて試し済みなら false。そのときは正解を押す）</returns>
    private bool TryPickChordWrong(IReadOnlyList<InputDirection> answers, out InputDirection wrong)
    {
        _candidates.Clear();

        foreach (InputDirection direction in AllDirections)
        {
            bool isAnswer = false;

            for (int i = 0; i < answers.Count; i++)
            {
                if (answers[i] == direction)
                {
                    isAnswer = true;
                }
            }

            if (!isAnswer && !_triedWrong.Contains(direction))
            {
                _candidates.Add(direction);
            }
        }

        if (_candidates.Count == 0)
        {
            wrong = InputDirection.Up;
            return false;
        }

        wrong = _candidates[Random.Range(0, _candidates.Count)];
        return true;
    }

    /// <summary>
    /// 間違える方向を選ぶ。定番の間違いがあれば、確率でそれを選ぶ
    /// </summary>
    /// <param name="expected">正解の方向</param>
    /// <param name="wrong">選んだ間違いの方向</param>
    /// <returns>選べたら true（選べる方向が残っていなければ false。そのときは正解を押す）</returns>
    private bool TryPickWrong(InputDirection expected, out InputDirection wrong)
    {
        _candidates.Clear();

        foreach (InputDirection direction in AllDirections)
        {
            if (direction != expected && !_triedWrong.Contains(direction) && !_pressedCorrect.Contains(direction))
            {
                _candidates.Add(direction);
            }
        }

        if (_candidates.Count == 0)
        {
            wrong = expected;
            return false;
        }

        if (_gimmick.TryGetDecoy(out InputDirection decoy) && _candidates.Contains(decoy) && _profile.RollUsesDecoy())
        {
            wrong = decoy;
            return true;
        }

        wrong = _candidates[Random.Range(0, _candidates.Count)];
        return true;
    }

    /// <summary>
    /// COMが間違えたので、押した方向を覚えて、最初の入力から考え直す
    /// </summary>
    /// <param name="player">間違えたプレイヤーの番号</param>
    /// <param name="seconds">減る秒数</param>
    private void OnMissed(int player, float seconds)
    {
        if (player != CpuIndex || !_isPlanned)
        {
            return;
        }

        _hasMissed = true;
        _triedWrong.Add(_lastPressed);
        _pressedCorrect.Clear();
        StartThinking(false);
    }
}
