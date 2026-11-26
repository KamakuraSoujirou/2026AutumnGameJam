using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// リザルト画面のキー操作。Space か Enter でメニューを開き、メニューでは ↑↓（W S）で選んで Space か Enter で決める。
/// Esc はメニューを閉じる（メニューが閉じているときはタイトルへ）
/// </summary>
[RequireComponent(typeof(UiResultView))]
public class UiResultInput : MonoBehaviour
{
    private UiResultView _view;

    /// <summary>
    /// 自分の表示を取得しておく
    /// </summary>
    private void Awake()
    {
        _view = GetComponent<UiResultView>();
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

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            HandleBack();
            return;
        }

        bool isDecidePressed = keyboard.spaceKey.wasPressedThisFrame
            || keyboard.enterKey.wasPressedThisFrame
            || keyboard.numpadEnterKey.wasPressedThisFrame;

        if (isDecidePressed)
        {
            HandleDecide();
            return;
        }

        if (!_view.IsMenuOpen)
        {
            return;
        }

        if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
        {
            _view.MoveMenuFocus(-1);
        }

        if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
        {
            _view.MoveMenuFocus(1);
        }
    }

    /// <summary>
    /// 決定キー：メニューが閉じていれば開き、開いていれば選んでいるものを決める
    /// </summary>
    private void HandleDecide()
    {
        if (_view.IsMenuOpen)
        {
            _view.DecideMenu();
        }
        else
        {
            _view.OpenMenu();
        }
    }

    /// <summary>
    /// Esc：メニューが開いていれば閉じ、閉じていればタイトルへ戻る
    /// </summary>
    private void HandleBack()
    {
        if (_view.IsMenuOpen)
        {
            _view.CloseMenu();
        }
        else
        {
            _view.RequestTitle();
        }
    }
}
