using UnityEngine;

/// <summary>
/// 爆弾から持ち主の側へ伸びる導火線。残り時間に合わせて、先端の火花が爆弾へ進みながら短くなる
/// </summary>
/// <remarks>
/// 導火線は「折れ線」で、爆弾の口金から外側へ向かう順に点を並べる。座標は画面（1920×1080）の左上が原点
/// </remarks>
public class UiFuse : MonoBehaviour
{
    private const float NormalFlickerSpeed = 30f;
    private const float UrgentFlickerSpeed = 70f;
    private const float FlickerScale = 0.2f;
    private const float SparkSpinSpeed = 120f;
    private const float MinVisibleLength = 0.01f;

    [SerializeField, Tooltip("折れ線の各区間の見た目（爆弾に近い順）")] private RectTransform[] _segments;
    [SerializeField, Tooltip("折れ線の点（爆弾の口金から順に。区間の数＋1個）")] private Vector2[] _points;
    [SerializeField, Tooltip("導火線の太さ")] private float _thickness = 22f;
    [SerializeField, Tooltip("先端の火花")] private RectTransform _spark;

    private float[] _segmentLengths;
    private float _totalLength;
    private bool _isUrgent;
    private bool _isSparkVisible;

    /// <summary>
    /// 全長に対する、いま残っている長さの割合（0〜1）
    /// </summary>
    public float Ratio { get; private set; } = 1f;

    /// <summary>
    /// 火花が見えているか
    /// </summary>
    public bool IsSparkVisible => _isSparkVisible;

    /// <summary>
    /// 火花をちらつかせる
    /// </summary>
    private void Update()
    {
        if (!_isSparkVisible)
        {
            return;
        }

        float speed = _isUrgent ? UrgentFlickerSpeed : NormalFlickerSpeed;
        float scale = 1f + FlickerScale * Mathf.Sin(Time.unscaledTime * speed);
        _spark.localScale = new Vector3(scale, scale, 1f);
        _spark.localRotation = Quaternion.Euler(0f, 0f, Time.unscaledTime * SparkSpinSpeed);
    }

    /// <summary>
    /// 残っている長さの割合を設定する。先端（火花）が爆弾へ近づくほど短くなる
    /// </summary>
    /// <param name="ratio">全長に対する割合（1：満タン、0：燃え尽きた）</param>
    public void SetRatio(float ratio)
    {
        CacheLengths();
        Ratio = Mathf.Clamp01(ratio);

        float remaining = Ratio * _totalLength;
        float segmentStart = 0f;
        Vector2 tip = _points[0];

        for (int i = 0; i < _segments.Length; i++)
        {
            float length = _segmentLengths[i];
            float drawn = Mathf.Clamp(remaining - segmentStart, 0f, length);
            bool isVisible = drawn > MinVisibleLength;
            _segments[i].gameObject.SetActive(isVisible);

            if (isVisible)
            {
                Vector2 from = _points[i];
                Vector2 direction = (_points[i + 1] - from).normalized;
                bool isJoint = drawn >= length - MinVisibleLength && i < _segments.Length - 1;
                float extra = isJoint ? _thickness * 0.5f : 0f;

                _segments[i].anchoredPosition = new Vector2(from.x, -from.y);
                _segments[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(-direction.y, direction.x) * Mathf.Rad2Deg);
                _segments[i].sizeDelta = new Vector2(drawn + extra, _thickness);
                tip = from + direction * drawn;
            }

            segmentStart += length;
        }

        _isSparkVisible = Ratio > MinVisibleLength;
        _spark.gameObject.SetActive(_isSparkVisible);
        _spark.anchoredPosition = new Vector2(tip.x, -tip.y);
    }

    /// <summary>
    /// 残り3秒以下のとき、火花を速くちらつかせる
    /// </summary>
    /// <param name="isUrgent">急いでいるなら true</param>
    public void SetUrgent(bool isUrgent)
    {
        _isUrgent = isUrgent;
    }

    /// <summary>
    /// 各区間の長さと全長を計算しておく
    /// </summary>
    private void CacheLengths()
    {
        if (_segmentLengths != null && _segmentLengths.Length == _segments.Length)
        {
            return;
        }

        _segmentLengths = new float[_segments.Length];
        _totalLength = 0f;

        for (int i = 0; i < _segments.Length; i++)
        {
            _segmentLengths[i] = Vector2.Distance(_points[i], _points[i + 1]);
            _totalLength += _segmentLengths[i];
        }
    }
}
