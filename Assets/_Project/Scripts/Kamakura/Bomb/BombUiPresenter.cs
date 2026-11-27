using UnityEngine;

/// <summary>
/// 爆弾本体の状態を、試合画面（UiGameView）に反映する
/// </summary>
/// <remarks>
/// 爆弾の中の表示は、ラウンド開始と爆弾が飛んだときに初期状態へ戻す。
/// ギミックごとの表示は、そのあとに呼ばれる CreatePuzzle で上書きできる
/// </remarks>
public class BombUiPresenter : MonoBehaviour
{
    private const int DirectionCount = 4;

    [SerializeField] private BombManager _bombManager;
    [SerializeField] private BombTimer _bombTimer;
    [SerializeField] private UiGameView _view;
    [SerializeField, Tooltip("ギミックに名前が付いていないときに出す名前")] private string _gimmickName = "テスト";
    [SerializeField, Tooltip("ギミックに説明が付いていないときに出す説明")] private string _gimmickDescription = "出ている順に方向を押せ！";

    private BombState? _shownState;
    private int _shownHolder = BombManager.NoPlayer;
    private int _shownHundredths = -1;
    private int _defuseChallenger = BombManager.NoPlayer;

    /// <summary>
    /// 爆弾本体の知らせを受け取れるようにする（BombManager の Start より先に登録される）
    /// </summary>
    private void OnEnable()
    {
        _bombManager.OnChanged += Refresh;
        _bombManager.OnPressed += ShowPress;
        _bombManager.OnMissed += ShowMiss;
    }

    /// <summary>
    /// 爆弾本体の知らせを受け取るのをやめる
    /// </summary>
    private void OnDisable()
    {
        _bombManager.OnChanged -= Refresh;
        _bombManager.OnPressed -= ShowPress;
        _bombManager.OnMissed -= ShowMiss;
    }

    /// <summary>
    /// 答えている間と解除中は、残り時間の表示を進める
    /// </summary>
    private void Update()
    {
        if (_shownState == BombState.Answering || _shownState == BombState.Defusing)
        {
            ShowTime(_bombTimer.Remaining);
        }
    }

    /// <summary>
    /// 状態が切り替わったときは画面を切り替え、答えている間は問題文と解除の状態を書き直す
    /// </summary>
    private void Refresh()
    {
        BombState state = _bombManager.State;

        if (_shownState != state)
        {
            // 解除中から別の状態になったら、暗くした画面を戻す
            if (_shownState == BombState.Defusing)
            {
                EndDefuse();
            }

            _shownState = state;
            ShowState(state);
        }

        if (state == BombState.Answering)
        {
            _view.Bomb.SetQuestion(_bombManager.QuestionText);
            _view.Bomb.SetDefuseState(_bombManager.DefuseStatus == DefuseStatus.Available ? UiDefuseState.Available : UiDefuseState.Locked);
        }
    }

    /// <summary>
    /// 切り替わった先の状態の画面にする
    /// </summary>
    /// <param name="state">新しい状態</param>
    private void ShowState(BombState state)
    {
        switch (state)
        {
            case BombState.RoundStart:
                ShowRoundStart();
                break;
            case BombState.Answering:
                ShowAnswering();
                break;
            case BombState.Flying:
                ShowFlying();
                break;
            case BombState.Defusing:
                ShowDefusing();
                break;
            case BombState.Exploding:
                ShowExploding();
                break;
            case BombState.Defused:
                ShowDefused();
                break;
            case BombState.MatchOver:
                _view.ShowResult(_bombManager.MatchWinner, _bombManager.GetWins(0), _bombManager.GetWins(1));
                break;
        }
    }

    /// <summary>
    /// 前のラウンドの表示を片付けて、開始のバナーを出す
    /// </summary>
    private void ShowRoundStart()
    {
        int round = _bombManager.GetWins(0) + _bombManager.GetWins(1) + 1;

        // そのラウンドのギミックの名前と説明。ギミックに付いていなければ、仮の名前と説明を出す
        string gimmickName = OrDefault(_bombManager.GimmickName, _gimmickName);
        string gimmickDescription = OrDefault(_bombManager.GimmickDescription, _gimmickDescription);

        _view.ResetEffects();
        ResetBomb();
        _view.SetRound(round);
        _view.SetPlayerNames("P1", "P2");
        _view.SetGimmickName(gimmickName);
        ShowWins();
        _view.SetPass(1, 1);
        _view.SetHoldTime(_bombManager.HoldTime);
        ShowHolder(_bombManager.Holder);
        ShowTime(_bombManager.HoldTime);
        _view.ShowBanner($"ROUND {round}", gimmickName, gimmickDescription);
    }

    /// <summary>
    /// 文字が空なら、代わりの文字を返す
    /// </summary>
    /// <param name="text">使いたい文字</param>
    /// <param name="fallback">空のときに使う文字</param>
    /// <returns>text か fallback</returns>
    private static string OrDefault(string text, string fallback)
    {
        return string.IsNullOrEmpty(text) ? fallback : text;
    }

    /// <summary>
    /// 爆弾を受け取った側を持ち主にする
    /// </summary>
    private void ShowAnswering()
    {
        _view.HideBanner();

        // 開始のバナーの間から持っている場合は、導火線を伸ばし直さない
        if (_shownHolder != _bombManager.Holder)
        {
            ShowHolder(_bombManager.Holder);
        }

        _view.SetPass(_bombManager.ReceiveCount, _bombManager.Stage);
        _view.SetHoldTime(_bombManager.HoldTime);
        _shownHundredths = -1;
        ShowTime(_bombTimer.Remaining);
    }

    /// <summary>
    /// 爆弾を相手に飛ばす
    /// </summary>
    private void ShowFlying()
    {
        _shownHolder = BombManager.NoPlayer;

        // 飛び終わったら時間の表示を入れ直して、導火線を出す
        _view.ThrowBomb(_bombManager.FlightTarget, _bombManager.FlightDuration, () => _shownHundredths = -1);
        ResetBomb();
    }

    /// <summary>
    /// 持っていた側を爆発させて、勝ち星を増やす
    /// </summary>
    private void ShowExploding()
    {
        ShowTime(0f);
        _view.ShowExplosion(_bombManager.Holder);
        _shownHolder = BombManager.NoPlayer;
        ShowWins();
    }

    /// <summary>
    /// 解除の挑戦を始める。画面を暗くして相手側に「解除中！」を出し、液晶に問題文、4つのボタンに選択肢を出す
    /// </summary>
    private void ShowDefusing()
    {
        UiBombView bomb = _view.Bomb;
        _defuseChallenger = _bombManager.Holder;
        _view.SetDefuse(_defuseChallenger, true);
        bomb.SetQuestion(_bombManager.DefuseQuestionText);
        bomb.SetInput(string.Empty);

        for (int i = 0; i < DirectionCount; i++)
        {
            InputDirection direction = (InputDirection)i;
            bomb.SetButtonLabel(direction, _bombManager.GetDefuseChoice(direction));
            bomb.SetWireSymbol(direction, string.Empty);
        }
    }

    /// <summary>
    /// 解除の挑戦が終わったので、暗くした画面と「解除中！」を戻す
    /// </summary>
    private void EndDefuse()
    {
        if (_defuseChallenger == BombManager.NoPlayer)
        {
            return;
        }

        _view.SetDefuse(_defuseChallenger, false);
        _defuseChallenger = BombManager.NoPlayer;
    }

    /// <summary>
    /// 解除に成功したので、「解除成功！」を出して、勝ち星を増やす
    /// </summary>
    private void ShowDefused()
    {
        _view.ShowDefuseSuccess(_bombManager.RoundWinner);
        _shownHolder = BombManager.NoPlayer;
        ShowWins();
    }

    /// <summary>
    /// 爆弾の中の表示を、4色の線がつながった状態に戻す
    /// </summary>
    private void ResetBomb()
    {
        UiBombView bomb = _view.Bomb;

        for (int i = 0; i < DirectionCount; i++)
        {
            InputDirection direction = (InputDirection)i;
            bomb.SetWire(direction, (UiWireColor)i, UiWireState.Normal);
            bomb.SetButtonLabel(direction, string.Empty);
            bomb.SetWireSymbol(direction, string.Empty);
        }

        bomb.SetQuestion(string.Empty);
        bomb.SetInput(string.Empty);
        bomb.SetDefuseState(UiDefuseState.Locked);
    }

    /// <summary>
    /// 持ち主を切り替える
    /// </summary>
    /// <param name="holder">持ち主の番号</param>
    private void ShowHolder(int holder)
    {
        _shownHolder = holder;
        _view.SetHolder(holder);
    }

    /// <summary>
    /// 勝ち星を今の勝ち数にする
    /// </summary>
    private void ShowWins()
    {
        _view.SetWins(_bombManager.GetWins(0), _bombManager.GetWins(1), _bombManager.WinsToMatch);
    }

    /// <summary>
    /// 残り時間を出す。表示が変わるときだけ書き換える（毎フレーム文字列を作らないため）
    /// </summary>
    /// <param name="seconds">残り時間（秒）</param>
    private void ShowTime(float seconds)
    {
        // 切り上げにして、「00:00」は本当に0になったときだけ出す
        int hundredths = Mathf.CeilToInt(seconds * 100f);

        if (hundredths == _shownHundredths)
        {
            return;
        }

        _shownHundredths = hundredths;
        _view.SetTime(hundredths / 100f);
    }

    /// <summary>
    /// 受け付けた入力のボタンを、押した動きにする
    /// </summary>
    /// <param name="direction">押された方向</param>
    private void ShowPress(InputDirection direction)
    {
        _view.Bomb.PressButton(direction);
    }

    /// <summary>
    /// 間違えた側に「−2秒」を出す
    /// </summary>
    /// <param name="player">間違えたプレイヤーの番号</param>
    /// <param name="seconds">減る秒数</param>
    private void ShowMiss(int player, float seconds)
    {
        _view.ShowPenalty(player, seconds);
    }
}
