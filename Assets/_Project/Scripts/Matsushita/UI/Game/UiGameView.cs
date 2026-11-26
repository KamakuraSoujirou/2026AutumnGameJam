using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// 試合画面の表示をまとめる。ゲームの進行（残り時間・パス回数・持ち主など）を渡すと、画面に反映する
/// </summary>
/// <remarks>
/// プレイヤーは 0 が P1（左）、1 が P2（右）。このクラスは表示だけで、勝敗やタイマーの進行はしない
/// </remarks>
public class UiGameView : MonoBehaviour
{
    private const int PlayerCount = 2;
    private const float DangerSeconds = 3f;
    private const char TimeSeparator = ':';
    private const float PenaltyShowSeconds = 0.9f;
    private const float PenaltyRiseDistance = 70f;
    private const float FuseGrowSeconds = 0.35f;
    private const float FlightArcHeight = 160f;
    private const float FlyingEndScale = 0.55f;
    private const float BannerPopSeconds = 0.25f;
    private const float ExplosionSeconds = 0.5f;
    private const float ExplosionFlashAlpha = 0.7f;

    [Header("上の表示")]
    [SerializeField, Tooltip("ラウンド（R1）")] private TMP_Text _roundText;
    [SerializeField, Tooltip("パス回数")] private TMP_Text _passText;
    [SerializeField, Tooltip("難易度（1〜4）")] private TMP_Text _stageText;
    [SerializeField, Tooltip("「戻る」が選ばれたときに呼ぶ（タイトルへ戻るなど）")] private UnityEvent _onBackRequested;
    [SerializeField, Tooltip("「戻る」をマウスで押すためのボタン")] private Button _backButton;
    [SerializeField, Tooltip("爆弾の液晶の解除ボタンをマウスで押すためのボタン")] private Button _defuseButton;

    [Header("下の帯")]
    [SerializeField, Tooltip("プレイヤー名（P1、P2の順）")] private TMP_Text[] _nameTexts = new TMP_Text[PlayerCount];
    [SerializeField, Tooltip("勝ち星（P1、P2の順）")] private UiWinStars[] _winStars = new UiWinStars[PlayerCount];
    [SerializeField, Tooltip("ギミック名")] private TMP_Text _gimmickText;

    [Header("盤面")]
    [SerializeField, Tooltip("爆弾")] private UiBombView _bomb;
    [SerializeField, Tooltip("プレイヤーの表示（P1、P2の順）")] private UiPlayerPanel[] _panels = new UiPlayerPanel[PlayerCount];
    [SerializeField, Tooltip("導火線（P1側、P2側の順）")] private UiFuse[] _fuses = new UiFuse[PlayerCount];
    [SerializeField, Tooltip("爆弾が飛んでいる間に動かす爆弾")] private RectTransform _flyingBomb;
    [SerializeField, Tooltip("「−2秒」の表示（P1、P2の順に別の場所）")] private TMP_Text[] _penaltyTexts = new TMP_Text[PlayerCount];

    [Header("演出")]
    [SerializeField, Tooltip("ラウンド開始のバナー全体")] private GameObject _bannerRoot;
    [SerializeField, Tooltip("バナーの見出し（ROUND 1）")] private TMP_Text _bannerTitle;
    [SerializeField, Tooltip("バナーのギミック名（配線切断式）")] private TMP_Text _bannerGimmick;
    [SerializeField, Tooltip("バナーの一行説明")] private TMP_Text _bannerDescription;
    [SerializeField, Tooltip("解除中に画面を暗くする幕")] private GameObject _dimmer;
    [SerializeField, Tooltip("爆発のときの白い閃光")] private Image _explosionFlash;
    [SerializeField, Tooltip("爆発の文字（ドカーン！）")] private RectTransform _explosionText;

    [Header("リザルト")]
    [SerializeField, Tooltip("リザルト全体")] private GameObject _resultRoot;
    [SerializeField, Tooltip("勝者の表示")] private TMP_Text _resultTitle;
    [SerializeField, Tooltip("勝ち星の数の表示")] private TMP_Text _resultScore;

    private readonly Vector2[] _penaltyStartPositions = new Vector2[PlayerCount];
    private readonly Coroutine[] _penaltyRoutines = new Coroutine[PlayerCount];
    private int _holderIndex = -1;
    private float _remainingTime = 10f;
    private float _holdSeconds = 10f;
    private float _fuseGrow = 1f;
    private bool _isFlying;
    private bool _isDanger;

    /// <summary>
    /// 解除ボタンがマウスで押されたときに呼ばれる（解除を始められるかの判断は、ゲーム進行側で行う）
    /// </summary>
    public event Action DefuseRequested;

    /// <summary>
    /// 爆弾のひし形ボタンがマウスでクリックされたときに呼ばれる。マウスは誰の入力か分からないので、持ち主の入力として扱うかはゲーム進行側で決める
    /// </summary>
    public event Action<InputDirection> DirectionClicked;

    /// <summary>
    /// 爆弾の表示
    /// </summary>
    public UiBombView Bomb => _bomb;

    /// <summary>
    /// マウスのクリックを受けられるようにして、ボタンの押下を結びつける
    /// </summary>
    private void Awake()
    {
        EnsureEventSystem();

        for (int i = 0; i < PlayerCount; i++)
        {
            _penaltyStartPositions[i] = _penaltyTexts[i].rectTransform.anchoredPosition;
        }

        _backButton.onClick.AddListener(RequestBack);
        _defuseButton.onClick.AddListener(OnDefuseClicked);
        _bomb.ButtonClicked += OnButtonClicked;
    }

    /// <summary>
    /// 導火線が伸びる動きを進める
    /// </summary>
    private void Update()
    {
        if (_fuseGrow >= 1f)
        {
            return;
        }

        _fuseGrow = Mathf.Min(1f, _fuseGrow + Time.unscaledDeltaTime / FuseGrowSeconds);
        ApplyFuse();
    }

    /// <summary>
    /// 残り時間を爆弾のタイマーと導火線に反映する。持っている側が3秒以下なら、赤い帯の点滅・焦り顔・爆弾の揺れも始める
    /// </summary>
    /// <param name="seconds">残り時間（秒）</param>
    public void SetTime(float seconds)
    {
        _remainingTime = Mathf.Max(0f, seconds);
        _isDanger = _holderIndex >= 0 && _remainingTime <= DangerSeconds;

        _bomb.SetTime(_remainingTime.ToString("00.00").Replace('.', TimeSeparator), _isDanger);
        _bomb.SetShaking(_isDanger);

        for (int i = 0; i < _panels.Length; i++)
        {
            _panels[i].SetDanger(_isDanger && i == _holderIndex);
        }

        ApplyFuse();
    }

    /// <summary>
    /// 受け取るたびの持ち時間を設定する。導火線の長さの基準と、吹き出しの「あとN秒！」が変わる
    /// </summary>
    /// <param name="seconds">持ち時間（秒）。通常は10、時間短縮のときは9〜5</param>
    public void SetHoldTime(float seconds)
    {
        _holdSeconds = Mathf.Max(1f, seconds);
        string text = $"あと{Mathf.RoundToInt(seconds)}秒！";

        foreach (UiPlayerPanel panel in _panels)
        {
            panel.SetHoldText(text);
        }

        ApplyFuse();
    }

    /// <summary>
    /// 上のラウンド表示を設定する（R1、R2…）
    /// </summary>
    /// <param name="round">ラウンド数（1から）</param>
    public void SetRound(int round)
    {
        _roundText.text = $"R{round}";
    }

    /// <summary>
    /// ラウンド内のパス回数と難易度（1〜4）を表示する
    /// </summary>
    /// <param name="passCount">パス回数</param>
    /// <param name="stage">難易度（1〜4）</param>
    public void SetPass(int passCount, int stage)
    {
        _passText.text = $"パス回数 {passCount}";
        _stageText.text = $"難易度 {stage}";
    }

    /// <summary>
    /// 下の帯のプレイヤー名を設定する（「P1」「P2」「COM」など）
    /// </summary>
    /// <param name="leftName">左側の名前</param>
    /// <param name="rightName">右側の名前</param>
    public void SetPlayerNames(string leftName, string rightName)
    {
        _nameTexts[0].text = leftName;
        _nameTexts[1].text = rightName;
    }

    /// <summary>
    /// 下の中央の帯にギミック名を出す
    /// </summary>
    /// <param name="gimmickName">ギミック名（例：配線切断式）</param>
    public void SetGimmickName(string gimmickName)
    {
        _gimmickText.text = gimmickName;
    }

    /// <summary>
    /// 勝ち星の数と、何本先取かを表示する
    /// </summary>
    /// <param name="player1Wins">P1が取ったラウンド数</param>
    /// <param name="player2Wins">P2が取ったラウンド数</param>
    /// <param name="winsToMatch">何本先取で勝ちか</param>
    public void SetWins(int player1Wins, int player2Wins, int winsToMatch)
    {
        _winStars[0].SetWinsToMatch(winsToMatch);
        _winStars[1].SetWinsToMatch(winsToMatch);
        _winStars[0].SetWins(player1Wins);
        _winStars[1].SetWins(player2Wins);
    }

    /// <summary>
    /// 爆弾を持っている側を切り替える。持っている側の吹き出しは「あと10秒！」、導火線はその側に伸びる
    /// </summary>
    /// <param name="holderIndex">持っている側（0：P1、1：P2、-1：どちらも持っていない）</param>
    public void SetHolder(int holderIndex)
    {
        _holderIndex = holderIndex;
        _fuseGrow = Application.isPlaying ? 0f : 1f;

        for (int i = 0; i < _panels.Length; i++)
        {
            _panels[i].SetHolding(i == holderIndex);
        }

        _bomb.SetShaking(false);
        ApplyFuse();
    }

    /// <summary>
    /// CPUが考えている吹き出し「…」を出す／戻す
    /// </summary>
    /// <param name="playerIndex">0：P1、1：P2</param>
    /// <param name="isThinking">考え中なら true</param>
    public void SetThinking(int playerIndex, bool isThinking)
    {
        _panels[playerIndex].SetThinking(isThinking);
    }

    /// <summary>
    /// 「戻る」が選ばれたことを知らせる
    /// </summary>
    public void RequestBack()
    {
        _onBackRequested.Invoke();
    }

    /// <summary>
    /// ラウンド開始のバナーを出す
    /// </summary>
    /// <param name="title">見出し（例：ROUND 1）</param>
    /// <param name="gimmickName">ギミック名（例：配線切断式）</param>
    /// <param name="description">一行説明</param>
    public void ShowBanner(string title, string gimmickName, string description)
    {
        _bannerTitle.text = title;
        _bannerGimmick.text = gimmickName;
        _bannerDescription.text = description;
        _bannerRoot.SetActive(true);

        if (Application.isPlaying)
        {
            StartCoroutine(PlayPop(_bannerRoot.transform, BannerPopSeconds, 0.6f));
        }
    }

    /// <summary>
    /// ラウンド開始のバナーを隠す
    /// </summary>
    public void HideBanner()
    {
        _bannerRoot.SetActive(false);
    }

    /// <summary>
    /// 不正解のペナルティ「−2秒」を、間違えた側に出す
    /// </summary>
    /// <param name="playerIndex">間違えた側（0：P1、1：P2）</param>
    /// <param name="penaltySeconds">減る秒数</param>
    public void ShowPenalty(int playerIndex, float penaltySeconds)
    {
        StopPenalty(playerIndex);
        _penaltyTexts[playerIndex].text = $"−{penaltySeconds:0.#}秒";
        _penaltyRoutines[playerIndex] = StartCoroutine(PlayPenalty(playerIndex));
    }

    /// <summary>
    /// 解除の挑戦を始める／終える。画面を暗くして、待機している相手側に「解除中！」を出す
    /// </summary>
    /// <param name="challengerIndex">解除に挑戦する側（0：P1、1：P2）</param>
    /// <param name="isChallenging">挑戦中なら true</param>
    public void SetDefuse(int challengerIndex, bool isChallenging)
    {
        int opponentIndex = 1 - challengerIndex;
        _dimmer.SetActive(isChallenging);
        _panels[opponentIndex].SetDefuseCaption(isChallenging);
        _bomb.SetDefuseState(isChallenging ? UiDefuseState.Challenging : UiDefuseState.Used);
    }

    /// <summary>
    /// 爆弾が相手のキャラへ飛ぶ演出を再生する。飛んでいる間は、導火線が消えて時間も止まる
    /// </summary>
    /// <param name="toPlayerIndex">受け取る側（0：P1、1：P2）</param>
    /// <param name="duration">飛んでいる時間（秒）</param>
    /// <param name="onArrived">届いたときに呼ぶ処理（ここで SetHolder を呼ぶと、受け取った側に導火線が伸びる）</param>
    public void ThrowBomb(int toPlayerIndex, float duration, Action onArrived)
    {
        _isFlying = true;
        SetHolder(-1);
        StartCoroutine(PlayFlight(toPlayerIndex, duration, onArrived));
    }

    /// <summary>
    /// 爆発の演出を出す。負けた側のキャラはアフロ頭になる
    /// </summary>
    /// <param name="loserIndex">負けた側（0：P1、1：P2）</param>
    public void ShowExplosion(int loserIndex)
    {
        _explosionFlash.gameObject.SetActive(true);
        _explosionText.gameObject.SetActive(true);
        SetHolder(-1);
        _panels[loserIndex].SetAfro(true);

        if (Application.isPlaying)
        {
            StartCoroutine(PlayExplosion());
        }
    }

    /// <summary>
    /// 爆発の演出を片付けて、キャラを元に戻す
    /// </summary>
    public void ClearExplosion()
    {
        _explosionFlash.gameObject.SetActive(false);
        _explosionText.gameObject.SetActive(false);
        _explosionText.localScale = Vector3.one;

        foreach (UiPlayerPanel panel in _panels)
        {
            panel.SetAfro(false);
        }
    }

    /// <summary>
    /// リザルトを出す
    /// </summary>
    /// <param name="winnerIndex">勝った側（0：P1、1：P2）</param>
    /// <param name="player1Wins">P1が取ったラウンド数</param>
    /// <param name="player2Wins">P2が取ったラウンド数</param>
    public void ShowResult(int winnerIndex, int player1Wins, int player2Wins)
    {
        _resultTitle.text = winnerIndex == 0 ? "P1の勝ち！" : "P2の勝ち！";
        _resultScore.text = $"{player1Wins} − {player2Wins}";
        _resultRoot.SetActive(true);
    }

    /// <summary>
    /// リザルトを隠す
    /// </summary>
    public void HideResult()
    {
        _resultRoot.SetActive(false);
    }

    /// <summary>
    /// 演出をすべて片付ける（新しい試合の前に呼ぶ）
    /// </summary>
    public void ResetEffects()
    {
        StopAllCoroutines();
        _isFlying = false;
        _flyingBomb.gameObject.SetActive(false);
        _bomb.gameObject.SetActive(true);
        _dimmer.SetActive(false);
        HideBanner();
        HideResult();
        ClearExplosion();

        for (int i = 0; i < PlayerCount; i++)
        {
            _panels[i].SetDefuseCaption(false);
            _panels[i].SetThinking(false);
            StopPenalty(i);
        }
    }

    /// <summary>
    /// ひし形ボタンが押されたことを知らせる
    /// </summary>
    /// <param name="direction">押された方向</param>
    private void OnButtonClicked(InputDirection direction)
    {
        DirectionClicked?.Invoke(direction);
    }

    /// <summary>
    /// 解除ボタンが押されたことを知らせる
    /// </summary>
    private void OnDefuseClicked()
    {
        DefuseRequested?.Invoke();
    }

    /// <summary>
    /// シーンに EventSystem がなければ作る（これがないとクリックが届かない）
    /// </summary>
    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
        InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
        inputModule.AssignDefaultActions();
    }

    /// <summary>
    /// 持ち主の側の導火線だけを出し、残り時間と伸びる動きに合わせた長さにする
    /// </summary>
    private void ApplyFuse()
    {
        float ratio = Mathf.Clamp01(_remainingTime / _holdSeconds);
        float grow = _fuseGrow * _fuseGrow * (3f - 2f * _fuseGrow);

        for (int i = 0; i < _fuses.Length; i++)
        {
            bool isVisible = !_isFlying && i == _holderIndex;
            _fuses[i].gameObject.SetActive(isVisible);

            if (isVisible)
            {
                _fuses[i].SetRatio(ratio * grow);
                _fuses[i].SetUrgent(_isDanger);
            }
        }
    }

    /// <summary>
    /// 四角の真ん中の、画面上の位置を返す
    /// </summary>
    /// <param name="rect">調べる四角</param>
    /// <returns>真ん中の位置</returns>
    private static Vector3 GetCenter(RectTransform rect)
    {
        return rect.TransformPoint(rect.rect.center);
    }

    /// <summary>
    /// 「−2秒」の動きを止めて、位置と透明度を元に戻して隠す（連続で呼ばれたときや、演出の片付け用）
    /// </summary>
    /// <param name="playerIndex">0：P1、1：P2</param>
    private void StopPenalty(int playerIndex)
    {
        if (_penaltyRoutines[playerIndex] != null)
        {
            StopCoroutine(_penaltyRoutines[playerIndex]);
            _penaltyRoutines[playerIndex] = null;
        }

        TMP_Text target = _penaltyTexts[playerIndex];
        target.rectTransform.anchoredPosition = _penaltyStartPositions[playerIndex];
        target.alpha = 1f;
        target.gameObject.SetActive(false);
    }

    /// <summary>
    /// 「−2秒」を少し上に動かしながら消す
    /// </summary>
    /// <param name="playerIndex">0：P1、1：P2</param>
    /// <returns>コルーチン</returns>
    private IEnumerator PlayPenalty(int playerIndex)
    {
        TMP_Text target = _penaltyTexts[playerIndex];
        RectTransform rect = target.rectTransform;
        Vector2 start = _penaltyStartPositions[playerIndex];
        target.gameObject.SetActive(true);

        for (float elapsed = 0f; elapsed < PenaltyShowSeconds; elapsed += Time.unscaledDeltaTime)
        {
            float rate = elapsed / PenaltyShowSeconds;
            rect.anchoredPosition = start + new Vector2(0f, PenaltyRiseDistance * rate);
            target.alpha = 1f - rate * rate;
            yield return null;
        }

        StopPenalty(playerIndex);
    }

    /// <summary>
    /// 爆弾を中央から相手のキャラへ、山なりに小さくしながら飛ばす。飛んでいる間、中央の爆弾は隠す
    /// </summary>
    /// <param name="toPlayerIndex">受け取る側</param>
    /// <param name="duration">飛んでいる時間（秒）</param>
    /// <param name="onArrived">届いたときに呼ぶ処理</param>
    /// <returns>コルーチン</returns>
    private IEnumerator PlayFlight(int toPlayerIndex, float duration, Action onArrived)
    {
        RectTransform bombRect = (RectTransform)_bomb.transform;
        Vector3 start = GetCenter(bombRect);
        Vector3 end = GetCenter((RectTransform)_panels[toPlayerIndex].transform);
        float startScale = bombRect.rect.width / _flyingBomb.rect.width;

        _bomb.gameObject.SetActive(false);
        _flyingBomb.gameObject.SetActive(true);

        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float rate = Mathf.Clamp01(elapsed / duration);
            float eased = rate * rate * (3f - 2f * rate);
            Vector3 position = Vector3.Lerp(start, end, eased);
            position.y += Mathf.Sin(Mathf.PI * eased) * FlightArcHeight;
            _flyingBomb.position = position;
            _flyingBomb.localScale = Vector3.one * Mathf.Lerp(startScale, FlyingEndScale, eased);
            _flyingBomb.localRotation = Quaternion.Euler(0f, 0f, -360f * eased * (toPlayerIndex == 0 ? -1f : 1f));
            yield return null;
        }

        _flyingBomb.gameObject.SetActive(false);
        _flyingBomb.localScale = Vector3.one;
        _flyingBomb.localRotation = Quaternion.identity;
        _bomb.gameObject.SetActive(true);
        _isFlying = false;
        onArrived?.Invoke();
    }

    /// <summary>
    /// 爆発の文字を勢いよく出して、閃光を薄れさせる
    /// </summary>
    /// <returns>コルーチン</returns>
    private IEnumerator PlayExplosion()
    {
        Color flash = _explosionFlash.color;

        for (float elapsed = 0f; elapsed < ExplosionSeconds; elapsed += Time.unscaledDeltaTime)
        {
            float rate = elapsed / ExplosionSeconds;
            flash.a = ExplosionFlashAlpha * (1f - rate);
            _explosionFlash.color = flash;
            float pop = rate < 0.4f ? Mathf.Lerp(0.3f, 1.15f, rate / 0.4f) : Mathf.Lerp(1.15f, 1f, (rate - 0.4f) / 0.6f);
            _explosionText.localScale = Vector3.one * pop;
            yield return null;
        }

        flash.a = 0f;
        _explosionFlash.color = flash;
        _explosionText.localScale = Vector3.one;
    }

    /// <summary>
    /// 出てくるときに、小さい状態から少し膨らんで落ち着く動きをつける
    /// </summary>
    /// <param name="target">動かす対象</param>
    /// <param name="seconds">かける時間（秒）</param>
    /// <param name="startScale">最初の大きさ</param>
    /// <returns>コルーチン</returns>
    private static IEnumerator PlayPop(Transform target, float seconds, float startScale)
    {
        for (float elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
        {
            float rate = elapsed / seconds;
            float scale = rate < 0.7f ? Mathf.Lerp(startScale, 1.08f, rate / 0.7f) : Mathf.Lerp(1.08f, 1f, (rate - 0.7f) / 0.3f);
            target.localScale = Vector3.one * scale;
            yield return null;
        }

        target.localScale = Vector3.one;
    }
}
