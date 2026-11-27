using UnityEngine;

/// <summary>
/// ビルドしたゲームの起動時に、解像度とフルスクリーンの種類を固定する
/// （前回起動したときの解像度がレジストリに残っていても、毎回これで上書きする）
/// </summary>
public static class ScreenSetup
{
    private const int Width = 1920;
    private const int Height = 1080;
    private const FullScreenMode Mode = FullScreenMode.FullScreenWindow;

    /// <summary>
    /// 最初のシーンを読む前に、解像度とフルスクリーンの種類を設定する（エディターでは何もしない）
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        if (Application.isEditor)
        {
            return;
        }

        Screen.SetResolution(Width, Height, Mode);
    }
}
