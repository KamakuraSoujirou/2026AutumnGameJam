using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 素材の差し込み口。UiSkin に画像があればそれを出し、なければ仮の四角を出す
/// </summary>
[RequireComponent(typeof(Image))]
public class UiArtSlot : MonoBehaviour
{
    [SerializeField, Tooltip("UiSkin の素材ID")] private string _artId;
    [SerializeField, Tooltip("素材がないときの色")] private Color _placeholderColor = Color.gray;
    [SerializeField, Tooltip("素材がないときの形（空なら四角）")] private Sprite _placeholderSprite;
    [SerializeField, Tooltip("素材の縦横比を保つか")] private bool _isPreserveAspect;
    [SerializeField, Tooltip("素材がないときだけ出すもの（仮の文字など）")] private GameObject _placeholderObject;
    [SerializeField, Tooltip("素材がないときに代わりに使う素材ID（空なら使わない。例：消灯の絵が届くまで点灯の絵を暗くして使う）")] private string _fallbackArtId;
    [SerializeField, Tooltip("代わりの素材を使うときにかける色")] private Color _fallbackColor = Color.white;

    private Image _image;
    private bool _isUsingFallback;

    /// <summary>
    /// UiSkin の素材ID
    /// </summary>
    public string ArtId => _artId;

    /// <summary>
    /// 素材の画像が入っているか（なければ仮の四角を出している）
    /// </summary>
    public bool HasArt { get; private set; }

    /// <summary>
    /// 素材の縦横比を保つか
    /// </summary>
    public bool IsPreserveAspect => _isPreserveAspect;

    /// <summary>
    /// 素材がないときの色
    /// </summary>
    public Color PlaceholderColor => _placeholderColor;

    /// <summary>
    /// 素材があるときにかける色
    /// </summary>
    public Color ArtColor => _isUsingFallback ? _fallbackColor : Color.white;

    /// <summary>
    /// 自分の Image を取得しておく
    /// </summary>
    private void Awake()
    {
        _image = GetComponent<Image>();
    }

    /// <summary>
    /// UiSkin から画像を受け取って表示を切り替える
    /// </summary>
    /// <param name="skin">素材の表</param>
    public void Apply(UiSkin skin)
    {
        if (_image == null)
        {
            _image = GetComponent<Image>();
        }

        Sprite sprite = skin != null ? skin.GetSprite(_artId) : null;
        _isUsingFallback = false;

        if (sprite == null && skin != null && !string.IsNullOrEmpty(_fallbackArtId))
        {
            sprite = skin.GetSprite(_fallbackArtId);
            _isUsingFallback = sprite != null;
        }

        bool hasArt = sprite != null;
        HasArt = hasArt;

        if (hasArt)
        {
            _image.sprite = sprite;
            _image.color = ArtColor;
            _image.preserveAspect = _isPreserveAspect;
        }
        else
        {
            _image.sprite = _placeholderSprite;
            _image.color = _placeholderColor;
            _image.preserveAspect = false;
        }

        _image.type = HasBorder(_image.sprite) ? Image.Type.Sliced : Image.Type.Simple;

        if (_placeholderObject != null)
        {
            _placeholderObject.SetActive(!hasArt);
        }
    }

    /// <summary>
    /// 9スライスの枠が設定された画像かを調べる
    /// </summary>
    /// <param name="sprite">調べる画像</param>
    /// <returns>枠があれば true</returns>
    private static bool HasBorder(Sprite sprite)
    {
        return sprite != null && sprite.border != Vector4.zero;
    }
}
