using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// キーボードとマウスの入力を爆弾本体に渡す。受け付けるかどうか（持ち手・スタン中など）は BombManager が決める
/// 操作：P1 は WASD、P2 は矢印キー。Esc でタイトルへ。試合が終わったら Space か Enter でもう一度
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
    /// 入力の知らせを受け取れるようにする
    /// </summary>
    private void OnEnable()
    {
        _inputReader.DirectionPressed += OnDirectionPressed;
        _inputReader.BackPressed += _view.RequestBack;
        _view.DirectionClicked += OnDirectionClicked;
    }

    /// <summary>
    /// 入力の知らせを受け取るのをやめる
    /// </summary>
    private void OnDisable()
    {
        _inputReader.DirectionPressed -= OnDirectionPressed;
        _inputReader.BackPressed -= _view.RequestBack;
        _view.DirectionClicked -= OnDirectionClicked;
    }

    /// <summary>
    /// 決定キー（Space か Enter）が押されたら、終わった試合をもう一度始める
    /// </summary>
    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
        {
            _bombManager.TryRematch();
        }
    }

    /// <summary>
    /// キーで方向が押されたことを爆弾本体に伝える
    /// </summary>
    /// <param name="player">押したプレイヤーの番号</param>
    /// <param name="direction">押された方向</param>
    private void OnDirectionPressed(int player, InputDirection direction)
    {
        _bombManager.TryPress(player, direction);
    }

    /// <summary>
    /// ひし形ボタンのクリックを爆弾本体に伝える。マウスはどちらが押したか分からないので、持ち主の入力として扱う
    /// </summary>
    /// <param name="direction">クリックされた方向</param>
    private void OnDirectionClicked(InputDirection direction)
    {
        _bombManager.TryPress(_bombManager.Holder, direction);

        // クリックしたボタンが選択されたままだと、Enter でもう一度押されてしまう
        UiEventSystem.ClearSelection();
    }
}
