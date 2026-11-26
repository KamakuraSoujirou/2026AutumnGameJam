using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// 仮のタイトル画面。決定キー（P1：Space、P2：Enter）でゲームシーンへ進む
/// </summary>
public class TitleSceneController : MonoBehaviour
{
    /// <summary>
    /// 決定キーが押されたらゲームシーンを読み込む
    /// </summary>
    private void Update()
    {
        if (IsStartPressed())
        {
            SceneManager.LoadScene(SceneNames.Game);
        }
    }

    /// <summary>
    /// どちらかのプレイヤーが決定キーを押したかを調べる
    /// </summary>
    /// <returns>このフレームで押されていれば true</returns>
    private bool IsStartPressed()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        return keyboard.spaceKey.wasPressedThisFrame
            || keyboard.enterKey.wasPressedThisFrame
            || keyboard.numpadEnterKey.wasPressedThisFrame;
    }
}
