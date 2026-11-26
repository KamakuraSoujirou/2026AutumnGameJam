using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// テストシーン専用：爆弾本体の代わりに、キー入力で暗号ギミックを動かす。
/// 矢印キー／WASD＝ボタン入力、Space＝新しい問題、1〜4キー＝難易度の段階を変える。
/// 本番の Game シーンには置かない
/// </summary>
public class CipherTestInput : MonoBehaviour
{
    [SerializeField] private CipherGimmick _gimmick;
    [SerializeField, Range(1, 4)] private int _stage = 1;

    /// <summary>
    /// ラウンド開始の扱いにして、最初の問題を出す
    /// </summary>
    private void Start()
    {
        _gimmick.ResetRound();
        CreatePuzzle();
    }

    /// <summary>
    /// キー入力を受け取って、ギミックに渡す
    /// </summary>
    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            CreatePuzzle();
        }

        CheckStageKey(keyboard);

        if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
        {
            Press(InputDirection.Up);
        }
        else if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
        {
            Press(InputDirection.Down);
        }
        else if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
        {
            Press(InputDirection.Left);
        }
        else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
        {
            Press(InputDirection.Right);
        }
    }

    /// <summary>
    /// 1〜4キーが押されたら、段階を変えて新しい問題を出す
    /// </summary>
    /// <param name="keyboard">今のキーボード</param>
    private void CheckStageKey(Keyboard keyboard)
    {
        int newStage = 0;

        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            newStage = 1;
        }
        else if (keyboard.digit2Key.wasPressedThisFrame)
        {
            newStage = 2;
        }
        else if (keyboard.digit3Key.wasPressedThisFrame)
        {
            newStage = 3;
        }
        else if (keyboard.digit4Key.wasPressedThisFrame)
        {
            newStage = 4;
        }

        if (newStage != 0)
        {
            _stage = newStage;
            CreatePuzzle();
        }
    }

    /// <summary>
    /// 今の段階で新しい問題を出す
    /// </summary>
    private void CreatePuzzle()
    {
        _gimmick.CreatePuzzle(_stage);
    }

    /// <summary>
    /// ボタン入力を渡す。正解したら次の問題を出す（本番では爆弾本体が行う）
    /// </summary>
    /// <param name="direction">押した方向</param>
    private void Press(InputDirection direction)
    {
        AnswerResult result = _gimmick.Answer(direction);

        if (result == AnswerResult.Correct)
        {
            CreatePuzzle();
        }
    }
}