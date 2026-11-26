using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 爆弾本体の進行役。2人の入力をギミックに渡し、パス・時間切れ・勝敗（2本先取）を管理する
/// 操作：P1 は WASD、P2 は矢印キー。試合が終わったら Space でもう一度
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
    private float _phaseTimer;
    private float _phaseDuration;
    private float _lockTimer;
    private float _stunTimer;

    /// <summary>
    /// 表示を変えるべきこと（持ち手・問題文・勝ち数・状態など）が起きたときに呼ばれる
    /// </summary>
    public event Action OnChanged;

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
    /// 状態ごとに、待ち時間を進めるか入力を受け取る
    /// </summary>
    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        switch (_state)
        {
            case BombState.RoundStart:
                if (TickPhase())
                {
                    Receive(_holder);
                }
                break;
            case BombState.Answering:
                UpdateAnswering(keyboard);
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
            case BombState.MatchOver:
                if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
                {
                    StartMatch();
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
        _bombTimer.ResetTime();
        BeginPhase(BombState.RoundStart, _config.RoundStartDuration);
        Debug.Log($"[BombManager] ラウンド開始 先攻：P{firstPlayer + 1}（P1 {_wins[0]} - {_wins[1]} P2）");
    }

    /// <summary>
    /// 爆弾を受け取る。持ち時間を戻し、パス回数に合った難易度の問題を作る
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
        _bombTimer.ResetTime();
        _bombTimer.StartTimer();
        BeginPhase(BombState.Answering, 0f);
        Debug.Log($"[BombManager] P{player + 1} が受け取った（{_receiveCount}回目、難易度{_stage}）");
    }

    /// <summary>
    /// 持っている人の入力だけをギミックに渡す。受け取った直後とスタン中は受け付けない
    /// </summary>
    /// <param name="keyboard">キーボード</param>
    private void UpdateAnswering(Keyboard keyboard)
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

        if (keyboard == null || _lockTimer > 0f || _stunTimer > 0f)
        {
            return;
        }

        if (TryGetDirection(keyboard, _holder, out InputDirection direction))
        {
            Press(direction);
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

    /// <summary>
    /// そのプレイヤーのキーで、この瞬間に押された方向を調べる（P1 は WASD、P2 は矢印キー）
    /// </summary>
    /// <param name="keyboard">キーボード</param>
    /// <param name="player">プレイヤーの番号</param>
    /// <param name="direction">押された方向</param>
    /// <returns>方向キーが押されていれば true</returns>
    private bool TryGetDirection(Keyboard keyboard, int player, out InputDirection direction)
    {
        bool isP1 = player == 0;
        direction = InputDirection.Up;

        if ((isP1 ? keyboard.wKey : keyboard.upArrowKey).wasPressedThisFrame)
        {
            direction = InputDirection.Up;
            return true;
        }

        if ((isP1 ? keyboard.sKey : keyboard.downArrowKey).wasPressedThisFrame)
        {
            direction = InputDirection.Down;
            return true;
        }

        if ((isP1 ? keyboard.aKey : keyboard.leftArrowKey).wasPressedThisFrame)
        {
            direction = InputDirection.Left;
            return true;
        }

        if ((isP1 ? keyboard.dKey : keyboard.rightArrowKey).wasPressedThisFrame)
        {
            direction = InputDirection.Right;
            return true;
        }

        return false;
    }
}
