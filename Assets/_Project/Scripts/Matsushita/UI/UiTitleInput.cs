using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// タイトル画面のキー操作。↑↓（W S）で行を選び、←→（A D）で設定を変え、Space か Enter で始める。
/// マウスのクリックは UiTitleView が受け取り、ゲームスタートだけここへ知らせてくる
/// </summary>
[RequireComponent(typeof(UiTitleView))]
public class UiTitleInput : MonoBehaviour
{
    [SerializeField, Tooltip("ゲームスタートが押されたときに呼ぶ（シーンの切り替えなど）")] private UnityEvent _onStartRequested;

    private UiTitleView _view;

    /// <summary>
    /// 自分の表示を取得しておく
    /// </summary>
    private void Awake()
    {
        _view = GetComponent<UiTitleView>();
    }

    /// <summary>
    /// マウスでゲームスタートが押されたときも、キーと同じように始める
    /// </summary>
    private void OnEnable()
    {
        _view.StartClicked += RequestStart;
    }

    /// <summary>
    /// マウスのクリックを受け取るのをやめる
    /// </summary>
    private void OnDisable()
    {
        _view.StartClicked -= RequestStart;
    }

    /// <summary>
    /// 勝利条件と時間短縮の行を、今の設定に合わせる（タイトルに戻ってきても、前に選んだ値が残るように）
    /// </summary>
    private void Start()
    {
        GameConfig config = GameConfig.Load();
        _view.SelectMatchRules(config.WinsToMatch, config.IsShortenEnabled);
    }

    /// <summary>
    /// キーの入力を調べる
    /// </summary>
    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
        {
            _view.MoveFocus(-1);
        }

        if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
        {
            _view.MoveFocus(1);
        }

        if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
        {
            _view.ChangeOption(-1);
        }

        if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
        {
            _view.ChangeOption(1);
        }

        bool isDecidePressed = keyboard.spaceKey.wasPressedThisFrame
            || keyboard.enterKey.wasPressedThisFrame
            || keyboard.numpadEnterKey.wasPressedThisFrame;

        if (isDecidePressed && _view.IsStartFocused)
        {
            RequestStart();
        }
    }

    /// <summary>
    /// 選んだ勝利条件と時間短縮を設定に入れてから、ゲームスタートを知らせる
    /// </summary>
    private void RequestStart()
    {
        GameConfig.Load().SetMatchRules(_view.WinsToMatch, _view.IsShortenEnabled);
        Debug.Log($"ゲームスタート（CPU対戦：{_view.IsCpuMode}、CPUの強さ：{_view.CpuLevel}、{_view.WinsToMatch}本先取、時間短縮：{_view.IsShortenEnabled}、読み上げ：{_view.IsVoiceEnabled}）");
        _onStartRequested.Invoke();
    }
}
