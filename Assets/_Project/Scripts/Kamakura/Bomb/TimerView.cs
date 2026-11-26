using TMPro;
using UnityEngine;

/// <summary>
/// 爆弾の残り時間を「0.00」の形で液晶に表示する
/// </summary>
public class TimerView : MonoBehaviour
{
    private const string TimeFormat = "0.00";

    [SerializeField] private BombTimer _bombTimer;
    [SerializeField] private TMP_Text _timerText;
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _warningColor = Color.red;

    private int _shownHundredths = -1;

    /// <summary>
    /// 表示が変わったときだけ文字を書き換える（毎フレーム文字列を作らないため）
    /// </summary>
    private void Update()
    {
        // 切り上げにして、「0.00」は本当に0になったときだけ出す
        int hundredths = Mathf.CeilToInt(_bombTimer.Remaining * 100f);

        if (hundredths == _shownHundredths)
        {
            return;
        }

        _shownHundredths = hundredths;
        float shownSeconds = hundredths / 100f;
        _timerText.text = shownSeconds.ToString(TimeFormat);
        _timerText.color = shownSeconds <= _bombTimer.WarningTime ? _warningColor : _normalColor;
    }
}
