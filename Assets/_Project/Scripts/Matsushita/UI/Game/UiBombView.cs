using System;
using TMPro;
using UnityEngine;

/// <summary>
/// 画面中央の爆弾の表示。4本の配線、中央のランプ、液晶の問題文と解除の状態を管理する
/// </summary>
public class UiBombView : MonoBehaviour
{
    private const float ShakeDistance = 6f;
    private const float ShakeSpeed = 60f;

    private static readonly Color NormalTimeColor = new Color(0.65f, 1f, 0.75f);
    private static readonly Color DangerTimeColor = new Color(1f, 0.25f, 0.25f);

    [SerializeField, Tooltip("ひし形ボタンと配線（上・下・左・右の順。InputDirection と同じ）")] private UiWire[] _wires;
    [SerializeField, Tooltip("タイマー液晶の残り時間")] private TMP_Text _timeText;
    [SerializeField, Tooltip("ランプが点灯している見た目")] private GameObject _lampOn;
    [SerializeField, Tooltip("ランプが消えている見た目")] private GameObject _lampOff;
    [SerializeField, Tooltip("消灯の見た目を一時的に隠す（ONの間、ランプが消えているときは何も出さない）")] private bool _isLampOffHidden;
    [SerializeField, Tooltip("液晶の問題文")] private TMP_Text _questionText;
    [SerializeField, Tooltip("液晶の下段に出す解除の状態")] private TMP_Text _defuseStatusText;
    [SerializeField, Tooltip("解除が使えるときに出す解除ボタン")] private GameObject _defuseButton;
    [SerializeField, Tooltip("暗号・順番スイッチで入力した文字")] private TMP_Text _inputText;
    [SerializeField, Tooltip("残り3秒以下で揺らす対象")] private RectTransform _shakeTarget;

    private Vector2 _basePosition;
    private bool _isShaking;

    /// <summary>
    /// ひし形ボタンがマウスでクリックされたときに呼ばれる
    /// </summary>
    public event Action<InputDirection> ButtonClicked;

    /// <summary>
    /// 揺らすときの基準の位置を覚えておく
    /// </summary>
    private void Awake()
    {
        _basePosition = _shakeTarget.anchoredPosition;

        for (int i = 0; i < _wires.Length; i++)
        {
            InputDirection direction = (InputDirection)i;
            _wires[i].ClickButton.onClick.AddListener(() => ButtonClicked?.Invoke(direction));
        }
    }

    /// <summary>
    /// 揺れている間、爆弾を小刻みに動かす
    /// </summary>
    private void Update()
    {
        if (!_isShaking)
        {
            return;
        }

        float x = Mathf.Sin(Time.unscaledTime * ShakeSpeed) * ShakeDistance;
        float y = Mathf.Cos(Time.unscaledTime * ShakeSpeed * 1.3f) * ShakeDistance * 0.6f;
        _shakeTarget.anchoredPosition = _basePosition + new Vector2(x, y);
    }

    /// <summary>
    /// 配線1本の色と状態を設定する
    /// </summary>
    /// <param name="direction">どの方向の線か</param>
    /// <param name="color">線の色</param>
    /// <param name="state">線の状態</param>
    public void SetWire(InputDirection direction, UiWireColor color, UiWireState state)
    {
        _wires[(int)direction].Show(color, state);
    }

    /// <summary>
    /// タイマー液晶に残り時間を出す。残り3秒以下は赤くする
    /// </summary>
    /// <param name="text">表示する文字（例：「05:00」）</param>
    /// <param name="isDanger">残り3秒以下なら true</param>
    public void SetTime(string text, bool isDanger)
    {
        _timeText.text = text;
        _timeText.color = isDanger ? DangerTimeColor : NormalTimeColor;
    }

    /// <summary>
    /// 押した見た目の残り時間（秒）。どれかのボタンが暗くなっている間は 0 より大きい
    /// </summary>
    public float PressRemainingSeconds
    {
        get
        {
            float remaining = 0f;

            foreach (UiWire wire in _wires)
            {
                remaining = Mathf.Max(remaining, wire.PressRemainingSeconds);
            }

            return remaining;
        }
    }

    /// <summary>
    /// キーで押したときの見た目を見せる（少しの間、暗くなって戻る。マウスで押したときは UiWire が自分で暗くする）
    /// </summary>
    /// <param name="direction">どの方向のボタンか</param>
    public void PressButton(InputDirection direction)
    {
        _wires[(int)direction].Press();
    }

    /// <summary>
    /// すべてのボタンの、重なり・押下の見た目を元に戻す
    /// </summary>
    public void ResetPress()
    {
        foreach (UiWire wire in _wires)
        {
            wire.ResetPress();
        }
    }

    /// <summary>
    /// ボタンに出す文字を設定する（暗号・順番スイッチで使う）。空にすると色名の頭文字に戻る
    /// </summary>
    /// <param name="direction">どの方向のボタンか</param>
    /// <param name="text">ボタンの文字</param>
    public void SetButtonLabel(InputDirection direction, string text)
    {
        _wires[(int)direction].SetLabel(text);
    }

    /// <summary>
    /// ボタンの文字の代わりに、言葉に合うシルエットを出す（大きさの問題で使う）。
    /// 先に SetButtonLabel で文字を設定してから呼ぶ。合う画像が無い言葉は、文字のまま出る
    /// </summary>
    /// <param name="direction">どの方向のボタンか</param>
    /// <param name="word">ボタンの言葉（例：「ゾウ」）。空にするとシルエットを消して文字に戻す</param>
    public void SetButtonSilhouette(InputDirection direction, string word)
    {
        _wires[(int)direction].SetSilhouette(UiSilhouetteLibrary.Find(word));
    }

    /// <summary>
    /// 配線の記号を設定する（仲間外れの謎と、ランプ条件のときだけ使う）
    /// </summary>
    /// <param name="direction">どの方向の線か</param>
    /// <param name="symbol">○△□☆や数字。出さないときは空</param>
    public void SetWireSymbol(InputDirection direction, string symbol)
    {
        _wires[(int)direction].SetSymbol(symbol);
    }

    /// <summary>
    /// 液晶に問題文を出す
    /// </summary>
    /// <param name="text">問題文</param>
    public void SetQuestion(string text)
    {
        _questionText.text = text;
    }

    /// <summary>
    /// ランプの点灯／消灯を切り替える
    /// </summary>
    /// <param name="isOn">点灯なら true</param>
    public void SetLamp(bool isOn)
    {
        _lampOn.SetActive(isOn);
        _lampOff.SetActive(!isOn && !_isLampOffHidden);
    }

    /// <summary>
    /// シリアル番号は廃止した（画面には出さない）。呼び出し側が残っていてもコンパイルが通るように、何もしないメソッドとして残してある
    /// </summary>
    /// <param name="serial">シリアル番号（使わない）</param>
    public void SetSerial(int serial)
    {
    }

    /// <summary>
    /// 暗号・順番スイッチで入力した文字を表示する。空にすると隠す
    /// </summary>
    /// <param name="inputs">入力済みの文字（例：「○ △」）</param>
    public void SetInput(string inputs)
    {
        _inputText.text = inputs;
        _inputText.gameObject.SetActive(!string.IsNullOrEmpty(inputs));
    }

    /// <summary>
    /// 解除の状態を液晶の下段に表示する。使えるときは解除ボタンも出す
    /// </summary>
    /// <param name="state">解除の状態</param>
    public void SetDefuseState(UiDefuseState state)
    {
        switch (state)
        {
            case UiDefuseState.Available:
                _defuseStatusText.text = "解除に挑戦できる！";
                break;
            case UiDefuseState.Used:
                _defuseStatusText.text = "解除：使用済み";
                break;
            case UiDefuseState.Challenging:
                _defuseStatusText.text = "解除に挑戦中…";
                break;
            default:
                _defuseStatusText.text = "解除：パス5回目から";
                break;
        }

        _defuseButton.SetActive(state == UiDefuseState.Available);
    }

    /// <summary>
    /// 爆弾を小刻みに揺らす／止める（残り3秒以下）
    /// </summary>
    /// <param name="isShaking">揺らすなら true</param>
    public void SetShaking(bool isShaking)
    {
        if (_isShaking == isShaking)
        {
            return;
        }

        _isShaking = isShaking;

        if (!isShaking)
        {
            _shakeTarget.anchoredPosition = _basePosition;
        }
    }
}
