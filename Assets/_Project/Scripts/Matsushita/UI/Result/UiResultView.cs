using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// リザルト画面の表示。勝ち数・勝者・各ラウンドの結果の表を出し、左下の「HOME」と右下の「再戦」の選択を管理する。
/// キー操作は UiResultInput、マウスのクリックはこのクラスが受け取る
/// </summary>
public class UiResultView : MonoBehaviour
{
    private const int HomeIndex = 0;
    private const int RestartIndex = 1;

    [SerializeField, Tooltip("キャラ（P1、P2の順）")] private UiResultPlayer[] _players;
    [SerializeField, Tooltip("上の勝ち数（P1側）")] private TMP_Text _player1ScoreText;
    [SerializeField, Tooltip("上の勝ち数（P2側）")] private TMP_Text _player2ScoreText;
    [SerializeField, Tooltip("表の行（上から順）。ラウンドが多いときは入りきる分だけ出す")] private UiResultRow[] _rows;
    [SerializeField, Tooltip("左下の「HOME」ボタン（タイトルへ）")] private Button _homeButton;
    [SerializeField, Tooltip("右下の「再戦」ボタン（もう一度）")] private Button _restartButton;
    [SerializeField, Tooltip("選んでいるボタンに出す枠（HOME、再戦の順）")] private GameObject[] _focusFrames;

    private int _focusIndex = RestartIndex;

    /// <summary>
    /// 再戦が選ばれたときに呼ばれる
    /// </summary>
    public event Action RestartRequested;

    /// <summary>
    /// HOME（タイトルへ）が選ばれたときに呼ばれる
    /// </summary>
    public event Action TitleRequested;

    /// <summary>
    /// マウスでクリックできるように準備する
    /// </summary>
    private void Awake()
    {
        UiEventSystem.Ensure();

        _homeButton.onClick.AddListener(OnHomeClicked);
        _restartButton.onClick.AddListener(OnRestartClicked);
    }

    /// <summary>
    /// 勝ち数・勝者・各ラウンドの結果を表示に反映する（選んでいるボタンは再戦に戻す）
    /// </summary>
    /// <param name="winnerIndex">試合に勝った側（0：P1、1：P2）</param>
    /// <param name="player1Name">P1の名前</param>
    /// <param name="player2Name">P2の名前</param>
    /// <param name="rounds">ラウンドごとの結果。先頭が1ラウンド目</param>
    public void Show(int winnerIndex, string player1Name, string player2Name, UiResultRound[] rounds)
    {
        _players[0].Show(winnerIndex == 0, player1Name);
        _players[1].Show(winnerIndex == 1, player2Name);

        int player1Wins = 0;
        int player2Wins = 0;

        foreach (UiResultRound round in rounds)
        {
            if (round.WinnerIndex == 0)
            {
                player1Wins++;
            }
            else
            {
                player2Wins++;
            }
        }

        _player1ScoreText.text = player1Wins.ToString();
        _player2ScoreText.text = player2Wins.ToString();

        for (int i = 0; i < _rows.Length; i++)
        {
            if (i < rounds.Length)
            {
                _rows[i].Show(i + 1, rounds[i]);
            }
            else
            {
                _rows[i].Hide();
            }
        }

        _focusIndex = RestartIndex;
        RefreshFocus();
    }

    /// <summary>
    /// 選んでいるボタンを左右に動かす（端で止まる）
    /// </summary>
    /// <param name="delta">左なら -1、右なら 1</param>
    public void MoveFocus(int delta)
    {
        _focusIndex = Mathf.Clamp(_focusIndex + delta, 0, _focusFrames.Length - 1);
        RefreshFocus();
    }

    /// <summary>
    /// 選んでいるボタンを決定する
    /// </summary>
    public void DecideFocus()
    {
        if (_focusIndex == HomeIndex)
        {
            RequestTitle();
        }
        else
        {
            RequestRestart();
        }
    }

    /// <summary>
    /// 再戦を知らせる
    /// </summary>
    public void RequestRestart()
    {
        RestartRequested?.Invoke();
    }

    /// <summary>
    /// タイトルへ戻るのを知らせる
    /// </summary>
    public void RequestTitle()
    {
        TitleRequested?.Invoke();
    }

    /// <summary>
    /// HOME がクリックされた
    /// </summary>
    private void OnHomeClicked()
    {
        _focusIndex = HomeIndex;
        RefreshFocus();
        UiEventSystem.ClearSelection();
        RequestTitle();
    }

    /// <summary>
    /// 再戦がクリックされた
    /// </summary>
    private void OnRestartClicked()
    {
        _focusIndex = RestartIndex;
        RefreshFocus();
        UiEventSystem.ClearSelection();
        RequestRestart();
    }

    /// <summary>
    /// 選んでいるボタンにだけ枠を出す
    /// </summary>
    private void RefreshFocus()
    {
        for (int i = 0; i < _focusFrames.Length; i++)
        {
            _focusFrames[i].SetActive(i == _focusIndex);
        }
    }
}
