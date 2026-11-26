using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// リザルト画面の確認用。ゲームの進行がまだなくても、仮の結果を出して操作を確かめられる
/// </summary>
/// <remarks>
/// 1 / 2 キーで勝者（P1／P2）を切り替える。本番のゲーム進行が入ったら、このコンポーネントは外す
/// </remarks>
[RequireComponent(typeof(UiResultView))]
public class UiResultPreview : MonoBehaviour
{
    private static readonly int[] SampleRoundWinners = { 0, 1, 0 };

    private UiResultView _view;

    /// <summary>
    /// 表示を取得して、操作が届いたらコンソールに出す
    /// </summary>
    private void Awake()
    {
        _view = GetComponent<UiResultView>();
        _view.RestartRequested += OnRestartRequested;
        _view.TitleRequested += OnTitleRequested;
    }

    /// <summary>
    /// 仮の結果を出す
    /// </summary>
    private void Start()
    {
        _view.Show(0, "P1", "COM", SampleRoundWinners);
    }

    /// <summary>
    /// 結びつけた処理を外す
    /// </summary>
    private void OnDestroy()
    {
        _view.RestartRequested -= OnRestartRequested;
        _view.TitleRequested -= OnTitleRequested;
    }

    /// <summary>
    /// 勝者を切り替えるキーを調べる
    /// </summary>
    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            _view.Show(0, "P1", "COM", SampleRoundWinners);
        }

        if (keyboard.digit2Key.wasPressedThisFrame)
        {
            _view.Show(1, "P1", "COM", new[] { 1, 0, 1 });
        }
    }

    /// <summary>
    /// リスタートが選ばれたことをコンソールに出す
    /// </summary>
    private void OnRestartRequested()
    {
        Debug.Log("リスタート（本番では試合を最初からやり直す）");
    }

    /// <summary>
    /// タイトルへが選ばれたことをコンソールに出す
    /// </summary>
    private void OnTitleRequested()
    {
        Debug.Log("タイトルへ（本番ではタイトルのシーンへ切り替える）");
    }
}
