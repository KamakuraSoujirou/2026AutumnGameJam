using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// リザルト画面のキー操作。←→（A D）で HOME と再戦を選び、Space か Enter で決める。Esc は HOME（タイトルへ）
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
            _view.RequestTitle();
            return;
        }

        if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
        {
            _view.MoveFocus(-1);
        }

        if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
        {
            _view.MoveFocus(1);
        }

        bool isDecidePressed = keyboard.spaceKey.wasPressedThisFrame
            || keyboard.enterKey.wasPressedThisFrame
            || keyboard.numpadEnterKey.wasPressedThisFrame;

        if (isDecidePressed)
        {
            _view.DecideFocus();
        }
    }
}
