using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// リザルト画面の表示。勝ち数・勝者・各ラウンドの結果の表を順番に出し、左下の「HOME」と右下の「再戦」の選択を管理する。
/// キー操作は UiResultInput、マウスのクリックはこのクラスが受け取る
/// </summary>
public class UiResultView : MonoBehaviour
{
    private const int HomeIndex = 0;
    private const int RestartIndex = 1;

    // 順番に出す動きのタイミング（表示を始めてからの秒数）
    private const float CharacterStartTime = 0.2f;
    private const float CharacterDuration = 0.55f;
    private const float TagStartTime = 0.8f;
    private const float TagDuration = 0.4f;
    private const float ScoreStartTime = 1.3f;
    private const float ScoreStepInterval = 0.23f;
    private const float RowsStartTime = 1.9f;
    private const float RowInterval = 0.3f;
    private const float RowDuration = 0.4f;
    private const float ButtonsDelay = 0.3f;
    private const float ButtonsDuration = 0.35f;

    [SerializeField, Tooltip("キャラ（P1、P2の順）")] private UiResultPlayer[] _players;
    [SerializeField, Tooltip("上の勝ち数（P1側）")] private TMP_Text _player1ScoreText;
    [SerializeField, Tooltip("上の勝ち数（P2側）")] private TMP_Text _player2ScoreText;
    [SerializeField, Tooltip("表の行（上から順）。ラウンドが多いときは入りきる分だけ出す")] private UiResultRow[] _rows;
    [SerializeField, Tooltip("左下の「HOME」ボタン（タイトルへ）")] private Button _homeButton;
    [SerializeField, Tooltip("右下の「再戦」ボタン（もう一度）")] private Button _restartButton;
    [SerializeField, Tooltip("選んでいるボタンに出す枠（HOME、再戦の順）")] private GameObject[] _focusFrames;
    [SerializeField, Tooltip("オフにすると、順番に出さずに最初から全部出す")] private bool _isIntroEnabled = true;

    private int _focusIndex = RestartIndex;
    private int _player1Wins;
    private int _player2Wins;
    private int _shownRowCount;
    private bool _isReady;
    private CanvasGroup _homeGroup;
    private CanvasGroup _restartGroup;

    /// <summary>
    /// 再戦が選ばれたときに呼ばれる
    /// </summary>
    public event Action RestartRequested;

    /// <summary>
    /// HOME（タイトルへ）が選ばれたときに呼ばれる
    /// </summary>
    public event Action TitleRequested;

    /// <summary>
    /// 順番に出す動きが終わって、ボタンを選べるか
    /// </summary>
    public bool IsReady => _isReady;

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
    /// 勝ち数・勝者・各ラウンドの結果を表示に反映し、順番に出す動きを始める（選んでいるボタンは再戦に戻す）
    /// </summary>
    /// <param name="winnerIndex">試合に勝った側（0：P1、1：P2）</param>
    /// <param name="player1Name">P1の名前</param>
    /// <param name="player2Name">P2の名前</param>
    /// <param name="rounds">ラウンドごとの結果。先頭が1ラウンド目</param>
    public void Show(int winnerIndex, string player1Name, string player2Name, UiResultRound[] rounds)
    {
        _players[0].Show(winnerIndex == 0, player1Name);
        _players[1].Show(winnerIndex == 1, player2Name);

        _player1Wins = 0;
        _player2Wins = 0;

        foreach (UiResultRound round in rounds)
        {
            if (round.WinnerIndex == 0)
            {
                _player1Wins++;
            }
            else
            {
                _player2Wins++;
            }
        }

        _shownRowCount = 0;

        for (int i = 0; i < _rows.Length; i++)
        {
            if (i < rounds.Length)
            {
                _rows[i].Show(i + 1, rounds[i]);
                _shownRowCount++;
            }
            else
            {
                _rows[i].Hide();
            }
        }

        _focusIndex = RestartIndex;
        StopAllCoroutinesIfPlaying();

        // エディタで組み立てているときは動かさず、そのまま全部出した状態にする
        if (!Application.isPlaying || !_isIntroEnabled)
        {
            FinishIntro();
            return;
        }

        PrepareIntro();
        StartCoroutine(PlayIntro());
    }

    /// <summary>
    /// 選んでいるボタンを左右に動かす（端で止まる。順番に出している間は動かない）
    /// </summary>
    /// <param name="delta">左なら -1、右なら 1</param>
    public void MoveFocus(int delta)
    {
        if (!_isReady)
        {
            return;
        }

        int next = Mathf.Clamp(_focusIndex + delta, 0, _focusFrames.Length - 1);

        if (next != _focusIndex)
        {
            SoundPlayer.PlaySe(SoundId.Cursor);
        }

        _focusIndex = next;
        RefreshFocus();
    }

    /// <summary>
    /// 選んでいるボタンを決定する。順番に出している間は、動きを飛ばして最後まで出す
    /// </summary>
    public void DecideFocus()
    {
        if (!_isReady)
        {
            FinishIntro();
            return;
        }

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
    /// 選んでいるボタンにだけ枠を出す（順番に出している間は出さない）
    /// </summary>
    private void RefreshFocus()
    {
        for (int i = 0; i < _focusFrames.Length; i++)
        {
            _focusFrames[i].SetActive(_isReady && i == _focusIndex);
        }
    }

    /// <summary>
    /// 動いているコルーチンを止める（実行中だけ）
    /// </summary>
    private void StopAllCoroutinesIfPlaying()
    {
        if (Application.isPlaying)
        {
            StopAllCoroutines();
        }
    }

    /// <summary>
    /// 順番に出す前の、何も出ていない状態にする
    /// </summary>
    private void PrepareIntro()
    {
        _isReady = false;

        foreach (UiResultPlayer player in _players)
        {
            player.SetCharacterProgress(0f);
            player.SetTagProgress(0f);
        }

        _player1ScoreText.text = "0";
        _player2ScoreText.text = "0";

        for (int i = 0; i < _shownRowCount; i++)
        {
            _rows[i].SetProgress(0f);
        }

        SetButtonsAlpha(0f);
        RefreshFocus();
    }

    /// <summary>
    /// 全部出した状態にして、ボタンを選べるようにする
    /// </summary>
    private void FinishIntro()
    {
        StopAllCoroutinesIfPlaying();

        if (Application.isPlaying)
        {
            foreach (UiResultPlayer player in _players)
            {
                player.SetCharacterProgress(1f);
                player.SetTagProgress(1f);
            }

            for (int i = 0; i < _shownRowCount; i++)
            {
                _rows[i].SetProgress(1f);
            }

            SetButtonsAlpha(1f);
        }

        _player1ScoreText.text = _player1Wins.ToString();
        _player2ScoreText.text = _player2Wins.ToString();
        _isReady = true;
        RefreshFocus();
    }

    /// <summary>
    /// キャラ → 「WIN」「LOSE」 → 勝ち数 → 表の各行 → ボタンの順に出す
    /// </summary>
    /// <returns>コルーチン</returns>
    private IEnumerator PlayIntro()
    {
        float startTime = Time.unscaledTime;

        yield return WaitUntilTime(startTime, CharacterStartTime);
        StartCoroutine(Animate(CharacterDuration, SetCharacters));

        yield return WaitUntilTime(startTime, TagStartTime);
        StartCoroutine(Animate(TagDuration, SetTags));

        yield return WaitUntilTime(startTime, ScoreStartTime);
        StartCoroutine(CountScore());

        yield return WaitUntilTime(startTime, RowsStartTime);

        for (int i = 0; i < _shownRowCount; i++)
        {
            UiResultRow row = _rows[i];
            StartCoroutine(Animate(RowDuration, progress => row.SetProgress(progress)));
            yield return WaitUntilTime(startTime, RowsStartTime + (i + 1) * RowInterval);
        }

        yield return new WaitForSecondsRealtime(ButtonsDelay);
        yield return Animate(ButtonsDuration, SetButtonsAlpha);
        FinishIntro();
    }

    /// <summary>
    /// 表示を始めてから指定の秒数が経つまで待つ
    /// </summary>
    /// <param name="startTime">表示を始めた時刻</param>
    /// <param name="seconds">待つ秒数（表示を始めてからの）</param>
    /// <returns>コルーチン</returns>
    private static IEnumerator WaitUntilTime(float startTime, float seconds)
    {
        while (Time.unscaledTime - startTime < seconds)
        {
            yield return null;
        }
    }

    /// <summary>
    /// 進み具合を 0 から 1 まで動かしながら、毎フレーム渡す
    /// </summary>
    /// <param name="duration">かける秒数</param>
    /// <param name="onProgress">進み具合を受け取る処理</param>
    /// <returns>コルーチン</returns>
    private static IEnumerator Animate(float duration, Action<float> onProgress)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            onProgress(elapsed / duration);
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }

        onProgress(1f);
    }

    /// <summary>
    /// 2人のキャラの出てくる動きを進める
    /// </summary>
    /// <param name="progress">進み具合（0〜1）</param>
    private void SetCharacters(float progress)
    {
        foreach (UiResultPlayer player in _players)
        {
            player.SetCharacterProgress(progress);
        }
    }

    /// <summary>
    /// 2人の「WIN」「LOSE」の出てくる動きを進める
    /// </summary>
    /// <param name="progress">進み具合（0〜1）</param>
    private void SetTags(float progress)
    {
        foreach (UiResultPlayer player in _players)
        {
            player.SetTagProgress(progress);
        }
    }

    /// <summary>
    /// 勝ち数を 0 から数え上げる
    /// </summary>
    /// <returns>コルーチン</returns>
    private IEnumerator CountScore()
    {
        int maxWins = Mathf.Max(_player1Wins, _player2Wins);

        for (int step = 1; step <= maxWins; step++)
        {
            _player1ScoreText.text = Mathf.Min(step, _player1Wins).ToString();
            _player2ScoreText.text = Mathf.Min(step, _player2Wins).ToString();
            yield return new WaitForSecondsRealtime(ScoreStepInterval);
        }
    }

    /// <summary>
    /// 「HOME」「再戦」ボタンの透明度を変える。透明な間は押せない
    /// </summary>
    /// <param name="alpha">透明度（0：見えない、1：見える）</param>
    private void SetButtonsAlpha(float alpha)
    {
        if (_homeGroup == null)
        {
            _homeGroup = GetOrAddGroup(_homeButton.gameObject);
            _restartGroup = GetOrAddGroup(_restartButton.gameObject);
        }

        bool isClickable = alpha >= 1f;

        _homeGroup.alpha = alpha;
        _homeGroup.blocksRaycasts = isClickable;
        _restartGroup.alpha = alpha;
        _restartGroup.blocksRaycasts = isClickable;
    }

    /// <summary>
    /// 透明度を変える部品を返す（なければ付ける）
    /// </summary>
    /// <param name="target">付ける先</param>
    /// <returns>透明度の部品</returns>
    private static CanvasGroup GetOrAddGroup(GameObject target)
    {
        if (!target.TryGetComponent(out CanvasGroup group))
        {
            group = target.AddComponent<CanvasGroup>();
        }

        return group;
    }
}
