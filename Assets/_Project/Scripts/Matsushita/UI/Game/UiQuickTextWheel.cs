using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// クイックテキストの円形の候補。方向キーを押し続けている間、キャラの上に出て、選んでいる方向の扇が黄色になる
/// </summary>
/// <remarks>
/// 扇・文・キー名は、画面の上・下・左・右の順（W・S・A・D のキーの位置）に並べる。開く／閉じる／選ぶだけで、いつ出すかの判断は QuickTextInput が行う
/// </remarks>
public class UiQuickTextWheel : MonoBehaviour
{
    private const int DirectionCount = 4;
    private const float OpenSeconds = 0.12f;
    private const float OpenStartScale = 0.85f;

    [SerializeField, Tooltip("扇の画像（画面の上・下・左・右の順）")] private Image[] _wedges = new Image[DirectionCount];
    [SerializeField, Tooltip("扇の中の文（画面の上・下・左・右の順）")] private TMP_Text[] _texts = new TMP_Text[DirectionCount];
    [SerializeField, Tooltip("扇の中のキー名（画面の上・下・左・右の順）")] private TMP_Text[] _keyTexts = new TMP_Text[DirectionCount];
    [SerializeField, Tooltip("選んでいない扇の色")] private Color _wedgeColor = new Color(0.06f, 0.08f, 0.14f, 0.9f);
    [SerializeField, Tooltip("選んでいる扇の色")] private Color _selectedWedgeColor = new Color(1f, 0.85f, 0.25f, 1f);
    [SerializeField, Tooltip("選んでいない扇の文字の色")] private Color _textColor = Color.white;
    [SerializeField, Tooltip("選んでいる扇の文字の色")] private Color _selectedTextColor = new Color(0.23f, 0.16f, 0f, 1f);

    private float _openElapsed = OpenSeconds;

    /// <summary>
    /// 出ているか
    /// </summary>
    public bool IsOpen => gameObject.activeSelf;

    /// <summary>
    /// 出すとき、少し小さい状態から元の大きさに戻す
    /// </summary>
    private void Update()
    {
        if (_openElapsed >= OpenSeconds)
        {
            return;
        }

        _openElapsed += Time.unscaledDeltaTime;
        float rate = Mathf.Clamp01(_openElapsed / OpenSeconds);
        transform.localScale = Vector3.one * Mathf.Lerp(OpenStartScale, 1f, rate);
    }

    /// <summary>
    /// 候補を出して、選んでいる方向を光らせる
    /// </summary>
    /// <param name="texts">文（画面の上・下・左・右の順）</param>
    /// <param name="selected">選んでいる方向</param>
    public void Open(string[] texts, InputDirection selected)
    {
        bool isOpening = !gameObject.activeSelf;

        for (int i = 0; i < _texts.Length && i < texts.Length; i++)
        {
            _texts[i].text = texts[i];
        }

        gameObject.SetActive(true);
        SetSelected(selected);

        if (isOpening && Application.isPlaying)
        {
            _openElapsed = 0f;
            transform.localScale = Vector3.one * OpenStartScale;
        }
    }

    /// <summary>
    /// 選んでいる方向の扇を黄色にして、ほかを元の色に戻す
    /// </summary>
    /// <param name="selected">選んでいる方向</param>
    public void SetSelected(InputDirection selected)
    {
        for (int i = 0; i < _wedges.Length; i++)
        {
            bool isSelected = i == UiQuickTextSet.SlotOf(selected);
            _wedges[i].color = isSelected ? _selectedWedgeColor : _wedgeColor;
            _texts[i].color = isSelected ? _selectedTextColor : _textColor;
            _keyTexts[i].color = isSelected ? _selectedTextColor : _textColor;
        }
    }

    /// <summary>
    /// 候補を閉じる
    /// </summary>
    public void Close()
    {
        _openElapsed = OpenSeconds;
        transform.localScale = Vector3.one;
        gameObject.SetActive(false);
    }
}
