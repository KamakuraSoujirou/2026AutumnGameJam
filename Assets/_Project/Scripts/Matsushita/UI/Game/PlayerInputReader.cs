using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// プレイヤーの入力を読み取って、「上下左右・解除・戻る」を1回ずつの入力として知らせる（仕様書 v2 の「4. 操作」）
/// </summary>
/// <remarks>
/// 押した瞬間だけを知らせ、長押しは無視する。爆弾を持っているか、スタン中か、といった判断はゲーム進行側で行う。
/// P1：W A S D と Q、P2：矢印キーと右Shift（または /）、戻る：Esc
/// </remarks>
public class PlayerInputReader : MonoBehaviour
{
    [SerializeField, Tooltip("CPU対戦のとき、どちらのキーセットでもP1を操作できるようにする")] private bool _isCpuMode;

    /// <summary>
    /// 方向が押されたときに呼ばれる（プレイヤー番号 0：P1、1：P2）
    /// </summary>
    public event Action<int, InputDirection> DirectionPressed;

    /// <summary>
    /// 解除が押されたときに呼ばれる（プレイヤー番号 0：P1、1：P2）
    /// </summary>
    public event Action<int> DefusePressed;

    /// <summary>
    /// 戻る（Esc）が押されたときに呼ばれる
    /// </summary>
    public event Action BackPressed;

    /// <summary>
    /// CPU対戦か。オンのとき、どちらのキーセットでもP1を操作できる
    /// </summary>
    public bool IsCpuMode
    {
        get => _isCpuMode;
        set => _isCpuMode = value;
    }

    /// <summary>
    /// 毎フレーム、入力を確認する
    /// </summary>
    private void Update()
    {
        ReadKeyboard();
    }

    /// <summary>
    /// キーボードの入力を調べる
    /// </summary>
    private void ReadKeyboard()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        int firstPlayerForArrows = _isCpuMode ? 0 : 1;

        ReadKeys(0, keyboard.wKey, keyboard.sKey, keyboard.aKey, keyboard.dKey, keyboard.qKey, null);
        ReadKeys(firstPlayerForArrows, keyboard.upArrowKey, keyboard.downArrowKey, keyboard.leftArrowKey, keyboard.rightArrowKey, keyboard.rightShiftKey, keyboard.slashKey);

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            BackPressed?.Invoke();
        }
    }

    /// <summary>
    /// 1人ぶんのキーを調べる
    /// </summary>
    /// <param name="player">プレイヤー番号</param>
    /// <param name="up">上のキー</param>
    /// <param name="down">下のキー</param>
    /// <param name="left">左のキー</param>
    /// <param name="right">右のキー</param>
    /// <param name="defuse">解除のキー</param>
    /// <param name="defuseAlternative">解除のもう1つのキー。なければ null</param>
    private void ReadKeys(int player, UnityEngine.InputSystem.Controls.KeyControl up, UnityEngine.InputSystem.Controls.KeyControl down, UnityEngine.InputSystem.Controls.KeyControl left, UnityEngine.InputSystem.Controls.KeyControl right, UnityEngine.InputSystem.Controls.KeyControl defuse, UnityEngine.InputSystem.Controls.KeyControl defuseAlternative)
    {
        if (up.wasPressedThisFrame)
        {
            DirectionPressed?.Invoke(player, InputDirection.Up);
        }

        if (down.wasPressedThisFrame)
        {
            DirectionPressed?.Invoke(player, InputDirection.Down);
        }

        if (left.wasPressedThisFrame)
        {
            DirectionPressed?.Invoke(player, InputDirection.Left);
        }

        if (right.wasPressedThisFrame)
        {
            DirectionPressed?.Invoke(player, InputDirection.Right);
        }

        if (defuse.wasPressedThisFrame || (defuseAlternative != null && defuseAlternative.wasPressedThisFrame))
        {
            DefusePressed?.Invoke(player);
        }
    }
}
