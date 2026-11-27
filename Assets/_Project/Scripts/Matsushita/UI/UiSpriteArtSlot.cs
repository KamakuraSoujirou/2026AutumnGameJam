using UnityEngine;

/// <summary>
/// 素材の差し込み口（2D オブジェクト版）。UiSkin に画像があれば SpriteRenderer に入れ、なければ仮の形を出す。
/// UI の Image ではなく、プレハブの中にある普通の GameObject（SpriteRenderer）として立ち絵や爆弾の本体を置くために使う。
/// 親の RectTransform の四角いっぱいに収まるよう、大きさと位置を合わせる。画面に映るのは、Canvas が UiWorldCanvas でカメラに映っているときだけ
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class UiSpriteArtSlot : MonoBehaviour
{
    [SerializeField, Tooltip("UiSkin の素材ID")] private string _artId;
    [SerializeField, Tooltip("素材がないときの色")] private Color _placeholderColor = Color.gray;
    [SerializeField, Tooltip("素材がないときの形（空なら四角）")] private Sprite _placeholderSprite;
    [SerializeField, Tooltip("素材の縦横比を保つか")] private bool _isPreserveAspect = true;
    [SerializeField, Tooltip("素材がないときだけ出すもの（仮の文字など）")] private GameObject _placeholderObject;
    [SerializeField, Tooltip("素材がないときに代わりに使う素材ID（空なら使わない）")] private string _fallbackArtId;
    [SerializeField, Tooltip("代わりの素材を使うときにかける色")] private Color _fallbackColor = Color.white;
    [SerializeField, Tooltip("描画順（大きいほど手前。UI は必ずこれより手前に出る）")] private int _sortingOrder;

    private static Sprite _solidSprite;

    private SpriteRenderer _renderer;

    /// <summary>
    /// UiSkin の素材ID
    /// </summary>
    public string ArtId => _artId;

    /// <summary>
    /// 素材の画像が入っているか（なければ仮の形を出している）
    /// </summary>
    public bool HasArt { get; private set; }

    /// <summary>
    /// UiSkin から画像を受け取って表示を切り替え、親の四角に大きさと位置を合わせる
    /// </summary>
    /// <param name="skin">素材の表</param>
    public void Apply(UiSkin skin)
    {
        if (_renderer == null)
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        Sprite sprite = skin != null ? skin.GetSprite(_artId) : null;
        bool isUsingFallback = false;

        if (sprite == null && skin != null && !string.IsNullOrEmpty(_fallbackArtId))
        {
            sprite = skin.GetSprite(_fallbackArtId);
            isUsingFallback = sprite != null;
        }

        HasArt = sprite != null;

        if (HasArt)
        {
            _renderer.sprite = sprite;
            _renderer.color = isUsingFallback ? _fallbackColor : Color.white;
        }
        else
        {
            _renderer.sprite = _placeholderSprite != null ? _placeholderSprite : GetSolidSprite();
            _renderer.color = _placeholderColor;
        }

        _renderer.sortingOrder = _sortingOrder;

        if (_placeholderObject != null)
        {
            _placeholderObject.SetActive(!HasArt);
        }

        Fit();
    }

    /// <summary>
    /// 親の四角いっぱいに収まるよう、位置と大きさを合わせる（四角の大きさを変えたときは、右クリックの「四角に合わせる」で呼べる）
    /// </summary>
    [ContextMenu("四角に合わせる")]
    public void Fit()
    {
        if (_renderer == null)
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        RectTransform frame = transform.parent as RectTransform;

        if (frame == null || _renderer.sprite == null)
        {
            return;
        }

        Rect rect = frame.rect;
        Vector2 spriteSize = _renderer.sprite.bounds.size;
        float scaleX = rect.width / spriteSize.x;
        float scaleY = rect.height / spriteSize.y;

        if (HasArt && _isPreserveAspect)
        {
            scaleX = Mathf.Min(scaleX, scaleY);
            scaleY = scaleX;
        }

        transform.localPosition = new Vector3(rect.center.x, rect.center.y, 0f);
        transform.localRotation = Quaternion.identity;
        transform.localScale = new Vector3(scaleX, scaleY, 1f);
    }

    /// <summary>
    /// 色の四角として使う白い画像を用意する
    /// </summary>
    /// <returns>白い画像</returns>
    private static Sprite GetSolidSprite()
    {
        if (_solidSprite == null)
        {
            _solidSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
        }

        return _solidSprite;
    }
}
