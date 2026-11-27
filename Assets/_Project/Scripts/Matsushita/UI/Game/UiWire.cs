using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 爆弾のひし形ボタン1つと、そこから伸びる配線1本の表示。色と状態に合わせて、4色×2状態の配線から1つだけ出す。
/// マウスが重なったら明るく、押している間は暗くする
/// </summary>
public class UiWire : MonoBehaviour
{
    private const int ColorCount = 4;
    private const float GoneAlpha = 0.3f;

    // この文字数までは1行のまま出す。それより長い言葉は折り返して、ひし形の中に収める
    private const int SingleLineMaxLength = 5;

    // 折り返すときの文字の枠（ひし形の中に収まる大きさ）と、いちばん小さくする文字の大きさ
    private const float WrapLabelWidth = 80f;
    private const float WrapLabelHeight = 64f;
    private const float WrapLabelMinFontSize = 10f;

    [SerializeField, Tooltip("つながっている配線の見た目（赤・青・黄・緑の順）")] private GameObject[] _normalVisuals = new GameObject[ColorCount];
    [SerializeField, Tooltip("切れている配線の見た目（赤・青・黄・緑の順）")] private GameObject[] _cutVisuals = new GameObject[ColorCount];
    [SerializeField, Tooltip("ひし形ボタンの見た目（赤・青・黄・緑の順）")] private GameObject[] _buttonVisuals = new GameObject[ColorCount];
    [SerializeField, Tooltip("マウスが重なったときのひし形ボタンの見た目＝light（赤・青・黄・緑の順。絵がないときは使わない）")] private GameObject[] _lightVisuals = new GameObject[ColorCount];
    [SerializeField, Tooltip("押している間のひし形ボタンの見た目＝blackout（赤・青・黄・緑の順。絵がないときは使わない）")] private GameObject[] _blackoutVisuals = new GameObject[ColorCount];
    [SerializeField, Tooltip("ボタン全体の透明度を変える用（線が消えたとき薄くする）")] private CanvasGroup _buttonGroup;
    [SerializeField, Tooltip("ボタンをマウスで押すためのボタン")] private Button _clickButton;
    [SerializeField, Tooltip("ボタンの文字。何も指定しなければ色名の頭文字（赤・青・黄・緑）")] private TMP_Text _label;
    [SerializeField, Tooltip("ボタンの隅に出す記号（○△□☆や数字）。出さないときは空")] private TMP_Text _symbolLabel;

    [SerializeField, Tooltip("配線の線を一時的に隠す（ONの間、つながっている線も切れている線も出さない。配線の絵が届くまでの間）")] private bool _isWireHidden;

    [Header("明るさの変化")]
    [SerializeField, Tooltip("素材の画像があるときに、ボタンの上に重ねる画像（素材と同じ形にする）")] private Image _artTint;
    [SerializeField, Tooltip("素材がないとき（仮の四角）に、ボタンの上に重ねる画像")] private Image _placeholderTint;
    [SerializeField, Range(0f, 0.6f), Tooltip("マウスが重なったときに白を重ねる濃さ")] private float _hoverStrength = 0.25f;
    [SerializeField, Range(0f, 0.6f), Tooltip("押している間に黒を重ねる濃さ")] private float _pressStrength = 0.3f;
    [SerializeField, Min(0f), Tooltip("押した見た目を最低でも見せる長さ（秒）。ちょんと押しても、暗くなったのが見える")] private float _minPressSeconds = 0.15f;
    [SerializeField, Min(0.001f), Tooltip("明るさが切り替わるのにかける長さ（秒）")] private float _changeSeconds = 0.05f;

    private static readonly string[] ColorNames = { "赤", "青", "黄", "緑" };

    /// <summary>
    /// ひし形ボタンの見た目（絵があるときの3状態）
    /// </summary>
    private enum ButtonLook
    {
        Normal,
        Light,
        Blackout
    }

    private ButtonLook _look;
    private string _customLabel;
    private int _colorIndex;
    private bool _isHovered;
    private bool _isHeld;
    private float _darkUntil;
    private float _tintLevel;

    private RectTransform _labelRect;
    private Vector2 _labelCenter;
    private Vector2 _labelSingleLineSize;
    private float _labelSingleLineMinFontSize;
    private bool _isLabelPrepared;

    /// <summary>
    /// ボタンをマウスで押すためのボタン
    /// </summary>
    public Button ClickButton => _clickButton;

    /// <summary>
    /// ボタンに出ている文字
    /// </summary>
    public string LabelText => _label.text;

    /// <summary>
    /// 押した見た目が終わるまでの残り時間（秒）。押していなければ 0
    /// </summary>
    public float PressRemainingSeconds => Mathf.Max(0f, _darkUntil - Time.unscaledTime);

    /// <summary>
    /// 見た目を目標に合わせる（重なっていれば明るく、押していれば暗く）。
    /// 3状態の絵があるときは絵を切り替え、ないときは白・黒を重ねて明るさを近づける
    /// </summary>
    private void Update()
    {
        bool isPressed = (_isHeld && _isHovered) || Time.unscaledTime < _darkUntil;
        float target = isPressed ? -_pressStrength : (_isHovered ? _hoverStrength : 0f);
        ButtonLook look = isPressed ? ButtonLook.Blackout : (_isHovered ? ButtonLook.Light : ButtonLook.Normal);
        bool isLookChanged = look != _look;
        _look = look;

        if (Mathf.Approximately(_tintLevel, target))
        {
            if (isLookChanged)
            {
                ApplyTint();
            }

            return;
        }

        float speed = Mathf.Max(_hoverStrength, _pressStrength, 0.01f) / _changeSeconds;
        _tintLevel = Mathf.MoveTowards(_tintLevel, target, speed * Time.unscaledDeltaTime);
        ApplyTint();
    }

    /// <summary>
    /// 隠れるときは、重なり・押下の状態を片付ける（戻ってきたときに暗いまま残らないように）
    /// </summary>
    private void OnDisable()
    {
        ResetPress();
    }

    /// <summary>
    /// キーで押したときの見た目を始める（最低限の長さだけ暗くして戻る）
    /// </summary>
    public void Press()
    {
        _darkUntil = Mathf.Max(_darkUntil, Time.unscaledTime + _minPressSeconds);
    }

    /// <summary>
    /// マウスが重なった
    /// </summary>
    public void PointerEntered()
    {
        _isHovered = true;
    }

    /// <summary>
    /// マウスが外れた
    /// </summary>
    public void PointerExited()
    {
        _isHovered = false;
    }

    /// <summary>
    /// マウスのボタンを押した
    /// </summary>
    public void PointerPressed()
    {
        _isHeld = true;
        _darkUntil = Mathf.Max(_darkUntil, Time.unscaledTime + _minPressSeconds);
    }

    /// <summary>
    /// マウスのボタンを離した
    /// </summary>
    public void PointerReleased()
    {
        _isHeld = false;
    }

    /// <summary>
    /// 重なり・押下の状態と明るさを元に戻す
    /// </summary>
    public void ResetPress()
    {
        _isHovered = false;
        _isHeld = false;
        _darkUntil = 0f;
        _tintLevel = 0f;
        _look = ButtonLook.Normal;
        ApplyTint();
    }

    /// <summary>
    /// 色と状態を指定して、ボタンと配線を表示する
    /// </summary>
    /// <param name="color">線の色（ボタンの色も同じ）</param>
    /// <param name="state">線の状態</param>
    public void Show(UiWireColor color, UiWireState state)
    {
        _colorIndex = (int)color;

        for (int i = 0; i < ColorCount; i++)
        {
            _normalVisuals[i].SetActive(!_isWireHidden && state == UiWireState.Normal && i == _colorIndex);
            _cutVisuals[i].SetActive(!_isWireHidden && state == UiWireState.Cut && i == _colorIndex);
            _buttonVisuals[i].SetActive(i == _colorIndex);
        }

        _buttonGroup.alpha = state == UiWireState.Gone ? GoneAlpha : 1f;
        RefreshLabel();
        ApplyTint();
    }

    /// <summary>
    /// ボタンに出す文字を設定する（暗号・順番スイッチで使う）。空にすると色名の頭文字に戻る
    /// </summary>
    /// <param name="text">ボタンの文字</param>
    public void SetLabel(string text)
    {
        _customLabel = text;
        RefreshLabel();
    }

    /// <summary>
    /// ボタンの隅に出す記号を設定する。空にすると隠す
    /// </summary>
    /// <param name="symbol">○△□☆や数字。出さないときは空</param>
    public void SetSymbol(string symbol)
    {
        _symbolLabel.text = symbol;
        _symbolLabel.gameObject.SetActive(!string.IsNullOrEmpty(symbol));
    }

    /// <summary>
    /// ボタンの文字を、指定があればそれ、なければ色名の頭文字にする
    /// </summary>
    private void RefreshLabel()
    {
        string text = string.IsNullOrEmpty(_customLabel) ? ColorNames[_colorIndex] : _customLabel;
        FitLabel(text);
        _label.text = text;
    }

    /// <summary>
    /// 文字の長さに合わせて、文字の枠と折り返しを切り替える。
    /// 短い言葉は今までどおり1行で大きく出し、長い言葉は2行以上に折り返して、ひし形からはみ出さないようにする
    /// </summary>
    /// <param name="text">ボタンに出す文字</param>
    private void FitLabel(string text)
    {
        PrepareLabel();

        bool isWrapped = text.Length > SingleLineMaxLength;
        Vector2 size = isWrapped ? new Vector2(WrapLabelWidth, WrapLabelHeight) : _labelSingleLineSize;

        // 枠の中心は変えずに、大きさだけ変える（基準点が中心でなくても動くように、中心から位置を計算し直す）
        _labelRect.sizeDelta = size;
        _labelRect.anchoredPosition = _labelCenter - Vector2.Scale(new Vector2(0.5f, 0.5f) - _labelRect.pivot, size);

        _label.enableAutoSizing = true;
        _label.fontSizeMin = isWrapped ? WrapLabelMinFontSize : _labelSingleLineMinFontSize;
        _label.textWrappingMode = isWrapped ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
    }

    /// <summary>
    /// 文字の枠の、もとの大きさと中心を覚えておく（最初に1回だけ）
    /// </summary>
    private void PrepareLabel()
    {
        if (_isLabelPrepared)
        {
            return;
        }

        _labelRect = (RectTransform)_label.transform;
        _labelSingleLineSize = _labelRect.sizeDelta;
        _labelSingleLineMinFontSize = _label.fontSizeMin;

        // 基準点（ピボット）から見た、枠の中心の位置
        _labelCenter = _labelRect.anchoredPosition + Vector2.Scale(new Vector2(0.5f, 0.5f) - _labelRect.pivot, _labelSingleLineSize);
        _isLabelPrepared = true;
    }

    /// <summary>
    /// 今の見た目を反映する。3状態（normal・light・blackout）の絵がそろっていればその絵を出し、
    /// なければボタンの上に重ねた画像に、今の明るさを反映する（プラスなら白、マイナスなら黒）
    /// </summary>
    private void ApplyTint()
    {
        // 古いプレハブにはまだ重ねる画像がないので、作り直すまでは何もしない
        // 片付けの最中（シーンを閉じるときなど）は、すでに壊れている物があるので何もしない
        if (_artTint == null || _placeholderTint == null || _buttonVisuals[_colorIndex] == null)
        {
            return;
        }

        bool hasLookArt = HasLookArt();
        ApplyLookVisuals(hasLookArt);

        if (hasLookArt)
        {
            _artTint.enabled = false;
            _placeholderTint.enabled = false;
            return;
        }

        Color color = _tintLevel >= 0f ? new Color(1f, 1f, 1f, _tintLevel) : new Color(0f, 0f, 0f, -_tintLevel);
        UiArtSlot artSlot = _buttonVisuals[_colorIndex].GetComponent<UiArtSlot>();
        bool hasArt = artSlot != null && artSlot.HasArt;

        _artTint.enabled = hasArt;
        _placeholderTint.enabled = !hasArt;

        if (hasArt)
        {
            Image artImage = _buttonVisuals[_colorIndex].GetComponent<Image>();
            _artTint.sprite = artImage.sprite;
            _artTint.type = artImage.type;
            _artTint.preserveAspect = artImage.preserveAspect;
            RectTransform visualRect = (RectTransform)_buttonVisuals[_colorIndex].transform;
            _artTint.rectTransform.pivot = visualRect.pivot;
            _artTint.rectTransform.localScale = visualRect.localScale;
            _artTint.color = color;
        }
        else
        {
            _placeholderTint.color = color;
        }
    }

    /// <summary>
    /// 今の色のボタンに、light と blackout の絵が両方入っているか（normal の絵もあること）
    /// </summary>
    /// <returns>3状態の絵がそろっていれば true</returns>
    private bool HasLookArt()
    {
        return HasArt(_buttonVisuals, _colorIndex) && HasArt(_lightVisuals, _colorIndex) && HasArt(_blackoutVisuals, _colorIndex);
    }

    /// <summary>
    /// 指定した見た目の素材が入っているか（古いプレハブで見た目がまだないときは false）
    /// </summary>
    /// <param name="visuals">見た目の一覧（赤・青・黄・緑の順）</param>
    /// <param name="index">色の番号</param>
    /// <returns>素材の画像が入っていれば true</returns>
    private static bool HasArt(GameObject[] visuals, int index)
    {
        if (visuals == null || index >= visuals.Length || visuals[index] == null)
        {
            return false;
        }

        UiArtSlot slot = visuals[index].GetComponent<UiArtSlot>();
        return slot != null && slot.HasArt;
    }

    /// <summary>
    /// light と blackout の見た目のうち、今の状態に合うものだけを出す（normal は常に下に出ている）
    /// </summary>
    /// <param name="hasLookArt">3状態の絵を使うか</param>
    private void ApplyLookVisuals(bool hasLookArt)
    {
        for (int i = 0; i < ColorCount; i++)
        {
            bool isCurrent = hasLookArt && i == _colorIndex;

            if (_lightVisuals != null && i < _lightVisuals.Length && _lightVisuals[i] != null)
            {
                _lightVisuals[i].SetActive(isCurrent && _look == ButtonLook.Light);
            }

            if (_blackoutVisuals != null && i < _blackoutVisuals.Length && _blackoutVisuals[i] != null)
            {
                _blackoutVisuals[i].SetActive(isCurrent && _look == ButtonLook.Blackout);
            }
        }
    }
}
