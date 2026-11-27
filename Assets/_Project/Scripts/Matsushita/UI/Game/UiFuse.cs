using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 爆弾から持ち主の側へ伸びる導火線。残り時間に合わせて、先端の火花が爆弾へ進みながら短くなる
/// </summary>
/// <remarks>
/// 導火線は「折れ線」で、爆弾の口金から外側へ向かう順に点を並べる。座標は画面（1920×1080）の左上が原点。
/// 導火線の絵（_pieces）が入っていれば、単色の区間の代わりに絵を「塗りつぶしの量」で減らしていく
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
    [SerializeField, Tooltip("導火線の絵（爆弾に近い順）。Image Type は Filled で、Fill Origin を爆弾側にしておく。入っていれば区間の代わりにこちらを使う")] private Image[] _pieces;

    private float[] _segmentLengths;
    private float _totalLength;
    private float[] _pieceLengths;
    private float _totalPieceLength;
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
    /// 導火線の絵を使っているか
    /// </summary>
    private bool IsArtMode => _pieces != null && _pieces.Length > 0;

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
        Ratio = Mathf.Clamp01(ratio);

        if (IsArtMode)
        {
            ApplyPieces();
        }
        else
        {
            ApplySegments();
        }

        _isSparkVisible = Ratio > MinVisibleLength;
        _spark.gameObject.SetActive(_isSparkVisible);
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
    /// 単色の区間を、残りの長さに合わせて伸び縮みさせ、先端に火花を置く
    /// </summary>
    private void ApplySegments()
    {
        CacheLengths();

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

        _spark.anchoredPosition = new Vector2(tip.x, -tip.y);
    }

    /// <summary>
    /// 導火線の絵を、残りの長さに合わせて外側から消していき、燃えている所に火花を置く
    /// </summary>
    private void ApplyPieces()
    {
        CachePieceLengths();

        foreach (RectTransform segment in _segments)
        {
            segment.gameObject.SetActive(false);
        }

        float remaining = Ratio * _totalPieceLength;
        float pieceStart = 0f;
        Vector3 tip = GetPieceTip(_pieces[0], 0f);

        for (int i = 0; i < _pieces.Length; i++)
        {
            float length = _pieceLengths[i];
            float fill = length > 0f ? Mathf.Clamp01((remaining - pieceStart) / length) : 0f;
            _pieces[i].fillAmount = fill;

            if (fill > 0f)
            {
                tip = GetPieceTip(_pieces[i], fill);
            }

            pieceStart += length;
        }

        _spark.position = tip;
    }

    /// <summary>
    /// 絵の塗りつぶしの先端（燃えている所）の、ワールド座標を返す
    /// </summary>
    /// <param name="piece">導火線の絵</param>
    /// <param name="fill">塗りつぶしの量（0〜1）</param>
    /// <returns>先端の位置</returns>
    private static Vector3 GetPieceTip(Image piece, float fill)
    {
        Rect rect = piece.rectTransform.rect;
        bool isHorizontal = piece.fillMethod == Image.FillMethod.Horizontal;
        Vector2 from;
        Vector2 to;

        if (isHorizontal)
        {
            bool isFromLeft = piece.fillOrigin == (int)Image.OriginHorizontal.Left;
            from = new Vector2(isFromLeft ? rect.xMin : rect.xMax, rect.center.y);
            to = new Vector2(isFromLeft ? rect.xMax : rect.xMin, rect.center.y);
        }
        else
        {
            bool isFromBottom = piece.fillOrigin == (int)Image.OriginVertical.Bottom;
            from = new Vector2(rect.center.x, isFromBottom ? rect.yMin : rect.yMax);
            to = new Vector2(rect.center.x, isFromBottom ? rect.yMax : rect.yMin);
        }

        return piece.rectTransform.TransformPoint(Vector2.Lerp(from, to, fill));
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

    /// <summary>
    /// 各絵の、燃えて減っていく向きの長さと全長を計算しておく
    /// </summary>
    private void CachePieceLengths()
    {
        if (_pieceLengths != null && _pieceLengths.Length == _pieces.Length)
        {
            return;
        }

        _pieceLengths = new float[_pieces.Length];
        _totalPieceLength = 0f;

        for (int i = 0; i < _pieces.Length; i++)
        {
            Rect rect = _pieces[i].rectTransform.rect;
            _pieceLengths[i] = _pieces[i].fillMethod == Image.FillMethod.Horizontal ? rect.width : rect.height;
            _totalPieceLength += _pieceLengths[i];
        }
    }
}
