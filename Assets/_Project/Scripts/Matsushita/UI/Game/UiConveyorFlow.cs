using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 背景のベルトコンベアの上を、粒が流れていくアニメーション。
/// 線（Lane）の点に沿って、一定の間隔で並んだ粒（SpriteRenderer）を動かす。奥に行くほど小さく、両端ではふわっと出て消える。
/// 付けた RectTransform（画面いっぱい）の座標で動く。画面に映るのは、Canvas が UiWorldCanvas でカメラに映っているときだけ
/// </summary>
public class UiConveyorFlow : MonoBehaviour
{
    private const float ScreenWidth = 1920f;
    private const float ScreenHeight = 1080f;
    private const string DotName = "Dot";
    private const int PlaceholderWidth = 64;
    private const int PlaceholderHeight = 44;

    private static readonly string[] DotArtIds = { "ART-24_pink", "ART-24_green", "ART-24_yellow" };

    private static readonly Color[] PlaceholderColors =
    {
        new Color(0.78f, 0.42f, 0.75f),
        new Color(0.47f, 0.75f, 0.43f),
        new Color(0.83f, 0.75f, 0.30f)
    };

    private static Sprite[] _placeholderSprites;

    /// <summary>
    /// 粒が通る線 1 本分
    /// </summary>
    [System.Serializable]
    private class Lane
    {
        [Tooltip("線の点。x・y は 1920×1080 の左上からの位置、z は粒の大きさの倍率。外から中央への順に並べる")]
        public Vector3[] Points;
    }

    [SerializeField, Tooltip("粒が通る線（左のベルト、右のベルトの順）")] private Lane[] _lanes;
    [SerializeField, Tooltip("粒の速さ（1920×1080 の画面で、1 秒に進む距離）")] private float _speed = 90f;
    [SerializeField, Tooltip("粒と粒の間隔（線に沿った距離）")] private float _gap = 110f;
    [SerializeField, Tooltip("線の両端で、粒が出る・消えるのにかける距離（小さくなりながら透けていく）")] private float _fadeLength = 60f;
    [SerializeField, Tooltip("線の始まり（外側）を、この距離だけ手前で切る。向きが中央から外のとき、粒はここで消える")] private float _startMargin = 120f;
    [SerializeField, Tooltip("線の終わり（中央側）を、この距離だけ手前で切る。0 ならドームの際（線の最後の点）から出る／まで流れる")] private float _endMargin;
    [SerializeField, Tooltip("オンにすると、線の点の順と逆（中央から外）に流す。オフなら外から中央")] private bool _isReversed = true;
    [SerializeField, Tooltip("粒の描画順（背景より大きく、立ち絵より小さくする）")] private int _sortingOrder = 1;
    [SerializeField, Tooltip("粒の絵（色ちがいを順に使う）。空なら仮の楕円")] private Sprite[] _dotSprites;
    [SerializeField, Tooltip("流すか")] private bool _isFlowing = true;

    private readonly List<List<SpriteRenderer>> _dots = new List<List<SpriteRenderer>>();
    private float[][] _laneLengths;
    private float _distance;
    private RectTransform _frame;

    /// <summary>
    /// 流しているか（false にすると粒はその場で止まる）
    /// </summary>
    public bool IsFlowing
    {
        get => _isFlowing;
        set => _isFlowing = value;
    }

    /// <summary>
    /// 粒の絵を UiSkin から受け取る（そろっているぶんだけ使う。1 つもなければ仮の楕円）
    /// </summary>
    /// <param name="skin">素材の表</param>
    public void Apply(UiSkin skin)
    {
        List<Sprite> found = new List<Sprite>();

        foreach (string artId in DotArtIds)
        {
            Sprite sprite = skin != null ? skin.GetSprite(artId) : null;

            if (sprite != null)
            {
                found.Add(sprite);
            }
        }

        _dotSprites = found.Count > 0 ? found.ToArray() : null;
    }

    /// <summary>
    /// 自分の四角と、線の長さを調べておく
    /// </summary>
    private void Awake()
    {
        _frame = (RectTransform)transform;
        BuildLaneLengths();
    }

    /// <summary>
    /// 粒を進めて、位置・大きさ・透明度を合わせる
    /// </summary>
    private void Update()
    {
        if (_laneLengths == null || _gap <= 0f)
        {
            return;
        }

        if (_isFlowing)
        {
            _distance += _speed * Time.deltaTime;
        }

        Sprite[] sprites = GetSprites();
        Rect rect = _frame.rect;

        for (int laneIndex = 0; laneIndex < _lanes.Length; laneIndex++)
        {
            UpdateLane(laneIndex, sprites, rect);
        }
    }

    /// <summary>
    /// 線 1 本ぶんの粒を、今の位置に置く
    /// </summary>
    /// <param name="laneIndex">線の番号</param>
    /// <param name="sprites">使う粒の絵</param>
    /// <param name="rect">画面いっぱいの四角</param>
    private void UpdateLane(int laneIndex, Sprite[] sprites, Rect rect)
    {
        float[] lengths = _laneLengths[laneIndex];

        if (lengths == null)
        {
            return;
        }

        // 外側・中央側を切ったあとの、粒が流れる範囲（線の始まりから lowest 〜 highest）
        float lowest = _startMargin;
        float highest = lengths[lengths.Length - 1] - _endMargin;
        float total = Mathf.Max(highest - lowest, _gap);

        // 2 本目は半分ずらして、左右がそろって並ばないようにする
        float head = _distance + (laneIndex % 2) * _gap * 0.5f;
        int newest = Mathf.FloorToInt(head / _gap);
        int oldest = Mathf.FloorToInt((head - total) / _gap);
        int usedCount = 0;

        for (int j = oldest; j <= newest; j++)
        {
            float travelled = head - j * _gap;

            if (travelled < 0f || travelled > total)
            {
                continue;
            }

            float along = _isReversed ? total - travelled : travelled;
            Vector3 point = Evaluate(_lanes[laneIndex].Points, lengths, lowest + along);
            float alpha = Mathf.Clamp01(Mathf.Min(travelled, total - travelled) / Mathf.Max(_fadeLength, 0.01f));
            Sprite sprite = sprites[((j + laneIndex) % sprites.Length + sprites.Length) % sprites.Length];

            SpriteRenderer dot = GetDot(laneIndex, usedCount);
            usedCount++;
            PlaceDot(dot, sprite, point, alpha, rect);
        }

        List<SpriteRenderer> dots = _dots[laneIndex];

        for (int k = usedCount; k < dots.Count; k++)
        {
            dots[k].gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 粒 1 つを、画面の位置・大きさに合わせる
    /// </summary>
    /// <param name="dot">粒</param>
    /// <param name="sprite">粒の絵</param>
    /// <param name="point">x・y は 1920×1080 の位置、z は大きさの倍率</param>
    /// <param name="alpha">透明度（両端では、この値に合わせて大きさも小さくする）</param>
    /// <param name="rect">画面いっぱいの四角</param>
    private void PlaceDot(SpriteRenderer dot, Sprite sprite, Vector3 point, float alpha, Rect rect)
    {
        float unit = rect.width / ScreenWidth;
        float scale = unit * point.z * sprite.pixelsPerUnit * Mathf.SmoothStep(0f, 1f, alpha);

        dot.gameObject.SetActive(true);
        dot.sprite = sprite;
        dot.color = new Color(1f, 1f, 1f, alpha);
        dot.transform.localPosition = new Vector3(rect.xMin + point.x * unit, rect.yMax - point.y * rect.height / ScreenHeight, 0f);
        dot.transform.localScale = new Vector3(scale, scale, 1f);
    }

    /// <summary>
    /// 使える粒を返す。足りなければ作る
    /// </summary>
    /// <param name="laneIndex">線の番号</param>
    /// <param name="index">その線の何番目か</param>
    /// <returns>粒</returns>
    private SpriteRenderer GetDot(int laneIndex, int index)
    {
        List<SpriteRenderer> dots = _dots[laneIndex];

        while (dots.Count <= index)
        {
            GameObject dotObject = new GameObject(DotName);
            dotObject.transform.SetParent(transform, false);
            SpriteRenderer dotRenderer = dotObject.AddComponent<SpriteRenderer>();
            dotRenderer.sortingOrder = _sortingOrder;
            dots.Add(dotRenderer);
        }

        return dots[index];
    }

    /// <summary>
    /// 使う粒の絵を返す（絵がなければ仮の楕円）
    /// </summary>
    /// <returns>粒の絵</returns>
    private Sprite[] GetSprites()
    {
        if (_dotSprites != null && _dotSprites.Length > 0)
        {
            return _dotSprites;
        }

        // 再生を止めると仮の絵が壊されることがあるので、壊れていたら作り直す
        if (_placeholderSprites == null || _placeholderSprites[0] == null)
        {
            _placeholderSprites = new Sprite[PlaceholderColors.Length];

            for (int i = 0; i < PlaceholderColors.Length; i++)
            {
                _placeholderSprites[i] = CreatePlaceholderSprite(PlaceholderColors[i]);
            }
        }

        return _placeholderSprites;
    }

    /// <summary>
    /// 線ごとに、点までの長さを調べる
    /// </summary>
    private void BuildLaneLengths()
    {
        int laneCount = _lanes != null ? _lanes.Length : 0;
        _laneLengths = laneCount > 0 ? new float[laneCount][] : null;
        _dots.Clear();

        for (int i = 0; i < laneCount; i++)
        {
            _dots.Add(new List<SpriteRenderer>());
            Vector3[] points = _lanes[i].Points;

            if (points == null || points.Length < 2)
            {
                continue;
            }

            float[] lengths = new float[points.Length];

            for (int k = 1; k < points.Length; k++)
            {
                lengths[k] = lengths[k - 1] + Vector2.Distance(points[k], points[k - 1]);
            }

            _laneLengths[i] = lengths;
        }
    }

    /// <summary>
    /// 線の上の、始まりから指定の距離にある位置と大きさを求める
    /// </summary>
    /// <param name="points">線の点</param>
    /// <param name="lengths">点までの長さ</param>
    /// <param name="distance">始まりからの距離</param>
    /// <returns>x・y は位置、z は大きさの倍率</returns>
    private static Vector3 Evaluate(Vector3[] points, float[] lengths, float distance)
    {
        for (int i = 1; i < points.Length; i++)
        {
            if (distance <= lengths[i])
            {
                float segment = lengths[i] - lengths[i - 1];
                float t = segment > 0f ? (distance - lengths[i - 1]) / segment : 0f;
                return Vector3.Lerp(points[i - 1], points[i], t);
            }
        }

        return points[points.Length - 1];
    }

    /// <summary>
    /// 絵がないときの、仮の粒（色つきの楕円）を作る
    /// </summary>
    /// <param name="color">粒の色</param>
    /// <returns>仮の粒の絵</returns>
    private static Sprite CreatePlaceholderSprite(Color color)
    {
        Texture2D texture = new Texture2D(PlaceholderWidth, PlaceholderHeight, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        Color rim = color * 0.7f;
        rim.a = 1f;

        for (int y = 0; y < PlaceholderHeight; y++)
        {
            for (int x = 0; x < PlaceholderWidth; x++)
            {
                float dx = (x + 0.5f - PlaceholderWidth * 0.5f) / (PlaceholderWidth * 0.5f);
                float dy = (y + 0.5f - PlaceholderHeight * 0.5f) / (PlaceholderHeight * 0.5f);
                float edge = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01((1f - edge) * 10f);
                bool isRim = edge > 0.82f || dy < -0.45f;
                texture.SetPixel(x, y, isRim ? new Color(rim.r, rim.g, rim.b, alpha) : new Color(color.r, color.g, color.b, alpha));
            }
        }

        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, PlaceholderWidth, PlaceholderHeight), new Vector2(0.5f, 0.5f), 100f);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }
}
