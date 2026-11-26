using System;
using UnityEngine;

/// <summary>
/// 爆弾本体の進行役。2人の入力をギミックに渡し、パス・時間切れ・勝敗（何本先取かは GameConfig）を管理する
/// キーやマウスは直接読まない。入力は TryPress と TryRematch で受け取り、受け付けるかどうかはここで決める
/// </summary>
public class BombManager : MonoBehaviour
{
    /// <summary>プレイヤーの人数</summary>
    public const int PlayerCount = 2;

    /// <summary>まだ決まっていないことを表す番号</summary>
    public const int NoPlayer = -1;

    [SerializeField] private BombTimer _bombTimer;
    [SerializeField] private Gimmick _gimmick;

    private readonly int[] _wins = new int[PlayerCount];

    private GameConfig _config;
    private BombState _state;
    private int _holder;
    private int _flightTarget;
    private int _receiveCount;
    private int _stage;
    private int _roundWinner = NoPlayer;
    private int _matchWinner = NoPlayer;
    private float _holdTime;
    private float _phaseTimer;
    private float _phaseDuration;
    private float _lockTimer;
    private float _stunTimer;

    /// <summary>
    /// 表示を変えるべきこと（持ち手・問題文・勝ち数・状態など）が起きたときに呼ばれる
    /// </summary>
    public event Action OnChanged;

    /// <summary>
    /// 入力を受け付けたときに、ギミックに渡す前に呼ばれる（ボタンを押した動き用）
    /// </summary>
    public event Action<InputDirection> OnPressed;

    /// <summary>
    /// 不正解のときに呼ばれる（間違えたプレイヤーの番号と、減る秒数）
    /// </summary>
    public event Action<int, float> OnMissed;

    /// <summary>今の持ち時間（秒）。受け取るたびにこの秒数に戻す。時間短縮が ON なら、パスが進むと短くなる</summary>
    public float HoldTime => _holdTime;

    /// <summary>爆弾が相手に飛んでいく長さ（秒）</summary>
    public float FlightDuration => _config.FlightDuration;

    /// <summary>何ラウンド取れば試合に勝つか</summary>
    public int WinsToMatch => _config.WinsToMatch;

    /// <summary>今の進行の状態</summary>
    public BombState State => _state;

    /// <summary>爆弾を持っている人（0 が P1、1 が P2）</summary>
    public int Holder => _holder;

    /// <summary>飛んでいる爆弾を受け取る人</summary>
    public int FlightTarget => _flightTarget;

    /// <summary>そのラウンドで何回目の受け取りか</summary>
    public int ReceiveCount => _receiveCount;

    /// <summary>今の問題の難易度（1〜4）</summary>
    public int Stage => _stage;

    /// <summary>直前のラウンドの勝者。決まっていなければ NoPlayer</summary>
    public int RoundWinner => _roundWinner;

    /// <summary>試合の勝者。決まっていなければ NoPlayer</summary>
    public int MatchWinner => _matchWinner;

    /// <summary>不正解のスタン中か</summary>
    public bool IsStunned => _stunTimer > 0f;

    /// <summary>今の問題文</summary>
    public string QuestionText => _gimmick.QuestionText;

    /// <summary>今のギミックの名前（空なら決まっていない）</summary>
    public string GimmickName => _gimmick.DisplayName;

    /// <summary>今のギミックの一行説明</summary>
    public string GimmickDescription => _gimmick.Description;

    /// <summary>今の待ち時間（開始・飛行・爆発）がどこまで進んだか（0〜1）</summary>
    public float PhaseProgress => _phaseDuration > 0f ? 1f - Mathf.Clamp01(_phaseTimer / _phaseDuration) : 1f;

    /// <summary>
    /// 指定したプレイヤーの勝ち数を返す
    /// </summary>
    /// <param name="player">プレイヤーの番号（0 か 1）</param>
    /// <returns>取ったラウンドの数</returns>
    public int GetWins(int player)
    {
        return _wins[player];
    }

    /// <summary>
    /// 方向の入力を受け取る。持っている人の入力だけを受け付け、受け取った直後とスタン中は受け付けない
    /// </summary>
    /// <param name="player">押したプレイヤーの番号（0 か 1）</param>
    /// <param name="direction">押された方向</param>
    /// <returns>受け付けたら true</returns>
    public bool TryPress(int player, InputDirection direction)
    {
        if (_state != BombState.Answering || player != _holder || _lockTimer > 0f || _stunTimer > 0f)
        {
            return false;
        }

        OnPressed?.Invoke(direction);
        Press(direction);
        return true;
    }

    /// <summary>
    /// 試合が終わっていれば、もう一度始める
    /// </summary>
    /// <returns>始めたら true</returns>
    public bool TryRematch()
    {
        if (_state != BombState.MatchOver)
        {
            return false;
        }

        StartMatch();
        return true;
    }

    /// <summary>
    /// 設定を読む
    /// </summary>
    private void Awake()
    {
        _config = GameConfig.Load();
    }

    /// <summary>
    /// 時間切れの知らせを受け取れるようにする
    /// </summary>
    private void OnEnable()
    {
        _bombTimer.OnExploded += Explode;
    }

    /// <summary>
    /// 時間切れの知らせを受け取るのをやめる
    /// </summary>
    private void OnDisable()
    {
        _bombTimer.OnExploded -= Explode;
    }

    /// <summary>
    /// 最初の試合を始める
    /// </summary>
    private void Start()
    {
        StartMatch();
    }

    /// <summary>
    /// 状態ごとに、待ち時間を進める
    /// </summary>
    private void Update()
    {
        switch (_state)
        {
            case BombState.RoundStart:
                if (TickPhase())
                {
                    Receive(_holder);
                }
                break;
            case BombState.Answering:
                TickInputLock();
                break;
            case BombState.Flying:
                if (TickPhase())
                {
                    Receive(_flightTarget);
                }
                break;
            case BombState.Exploding:
                if (TickPhase())
                {
                    FinishRound();
                }
                break;
        }
    }

    /// <summary>
    /// 勝ち数を0に戻して、先攻をランダムに決めて試合を始める
    /// </summary>
    private void StartMatch()
    {
        for (int i = 0; i < PlayerCount; i++)
        {
            _wins[i] = 0;
        }

        _matchWinner = NoPlayer;
        StartRound(UnityEngine.Random.Range(0, PlayerCount));
    }

    /// <summary>
    /// ラウンドを始める。開始の待ち時間のあと、先攻が爆弾を受け取る
    /// </summary>
    /// <param name="firstPlayer">先攻のプレイヤー</param>
    private void StartRound(int firstPlayer)
    {
        _holder = firstPlayer;
        _flightTarget = firstPlayer;
        _receiveCount = 0;
        _stage = 0;
        _roundWinner = NoPlayer;
        _stunTimer = 0f;
        _lockTimer = 0f;
        _gimmick.ResetRound();
        _holdTime = _bombTimer.TimeLimit;
        _bombTimer.ResetTime(_holdTime);
        BeginPhase(BombState.RoundStart, _config.RoundStartDuration);
        Debug.Log($"[BombManager] ラウンド開始 先攻：P{firstPlayer + 1}（P1 {_wins[0]} - {_wins[1]} P2）");
    }

    /// <summary>
    /// 爆弾を受け取る。パス回数に合った持ち時間に戻し（時間短縮）、難易度に合った問題を作る
    /// </summary>
    /// <param name="player">受け取るプレイヤー</param>
    private void Receive(int player)
    {
        _holder = player;
        _receiveCount++;
        _stage = DifficultyCalculator.GetStage(_receiveCount, _config.StagePasses);
        _gimmick.CreatePuzzle(_stage);
        _lockTimer = _config.ReceiveLockDuration;
        _stunTimer = 0f;
        _holdTime = _bombTimer.GetHoldTime(_receiveCount);
        _bombTimer.ResetTime(_holdTime);
        _bombTimer.StartTimer();
        BeginPhase(BombState.Answering, 0f);
        Debug.Log($"[BombManager] P{player + 1} が受け取った（{_receiveCount}回目、難易度{_stage}、持ち時間{_holdTime}秒）");
    }

    /// <summary>
    /// 受け取った直後とスタン中の、入力できない時間を進める
    /// </summary>
    private void TickInputLock()
    {
        if (_lockTimer > 0f)
        {
            _lockTimer -= Time.deltaTime;
        }

        if (_stunTimer > 0f)
        {
            _stunTimer -= Time.deltaTime;

            if (_stunTimer <= 0f)
            {
                OnChanged?.Invoke();
            }
        }
    }

    /// <summary>
    /// ギミックに答えを渡し、結果に合わせてパス・ペナルティを行う
    /// </summary>
    /// <param name="direction">押された方向</param>
    private void Press(InputDirection direction)
    {
        AnswerResult result = _gimmick.Answer(direction);

        switch (result)
        {
            case AnswerResult.Correct:
                Pass();
                break;
            case AnswerResult.Wrong:
                Debug.Log($"[BombManager] P{_holder + 1} 不正解（{direction}）");
                _stunTimer = _config.StunDuration;
                OnMissed?.Invoke(_holder, _config.WrongPenalty);
                OnChanged?.Invoke();
                _bombTimer.Subtract(_config.WrongPenalty);
                break;
            case AnswerResult.Continue:
                OnChanged?.Invoke();
                break;
        }
    }

    /// <summary>
    /// 正解したので、爆弾を相手に飛ばす。飛んでいる間は時間を止める
    /// </summary>
    private void Pass()
    {
        _bombTimer.StopTimer();
        _flightTarget = GetOpponent(_holder);
        Debug.Log($"[BombManager] P{_holder + 1} 正解 → P{_flightTarget + 1} へパス");
        BeginPhase(BombState.Flying, _config.FlightDuration);
    }

    /// <summary>
    /// 時間切れで爆発する。持っていた人の負けで、相手がラウンドを取る
    /// </summary>
    private void Explode()
    {
        if (_state != BombState.Answering)
        {
            return;
        }

        _stunTimer = 0f;
        _roundWinner = GetOpponent(_holder);
        _wins[_roundWinner]++;
        Debug.Log($"[BombManager] 爆発！ P{_holder + 1} の負け（P1 {_wins[0]} - {_wins[1]} P2）");
        BeginPhase(BombState.Exploding, _config.ExplosionDuration);
    }

    /// <summary>
    /// 爆発の演出のあと、試合が決まったか調べる。決まっていなければ勝者を先攻にして次のラウンドへ
    /// </summary>
    private void FinishRound()
    {
        if (_wins[_roundWinner] >= _config.WinsToMatch)
        {
            _matchWinner = _roundWinner;
            Debug.Log($"[BombManager] 試合終了 P{_matchWinner + 1} の勝ち（P1 {_wins[0]} - {_wins[1]} P2）");
            BeginPhase(BombState.MatchOver, 0f);
            return;
        }

        StartRound(_roundWinner);
    }

    /// <summary>
    /// 状態を切り替え、待ち時間を設定して、表示に知らせる
    /// </summary>
    /// <param name="state">新しい状態</param>
    /// <param name="duration">待ち時間（秒）。待たない状態では0</param>
    private void BeginPhase(BombState state, float duration)
    {
        _state = state;
        _phaseDuration = duration;
        _phaseTimer = duration;
        OnChanged?.Invoke();
    }

    /// <summary>
    /// 待ち時間を進める
    /// </summary>
    /// <returns>待ち時間が終わったら true</returns>
    private bool TickPhase()
    {
        _phaseTimer -= Time.deltaTime;
        return _phaseTimer <= 0f;
    }

    /// <summary>
    /// 相手のプレイヤーの番号を返す
    /// </summary>
    /// <param name="player">プレイヤーの番号</param>
    /// <returns>相手の番号</returns>
    private int GetOpponent(int player)
    {
        return (player + 1) % PlayerCount;
    }
}
