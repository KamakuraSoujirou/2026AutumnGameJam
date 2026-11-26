using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 試合画面の確認用。ゲームの進行がまだなくても、キー操作で表示と演出を一通り確かめられる
/// </summary>
/// <remarks>
/// 本番のゲーム進行が入ったら、このコンポーネントは外す
/// </remarks>
[RequireComponent(typeof(UiGameView))]
public class UiGamePreview : MonoBehaviour
{
    private const float HoldTime = 10f;
    private const float MissPenalty = 2f;
    private const float FlightTime = 0.4f;
    private const int WinsToMatch = 2;
    private const int DefuseMinPass = 5;
    private const float IntroTime = 1.8f;

    private static readonly string[] Questions =
    {
        "赤い★の線を切れ",
        "青いインクで「あか」\n文字の意味の線を切れ",
        "「空と海に共通する色」の線を切れ",
        "シリアルが偶数なら赤、\n奇数なら青の線を切れ"
    };

    private UiGameView _view;
    private PlayerInputReader _reader;
    private float _remainingTime = HoldTime;
    private float _introRemaining;
    private int _holderIndex;
    private int _passCount = 1;
    private int _player1Wins;
    private int _player2Wins;
    private bool _isFlying;
    private bool _isExploded;
    private bool _isDefusing;
    private bool _isDefuseUsed;

    /// <summary>
    /// 表示を取得して、最初の状態にする
    /// </summary>
    private void Awake()
    {
        _view = GetComponent<UiGameView>();
        _view.DefuseRequested += ToggleDefuse;
        _view.DirectionClicked += OnDirectionClicked;
        _reader = GetComponent<PlayerInputReader>();

        if (_reader != null)
        {
            _reader.DirectionPressed += OnDirectionPressed;
            _reader.DefusePressed += OnDefusePressed;
            _reader.BackPressed += _view.RequestBack;
        }
    }

    /// <summary>
    /// 結びつけた処理を外す
    /// </summary>
    private void OnDestroy()
    {
        if (_view != null)
        {
            _view.DefuseRequested -= ToggleDefuse;
            _view.DirectionClicked -= OnDirectionClicked;
        }

        if (_reader != null)
        {
            _reader.DirectionPressed -= OnDirectionPressed;
            _reader.DefusePressed -= OnDefusePressed;
        }
    }

    /// <summary>
    /// 最初の状態を表示して、ラウンド開始のバナーを出す
    /// </summary>
    private void Start()
    {
        StartRound();
    }

    /// <summary>
    /// 時間を減らし、キー操作で演出を切り替える
    /// </summary>
    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (_introRemaining > 0f)
        {
            _introRemaining -= Time.deltaTime;

            if (_introRemaining <= 0f)
            {
                _view.HideBanner();
            }
        }
        else if (!_isFlying && !_isExploded)
        {
            _remainingTime -= Time.deltaTime;
            _view.SetTime(_remainingTime);

            if (_remainingTime <= 0f)
            {
                Explode();
            }
        }

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            PassBomb();
        }

        if (keyboard.xKey.wasPressedThisFrame && !_isFlying && !_isExploded)
        {
            _remainingTime -= MissPenalty;
            _view.ShowPenalty(_holderIndex, MissPenalty);
        }

        if (keyboard.qKey.wasPressedThisFrame)
        {
            ToggleDefuse();
        }

        if (keyboard.bKey.wasPressedThisFrame && !_isExploded)
        {
            Explode();
        }

        if (keyboard.rKey.wasPressedThisFrame)
        {
            StartRound();
        }

        if (keyboard.tKey.wasPressedThisFrame)
        {
            _view.SetThinking(_holderIndex, true);
        }

        if (keyboard.enterKey.wasPressedThisFrame)
        {
            _view.ShowResult(_holderIndex, _player1Wins, _player2Wins);
        }
    }

    /// <summary>
    /// 方向が押されたとき、持っている側ならボタンを押した動きを見せる（持っていない側の入力は無視する）
    /// </summary>
    /// <param name="player">押したプレイヤー</param>
    /// <param name="direction">押した方向</param>
    private void OnDirectionPressed(int player, InputDirection direction)
    {
        if (player != _holderIndex || _isFlying || _isExploded || _introRemaining > 0f)
        {
            return;
        }

        _view.Bomb.PressButton(direction);
    }

    /// <summary>
    /// ひし形ボタンがクリックされたとき、持っている側の入力として扱う
    /// </summary>
    /// <param name="direction">クリックされた方向</param>
    private void OnDirectionClicked(InputDirection direction)
    {
        OnDirectionPressed(_holderIndex, direction);
    }

    /// <summary>
    /// 解除が押されたとき、持っている側なら解除を始める／終える
    /// </summary>
    /// <param name="player">押したプレイヤー</param>
    private void OnDefusePressed(int player)
    {
        if (player == _holderIndex)
        {
            ToggleDefuse();
        }
    }

    /// <summary>
    /// ラウンドを最初の状態に戻す
    /// </summary>
    private void StartRound()
    {
        _view.ResetEffects();
        _isFlying = false;
        _isExploded = false;
        _isDefusing = false;
        _isDefuseUsed = false;
        _passCount = 1;
        _remainingTime = HoldTime;
        _holderIndex = Random.Range(0, 2);

        _view.SetRound(1);
        _view.SetPlayerNames("P1", "COM");
        _view.SetGimmickName("配線切断式");
        _view.SetWins(_player1Wins, _player2Wins, WinsToMatch);
        _view.SetHoldTime(HoldTime);
        _view.SetHolder(_holderIndex);
        _view.SetTime(_remainingTime);
        FillBomb();
        _view.ShowBanner("ROUND 1", "配線切断式", "正しい線を1本切れ！");
        _introRemaining = IntroTime;
    }

    /// <summary>
    /// 爆弾を相手に渡す。飛んでいる間は時間が減らない
    /// </summary>
    private void PassBomb()
    {
        if (_isFlying || _isExploded)
        {
            return;
        }

        if (_isDefusing)
        {
            ToggleDefuse();
        }

        _isFlying = true;
        _view.SetThinking(_holderIndex, false);
        _view.Bomb.SetWire(InputDirection.Up, UiWireColor.Red, UiWireState.Cut);
        int nextHolder = 1 - _holderIndex;
        _view.ThrowBomb(nextHolder, FlightTime, () => OnBombArrived(nextHolder));
    }

    /// <summary>
    /// 爆弾が届いたら、時間を戻して新しい問題を出す
    /// </summary>
    /// <param name="holderIndex">受け取った側</param>
    private void OnBombArrived(int holderIndex)
    {
        _isFlying = false;
        _holderIndex = holderIndex;
        _passCount++;
        _remainingTime = HoldTime;
        _view.SetHolder(_holderIndex);
        _view.SetTime(_remainingTime);
        FillBomb();
    }

    /// <summary>
    /// 解除の挑戦を始める／終える（5回目のパスから、1ラウンド1回）
    /// </summary>
    private void ToggleDefuse()
    {
        if (_isExploded || _isFlying)
        {
            return;
        }

        if (_isDefusing)
        {
            _isDefusing = false;
            _view.SetDefuse(_holderIndex, false);
            return;
        }

        if (_passCount < DefuseMinPass || _isDefuseUsed)
        {
            return;
        }

        _isDefusing = true;
        _isDefuseUsed = true;
        _view.SetDefuse(_holderIndex, true);
    }

    /// <summary>
    /// 爆発させる。持っていた側の負け
    /// </summary>
    private void Explode()
    {
        _isExploded = true;
        _remainingTime = 0f;
        _view.SetTime(0f);
        _view.ShowExplosion(_holderIndex);

        if (_holderIndex == 0)
        {
            _player2Wins++;
        }
        else
        {
            _player1Wins++;
        }

        _view.SetWins(_player1Wins, _player2Wins, WinsToMatch);
    }

    /// <summary>
    /// 爆弾に仮のパズルを表示する（本番では Gimmick から受け取る）
    /// </summary>
    private void FillBomb()
    {
        UiBombView bomb = _view.Bomb;
        UiWireColor[] colors = { UiWireColor.Red, UiWireColor.Blue, UiWireColor.Yellow, UiWireColor.Green };

        for (int i = colors.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (colors[i], colors[j]) = (colors[j], colors[i]);
        }

        for (int i = 0; i < colors.Length; i++)
        {
            bomb.SetWire((InputDirection)i, colors[i], UiWireState.Normal);
            bomb.SetWireSymbol((InputDirection)i, string.Empty);
        }

        bomb.SetQuestion(Questions[Random.Range(0, Questions.Length)]);
        bomb.SetLamp(Random.value < 0.5f);
        bomb.SetSerial(Random.Range(10, 100));
        bomb.SetInput(string.Empty);

        UiDefuseState defuseState = UiDefuseState.Locked;

        if (_isDefuseUsed)
        {
            defuseState = UiDefuseState.Used;
        }
        else if (_passCount >= DefuseMinPass)
        {
            defuseState = UiDefuseState.Available;
        }

        bomb.SetDefuseState(defuseState);
        _view.SetPass(_passCount, Mathf.Clamp((_passCount + 1) / 2, 1, 4));
    }
}
