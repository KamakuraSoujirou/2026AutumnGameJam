using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// リザルト画面の表示。勝者と各ラウンドの結果を出し、「次へ」で出るメニュー（リスタート／タイトルへ）を管理する。
/// キー操作は UiResultInput、マウスのクリックはこのクラスが受け取る
/// </summary>
public class UiResultView : MonoBehaviour
{
    private const int RestartIndex = 0;
    private const int TitleIndex = 1;

    [SerializeField, Tooltip("キャラ（P1、P2の順）")] private UiResultPlayer[] _players;
    [SerializeField, Tooltip("表の行（上から順）。ラウンドが多いときは入りきる分だけ出す")] private UiResultRow[] _rows;
    [SerializeField, Tooltip("「次へ」のボタン（マウスで押す用）")] private Button _nextButton;
    [SerializeField, Tooltip("メニュー全体（暗い幕を含む）")] private GameObject _menuRoot;
    [SerializeField, Tooltip("メニューのボタン（リスタート、タイトルへの順）")] private Button[] _menuButtons;
    [SerializeField, Tooltip("メニューで選んでいるボタンに出す枠（ボタンと同じ順）")] private GameObject[] _menuFocusFrames;

    private int _menuFocusIndex;

    /// <summary>
    /// リスタートが選ばれたときに呼ばれる
    /// </summary>
    public event Action RestartRequested;

    /// <summary>
    /// タイトルへが選ばれたときに呼ばれる
    /// </summary>
    public event Action TitleRequested;

    /// <summary>
    /// メニューが開いているか
    /// </summary>
    public bool IsMenuOpen => _menuRoot.activeSelf;

    /// <summary>
    /// マウスでクリックできるように準備する
    /// </summary>
    private void Awake()
    {
        UiEventSystem.Ensure();

        _nextButton.onClick.AddListener(OnNextClicked);
        _menuButtons[RestartIndex].onClick.AddListener(OnRestartClicked);
        _menuButtons[TitleIndex].onClick.AddListener(OnTitleClicked);
    }

    /// <summary>
    /// 勝者と各ラウンドの結果を表示に反映する（メニューは閉じる）
    /// </summary>
    /// <param name="winnerIndex">試合に勝った側（0：P1、1：P2）</param>
    /// <param name="player1Name">P1の名前</param>
    /// <param name="player2Name">P2の名前</param>
    /// <param name="roundWinners">ラウンドごとに取った側（0：P1、1：P2）。先頭が1ラウンド目</param>
    public void Show(int winnerIndex, string player1Name, string player2Name, int[] roundWinners)
    {
        _players[0].Show(winnerIndex == 0, player1Name);
        _players[1].Show(winnerIndex == 1, player2Name);

        for (int i = 0; i < _rows.Length; i++)
        {
            if (i < roundWinners.Length)
            {
                _rows[i].Show(i + 1, roundWinners[i]);
            }
            else
            {
                _rows[i].Hide();
            }
        }

        _menuRoot.SetActive(false);
    }

    /// <summary>
    /// メニューを開く（最初はリスタートを選んだ状態）
    /// </summary>
    public void OpenMenu()
    {
        SoundPlayer.PlaySe(SoundId.Decide);
        _menuRoot.SetActive(true);
        _menuFocusIndex = RestartIndex;
        RefreshMenuFocus();
    }

    /// <summary>
    /// メニューを閉じる
    /// </summary>
    public void CloseMenu()
    {
        SoundPlayer.PlaySe(SoundId.Cancel);
        _menuRoot.SetActive(false);
    }

    /// <summary>
    /// メニューで選んでいるボタンを上下に動かす（端で止まる）
    /// </summary>
    /// <param name="delta">上なら -1、下なら 1</param>
    public void MoveMenuFocus(int delta)
    {
        int next = Mathf.Clamp(_menuFocusIndex + delta, 0, _menuButtons.Length - 1);

        if (next != _menuFocusIndex)
        {
            SoundPlayer.PlaySe(SoundId.Cursor);
        }

        _menuFocusIndex = next;
        RefreshMenuFocus();
    }

    /// <summary>
    /// メニューで選んでいるボタンを決定する
    /// </summary>
    public void DecideMenu()
    {
        if (_menuFocusIndex == RestartIndex)
        {
            RequestRestart();
        }
        else
        {
            RequestTitle();
        }
    }

    /// <summary>
    /// リスタートを知らせる
    /// </summary>
    public void RequestRestart()
    {
        SoundPlayer.PlaySe(SoundId.Decide);
        RestartRequested?.Invoke();
    }

    /// <summary>
    /// タイトルへ戻るのを知らせる
    /// </summary>
    public void RequestTitle()
    {
        SoundPlayer.PlaySe(SoundId.Decide);
        TitleRequested?.Invoke();
    }

    /// <summary>
    /// 「次へ」がクリックされたので、メニューを開く
    /// </summary>
    private void OnNextClicked()
    {
        OpenMenu();
        UiEventSystem.ClearSelection();
    }

    /// <summary>
    /// メニューの「リスタート」がクリックされた
    /// </summary>
    private void OnRestartClicked()
    {
        _menuFocusIndex = RestartIndex;
        RefreshMenuFocus();
        UiEventSystem.ClearSelection();
        RequestRestart();
    }

    /// <summary>
    /// メニューの「タイトルへ」がクリックされた
    /// </summary>
    private void OnTitleClicked()
    {
        _menuFocusIndex = TitleIndex;
        RefreshMenuFocus();
        UiEventSystem.ClearSelection();
        RequestTitle();
    }

    /// <summary>
    /// 選んでいるボタンにだけ枠を出す
    /// </summary>
    private void RefreshMenuFocus()
    {
        for (int i = 0; i < _menuFocusFrames.Length; i++)
        {
            _menuFocusFrames[i].SetActive(i == _menuFocusIndex);
        }
    }
}
