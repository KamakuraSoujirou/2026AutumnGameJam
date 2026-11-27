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
        ShowSample(0);
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
            ShowSample(0);
        }

        if (keyboard.digit2Key.wasPressedThisFrame)
        {
            ShowSample(1);
        }
    }

    /// <summary>
    /// 仮の結果（5ラウンド、3勝2敗）を出す。勝者が P2 のときは、勝ち負けを入れ替える
    /// </summary>
    /// <param name="winnerIndex">試合に勝った側（0：P1、1：P2）</param>
    private void ShowSample(int winnerIndex)
    {
        int loserIndex = 1 - winnerIndex;
        UiResultRound[] rounds =
        {
            new UiResultRound("配線切断式", winnerIndex, false, 3),
            new UiResultRound("暗号式", loserIndex, true, 5),
            new UiResultRound("順番スイッチ式", loserIndex, false, 2),
            new UiResultRound("配線切断式", winnerIndex, false, 4),
            new UiResultRound("仲間外れ式", winnerIndex, false, 6)
        };

        _view.Show(winnerIndex, "P1", "COM", rounds);
    }

    /// <summary>
    /// 再戦が選ばれたことをコンソールに出す
    /// </summary>
    private void OnRestartRequested()
    {
        Debug.Log("再戦（本番では試合を最初からやり直す）");
    }

    /// <summary>
    /// HOMEが選ばれたことをコンソールに出す
    /// </summary>
    private void OnTitleRequested()
    {
        Debug.Log("HOME（本番ではタイトルのシーンへ切り替える）");
    }
}
