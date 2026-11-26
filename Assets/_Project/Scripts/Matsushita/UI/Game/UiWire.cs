using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 爆弾のひし形ボタン1つと、そこから伸びる配線1本の表示。色と状態に合わせて、4色×2状態の配線から1つだけ出す
/// </summary>
public class UiWire : MonoBehaviour
{
    private const int ColorCount = 4;
    private const float GoneAlpha = 0.3f;
    private const float PressSeconds = 0.15f;
    private const float PressDepth = 0.2f;

    [SerializeField, Tooltip("つながっている配線の見た目（赤・青・黄・緑の順）")] private GameObject[] _normalVisuals = new GameObject[ColorCount];
    [SerializeField, Tooltip("切れている配線の見た目（赤・青・黄・緑の順）")] private GameObject[] _cutVisuals = new GameObject[ColorCount];
    [SerializeField, Tooltip("ひし形ボタンの見た目（赤・青・黄・緑の順）")] private GameObject[] _buttonVisuals = new GameObject[ColorCount];
    [SerializeField, Tooltip("ボタン全体の透明度を変える用（線が消えたとき薄くする）")] private CanvasGroup _buttonGroup;
    [SerializeField, Tooltip("ボタンをマウスで押すためのボタン")] private Button _clickButton;
    [SerializeField, Tooltip("ボタンの文字。何も指定しなければ色名の頭文字（赤・青・黄・緑）")] private TMP_Text _label;
    [SerializeField, Tooltip("ボタンの隅に出す記号（○△□☆や数字）。出さないときは空")] private TMP_Text _symbolLabel;

    private static readonly string[] ColorNames = { "赤", "青", "黄", "緑" };

    private string _customLabel;
    private int _colorIndex;
    private float _pressElapsed = PressSeconds;

    /// <summary>
    /// ボタンをマウスで押すためのボタン
    /// </summary>
    public Button ClickButton => _clickButton;

    /// <summary>
    /// ボタンに出ている文字
    /// </summary>
    public string LabelText => _label.text;

    /// <summary>
    /// 押している間のボタンを、少し縮めて戻す
    /// </summary>
    private void Update()
    {
        if (_pressElapsed >= PressSeconds)
        {
            return;
        }

        _pressElapsed += Time.unscaledDeltaTime;
        float rate = Mathf.Clamp01(_pressElapsed / PressSeconds);
        _buttonGroup.transform.localScale = Vector3.one * (1f - PressDepth * Mathf.Sin(Mathf.PI * rate));
    }

    /// <summary>
    /// ボタンを押した動き（ぎゅっと縮んで戻る）を始める
    /// </summary>
    public void Press()
    {
        _pressElapsed = 0f;
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
            _normalVisuals[i].SetActive(state == UiWireState.Normal && i == _colorIndex);
            _cutVisuals[i].SetActive(state == UiWireState.Cut && i == _colorIndex);
            _buttonVisuals[i].SetActive(i == _colorIndex);
        }

        _buttonGroup.alpha = state == UiWireState.Gone ? GoneAlpha : 1f;
        RefreshLabel();
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
        _label.text = string.IsNullOrEmpty(_customLabel) ? ColorNames[_colorIndex] : _customLabel;
    }
}
