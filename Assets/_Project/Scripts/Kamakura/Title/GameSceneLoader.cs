using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// タイトルから試合（Game シーン）へ切り替える。UiTitleInput の「ゲームスタート」（On Start Requested）から呼ぶ
/// </summary>
public class GameSceneLoader : MonoBehaviour
{
    private const string GameSceneName = "Game";

    /// <summary>
    /// 試合のシーンを読み込む
    /// </summary>
    public void LoadGame()
    {
        SceneManager.LoadScene(GameSceneName);
    }
}
