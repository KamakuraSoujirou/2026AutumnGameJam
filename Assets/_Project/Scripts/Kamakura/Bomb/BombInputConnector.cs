using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// キーボードとマウスの入力を爆弾本体に渡す。受け付けるかどうか（持ち手・スタン中など）は BombManager が決める
/// 操作：P1 は WASD、P2 は矢印キー。解除は P1 が Q、P2 が右Shift か /（液晶の下の解除ボタンでも始まる）。Esc でタイトルへ。
/// 試合が終わったら、←→（A D）で「再戦」「タイトルへ」を選び、Space か Enter で決める（マウスでも押せる）
/// </summary>
public class BombInputConnector : MonoBehaviour
{
    private const string TitleSceneName = "Title";

    [SerializeField] private BombManager _bombManager;
    [SerializeField] private PlayerInputReader _inputReader;
    [SerializeField] private UiGameView _view;

    /// <summary>
    /// タイトル画面へ戻る（UiGameView の「戻る」（On Back Requested）から呼ぶ）
    /// </summary>
    public void LoadTitle()
    {
        SceneManager.LoadScene(TitleSceneName);
    }

    /// <summary>
    /// CPU対戦なら、どちらのキーセットでもP1を操作できるようにする（CpuPlayer が設定を決めたあと、全部の Awake が終わってから）
    /// </summary>
    private void Start()
    {
        _inputReader.IsCpuMode = CpuSettings.IsCpuMode;
    }

    /// <summary>
    /// 入力の知らせを受け取れるようにする
    /// </summary>
    private void OnEnable()
    {
        _inputReader.DirectionPressed += OnDirectionPressed;
        _inputReader.DefusePressed += OnDefusePressed;
        _inputReader.BackPressed += OnBackPressed;
        _view.DirectionClicked += OnDirectionClicked;
        _view.DefuseRequested += OnDefuseClicked;
        _view.RematchRequested += OnRematchRequested;
    }

    /// <summary>
    /// 入力の知らせを受け取るのをやめる
    /// </summary>
    private void OnDisable()
    {
        _inputReader.DirectionPressed -= OnDirectionPressed;
        _inputReader.DefusePressed -= OnDefusePressed;
        _inputReader.BackPressed -= OnBackPressed;
        _view.DirectionClicked -= OnDirectionClicked;
        _view.DefuseRequested -= OnDefuseClicked;
        _view.RematchRequested -= OnRematchRequested;
    }

    /// <summary>
    /// 試合が終わってリザルトが出ているときに、決定キー（Space か Enter）が押されたら、選んでいるボタンを決める
    /// </summary>
    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null || !IsResultSelectable())
        {
            return;
        }

        if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
        {
            _view.DecideResult();
        }
    }

    /// <summary>
    /// キーで方向が押されたことを伝える。リザルト中は左右でボタンを選び、それ以外は爆弾本体に渡す
    /// </summary>
    /// <param name="player">押したプレイヤーの番号</param>
    /// <param name="direction">押された方向</param>
    private void OnDirectionPressed(int player, InputDirection direction)
    {
        if (IsResultSelectable())
        {
            MoveResultFocus(direction);
            return;
        }

        _bombManager.TryPress(player, direction);
    }

    /// <summary>
    /// Esc が押されたので、タイトルへ戻る。リザルト中はリザルト側（UiResultInput）が Esc を受け取ってタイトルへ戻るので、ここでは何もしない
    /// （両方で戻ると、タイトルが2回読み込まれてしまう）
    /// </summary>
    private void OnBackPressed()
    {
        if (IsResultSelectable())
        {
            return;
        }

        _view.RequestBack();
    }

    /// <summary>
    /// 解除キーが押されたことを爆弾本体に伝える（始められるかどうかは BombManager が決める）
    /// </summary>
    /// <param name="player">押したプレイヤーの番号</param>
    private void OnDefusePressed(int player)
    {
        _bombManager.TryStartDefuse(player);
    }

    /// <summary>
    /// 液晶の下の解除ボタンのクリックを爆弾本体に伝える。マウスは誰が押したか分からないので、持ち主が押したものとして扱う
    /// </summary>
    private void OnDefuseClicked()
    {
        _bombManager.TryStartDefuse(_bombManager.Holder);

        // クリックしたボタンが選択されたままだと、Enter でもう一度押されてしまう
        UiEventSystem.ClearSelection();
    }

    /// <summary>
    /// リザルトのボタンを選べる状態か（試合が終わって、リザルトが出ている）
    /// </summary>
    /// <returns>選べるなら true</returns>
    private bool IsResultSelectable()
    {
        return _bombManager.State == BombState.MatchOver && _view.IsResultShown;
    }

    /// <summary>
    /// 左右の入力で、リザルトで選んでいるボタンを動かす（上下は使わない）
    /// </summary>
    /// <param name="direction">押された方向</param>
    private void MoveResultFocus(InputDirection direction)
    {
        if (direction == InputDirection.Left)
        {
            _view.MoveResultFocus(-1);
        }
        else if (direction == InputDirection.Right)
        {
            _view.MoveResultFocus(1);
        }
    }

    /// <summary>
    /// リザルトで「再戦」が選ばれたので、もう一度試合を始める
    /// </summary>
    private void OnRematchRequested()
    {
        _bombManager.TryRematch();
    }

    /// <summary>
    /// ひし形ボタンのクリックを爆弾本体に伝える。マウスはどちらが押したか分からないので、持ち主の入力として扱う
    /// ただしCPU対戦でCOMが持っているときは、COMの代わりに解いてしまうので無視する
    /// </summary>
    /// <param name="direction">クリックされた方向</param>
    private void OnDirectionClicked(InputDirection direction)
    {
        if (CpuSettings.IsCpuMode && _bombManager.Holder == BombManager.PlayerCount - 1)
        {
            return;
        }

        _bombManager.TryPress(_bombManager.Holder, direction, true);

        // クリックしたボタンが選択されたままだと、Enter でもう一度押されてしまう
        UiEventSystem.ClearSelection();
    }
}
