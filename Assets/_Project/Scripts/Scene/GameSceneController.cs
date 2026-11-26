using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// 仮のゲームシーン。試合の流れができるまでは、Esc でタイトルに戻れるだけにしておく
/// </summary>
public class GameSceneController : MonoBehaviour
{
    /// <summary>
    /// Esc が押されたらタイトルシーンに戻る
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
            SceneManager.LoadScene(SceneNames.Title);
        }
    }
}
