using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ギミック担当がテストシーンで使う部品。キーを押すとギミックに入力し、結果をコンソールに出す
/// 操作：矢印キーかWASDで入力、Rで問題を作り直す、1〜4で難易度を変える
/// </summary>
public class GimmickTester : MonoBehaviour
{
    [SerializeField] private Gimmick _gimmick;
    [SerializeField, Range(1, 4)] private int _stage = 1;

    /// <summary>
    /// 最初の問題を作る
    /// </summary>
    private void Start()
    {
        _gimmick.ResetRound();
        CreatePuzzle();
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

        if (keyboard.rKey.wasPressedThisFrame)
        {
            CreatePuzzle();
        }

        ChangeStage(keyboard);

        if (TryGetDirection(keyboard, out InputDirection direction))
        {
            Press(direction);
        }
    }

    /// <summary>
    /// 1〜4のキーで難易度を変えて、問題を作り直す
    /// </summary>
    /// <param name="keyboard">キーボード</param>
    private void ChangeStage(Keyboard keyboard)
    {
        Key[] stageKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 };

        for (int i = 0; i < stageKeys.Length; i++)
        {
            if (keyboard[stageKeys[i]].wasPressedThisFrame)
            {
                _stage = i + 1;
                CreatePuzzle();
            }
        }
    }

    /// <summary>
    /// 押された方向キーを調べる
    /// </summary>
    /// <param name="keyboard">キーボード</param>
    /// <param name="direction">押された方向</param>
    /// <returns>方向キーが押されていれば true</returns>
    private bool TryGetDirection(Keyboard keyboard, out InputDirection direction)
    {
        direction = InputDirection.Up;

        if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
        {
            direction = InputDirection.Up;
            return true;
        }

        if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
        {
            direction = InputDirection.Down;
            return true;
        }

        if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
        {
            direction = InputDirection.Left;
            return true;
        }

        if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
        {
            direction = InputDirection.Right;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 難易度を渡して問題を作り、問題文と正解をコンソールに出す
    /// </summary>
    private void CreatePuzzle()
    {
        _gimmick.CreatePuzzle(_stage);
        Debug.Log($"[GimmickTester] 難易度{_stage}の問題：{_gimmick.QuestionText}（次の正解：{_gimmick.NextAnswer}）");
    }

    /// <summary>
    /// ギミックに入力して結果を出す。正解なら次の問題を作る
    /// </summary>
    /// <param name="direction">押された方向</param>
    private void Press(InputDirection direction)
    {
        AnswerResult result = _gimmick.Answer(direction);
        Debug.Log($"[GimmickTester] {direction} → {result}");

        if (result == AnswerResult.Correct)
        {
            CreatePuzzle();
        }
    }
}
