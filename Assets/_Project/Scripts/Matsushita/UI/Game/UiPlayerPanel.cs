using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 画面の左右どちらかのプレイヤーの表示。キャラ、頭上の吹き出し（「あと10秒！」「待機中」「…」「解除成功！」など、待機中はクイックテキストの文）、クイックテキストの候補、残り3秒以下の赤い帯を管理する
/// </summary>
public class UiPlayerPanel : MonoBehaviour
{
    private const string WaitingText = "待機中";
    private const string ThinkingText = "…";
    private const float BlinkSpeed = 8f;
    private const float PopSeconds = 0.25f;
    private const float PopScale = 0.15f;

    [SerializeField, Tooltip("通常のキャラ")] private GameObject _normalVisual;
    [SerializeField, Tooltip("残り3秒以下の焦り顔のキャラ")] private GameObject _panicVisual;
    [SerializeField, Tooltip("爆発後のアフロ頭のキャラ")] private GameObject _afroVisual;
    [SerializeField, Tooltip("頭上の吹き出し")] private RectTransform _bubble;
    [SerializeField, Tooltip("吹き出しの文字")] private TMP_Text _bubbleText;
    [SerializeField, Tooltip("残り3秒以下に出す、画面の端の赤い帯")] private Image _edgeBand;
    [SerializeField, Tooltip("相手が解除中のときに出す「解除中！」")] private GameObject _defuseCaption;
    [SerializeField, Tooltip("立ち絵が手に持っている爆弾。手の位置に合わせて動かす。爆弾が飛ぶときの出発点・到着点になる")] private RectTransform _heldBomb;
    [SerializeField, Tooltip("方向キーを押し続けている間に出す、クイックテキストの円形の候補。なければ候補は出さない")] private UiQuickTextWheel _quickTextWheel;

    private string _holdText = "あと10秒！";
    private string _quickText = string.Empty;
    private float _quickTextRemaining;
    private string _caption;
    private bool _isHolding;
    private bool _isThinking;
    private bool _isDanger;
    private bool _isAfro;
    private float _popElapsed = PopSeconds;

    /// <summary>
    /// 爆弾を持っているか
    /// </summary>
    public bool IsHolding => _isHolding;

    /// <summary>
    /// 吹き出しに出ている文字
    /// </summary>
    public string BubbleText => _bubbleText.text;

    /// <summary>
    /// 手に持っている爆弾の四角（爆弾が飛ぶときの出発点・到着点）。まだ置いていない古いプレハブでは、パネル全体を返す
    /// </summary>
    public RectTransform HeldBombRect => _heldBomb != null ? _heldBomb : (RectTransform)transform;

    /// <summary>
    /// 手に持っている爆弾が出ているか
    /// </summary>
    public bool IsHeldBombVisible => _heldBomb != null && _heldBomb.gameObject.activeSelf;

    /// <summary>
    /// クイックテキストを出せるか（爆弾を持っていなくて、相手が解除中でないとき）
    /// </summary>
    public bool IsQuickTextAvailable => !_isHolding && (_defuseCaption == null || !_defuseCaption.activeSelf);

    /// <summary>
    /// 吹き出しを弾ませ、残り3秒以下は赤い帯を点滅させる。クイックテキストを出している間は、時間が来たら「待機中」に戻す
    /// </summary>
    private void Update()
    {
        if (_quickTextRemaining > 0f)
        {
            _quickTextRemaining -= Time.unscaledDeltaTime;

            if (_quickTextRemaining <= 0f)
            {
                RefreshBubble();
            }
        }

        if (_popElapsed < PopSeconds)
        {
            _popElapsed += Time.unscaledDeltaTime;
            float rate = Mathf.Clamp01(_popElapsed / PopSeconds);
            _bubble.localScale = Vector3.one * (1f + PopScale * Mathf.Sin(Mathf.PI * rate));
        }

        if (_isDanger)
        {
            Color color = _edgeBand.color;
            color.a = Mathf.Lerp(0.15f, 0.9f, (Mathf.Sin(Time.unscaledTime * BlinkSpeed) + 1f) * 0.5f);
            _edgeBand.color = color;
        }
    }

    /// <summary>
    /// 爆弾を持っているかを切り替える。持っていれば「あと10秒！」、持っていなければ「待機中」を吹き出しに出す
    /// </summary>
    /// <param name="isHolding">持っていれば true</param>
    public void SetHolding(bool isHolding)
    {
        _isHolding = isHolding;

        if (!isHolding)
        {
            _isThinking = false;
            SetDanger(false);
        }
        else
        {
            ClearQuickText();
            CloseQuickTextWheel();
        }

        RefreshBubble();
    }

    /// <summary>
    /// 吹き出しをクイックテキストの文にする（待機中のときだけ）。決めた時間がたつと「待機中」に戻る。出ている文があれば置き換える
    /// </summary>
    /// <param name="text">吹き出しに出す文</param>
    /// <param name="seconds">出しておく時間（秒）</param>
    /// <returns>出せたら true（持っているとき、解除中のときは出さない）</returns>
    public bool ShowQuickText(string text, float seconds)
    {
        if (!IsQuickTextAvailable)
        {
            return false;
        }

        _quickText = text;
        _quickTextRemaining = seconds;
        RefreshBubble();

        if (Application.isPlaying)
        {
            _popElapsed = 0f;
        }

        return true;
    }

    /// <summary>
    /// 出しているクイックテキストを消して、「待機中」に戻す
    /// </summary>
    public void ClearQuickText()
    {
        _quickTextRemaining = 0f;
        RefreshBubble();
    }

    /// <summary>
    /// クイックテキストの円形の候補を出して、選んでいる方向を光らせる
    /// </summary>
    /// <param name="texts">文（画面の上・下・左・右の順）</param>
    /// <param name="selected">選んでいる方向</param>
    public void OpenQuickTextWheel(string[] texts, InputDirection selected)
    {
        if (_quickTextWheel != null && IsQuickTextAvailable)
        {
            _quickTextWheel.Open(texts, selected);
        }
    }

    /// <summary>
    /// クイックテキストの円形の候補を閉じる
    /// </summary>
    public void CloseQuickTextWheel()
    {
        if (_quickTextWheel != null && _quickTextWheel.IsOpen)
        {
            _quickTextWheel.Close();
        }
    }

    /// <summary>
    /// 手に持っている爆弾を出す／隠す
    /// </summary>
    /// <param name="isVisible">出すなら true</param>
    public void SetHeldBomb(bool isVisible)
    {
        if (_heldBomb != null)
        {
            _heldBomb.gameObject.SetActive(isVisible);
        }
    }

    /// <summary>
    /// 持っているときに吹き出しへ出す文字を設定する（例：「あと9秒！」）
    /// </summary>
    /// <param name="holdText">吹き出しの文字</param>
    public void SetHoldText(string holdText)
    {
        _holdText = holdText;
        RefreshBubble();
    }

    /// <summary>
    /// CPUが考えている間、吹き出しを「…」にする
    /// </summary>
    /// <param name="isThinking">考え中なら true</param>
    public void SetThinking(bool isThinking)
    {
        _isThinking = isThinking;
        RefreshBubble();
    }

    /// <summary>
    /// 残り3秒以下の危険な状態かを切り替える（画面の端の赤い帯が点滅し、キャラが焦り顔になる）
    /// </summary>
    /// <param name="isDanger">危険なら true</param>
    public void SetDanger(bool isDanger)
    {
        _isDanger = isDanger;
        _edgeBand.gameObject.SetActive(isDanger);
        RefreshVisual();
    }

    /// <summary>
    /// アフロ頭の見た目に切り替える
    /// </summary>
    /// <param name="isAfro">アフロなら true</param>
    public void SetAfro(bool isAfro)
    {
        _isAfro = isAfro;
        RefreshVisual();
    }

    /// <summary>
    /// 状態に合ったキャラの見た目を1つだけ出す（アフロ、焦り顔、通常の順に優先）
    /// </summary>
    private void RefreshVisual()
    {
        _afroVisual.SetActive(_isAfro);
        _panicVisual.SetActive(!_isAfro && _isDanger);
        _normalVisual.SetActive(!_isAfro && !_isDanger);
    }

    /// <summary>
    /// 「解除中！」の表示を出す／隠す（解除に挑戦している相手側のパネルに出す）
    /// </summary>
    /// <param name="isVisible">出すなら true</param>
    public void SetDefuseCaption(bool isVisible)
    {
        _defuseCaption.SetActive(isVisible);

        if (isVisible)
        {
            ClearQuickText();
            CloseQuickTextWheel();
        }
    }

    /// <summary>
    /// 吹き出しに好きな文字を出す（例：「解除成功！」）。出している間は、持っているか・考え中かよりも優先する
    /// </summary>
    /// <param name="caption">出す文字。null か空なら、元の吹き出しに戻す</param>
    public void SetCaption(string caption)
    {
        _caption = caption;
        RefreshBubble();
    }

    /// <summary>
    /// 状態に合った文字を吹き出しに出す（好きな文字があれば何より先に出す）。文字が変わったときは弾ませる
    /// </summary>
    private void RefreshBubble()
    {
        string text = WaitingText;

        if (!string.IsNullOrEmpty(_caption))
        {
            text = _caption;
        }
        else if (_isHolding)
        {
            text = _isThinking ? ThinkingText : _holdText;
        }
        else if (_quickTextRemaining > 0f)
        {
            text = _quickText;
        }

        if (_bubbleText.text == text)
        {
            return;
        }

        _bubbleText.text = text;

        if (Application.isPlaying)
        {
            _popElapsed = 0f;
        }
    }
}
